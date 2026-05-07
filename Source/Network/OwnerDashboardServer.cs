using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Network
{
    /// <summary>
    /// READ-ONLY owner dashboard HTTP server on port 5001.
    /// Serves a mobile-friendly HTML dashboard protected by PIN auth.
    /// Cloudflare Tunnel wraps this port and exposes it to the internet.
    ///
    /// ENDPOINTS:
    ///   GET  /          → Dashboard HTML  (requires cookie auth)
    ///   GET  /login     → Login page HTML (public)
    ///   POST /login     → PIN check, sets HttpOnly cookie
    ///   GET  /logout    → Clears cookie
    ///   GET  /api/kpi   → JSON KPI stats  (requires cookie auth)
    ///   GET  /api/sales → JSON last 20 sales (requires cookie auth)
    /// </summary>
    public class OwnerDashboardServer
    {
        public const int Port = 5001;
        public static OwnerDashboardServer Instance { get; private set; }

        private HttpListener _listener;
        private Thread _thread;
        private bool _running;

        // In-memory valid tokens (cleared on server stop/restart)
        private readonly HashSet<string> _validTokens = new HashSet<string>();
        private readonly Dictionary<string, int>  _failCounts  = new Dictionary<string, int>();
        private readonly Dictionary<string, DateTime> _blockUntil = new Dictionary<string, DateTime>();

        public bool IsRunning => _running;

        // ── Public API ────────────────────────────────────────────────────────
        public void Start()
        {
            if (_running) return;
            Instance = this;

            _listener = new HttpListener();
            _listener.Prefixes.Add(string.Format("http://+:{0}/", Port));

            try { _listener.Start(); }
            catch
            {
                // Fall back to wildcard prefix if URL ACL not registered
                _listener = new HttpListener();
                _listener.Prefixes.Add(string.Format("http://*:{0}/", Port));
                _listener.Start();
            }

            _running = true;
            _thread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "OwnerDashboardServer"
            };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            try { if (_listener != null) _listener.Stop(); } catch { }
        }

        // ── Listen loop ───────────────────────────────────────────────────────
        private void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(HandleRequest, ctx);
                }
                catch { /* listener stopped */ }
            }
        }

        // ── Request router ────────────────────────────────────────────────────
        private void HandleRequest(object state)
        {
            var ctx = (HttpListenerContext)state;
            try
            {
                // Add CORS & security headers
                ctx.Response.Headers["X-Frame-Options"]        = "DENY";
                ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
                ctx.Response.Headers["Cache-Control"]          = "no-store";

                string path   = ctx.Request.Url.AbsolutePath.ToLower().TrimEnd('/');
                string method = ctx.Request.HttpMethod.ToUpper();

                // ── Public: Login page ──────────────────────────────────────
                if (path == "/login" && method == "GET")
                {
                    ServeHtml(ctx, 200, BuildLoginHtml(""));
                    return;
                }

                // ── Public: PIN submit ──────────────────────────────────────
                if (path == "/login" && method == "POST")
                {
                    HandleLoginPost(ctx);
                    return;
                }

                // ── Public: Logout ─────────────────────────────────────────
                if (path == "/logout")
                {
                    string tok = GetCookie(ctx, "medixa_owner");
                    if (tok != null) lock (_validTokens) _validTokens.Remove(tok);
                    ctx.Response.Headers["Set-Cookie"] =
                        "medixa_owner=; HttpOnly; Max-Age=0; Path=/";
                    Redirect(ctx, "/login");
                    return;
                }

                // ── Root → redirect to dashboard or login ──────────────────
                if (path == "" && method == "GET")
                {
                    Redirect(ctx, IsAuthenticated(ctx) ? "/dashboard" : "/login");
                    return;
                }

                // ── All remaining routes need authentication ────────────────
                if (!IsAuthenticated(ctx))
                {
                    Redirect(ctx, "/login");
                    return;
                }

                if (path == "/dashboard" && method == "GET")
                {
                    ServeHtml(ctx, 200, BuildDashboardHtml());
                    return;
                }

                if (path == "/settings" && method == "GET")
                {
                    ServeHtml(ctx, 200, BuildSettingsHtml("", ""));
                    return;
                }

                if (path == "/settings/pin" && method == "POST")
                {
                    HandlePinChange(ctx);
                    return;
                }

                if (path == "/api/kpi" && method == "GET")
                {
                    ServeJson(ctx, 200, GetKpiJson());
                    return;
                }

                if (path == "/api/sales" && method == "GET")
                {
                    string filter = ctx.Request.QueryString["filter"] ?? "today";
                    ServeJson(ctx, 200, GetSalesJson(filter));
                    return;
                }

                // 404
                ServeHtml(ctx, 404, "<h2>Not Found</h2>");
            }
            catch (Exception ex)
            {
                try { ServeJson(ctx, 500, "{\"error\":\"" + Esc(ex.Message) + "\"}"); }
                catch { }
            }
        }

        // ── PIN login handler ─────────────────────────────────────────────────
        private void HandleLoginPost(HttpListenerContext ctx)
        {
            string ip = ctx.Request.RemoteEndPoint?.Address?.ToString() ?? "unknown";

            // Rate-limit: block IP after 5 wrong PINs
            lock (_blockUntil)
            {
                if (_blockUntil.ContainsKey(ip) && _blockUntil[ip] > DateTime.UtcNow)
                {
                    ServeHtml(ctx, 429,
                        BuildLoginHtml("⚠️ Too many attempts. Try again in 15 minutes."));
                    return;
                }
            }

            string body = new StreamReader(ctx.Request.InputStream, Encoding.UTF8).ReadToEnd();
            string pin  = "";
            foreach (string part in body.Split('&'))
            {
                var kv = part.Split('=');
                if (kv.Length == 2 && kv[0] == "pin")
                    pin = Uri.UnescapeDataString(kv[1]).Trim();
            }

            if (pin == GetOwnerPin())
            {
                // Success — create session token
                string token = Guid.NewGuid().ToString("N");
                lock (_validTokens) _validTokens.Add(token);
                lock (_failCounts) _failCounts.Remove(ip);

                ctx.Response.StatusCode = 302;
                ctx.Response.Headers["Set-Cookie"] =
                    string.Format("medixa_owner={0}; HttpOnly; Max-Age=86400; Path=/", token);
                ctx.Response.Headers["Location"] = "/dashboard";
                ctx.Response.OutputStream.Close();
            }
            else
            {
                // Wrong PIN
                lock (_failCounts)
                {
                    if (!_failCounts.ContainsKey(ip)) _failCounts[ip] = 0;
                    _failCounts[ip]++;
                    if (_failCounts[ip] >= 5)
                    {
                        lock (_blockUntil) _blockUntil[ip] = DateTime.UtcNow.AddMinutes(15);
                        _failCounts.Remove(ip);
                        ServeHtml(ctx, 429,
                            BuildLoginHtml("⚠️ Too many attempts. Blocked for 15 minutes."));
                        return;
                    }
                }
                ServeHtml(ctx, 200, BuildLoginHtml("❌ Wrong PIN. Please try again."));
            }
        }

        // ── PIN change handler ────────────────────────────────────────────────
        private void HandlePinChange(HttpListenerContext ctx)
        {
            string body = new System.IO.StreamReader(ctx.Request.InputStream, Encoding.UTF8).ReadToEnd();
            string currentPin = "", newPin = "", confirmPin = "";

            foreach (string part in body.Split('&'))
            {
                var kv = part.Split('=');
                if (kv.Length != 2) continue;
                string key = kv[0], val = Uri.UnescapeDataString(kv[1]).Trim();
                if (key == "current") currentPin  = val;
                if (key == "newpin")  newPin       = val;
                if (key == "confirm") confirmPin   = val;
            }

            if (currentPin != GetOwnerPin())
            {
                ServeHtml(ctx, 200, BuildSettingsHtml("error", "❌ Current PIN is wrong."));
                return;
            }
            if (newPin.Length < 4 || newPin.Length > 8)
            {
                ServeHtml(ctx, 200, BuildSettingsHtml("error", "❌ New PIN must be 4–8 digits."));
                return;
            }
            if (newPin != confirmPin)
            {
                ServeHtml(ctx, 200, BuildSettingsHtml("error", "❌ PINs do not match."));
                return;
            }

            // Save new PIN to config.json via ApiClient
            try
            {
                var api = ApiClient.Instance;
                api.SaveConfig(api.PharmacyName, api.PharmacyTagline,
                               api.ContactInfo, api.PharmacyAddress,
                               api.PharmacyEmail, newPin);
                ServeHtml(ctx, 200, BuildSettingsHtml("success", "✅ PIN changed successfully!"));
            }
            catch (Exception ex)
            {
                ServeHtml(ctx, 200, BuildSettingsHtml("error", "❌ Save failed: " + ex.Message));
            }
        }

        // ── Settings HTML ─────────────────────────────────────────────────────
        private static string BuildSettingsHtml(string alertType, string alertMsg)
        {
            string pharmName = SafePharmName();
            string alert = "";
            if (!string.IsNullOrEmpty(alertMsg))
            {
                string color = alertType == "success" ? "#1abc9c" : "#f85149";
                string bg    = alertType == "success" ? "rgba(26,188,156,.15)" : "rgba(248,81,73,.15)";
                alert = string.Format(
                    "<div style='background:{0};border:1px solid {1};color:{1};" +
                    "padding:12px;border-radius:10px;font-size:13px;margin-bottom:20px'>{2}</div>",
                    bg, color, alertMsg);
            }

            return @"<!DOCTYPE html><html lang='en'><head>
<meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<title>Settings – " + pharmName + @"</title>
<style>
*{box-sizing:border-box;margin:0;padding:0}
:root{--bg:#0d1117;--card:#161b22;--border:#30363d;--text:#e6edf3;--sub:#8b949e;--green:#1abc9c}
body{background:var(--bg);color:var(--text);font-family:system-ui,sans-serif;min-height:100vh}
.header{background:var(--card);border-bottom:1px solid var(--border);padding:14px 20px;
        display:flex;align-items:center;justify-content:space-between;position:sticky;top:0;z-index:10}
.header h1{font-size:16px;font-weight:700}
.back{color:var(--green);font-size:13px;text-decoration:none}
.content{padding:24px 16px;max-width:480px;margin:0 auto}
.card{background:var(--card);border:1px solid var(--border);border-radius:14px;padding:24px;margin-bottom:16px}
.card-title{font-size:14px;font-weight:700;margin-bottom:18px;color:var(--text)}
label{display:block;font-size:12px;color:var(--sub);margin-bottom:6px;font-weight:600;text-transform:uppercase;letter-spacing:.5px}
input{width:100%;padding:12px;border-radius:10px;border:1px solid var(--border);
      background:rgba(255,255,255,.05);color:var(--text);font-size:15px;outline:none;margin-bottom:14px}
input:focus{border-color:var(--green);box-shadow:0 0 0 3px rgba(26,188,156,.2)}
button{width:100%;padding:13px;border-radius:10px;border:none;
       background:linear-gradient(135deg,#1abc9c,#16a085);color:#fff;
       font-size:15px;font-weight:600;cursor:pointer;transition:.2s}
button:hover{transform:translateY(-1px);box-shadow:0 6px 20px rgba(26,188,156,.35)}
.info{font-size:12px;color:var(--sub);margin-top:16px;line-height:1.6}
</style></head><body>
<div class='header'>
  <h1>⚙️ Settings</h1>
  <a class='back' href='/dashboard'>← Back to Dashboard</a>
</div>
<div class='content'>
  <div class='card'>
    <div class='card-title'>🔐 Change Owner PIN</div>
    " + alert + @"
    <form method='POST' action='/settings/pin'>
      <label>Current PIN</label>
      <input type='password' name='current' placeholder='Enter current PIN' maxlength='8' required>
      <label>New PIN</label>
      <input type='password' name='newpin' placeholder='Enter new PIN (4–8 digits)' maxlength='8' required>
      <label>Confirm New PIN</label>
      <input type='password' name='confirm' placeholder='Repeat new PIN' maxlength='8' required>
      <button type='submit'>💾 Save New PIN</button>
    </form>
    <div class='info'>
      PIN is stored locally on this PC in config.json.<br>
      Each pharmacy has its own separate PIN and dashboard.
    </div>
  </div>

  <div class='card'>
    <div class='card-title'>🏥 Pharmacy Info</div>
    <div style='font-size:13px;color:var(--sub);line-height:1.8'>
      <b style='color:var(--text)'>Name:</b> " + pharmName + @"<br>
      <b style='color:var(--text)'>Dashboard:</b> Port 5001 (Cloudflare Tunnel)<br>
      <b style='color:var(--text)'>Data:</b> Local SQLite — Private &amp; Secure
    </div>
  </div>
</div>
</body></html>";
        }
        private bool IsAuthenticated(HttpListenerContext ctx)
        {
            string tok = GetCookie(ctx, "medixa_owner");
            if (string.IsNullOrEmpty(tok)) return false;
            lock (_validTokens) return _validTokens.Contains(tok);
        }

        private static string GetCookie(HttpListenerContext ctx, string name)
        {
            string header = ctx.Request.Headers["Cookie"] ?? "";
            foreach (string part in header.Split(';'))
            {
                var kv = part.Trim().Split('=');
                if (kv.Length >= 2 && kv[0] == name)
                    return kv[1];
            }
            return null;
        }

        // ── Owner PIN from config ─────────────────────────────────────────────
        private static string GetOwnerPin()
        {
            try
            {
                string pin = ApiClient.Instance.OwnerPin;
                return string.IsNullOrWhiteSpace(pin) ? "1234" : pin;
            }
            catch { return "1234"; }
        }

        // ── Data queries ──────────────────────────────────────────────────────
        private static string GetKpiJson()
        {
            var db = new DbHelper();
            try
            {
                // Today
                var dtToday = db.GetDataTable(
                    "SELECT COUNT(*) AS Cnt, COALESCE(SUM(NetPaid),0) AS Rev " +
                    "FROM Sales WHERE date(SaleDate)=date('now','localtime')");
                long todayCnt = ToLong(dtToday.Rows[0]["Cnt"]);
                double todayRev = ToDouble(dtToday.Rows[0]["Rev"]);

                // This week
                var dtWeek = db.GetDataTable(
                    "SELECT COUNT(*) AS Cnt, COALESCE(SUM(NetPaid),0) AS Rev " +
                    "FROM Sales WHERE SaleDate >= date('now','-6 days','localtime')");
                long weekCnt = ToLong(dtWeek.Rows[0]["Cnt"]);
                double weekRev = ToDouble(dtWeek.Rows[0]["Rev"]);

                // This month
                var dtMonth = db.GetDataTable(
                    "SELECT COUNT(*) AS Cnt, COALESCE(SUM(NetPaid),0) AS Rev " +
                    "FROM Sales WHERE strftime('%Y-%m',SaleDate)=strftime('%Y-%m','now','localtime')");
                long monthCnt = ToLong(dtMonth.Rows[0]["Cnt"]);
                double monthRev = ToDouble(dtMonth.Rows[0]["Rev"]);

                return string.Format(
                    "{{\"today\":{{\"sales\":{0},\"revenue\":{1}}}," +
                    "\"week\":{{\"sales\":{2},\"revenue\":{3}}}," +
                    "\"month\":{{\"sales\":{4},\"revenue\":{5}}}," +
                    "\"updated\":\"{6}\"}}",
                    todayCnt, todayRev.ToString("F2"),
                    weekCnt,  weekRev.ToString("F2"),
                    monthCnt, monthRev.ToString("F2"),
                    DateTime.Now.ToString("hh:mm tt"));
            }
            catch (Exception ex)
            {
                return "{\"error\":\"" + Esc(ex.Message) + "\"}";
            }
        }

        private static string GetSalesJson(string filter)
        {
            var db = new DbHelper();
            try
            {
                string where = "";
                if (filter == "today")
                    where = "WHERE date(s.SaleDate)=date('now','localtime') ";
                else if (filter == "week")
                    where = "WHERE s.SaleDate >= date('now','-6 days','localtime') ";
                else if (filter == "month")
                    where = "WHERE strftime('%Y-%m',s.SaleDate)=strftime('%Y-%m','now','localtime') ";

                var dt = db.GetDataTable(
                    "SELECT s.SaleID, s.SaleDate, s.NetPaid, s.Status, " +
                    "COALESCE(c.Name,'Walk-in') AS Customer " +
                    "FROM Sales s " +
                    "LEFT JOIN Customers c ON s.CustomerID=c.CustomerID " +
                    where +
                    "ORDER BY s.SaleID DESC LIMIT 50");

                var rows = new List<string>();
                foreach (DataRow r in dt.Rows)
                {
                    rows.Add(string.Format(
                        "{{\"id\":{0},\"date\":\"{1}\",\"amount\":{2}," +
                        "\"status\":\"{3}\",\"customer\":\"{4}\"}}",
                        r["SaleID"],
                        Esc(r["SaleDate"].ToString()),
                        ToDouble(r["NetPaid"]).ToString("F2"),
                        Esc(r["Status"].ToString()),
                        Esc(r["Customer"].ToString())));
                }
                return "{\"sales\":[" + string.Join(",", rows) + "]}";
            }
            catch (Exception ex)
            {
                return "{\"error\":\"" + Esc(ex.Message) + "\"}";
            }
        }

        // ── HTML builders ─────────────────────────────────────────────────────
        private static string BuildLoginHtml(string errorMsg)
        {
            string pharmName = SafePharmName();
            string err = string.IsNullOrEmpty(errorMsg) ? "" :
                string.Format("<div class='err'>{0}</div>", errorMsg);

            return @"<!DOCTYPE html><html lang='en'><head>
<meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<title>Owner Login – " + pharmName + @"</title>
<style>
*{box-sizing:border-box;margin:0;padding:0}
body{min-height:100vh;display:flex;align-items:center;justify-content:center;
     background:linear-gradient(135deg,#0f2027,#203a43,#2c5364);font-family:system-ui,sans-serif}
.card{background:rgba(255,255,255,.07);backdrop-filter:blur(20px);border:1px solid rgba(255,255,255,.15);
      border-radius:20px;padding:40px 36px;width:340px;text-align:center;box-shadow:0 20px 60px rgba(0,0,0,.4)}
.icon{font-size:48px;margin-bottom:12px}
h1{color:#fff;font-size:22px;margin-bottom:4px}
.sub{color:rgba(255,255,255,.5);font-size:13px;margin-bottom:28px}
.err{background:rgba(231,76,60,.2);border:1px solid rgba(231,76,60,.5);color:#ff6b6b;
     padding:10px;border-radius:10px;font-size:13px;margin-bottom:16px}
input[type=password]{width:100%;padding:14px;border-radius:12px;border:1px solid rgba(255,255,255,.2);
     background:rgba(255,255,255,.1);color:#fff;font-size:20px;letter-spacing:6px;text-align:center;
     outline:none;margin-bottom:16px}
input[type=password]::placeholder{letter-spacing:normal;font-size:14px;color:rgba(255,255,255,.4)}
input[type=password]:focus{border-color:#1abc9c;box-shadow:0 0 0 3px rgba(26,188,156,.3)}
button{width:100%;padding:14px;border-radius:12px;border:none;
       background:linear-gradient(135deg,#1abc9c,#16a085);color:#fff;
       font-size:16px;font-weight:600;cursor:pointer;transition:.2s}
button:hover{transform:translateY(-2px);box-shadow:0 8px 25px rgba(26,188,156,.4)}
.brand{color:rgba(255,255,255,.3);font-size:11px;margin-top:24px}
</style></head><body>
<div class='card'>
  <div class='icon'>🏥</div>
  <h1>" + pharmName + @"</h1>
  <p class='sub'>Owner Dashboard — Enter PIN</p>
  " + err + @"
  <form method='POST' action='/login'>
    <input type='password' name='pin' placeholder='Enter PIN' maxlength='8' autofocus autocomplete='off'>
    <button type='submit'>🔓 Login</button>
  </form>
  <p class='brand'>Medixa Pharmacy Software</p>
</div>
</body></html>";
        }

        private static string BuildDashboardHtml()
        {
            string pharmName = SafePharmName();

            return @"<!DOCTYPE html><html lang='en'><head>
<meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<title>" + pharmName + @" — Owner Dashboard</title>
<style>
*{box-sizing:border-box;margin:0;padding:0}
:root{--bg:#0d1117;--card:#161b22;--border:#30363d;--text:#e6edf3;--sub:#8b949e;
      --green:#1abc9c;--blue:#58a6ff;--orange:#f0883e;--red:#f85149}
body{background:var(--bg);color:var(--text);font-family:system-ui,-apple-system,sans-serif;
     min-height:100vh;padding-bottom:40px}
/* Header */
.header{background:var(--card);border-bottom:1px solid var(--border);
        padding:14px 20px;display:flex;align-items:center;justify-content:space-between;
        position:sticky;top:0;z-index:10}
.header-left h1{font-size:16px;font-weight:700;color:var(--text)}
.header-left span{font-size:11px;color:var(--sub)}
.live-badge{background:rgba(26,188,156,.15);border:1px solid rgba(26,188,156,.4);
            color:var(--green);font-size:11px;font-weight:600;padding:4px 10px;border-radius:20px;
            display:flex;align-items:center;gap:5px}
.live-dot{width:7px;height:7px;border-radius:50%;background:var(--green);animation:pulse 2s infinite}
@keyframes pulse{0%,100%{opacity:1}50%{opacity:.4}}
/* Grid */
.content{padding:20px 16px;max-width:600px;margin:0 auto}
.kpi-grid{display:grid;grid-template-columns:1fr 1fr;gap:12px;margin-bottom:12px}
.kpi-card{background:var(--card);border:1px solid var(--border);border-radius:14px;padding:18px}
.kpi-card.full{grid-column:span 2}
.kpi-label{font-size:11px;color:var(--sub);font-weight:600;text-transform:uppercase;
           letter-spacing:.5px;margin-bottom:8px}
.kpi-amount{font-size:22px;font-weight:700;color:var(--text);line-height:1}
.kpi-count{font-size:12px;color:var(--sub);margin-top:4px}
.kpi-today .kpi-amount{color:var(--green)}
.kpi-week  .kpi-amount{color:var(--blue)}
.kpi-month .kpi-amount{font-size:26px;color:var(--orange)}
/* Sales list */
.section-title{font-size:13px;font-weight:700;color:var(--sub);text-transform:uppercase;
               letter-spacing:.5px;margin:20px 0 10px}
.sale-item{background:var(--card);border:1px solid var(--border);border-radius:12px;
           padding:14px 16px;margin-bottom:8px;display:flex;align-items:center;justify-content:space-between}
.sale-left .sale-id{font-size:13px;font-weight:600;color:var(--text)}
.sale-left .sale-meta{font-size:11px;color:var(--sub);margin-top:2px}
.sale-right{text-align:right}
.sale-amount{font-size:15px;font-weight:700;color:var(--green)}
.sale-badge{font-size:10px;font-weight:600;padding:2px 8px;border-radius:10px;margin-top:3px;display:inline-block}
.badge-paid{background:rgba(26,188,156,.15);color:var(--green)}
.badge-credit{background:rgba(240,136,62,.15);color:var(--orange)}
.badge-partial{background:rgba(88,166,255,.15);color:var(--blue)}
/* Tabs */
.tabs{display:flex;gap:4px;background:var(--bg);padding:4px;border-radius:8px;border:1px solid var(--border)}
.tab{padding:4px 10px;font-size:11px;font-weight:600;background:transparent;border:none;color:var(--sub);border-radius:6px;cursor:pointer;width:auto;transition:.2s}
.tab.active{background:var(--card);color:var(--text);box-shadow:0 1px 3px rgba(0,0,0,.3)}
/* Refresh bar */
.refresh-bar{text-align:center;font-size:11px;color:var(--sub);margin-top:20px;padding:12px;
             background:var(--card);border-radius:10px;border:1px solid var(--border)}
.spinner{display:inline-block;width:10px;height:10px;border:2px solid var(--border);
         border-top-color:var(--green);border-radius:50%;animation:spin .8s linear infinite;margin-right:5px}
@keyframes spin{to{transform:rotate(360deg)}}
/* Logout */
.logout{display:block;text-align:center;margin-top:16px;color:var(--sub);font-size:12px;text-decoration:none}
.logout:hover{color:var(--red)}
</style></head>
<body>

<div class='header'>
  <div class='header-left'>
    <h1>🏥 " + pharmName + @"</h1>
    <span>Owner Dashboard</span>
  </div>
  <div class='live-badge'><span class='live-dot'></span>LIVE</div>
</div>

<div class='content'>
  <!-- KPI Cards -->
  <div class='kpi-grid'>
    <div class='kpi-card kpi-today'>
      <div class='kpi-label'>📅 Today</div>
      <div class='kpi-amount' id='todayRev'>—</div>
      <div class='kpi-count' id='todayCnt'>Loading…</div>
    </div>
    <div class='kpi-card kpi-week'>
      <div class='kpi-label'>📆 This Week</div>
      <div class='kpi-amount' id='weekRev'>—</div>
      <div class='kpi-count' id='weekCnt'>Loading…</div>
    </div>
    <div class='kpi-card kpi-month full'>
      <div class='kpi-label'>🗓️ This Month</div>
      <div class='kpi-amount' id='monthRev'>—</div>
      <div class='kpi-count' id='monthCnt'>Loading…</div>
    </div>
  </div>

  <!-- Recent Sales -->
  <div class='section-title' style='display:flex;justify-content:space-between;align-items:center;margin-top:24px'>
    <span>📋 Sales</span>
    <div class='tabs'>
      <button id='tab-today' class='tab active' onclick='setFilter(""today"")'>Today</button>
      <button id='tab-week' class='tab' onclick='setFilter(""week"")'>7 Days</button>
      <button id='tab-month' class='tab' onclick='setFilter(""month"")'>Month</button>
    </div>
  </div>
  <div id='salesList'><div style='text-align:center;color:#8b949e;padding:20px'>Loading…</div></div>

  <!-- Refresh info -->
  <div class='refresh-bar' onclick='countdown=0;tick()' style='cursor:pointer'>
    <span id='spinnerEl' class='spinner' style='display:none'></span>
    <span id='refreshTxt'>Live (next check in 5s)</span>
  </div>

  <a class='logout' href='/logout'>Sign out</a>
</div>

<script>
var countdown=5,loading=false,currentFilter='today';

function fmt(n){
  if(n>=100000)return'Rs. '+(n/100000).toFixed(2)+'L';
  if(n>=1000)return'Rs. '+(n/1000).toFixed(1)+'K';
  return'Rs. '+Math.round(n).toLocaleString();
}

function setFilter(f){
  currentFilter=f;
  document.querySelectorAll('.tab').forEach(e=>e.classList.remove('active'));
  document.getElementById('tab-'+f).classList.add('active');
  document.getElementById('salesList').innerHTML='<div style=\'text-align:center;color:#8b949e;padding:20px\'>Loading…</div>';
  loadSales();
}

function loadKpi(){
  loading=true;
  document.getElementById('spinnerEl').style.display='inline-block';
  fetch('/api/kpi').then(r=>r.json()).then(d=>{
    if(d.error){return;}
    document.getElementById('todayRev').textContent=fmt(d.today.revenue);
    document.getElementById('todayCnt').textContent=d.today.sales+' sales today';
    document.getElementById('weekRev').textContent=fmt(d.week.revenue);
    document.getElementById('weekCnt').textContent=d.week.sales+' sales this week';
    document.getElementById('monthRev').textContent=fmt(d.month.revenue);
    document.getElementById('monthCnt').textContent=d.month.sales+' sales this month';
  }).catch(function(){}).finally(function(){
    loading=false;
    document.getElementById('spinnerEl').style.display='none';
    countdown=5;
  });
}

function loadSales(){
  fetch('/api/sales?filter='+currentFilter).then(r=>r.json()).then(d=>{
    if(!d.sales||d.error)return;
    var html='';
    d.sales.forEach(function(s){
      var badge=s.status==='Paid'?'badge-paid':s.status==='Credit'?'badge-credit':'badge-partial';
      var dt=s.date.substring(0,16).replace('T',' ');
      var cls='sale-badge '+badge;
      html+='<div class=\'sale-item\'><div class=\'sale-left\'><div class=\'sale-id\'>#'+s.id+' -- '+s.customer+'</div><div class=\'sale-meta\'>'+dt+'</div></div><div class=\'sale-right\'><div class=\'sale-amount\'>Rs. '+Math.round(parseFloat(s.amount)).toLocaleString()+'</div><span class=\''+cls+'\'>'+s.status+'</span></div></div>';
    });
    document.getElementById('salesList').innerHTML=html||'<p style=\'color:#8b949e;text-align:center;padding:20px\'>No sales '+currentFilter+'.</p>';
  }).catch(function(){});
}

function tick(){
  if(countdown<=0&&!loading){loadKpi();loadSales();}
  else if(countdown>0){
    document.getElementById('refreshTxt').textContent='Live (refreshing in '+countdown+'s) - Tap to refresh now';
  }
  countdown--;
}

loadKpi();loadSales();
setInterval(tick,1000);
</script>
</body></html>";
        }

        // ── HTTP response helpers ─────────────────────────────────────────────
        private static void ServeHtml(HttpListenerContext ctx, int code, string html)
        {
            byte[] buf = Encoding.UTF8.GetBytes(html);
            ctx.Response.StatusCode = code;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.ContentLength64 = buf.Length;
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
            ctx.Response.OutputStream.Close();
        }

        private static void ServeJson(HttpListenerContext ctx, int code, string json)
        {
            byte[] buf = Encoding.UTF8.GetBytes(json);
            ctx.Response.StatusCode = code;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            ctx.Response.ContentLength64 = buf.Length;
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
            ctx.Response.OutputStream.Close();
        }

        private static void Redirect(HttpListenerContext ctx, string location)
        {
            ctx.Response.StatusCode = 302;
            ctx.Response.Headers["Location"] = location;
            ctx.Response.OutputStream.Close();
        }

        // ── Utilities ─────────────────────────────────────────────────────────
        private static string Esc(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static long ToLong(object o)
        {
            if (o == null || o is DBNull) return 0;
            return Convert.ToInt64(o);
        }

        private static double ToDouble(object o)
        {
            if (o == null || o is DBNull) return 0;
            return Convert.ToDouble(o);
        }

        private static string SafePharmName()
        {
            try { return ApiClient.Instance.PharmacyName ?? "Medixa"; }
            catch { return "Medixa"; }
        }

        /// <summary>
        /// Register URL ACL once (needs admin) so HttpListener works without elevation on future runs.
        /// </summary>
        public static void RegisterUrlAcl()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName  = "netsh",
                    Arguments = string.Format("http add urlacl url=http://+:{0}/ user=Everyone", Port),
                    UseShellExecute  = true,
                    Verb             = "runas",
                    CreateNoWindow   = true,
                    WindowStyle      = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                var p = System.Diagnostics.Process.Start(psi);
                if (p != null) p.WaitForExit();
            }
            catch { }
        }
    }
}
