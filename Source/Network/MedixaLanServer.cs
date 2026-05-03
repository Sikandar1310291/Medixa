using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using PharmaBilling.Source.Data;

namespace PharmaBilling.Source.Network
{
    /// <summary>
    /// Built-in HTTP server that exposes the SQLite database over LAN on port 5000.
    ///
    /// WHY THIS EXISTS:
    ///   SQLite's official documentation explicitly states: "SQLite does not support
    ///   writing to databases on network file systems (SMB/NFS) because file locking
    ///   does not work reliably."  The only robust solution is a proper client-server
    ///   architecture where the database is accessed only locally on the server and
    ///   clients communicate via HTTP.
    ///
    /// SECURITY: All requests must include the X-Medixa-Key header.
    ///
    /// ENDPOINTS:
    ///   GET  /ping        → health check
    ///   POST /query       → SELECT, returns JSON DataTable
    ///   POST /execute     → INSERT/UPDATE/DELETE, returns rows affected
    ///   POST /scalar      → returns single value
    ///   POST /transaction → executes multiple statements atomically
    /// </summary>
    public class MedixaLanServer
    {
        private HttpListener _listener;
        private Thread _thread;
        private bool _running;

        public const int Port = 5000;
        public const string SecretKey = "MEDIXA_LAN_2025";

        public static MedixaLanServer Instance { get; private set; }

        public bool IsRunning { get { return _running; } }

        public void Start()
        {
            if (_running) return;
            Instance = this;

            _listener = new HttpListener();
            _listener.Prefixes.Add(string.Format("http://+:{0}/", Port));

            try
            {
                _listener.Start();
            }
            catch (HttpListenerException)
            {
                // If + binding fails (needs URL ACL), fall back to * binding
                _listener = new HttpListener();
                _listener.Prefixes.Add(string.Format("http://*:{0}/", Port));
                _listener.Start();
            }

            _running = true;
            _thread = new Thread(ListenLoop) { IsBackground = true, Name = "MedixaLanServer" };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            try { if (_listener != null) _listener.Stop(); } catch { }
        }

        private void ListenLoop()
        {
            while (_running)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(HandleRequest, context);
                }
                catch { }
            }
        }

        private void HandleRequest(object state)
        {
            var ctx = (HttpListenerContext)state;
            try
            {
                // Validate shared secret
                if (ctx.Request.Headers["X-Medixa-Key"] != SecretKey)
                {
                    SendResponse(ctx, 401, "{\"error\":\"Unauthorized\"}");
                    return;
                }

                string path = ctx.Request.Url.AbsolutePath.ToLower().TrimEnd('/');

                // GET /ping → health check
                if (path == "/ping" && ctx.Request.HttpMethod == "GET")
                {
                    SendResponse(ctx, 200, "{\"status\":\"ok\",\"server\":\"" + EscapeJson(Environment.MachineName) + "\"}");
                    return;
                }

                // All other endpoints need a JSON body
                string body = new StreamReader(ctx.Request.InputStream, Encoding.UTF8).ReadToEnd();
                var json = new JavaScriptSerializer();
                json.MaxJsonLength = int.MaxValue;
                var req = json.Deserialize<Dictionary<string, object>>(body);

                string sql = req.ContainsKey("sql") ? req["sql"].ToString() : "";
                SQLiteParameter[] sqlParams = ParseParameters(req);
                var db = new DbHelper(); // SERVER mode DbHelper (local SQLite)

                if (path == "/query")
                {
                    DataTable dt = db.GetDataTable(sql, sqlParams);
                    SendResponse(ctx, 200, SerializeDataTable(dt, json));
                }
                else if (path == "/execute")
                {
                    int affected = db.ExecuteNonQuery(sql, sqlParams);
                    SendResponse(ctx, 200, "{\"rowsAffected\":" + affected + "}");
                }
                else if (path == "/scalar")
                {
                    object result = db.ExecuteScalar(sql, sqlParams);
                    string val = result == null || result is DBNull ? "null" : "\"" + EscapeJson(result.ToString()) + "\"";
                    SendResponse(ctx, 200, "{\"value\":" + val + "}");
                }
                else if (path == "/transaction")
                {
                    var cmds = new List<Tuple<string, SQLiteParameter[]>>();
                    if (req.ContainsKey("commands"))
                    {
                        var cmdArr = req["commands"] as ArrayList;
                        if (cmdArr != null)
                        {
                            foreach (Dictionary<string, object> cmd in cmdArr)
                            {
                                string cmdSql = cmd.ContainsKey("sql") ? cmd["sql"].ToString() : "";
                                SQLiteParameter[] cmdParams = ParseParameters(cmd);
                                cmds.Add(Tuple.Create(cmdSql, cmdParams));
                            }
                        }
                    }
                    bool ok = db.ExecuteTransaction(cmds);
                    SendResponse(ctx, 200, "{\"success\":" + (ok ? "true" : "false") + "}");
                }
                else
                {
                    SendResponse(ctx, 404, "{\"error\":\"Not Found\"}");
                }
            }
            catch (Exception ex)
            {
                try { SendResponse(ctx, 500, "{\"error\":\"" + EscapeJson(ex.Message) + "\"}"); } catch { }
            }
        }

        private SQLiteParameter[] ParseParameters(Dictionary<string, object> req)
        {
            var list = new List<SQLiteParameter>();
            if (!req.ContainsKey("parameters")) return list.ToArray();

            var paramsArr = req["parameters"] as ArrayList;
            if (paramsArr == null) return list.ToArray();

            foreach (Dictionary<string, object> p in paramsArr)
            {
                string name = p.ContainsKey("name") ? p["name"].ToString() : "";
                object val = p.ContainsKey("value") ? p["value"] : DBNull.Value;
                list.Add(new SQLiteParameter(name, val ?? DBNull.Value));
            }
            return list.ToArray();
        }

        private string SerializeDataTable(DataTable dt, JavaScriptSerializer json)
        {
            var cols = new List<string>();
            foreach (DataColumn col in dt.Columns)
                cols.Add("\"" + EscapeJson(col.ColumnName) + "\"");

            var rows = new List<string>();
            foreach (DataRow row in dt.Rows)
            {
                var fields = new List<string>();
                foreach (DataColumn col in dt.Columns)
                {
                    string colName = "\"" + EscapeJson(col.ColumnName) + "\"";
                    string colVal;
                    if (row[col] == null || row[col] is DBNull)
                        colVal = "null";
                    else if (row[col] is string)
                        colVal = "\"" + EscapeJson(row[col].ToString()) + "\"";
                    else if (row[col] is bool)
                        colVal = ((bool)row[col]) ? "true" : "false";
                    else
                        colVal = row[col].ToString(); // numbers, etc.
                    fields.Add(colName + ":" + colVal);
                }
                rows.Add("{" + string.Join(",", fields) + "}");
            }

            return "{\"columns\":[" + string.Join(",", cols) + "]," +
                   "\"rows\":[" + string.Join(",", rows) + "]}";
        }

        private string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        private void SendResponse(HttpListenerContext ctx, int statusCode, string json)
        {
            byte[] buf = Encoding.UTF8.GetBytes(json);
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            ctx.Response.ContentLength64 = buf.Length;
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
            ctx.Response.OutputStream.Close();
        }

        /// <summary>
        /// Registers the HTTP URL ACL so HttpListener can bind without admin on future runs.
        /// Call this once during setup (requires admin elevation).
        /// </summary>
        public static void RegisterUrlAcl()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = string.Format("http add urlacl url=http://+:{0}/ user=Everyone", Port),
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                var p = System.Diagnostics.Process.Start(psi);
                if (p != null) p.WaitForExit();
            }
            catch { }
        }
    }
}

