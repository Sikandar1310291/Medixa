using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace PharmaBilling.Source.Network
{
    /// <summary>
    /// Manages the Cloudflare Tunnel (cloudflared.exe) that exposes the
    /// OwnerDashboardServer (port 5001) to the internet for free.
    ///
    /// Uses trycloudflare.com — no account or API key required.
    /// URL changes on each restart (limitation of free tunnels).
    ///
    /// HOW IT WORKS:
    ///   1. Downloads cloudflared.exe from GitHub if not present.
    ///   2. Runs: cloudflared tunnel --url http://localhost:5001
    ///   3. Parses the tunnel URL from stdout/stderr output.
    ///   4. Fires TunnelReady event with the public HTTPS URL.
    /// </summary>
    public class CloudflareManager
    {
        public static CloudflareManager Instance { get; private set; }

        // Events
        public event Action<string> TunnelReady;   // fired with the public URL
        public event Action<string> StatusChanged; // fired with status messages
        public event Action         TunnelStopped; // fired when tunnel exits

        private Process _process;
        private Thread  _monitorThread;
        private bool    _running;

        public string  TunnelUrl  { get; private set; }
        public bool    IsRunning  => _running;

        // Where cloudflared.exe will be stored
        private static readonly string CloudflaredPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Medixa", "cloudflared.exe");

        private const string DownloadUrl =
            "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe";

        // Regex to capture the trycloudflare URL from cloudflared output
        private static readonly Regex UrlRegex = new Regex(
            @"https://[a-z0-9\-]+\.trycloudflare\.com",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // ── Public API ────────────────────────────────────────────────────────
        public void Start()
        {
            if (_running) return;
            Instance = this;
            _running = true;

            _monitorThread = new Thread(RunTunnel)
            {
                IsBackground = true,
                Name = "CloudflareManager"
            };
            _monitorThread.Start();
        }

        public void Stop()
        {
            _running = false;
            try
            {
                if (_process != null && !_process.HasExited)
                    _process.Kill();
            }
            catch { }
            TunnelUrl = null;
            TunnelStopped?.Invoke();
        }

        // ── Internal tunnel loop ──────────────────────────────────────────────
        private void RunTunnel()
        {
            try
            {
                FireStatus("⏳ Checking cloudflared.exe…");

                if (!EnsureCloudflared())
                {
                    FireStatus("❌ Could not download cloudflared.exe. Check internet connection.");
                    _running = false;
                    return;
                }

                FireStatus("🚀 Starting Cloudflare Tunnel…");

                _process = new Process();
                _process.StartInfo = new ProcessStartInfo
                {
                    FileName               = CloudflaredPath,
                    Arguments              = string.Format(
                        "tunnel --url http://localhost:{0} --no-autoupdate",
                        OwnerDashboardServer.Port),
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    CreateNoWindow         = true
                };

                _process.OutputDataReceived += ParseOutput;
                _process.ErrorDataReceived  += ParseOutput;

                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();

                _process.WaitForExit();
            }
            catch (Exception ex)
            {
                FireStatus("❌ Tunnel error: " + ex.Message);
            }
            finally
            {
                _running  = false;
                TunnelUrl = null;
                TunnelStopped?.Invoke();
            }
        }

        private void ParseOutput(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            // Look for the tunnel URL in any output line
            var match = UrlRegex.Match(e.Data);
            if (match.Success)
            {
                TunnelUrl = match.Value;
                FireStatus("🌐 Tunnel live: " + TunnelUrl);
                TunnelReady?.Invoke(TunnelUrl);
            }
            else if (e.Data.Contains("error") || e.Data.Contains("ERR"))
            {
                FireStatus("⚠️ " + e.Data.Trim());
            }
        }

        // ── Download cloudflared.exe if missing ───────────────────────────────
        private bool EnsureCloudflared()
        {
            if (File.Exists(CloudflaredPath)) return true;

            try
            {
                string dir = Path.GetDirectoryName(CloudflaredPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                FireStatus("📥 Downloading cloudflared.exe (~35 MB)…");

                using (var wc = new WebClient())
                {
                    wc.Headers["User-Agent"] = "Medixa-Pharmacy/1.9";
                    wc.DownloadFile(DownloadUrl, CloudflaredPath);
                }

                FireStatus("✅ cloudflared.exe downloaded.");
                return true;
            }
            catch (Exception ex)
            {
                FireStatus("❌ Download failed: " + ex.Message);
                return false;
            }
        }

        private void FireStatus(string msg)
        {
            StatusChanged?.Invoke(msg);
        }
    }
}
