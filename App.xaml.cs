using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Timers;
using System.Windows;
using PharmaBilling.Source.Data;
using PharmaBilling.Source.Network;

namespace PharmaBilling
{
    public partial class App : Application
    {
        private static MedixaLanServer      _lanServer;
        private static OwnerDashboardServer  _ownerServer;
        private static CloudflareManager     _cloudflare;

        // Fires every 10 minutes while the app is open.
        // When internet is available it silently pushes the activation
        // heartbeat to Supabase and resets the 30-day offline window.
        private static System.Timers.Timer _heartbeatTimer;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Force US Culture globally so that floating point numbers ALWAYS use '.'
            // This prevents regional settings (like Urdu/European comma) from rejecting decimal point inputs.
            var culture = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            base.OnStartup(e);

            // Global Exception Handlers for deep crash debugging
            this.DispatcherUnhandledException += (s, args) => 
            {
                File.WriteAllText("crash_log.txt", "Dispatcher Crash:\n" + args.Exception.ToString());
                MessageBox.Show("A critical layout error occurred. The application must close.\nCrash logged to crash_log.txt", "App Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
                Application.Current.Shutdown();
            };
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                File.WriteAllText("crash_log_domain.txt", "Domain Crash:\n" + args.ExceptionObject.ToString());
            };

            var config = AppConfig.Current;

            if (config.IsServer)
            {
                // Start the built-in LAN HTTP server on a background thread.
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        _lanServer = new MedixaLanServer();
                        _lanServer.Start();
                    }
                    catch (Exception)
                    {
                        MedixaLanServer.RegisterUrlAcl();
                        Thread.Sleep(1000);
                        try
                        {
                            _lanServer = new MedixaLanServer();
                            _lanServer.Start();
                        }
                        catch { /* Will still work for local use */ }
                    }
                });

                // Start Owner Dashboard server on port 5001 (read-only, internet-facing)
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        _ownerServer = new OwnerDashboardServer();
                        _ownerServer.Start();
                    }
                    catch (Exception)
                    {
                        OwnerDashboardServer.RegisterUrlAcl();
                        Thread.Sleep(1000);
                        try
                        {
                            _ownerServer = new OwnerDashboardServer();
                            _ownerServer.Start();
                        }
                        catch { /* Dashboard unavailable but main app still works */ }
                    }
                });

                // Start Cloudflare Tunnel — exposes port 5001 to internet
                System.Threading.Tasks.Task.Run(() =>
                {
                    Thread.Sleep(2000); // Wait for OwnerDashboardServer to fully start first
                    _cloudflare = new CloudflareManager();
                    _cloudflare.TunnelReady += (url) => 
                    {
                        SupabaseDashboardSync.SaveTunnelUrl(url);
                    };
                    _cloudflare.Start();
                });
            }

            // Start the background heartbeat timer.
            // Interval: 10 minutes (600,000 ms).
            // Runs entirely on a background thread — never touches the UI.
            // When internet comes back after days of offline use:
            //   → TryBackgroundHeartbeat() pings Supabase
            //   → Updates last_check.txt (resets 30-day offline window)
            //   → Pushes machine activation record to dashboard
            _heartbeatTimer = new System.Timers.Timer(600000); // 10 minutes
            _heartbeatTimer.Elapsed += OnHeartbeatTick;
            _heartbeatTimer.AutoReset = true;
            _heartbeatTimer.Start();

            // Pre-load all data into RAM cache immediately
            PharmaBilling.Source.Data.AppCache.WarmUp();

            // Fire an initial background bulk sync
            PharmaBilling.Source.Data.CloudSyncService.SyncRecentDataAsync();
            PharmaBilling.Source.Data.CloudSyncService.SyncRecentPurchasesAsync();
            PharmaBilling.Source.Data.CloudSyncService.SyncMetricsAsync();

            var licenseWin = new Source.Views.LicenseWindow();
            licenseWin.Show();
        }

        private static void OnHeartbeatTick(object sender, ElapsedEventArgs e)
        {
            // Runs on a ThreadPool thread — safe to do HTTP here
            try
            {
                LicenseManager.TryBackgroundHeartbeat();
            }
            catch { /* never crash the app from a background timer */ }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            try { if (_heartbeatTimer != null) { _heartbeatTimer.Stop(); _heartbeatTimer.Dispose(); } } catch { }
            try { if (_lanServer    != null) _lanServer.Stop();    } catch { }
            try { if (_ownerServer  != null) _ownerServer.Stop();  } catch { }
            try { if (_cloudflare   != null) _cloudflare.Stop();   } catch { }
        }
    }
}
