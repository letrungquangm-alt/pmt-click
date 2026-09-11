using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace QuinGMMenu
{
    public class GameItem
    {
        public string id { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public string genre { get; set; }
        public double rating { get; set; }
        public int playCount { get; set; }
        public bool isHot { get; set; }
        public string exePath { get; set; }
        public string fallbackPath { get; set; }
        public string banner { get; set; }
        public string icon { get; set; }
        public string color { get; set; }
        public string desc { get; set; }
    }

    public static class UITheme
    {
        public static readonly Color BgMain = Color.FromArgb(14, 7, 9);
        public static readonly Color BgHeader = Color.FromArgb(24, 11, 13);
        public static readonly Color BgHeaderGradient = Color.FromArgb(32, 14, 17);
        public static readonly Color BgFilterBar = Color.FromArgb(19, 9, 11);
        public static readonly Color BgCardNormal = Color.FromArgb(28, 14, 16);
        public static readonly Color BgCardHover = Color.FromArgb(44, 20, 24);

        public static readonly Color NeonCoral = Color.FromArgb(255, 112, 85);
        public static readonly Color NeonAmber = Color.FromArgb(251, 146, 60);
        public static readonly Color NeonOrange = Color.FromArgb(234, 88, 12);
        public static readonly Color NeonFlame = Color.FromArgb(215, 55, 30);
        public static readonly Color NeonFlameBright = Color.FromArgb(245, 95, 45);

        public static readonly Color BorderDim = Color.FromArgb(64, 28, 32);
        public static readonly Color BorderGlow = Color.FromArgb(255, 92, 60);
        public static readonly Color BorderActive = Color.FromArgb(251, 146, 60);
        public static readonly Color TextMain = Color.FromArgb(255, 255, 255);
        public static readonly Color TextMuted = Color.FromArgb(214, 165, 150);
        public static readonly Color TextDim = Color.FromArgb(150, 110, 105);

        public static GraphicsPath CreateRoundRect(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0.1f) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Color Blend(Color c1, Color c2, float t)
        {
            if (t <= 0f) return c1;
            if (t >= 1f) return c2;
            int a = (int)(c1.A + (c2.A - c1.A) * t);
            int r = (int)(c1.R + (c2.R - c1.R) * t);
            int g = (int)(c1.G + (c2.G - c1.G) * t);
            int b = (int)(c1.B + (c2.B - c1.B) * t);
            return Color.FromArgb(a, r, g, b);
        }
    }

    public static class PmtClickManager
    {
        private static PhonePcKeyboard.AppServer inProcessServer = null;
        private static Process cloudflareProcess = null;
        private static readonly object syncLock = new object();
        private static readonly List<string> logHistory = new List<string>();

        public static event Action<string> OnLogReceived;
        public static event Action OnStatusChanged;
        public static event Action<string> OnPublicUrlReady;

        private static bool showLogDetails = false;
        private static string cachedPublicUrl = "";
        private static string cachedRemoteUrl = "";
        private static string currentDriveRoot = "";

        public static bool IsRunning()
        {
            lock (syncLock)
            {
                return inProcessServer != null && inProcessServer.IsRunning;
            }
        }

        public static int ActivePort
        {
            get
            {
                lock (syncLock)
                {
                    return (inProcessServer != null && inProcessServer.IsRunning) ? inProcessServer.ActivePort : 5000;
                }
            }
        }

        public static string DeviceId
        {
            get
            {
                lock (syncLock)
                {
                    if (inProcessServer != null && inProcessServer.IsRunning && !string.IsNullOrEmpty(inProcessServer.DeviceId))
                    {
                        return inProcessServer.DeviceId;
                    }
                    return "--- ---";
                }
            }
        }

        public static string FormattedDeviceId
        {
            get
            {
                string id = DeviceId;
                if (!string.IsNullOrEmpty(id) && id.Length == 6)
                {
                    return id.Substring(0, 3) + " " + id.Substring(3);
                }
                return id;
            }
        }

        public static string WifiUrl
        {
            get
            {
                if (!IsRunning()) return "";
                string ip = GetLanIp();
                return string.Format("http://{0}:{1}", ip, ActivePort);
            }
        }

        public static readonly string OfficialDomain = "www.quiniumthu.qd.je";
        public static readonly string OfficialUrl = "https://www.quiniumthu.qd.je";
        public static readonly string OfficialApkUrl = "https://raw.githubusercontent.com/letrungquangm-alt/pmt-click/main/PMT_Click.apk";

        public static string ApkUrl
        {
            get
            {
                if (!IsRunning()) return "";
                return OfficialApkUrl;
            }
        }

        public static string RemoteUrl
        {
            get
            {
                if (!IsRunning()) return "";
                return OfficialUrl;
            }
        }

        public static string FallbackUrl
        {
            get
            {
                return !string.IsNullOrEmpty(cachedPublicUrl) ? cachedPublicUrl : "";
            }
        }

        public static bool ShowLog
        {
            get { return showLogDetails; }
            set
            {
                showLogDetails = value;
                lock (syncLock)
                {
                    if (inProcessServer != null)
                    {
                        inProcessServer.ShowLog = value;
                    }
                }
            }
        }

        public static List<string> GetLogHistory()
        {
            lock (syncLock)
            {
                return new List<string>(logHistory);
            }
        }

        public static void ClearLogs()
        {
            lock (syncLock)
            {
                logHistory.Clear();
            }
        }

        public static void AppendLog(string text)
        {
            lock (syncLock)
            {
                if (logHistory.Count > 1000)
                {
                    logHistory.RemoveAt(0);
                }
                logHistory.Add(text);
            }

            Action<string> handler = OnLogReceived;
            if (handler != null)
            {
                try { handler(text); } catch { }
            }
        }

        public static bool Start(string driveRoot)
        {
            lock (syncLock)
            {
                if (IsRunning()) return true;
                currentDriveRoot = driveRoot;

                try
                {
                    if (inProcessServer == null)
                    {
                        inProcessServer = new PhonePcKeyboard.AppServer();
                    }

                    inProcessServer.ShowLog = showLogDetails;
                    inProcessServer.OnLog += delegate(string msg) {
                        AppendLog(msg);
                    };
                    inProcessServer.OnClientConnected += delegate(string ip) {
                        AppendLog(string.Format("[{0:HH:mm:ss}] 🟢 [Client] Điện thoại đã kết nối! IP: {1}", DateTime.Now, ip));
                    };
                    inProcessServer.OnClientDisconnected += delegate(string ip) {
                        AppendLog(string.Format("[{0:HH:mm:ss}] 🔴 [Client] Điện thoại đã ngắt kết nối ({1})", DateTime.Now, ip));
                    };

                    inProcessServer.Start(5000);
                    int port = inProcessServer.ActivePort;
                    string rawId = inProcessServer.DeviceId;
                    string fmtId = (rawId != null && rawId.Length == 6) ? rawId.Substring(0, 3) + " " + rawId.Substring(3) : rawId;
                    string lanIp = GetLanIp();
                    string wifi = string.Format("http://{0}:{1}", lanIp, port);

                    cachedRemoteUrl = OfficialUrl;
                    cachedPublicUrl = OfficialUrl;

                    AppendLog("================================================================================");
                    AppendLog(string.Format("[{0:HH:mm:ss}] ⚡ PMT CLICK SERVER ĐÃ KHỞI ĐỘNG THÀNH CÔNG!", DateTime.Now));
                    AppendLog(string.Format("   - Cổng lắng nghe (Port): {0}", port));
                    AppendLog(string.Format("   - Mã ID Kết Nối (Device ID): {0}", fmtId));
                    AppendLog(string.Format("   - 🌐 Domain Chính Thức (4G/5G): {0}", OfficialUrl));
                    AppendLog(string.Format("   - 🤖 Link Tải Native APK: {0}", OfficialApkUrl));
                    AppendLog(string.Format("   - 🏠 Link Wi-Fi LAN trong nhà: {0}", wifi));
                    AppendLog("   - Engine: Windows SendInput Win32 & Touchpad (Độ trễ < 1ms)");
                    AppendLog("================================================================================");

                    // Start Cloudflare Tunnel in background thread
                    ThreadPool.QueueUserWorkItem(delegate(object state) {
                        try
                        {
                            Thread.Sleep(500);
                            StartCloudflareTunnel(port, driveRoot);
                        }
                        catch { }
                    });

                    FireStatusChanged();
                    return true;
                }
                catch (Exception ex)
                {
                    AppendLog(string.Format("[{0:HH:mm:ss}] ❌ Lỗi khởi động Server: {1}", DateTime.Now, ex.Message));
                    return false;
                }
            }
        }

        public static void Stop()
        {
            lock (syncLock)
            {
                if (inProcessServer != null)
                {
                    try { inProcessServer.Stop(); } catch { }
                    inProcessServer = null;
                }

                if (cloudflareProcess != null)
                {
                    try
                    {
                        if (!cloudflareProcess.HasExited) cloudflareProcess.Kill();
                    }
                    catch { }
                    cloudflareProcess = null;
                }

                cachedPublicUrl = "";
                cachedRemoteUrl = "";
                AppendLog(string.Format("[{0:HH:mm:ss}] ⏹️ Dịch vụ PMT Click đã dừng hoàn toàn.", DateTime.Now));
            }
            FireStatusChanged();
        }

        private static void FireStatusChanged()
        {
            Action handler = OnStatusChanged;
            if (handler != null)
            {
                try { handler(); } catch { }
            }
        }

        private static void StartCloudflareTunnel(int targetPort, string driveRoot)
        {
            string cfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cloudflared.exe");
            if (!File.Exists(cfPath) && !string.IsNullOrEmpty(driveRoot))
            {
                string p1 = Path.Combine(driveRoot.TrimEnd('\\') + "\\", "cloudflared.exe");
                string p2 = Path.Combine(driveRoot.TrimEnd('\\') + "\\", @"code\phone_pc_keyboard\cloudflared.exe");
                string p3 = Path.Combine(driveRoot.TrimEnd('\\') + "\\", @"phone_pc_keyboard\cloudflared.exe");
                if (File.Exists(p1)) cfPath = p1;
                else if (File.Exists(p2)) cfPath = p2;
                else if (File.Exists(p3)) cfPath = p3;
            }

            if (!File.Exists(cfPath))
            {
                cfPath = @"C:\Program Files (x86)\cloudflared\cloudflared.exe";
                if (!File.Exists(cfPath)) cfPath = @"C:\Program Files\cloudflared\cloudflared.exe";
            }

            if (!File.Exists(cfPath))
            {
                cachedRemoteUrl = OfficialUrl;
                AppendLog(string.Format("[{0:HH:mm:ss}] ℹ️ [Cloudflare Tunnel] Không tìm thấy cloudflared.exe. Chế độ Wi-Fi LAN hoạt động bình thường.", DateTime.Now));
                FireStatusChanged();
                return;
            }

            // Check if user has provided a Cloudflare Tunnel Token in cloudflare_token.txt
            string token = "";
            string tokenFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cloudflare_token.txt");
            if (!File.Exists(tokenFile) && !string.IsNullOrEmpty(driveRoot))
            {
                string p1 = Path.Combine(driveRoot.TrimEnd('\\') + "\\", "cloudflare_token.txt");
                if (File.Exists(p1)) tokenFile = p1;
            }

            if (File.Exists(tokenFile))
            {
                try
                {
                    string raw = File.ReadAllText(tokenFile).Trim();
                    string[] lines = raw.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string l in lines)
                    {
                        string trimmed = l.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                        {
                            token = trimmed;
                            break;
                        }
                    }
                }
                catch { }
            }

            try
            {
                string cfArgs = !string.IsNullOrEmpty(token)
                    ? ("tunnel run --token " + token)
                    : string.Format("tunnel --url http://127.0.0.1:{0} --no-autoupdate", targetPort);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = cfPath,
                    Arguments = cfArgs,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                if (!string.IsNullOrEmpty(token))
                {
                    AppendLog(string.Format("[{0:HH:mm:ss}] 🔑 [Cloudflare Zero Trust] Đang chạy Tunnel với Token cấu hình cho {1}...", DateTime.Now, OfficialDomain));
                }
                else
                {
                    AppendLog(string.Format("[{0:HH:mm:ss}] ℹ️ [Cloudflare Tunnel] Đang khởi tạo đường truyền cho {1}...", DateTime.Now, OfficialDomain));
                }

                cloudflareProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
                cloudflareProcess.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data)) ParseTunnelOutput(e.Data);
                };
                cloudflareProcess.OutputDataReceived += delegate(object s, DataReceivedEventArgs e) {
                    if (!string.IsNullOrEmpty(e.Data)) ParseTunnelOutput(e.Data);
                };
                cloudflareProcess.Start();
                cloudflareProcess.BeginErrorReadLine();
                cloudflareProcess.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                cachedRemoteUrl = OfficialUrl;
                AppendLog(string.Format("[{0:HH:mm:ss}] ⚠️ [Cloudflare Tunnel Error] {1}", DateTime.Now, ex.Message));
                FireStatusChanged();
            }
        }

        private static void ParseTunnelOutput(string line)
        {
            if (line.IndexOf("Registered tunnel connection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.IndexOf("INF Connection", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                cachedPublicUrl = OfficialUrl;
                cachedRemoteUrl = OfficialUrl;
                AppendLog(string.Format("[{0:HH:mm:ss}] 🟢 [Cloudflare Zero Trust Active] Domain {1} đã kết nối trực tiếp thành công!", DateTime.Now, OfficialDomain));
                Action<string> pubHandler = OnPublicUrlReady;
                if (pubHandler != null)
                {
                    try { pubHandler(OfficialUrl); } catch { }
                }
                FireStatusChanged();
                return;
            }

            Match m = Regex.Match(line, @"https://[a-zA-Z0-9-]+\.trycloudflare\.com");
            if (m.Success)
            {
                string pubUrl = m.Value;
                cachedPublicUrl = OfficialUrl;
                cachedRemoteUrl = OfficialUrl;

                lock (syncLock)
                {
                    if (inProcessServer != null) inProcessServer.PublicUrl = OfficialUrl;
                }

                AppendLog(string.Format("[{0:HH:mm:ss}] 🚀 [Cloudflare Edge Sẵn Sàng] Domain chính thức: {0}", DateTime.Now, OfficialUrl));
                AppendLog(string.Format("   - Link tải APK chính thức: {0}", OfficialApkUrl));
                AppendLog(string.Format("   - Đường truyền tunnel dự phòng: {0}", pubUrl));

                Action<string> pubHandler = OnPublicUrlReady;
                if (pubHandler != null)
                {
                    try { pubHandler(OfficialUrl); } catch { }
                }

                FireStatusChanged();
            }
        }

        public static void OpenFullGui(string driveRoot)
        {
            // Kept for backward compatibility
        }

        public static void BringToFront()
        {
            // Kept for backward compatibility
        }

        public static string GetLanIp()
        {
            try
            {
                return PhonePcKeyboard.MainForm.GetLanIpAddress();
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        public static Image GenerateQr(string url, int scale)
        {
            if (string.IsNullOrEmpty(url)) return null;
            try
            {
                PhonePcKeyboard.QrCode qr = PhonePcKeyboard.QrCode.EncodeText(url, PhonePcKeyboard.Ecc.Medium);
                return qr.ToBitmap(scale, 2);
            }
            catch
            {
                return null;
            }
        }
    }

    public static class IconCache
    {
        private static Dictionary<string, Image> cache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        public static Image GetIcon(GameItem game, string driveRoot)
        {
            if (game == null) return null;
            string key = !string.IsNullOrEmpty(game.id) ? game.id : (game.name ?? game.exePath ?? "");
            key = key.ToLower();

            if (cache.ContainsKey(key))
            {
                return cache[key];
            }

            Image img = ExtractGameIcon(game, driveRoot);
            cache[key] = img;
            return img;
        }

        private static Image ExtractGameIcon(GameItem game, string driveRoot)
        {
            try
            {
                // 1. Try fallbackPath first if it is an .exe
                if (!string.IsNullOrEmpty(game.fallbackPath))
                {
                    string pFb = Rebase(game.fallbackPath, driveRoot);
                    if (pFb.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(pFb))
                    {
                        Image img = TryExtractExe(pFb);
                        if (img != null) return img;
                    }
                }

                // 2. Try exePath if it's an .exe directly
                string p1 = Rebase(game.exePath, driveRoot);
                if (p1.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(p1))
                {
                    Image img = TryExtractExe(p1);
                    if (img != null) return img;
                }

                // 3. Known application signatures
                string lowerName = (game.name ?? "").ToLower();
                if (lowerName.Contains("antigravity"))
                {
                    Image img = TryExtractExe(Rebase("E:\\Antigravity IDE\\Antigravity IDE.exe", driveRoot));
                    if (img != null) return img;
                }
                else if (lowerName.Contains("discord"))
                {
                    Image img = TryExtractExe(Rebase("E:\\Discord\\app-1.0.9257\\Discord.exe", driveRoot));
                    if (img != null) return img;
                    string icoPath = Rebase("E:\\Discord\\app.ico", driveRoot);
                    if (File.Exists(icoPath))
                    {
                        try { return (Image)new Bitmap(icoPath); } catch {}
                    }
                }
                else if (lowerName.Contains("chrome"))
                {
                    Image img = TryExtractExe(Rebase("E:\\Browsers\\Chrome\\App\\chrome.exe", driveRoot));
                    if (img != null) return img;
                    img = TryExtractExe("C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe");
                    if (img != null) return img;
                    img = TryExtractExe("C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe");
                    if (img != null) return img;
                }
                else if (lowerName.Contains("steam"))
                {
                    Image img = TryExtractExe(Rebase("E:\\Steam\\steam.exe", driveRoot));
                    if (img != null) return img;
                    img = TryExtractExe("D:\\steam\\steam.exe");
                    if (img != null) return img;
                }
                else if (lowerName.Contains("cốc cốc") || lowerName.Contains("coccoc"))
                {
                    Image img = TryExtractExe(Rebase("E:\\Browsers\\CocCoc\\App\\browser.exe", driveRoot));
                    if (img != null) return img;
                }
                else if (lowerName.Contains("edge"))
                {
                    Image img = TryExtractExe(Rebase("E:\\Browsers\\Edge\\App\\msedge.exe", driveRoot));
                    if (img != null) return img;
                    img = TryExtractExe("C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe");
                    if (img != null) return img;
                }
                else if (lowerName.Contains("minecraft"))
                {
                    Image img = TryExtractExe(Rebase("E:\\game\\Minecraft.exe", driveRoot));
                    if (img != null) return img;
                }
                else if (lowerName.Contains("pmt"))
                {
                    Image img = TryExtractExe(Rebase("E:\\code\\phone_pc_keyboard\\pmt_click.exe", driveRoot));
                    if (img != null) return img;
                }

                // 4. If batch file, search inside batch folder for target .exe
                if (p1.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) || p1.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
                {
                    Image img = TryExtractFromBatch(p1);
                    if (img != null) return img;
                }
            }
            catch { }
            return null;
        }

        private static Image TryExtractExe(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    using (Icon ico = Icon.ExtractAssociatedIcon(path))
                    {
                        if (ico != null)
                        {
                            return (Image)ico.ToBitmap();
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static Image TryExtractFromBatch(string batPath)
        {
            try
            {
                if (File.Exists(batPath))
                {
                    string dir = Path.GetDirectoryName(batPath);
                    if (Directory.Exists(dir))
                    {
                        string[] directExes = Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly);
                        foreach (string e in directExes)
                        {
                            string fn = Path.GetFileName(e).ToLower();
                            if (!fn.Contains("unins") && !fn.Contains("update") && !fn.Contains("crash"))
                            {
                                Image img = TryExtractExe(e);
                                if (img != null) return img;
                            }
                        }

                        string[] subExes = Directory.GetFiles(dir, "*.exe", SearchOption.AllDirectories);
                        foreach (string e in subExes)
                        {
                            string fn = Path.GetFileName(e).ToLower();
                            if (!fn.Contains("unins") && !fn.Contains("update") && !fn.Contains("crash") && !fn.Contains("helper") && !fn.Contains("setup"))
                            {
                                Image img = TryExtractExe(e);
                                if (img != null) return img;
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private static string Rebase(string path, string driveRoot)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (path.Length >= 3 && path[1] == ':' && path[2] == '\\')
            {
                return driveRoot.TrimEnd('\\') + path.Substring(2);
            }
            return path;
        }
    }

    public class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public DoubleBufferedFlowLayoutPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }
    }

    public class NeonScrollBar : Control
    {
        private int minimum = 0;
        private int maximum = 100;
        private int val = 0;
        private int largeChange = 10;
        private bool isDragging = false;
        private float dragStartMouseY = 0f;
        private int dragStartValue = 0;
        private bool isHovered = false;
        private float hoverProgress = 0f;
        private float targetHover = 0f;
        private System.Windows.Forms.Timer animTimer;

        public event EventHandler ValueChanged;

        public int Minimum
        {
            get { return minimum; }
            set { minimum = value; this.Invalidate(); }
        }

        public int Maximum
        {
            get { return maximum; }
            set { maximum = value; this.Invalidate(); }
        }

        public int LargeChange
        {
            get { return largeChange; }
            set { largeChange = value; this.Invalidate(); }
        }

        public int Value
        {
            get { return val; }
            set
            {
                int newVal = Math.Max(minimum, Math.Min(maximum, value));
                if (this.val != newVal)
                {
                    this.val = newVal;
                    this.Invalidate();
                    if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
                }
            }
        }

        public NeonScrollBar()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.Transparent;
            this.Width = 10;
            this.Cursor = Cursors.Hand;

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 15;
            animTimer.Tick += (s, e) => {
                float step = 0.16f;
                if (Math.Abs(hoverProgress - targetHover) < 0.02f)
                {
                    hoverProgress = targetHover;
                    animTimer.Stop();
                }
                else if (hoverProgress < targetHover)
                {
                    hoverProgress += step;
                    if (hoverProgress > 1f) hoverProgress = 1f;
                }
                else
                {
                    hoverProgress -= step;
                    if (hoverProgress < 0f) hoverProgress = 0f;
                }
                this.Invalidate();
            };

            this.MouseEnter += (s, e) => { isHovered = true; targetHover = 1f; animTimer.Start(); };
            this.MouseLeave += (s, e) => { isHovered = false; if (!isDragging) { targetHover = 0f; animTimer.Start(); } };
        }

        private RectangleF GetThumbRect()
        {
            int range = maximum - minimum;
            if (range <= 0 || this.Height <= 0) return RectangleF.Empty;

            float visibleRatio = (float)largeChange / (range + largeChange);
            float thumbH = Math.Max(32f, this.Height * visibleRatio);
            float trackAvailable = this.Height - thumbH;
            float thumbY = ((float)(val - minimum) / range) * trackAvailable;

            return new RectangleF(1, thumbY, this.Width - 2, thumbH);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                RectangleF thumb = GetThumbRect();
                if (thumb.Contains(e.Location))
                {
                    isDragging = true;
                    dragStartMouseY = e.Y;
                    dragStartValue = val;
                }
                else
                {
                    if (e.Y < thumb.Y)
                    {
                        this.Value -= largeChange;
                    }
                    else
                    {
                        this.Value += largeChange;
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (isDragging)
            {
                int range = maximum - minimum;
                RectangleF thumb = GetThumbRect();
                float trackAvailable = this.Height - thumb.Height;
                if (trackAvailable > 0 && range > 0)
                {
                    float deltaY = e.Y - dragStartMouseY;
                    int deltaVal = (int)((deltaY / trackAvailable) * range);
                    this.Value = dragStartValue + deltaVal;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isDragging = false;
            if (!isHovered)
            {
                targetHover = 0f;
                animTimer.Start();
            }
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Track Background
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(16, 8, 10)))
            {
                g.FillRectangle(trackBrush, this.ClientRectangle);
            }

            // Track center hairline
            using (Pen guidePen = new Pen(Color.FromArgb(30, 14, 16), 1f))
            {
                g.DrawLine(guidePen, this.Width / 2f, 4, this.Width / 2f, this.Height - 4);
            }

            // Rounded Thumb
            RectangleF thumb = GetThumbRect();
            if (!thumb.IsEmpty && thumb.Height > 4)
            {
                Color thumbColor1 = UITheme.Blend(Color.FromArgb(64, 24, 28), Color.FromArgb(235, 75, 35), hoverProgress);
                Color thumbColor2 = UITheme.Blend(Color.FromArgb(42, 16, 20), Color.FromArgb(195, 45, 25), hoverProgress);
                Color borderColor = UITheme.Blend(Color.FromArgb(92, 36, 42), UITheme.NeonAmber, hoverProgress);

                if (hoverProgress > 0.05f || isDragging)
                {
                    int glowA = isDragging ? 70 : (int)(45 * hoverProgress);
                    using (Pen glowPen = new Pen(Color.FromArgb(glowA, UITheme.NeonOrange), 2.5f))
                    using (GraphicsPath gp = UITheme.CreateRoundRect(thumb, 4f))
                    {
                        g.DrawPath(glowPen, gp);
                    }
                }

                using (GraphicsPath gp = UITheme.CreateRoundRect(thumb, 4f))
                {
                    using (LinearGradientBrush lgb = new LinearGradientBrush(thumb, thumbColor1, thumbColor2, 90f))
                    {
                        g.FillPath(lgb, gp);
                    }

                    using (Pen bp = new Pen(borderColor, 1f))
                    {
                        g.DrawPath(bp, gp);
                    }
                }
            }
        }
    }

    public class HeaderActionButton : Control
    {
        private System.Windows.Forms.Timer animTimer;
        private float hoverProgress = 0f;
        private float targetHover = 0f;
        private bool isPressed = false;

        public string IconSymbol { get; set; }
        public string ButtonText { get; set; }
        public Color NormalBg1 { get; set; }
        public Color NormalBg2 { get; set; }
        public Color HoverBg1 { get; set; }
        public Color HoverBg2 { get; set; }
        public Color NormalBorder { get; set; }
        public Color HoverBorder { get; set; }

        public HeaderActionButton()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.Font = new Font("Segoe UI", 9.2F, FontStyle.Bold);
            this.ForeColor = Color.White;

            this.NormalBg1 = Color.FromArgb(42, 18, 22);
            this.NormalBg2 = Color.FromArgb(28, 12, 14);
            this.HoverBg1 = Color.FromArgb(64, 26, 32);
            this.HoverBg2 = Color.FromArgb(40, 16, 20);
            this.NormalBorder = Color.FromArgb(140, 50, 35);
            this.HoverBorder = UITheme.NeonAmber;

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 15;
            animTimer.Tick += (s, e) => {
                float step = 0.16f;
                if (Math.Abs(hoverProgress - targetHover) < 0.02f)
                {
                    hoverProgress = targetHover;
                    animTimer.Stop();
                }
                else if (hoverProgress < targetHover)
                {
                    hoverProgress += step;
                    if (hoverProgress > 1f) hoverProgress = 1f;
                }
                else
                {
                    hoverProgress -= step;
                    if (hoverProgress < 0f) hoverProgress = 0f;
                }
                this.Invalidate();
            };

            this.MouseEnter += (s, e) => { targetHover = 1f; animTimer.Start(); };
            this.MouseLeave += (s, e) => { targetHover = 0f; animTimer.Start(); };
            this.MouseDown += (s, e) => { isPressed = true; this.Invalidate(); };
            this.MouseUp += (s, e) => { isPressed = false; this.Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            RectangleF rect = new RectangleF(2, 2, this.Width - 5, this.Height - 5);

            Color curBg1 = UITheme.Blend(NormalBg1, HoverBg1, hoverProgress);
            Color curBg2 = UITheme.Blend(NormalBg2, HoverBg2, hoverProgress);
            Color curBorder = UITheme.Blend(NormalBorder, HoverBorder, hoverProgress);

            if (hoverProgress > 0.05f)
            {
                int glowA = (int)(45 * hoverProgress);
                using (Pen glowPen = new Pen(Color.FromArgb(glowA, HoverBorder), 3f))
                using (GraphicsPath gp = UITheme.CreateRoundRect(rect, 10f))
                {
                    g.DrawPath(glowPen, gp);
                }
            }

            using (GraphicsPath gp = UITheme.CreateRoundRect(rect, 9f))
            {
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(rect, curBg1, curBg2, 90f))
                {
                    g.FillPath(bgBrush, gp);
                }

                using (Pen glassPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
                {
                    g.DrawLine(glassPen, rect.X + 6, rect.Y + 1, rect.Right - 6, rect.Y + 1);
                }

                using (Pen p = new Pen(curBorder, 1.2f))
                {
                    g.DrawPath(p, gp);
                }
            }

            float offset = isPressed ? 1f : 0f;
            float iconWidth = 24f;
            RectangleF iconRect = new RectangleF(rect.X + 8, rect.Y + offset, iconWidth, rect.Height);
            RectangleF textRect = new RectangleF(rect.X + 28, rect.Y + offset, rect.Width - 32, rect.Height);

            if (!string.IsNullOrEmpty(IconSymbol))
            {
                using (Font iconFont = new Font("Segoe UI Emoji", 9.5F, FontStyle.Regular))
                using (Brush iconBrush = new SolidBrush(curBorder))
                using (StringFormat sfIcon = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(IconSymbol, iconFont, iconBrush, iconRect, sfIcon);
                }
            }

            using (Brush textBrush = new SolidBrush(this.ForeColor))
            using (StringFormat sfText = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(ButtonText ?? this.Text, this.Font, textBrush, textRect, sfText);
            }
        }
    }

    public class NeonPillButton : Control
    {
        private bool isPressed = false;
        private bool isActive = false;
        private System.Windows.Forms.Timer animTimer;
        private float hoverProgress = 0f;
        private float targetHover = 0f;

        public string BadgeText { get; set; }
        public float CornerRadius { get; set; }
        public Color NormalBg { get; set; }
        public Color HoverBg { get; set; }
        public Color ActiveBg { get; set; }
        public Color NormalBorder { get; set; }
        public Color HoverBorder { get; set; }
        public Color ActiveBorder { get; set; }

        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; this.Invalidate(); }
        }

        public NeonPillButton()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.CornerRadius = 14f;
            this.NormalBg = Color.FromArgb(36, 16, 18);
            this.HoverBg = Color.FromArgb(56, 24, 28);
            this.ActiveBg = UITheme.NeonFlame;
            this.NormalBorder = Color.FromArgb(64, 28, 32);
            this.HoverBorder = UITheme.NeonOrange;
            this.ActiveBorder = UITheme.NeonCoral;
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 15;
            animTimer.Tick += (s, e) => {
                float step = 0.15f;
                if (Math.Abs(hoverProgress - targetHover) < 0.02f)
                {
                    hoverProgress = targetHover;
                    animTimer.Stop();
                }
                else if (hoverProgress < targetHover)
                {
                    hoverProgress += step;
                    if (hoverProgress > 1f) hoverProgress = 1f;
                }
                else
                {
                    hoverProgress -= step;
                    if (hoverProgress < 0f) hoverProgress = 0f;
                }
                this.Invalidate();
            };

            this.MouseEnter += (s, e) => {
                targetHover = 1f;
                animTimer.Start();
            };
            this.MouseLeave += (s, e) => {
                targetHover = 0f;
                animTimer.Start();
            };
            this.MouseDown += (s, e) => { isPressed = true; this.Invalidate(); };
            this.MouseUp += (s, e) => { isPressed = false; this.Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            RectangleF rect = new RectangleF(2, 2, this.Width - 5, this.Height - 5);

            Color curBg = isActive ? ActiveBg : UITheme.Blend(NormalBg, HoverBg, hoverProgress);
            Color curBorder = isActive ? ActiveBorder : UITheme.Blend(NormalBorder, HoverBorder, hoverProgress);

            if (isActive || hoverProgress > 0.05f)
            {
                int glowA = isActive ? 55 : (int)(35 * hoverProgress);
                Color glowColor = isActive ? UITheme.NeonCoral : UITheme.NeonOrange;
                using (Pen glowPen = new Pen(Color.FromArgb(glowA, glowColor), 3f))
                using (GraphicsPath gp = UITheme.CreateRoundRect(rect, CornerRadius + 1f))
                {
                    g.DrawPath(glowPen, gp);
                }
            }

            using (GraphicsPath gp = UITheme.CreateRoundRect(rect, CornerRadius))
            {
                if (isActive)
                {
                    using (LinearGradientBrush lgb = new LinearGradientBrush(rect, UITheme.NeonFlame, UITheme.NeonOrange, 90f))
                    {
                        g.FillPath(lgb, gp);
                    }
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(curBg))
                    {
                        g.FillPath(b, gp);
                    }
                }

                using (Pen p = new Pen(curBorder, 1.2f))
                {
                    g.DrawPath(p, gp);
                }
            }

            string fullText = this.Text;
            if (!string.IsNullOrEmpty(BadgeText))
            {
                fullText += " " + BadgeText;
            }

            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (SolidBrush tb = new SolidBrush(this.ForeColor))
            {
                g.DrawString(fullText, this.Font, tb, new RectangleF(0, isPressed ? 1 : 0, this.Width, this.Height), sf);
            }
        }
    }

    public class NeonToolTipPopup : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static NeonToolTipPopup instance;
        public static NeonToolTipPopup Instance
        {
            get
            {
                if (instance == null || instance.IsDisposed)
                {
                    instance = new NeonToolTipPopup();
                }
                return instance;
            }
        }

        private System.Windows.Forms.Timer richTimer;
        private string mode = "none";
        private string miniText = "";
        private Rectangle targetAnchor;
        private GameItem targetGame;
        private string targetDriveRoot;

        public NeonToolTipPopup()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.DoubleBuffered = true;
            this.BackColor = UITheme.BgMain;
            this.TopMost = true;

            richTimer = new System.Windows.Forms.Timer();
            richTimer.Interval = 280;
            richTimer.Tick += (s, e) => {
                richTimer.Stop();
                DisplayRich();
            };

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST
                return cp;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        public void ShowMini(string text, Rectangle anchor)
        {
            richTimer.Stop();
            if (mode == "mini" && miniText == text && this.Visible) return;

            mode = "mini";
            miniText = text;
            targetAnchor = anchor;
            DisplayMini();
        }

        public void QueueRich(GameItem game, string driveRoot, Rectangle anchor)
        {
            if (mode == "rich" && targetGame == game && this.Visible) return;

            targetGame = game;
            targetDriveRoot = driveRoot;
            targetAnchor = anchor;

            richTimer.Stop();
            richTimer.Start();
        }

        public void HidePopup()
        {
            richTimer.Stop();
            mode = "none";
            if (this.Visible)
            {
                this.Hide();
            }
        }

        private void DisplayMini()
        {
            if (string.IsNullOrEmpty(miniText)) return;

            using (Graphics g = this.CreateGraphics())
            using (Font font = new Font("Segoe UI", 9F, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(miniText, font);
                int w = (int)size.Width + 24;
                int h = 30;

                int x = targetAnchor.X + (targetAnchor.Width - w) / 2;
                int y = targetAnchor.Bottom + 6;

                Screen scr = Screen.FromRectangle(targetAnchor);
                if (y + h > scr.WorkingArea.Bottom)
                {
                    y = targetAnchor.Top - h - 6;
                }
                if (x + w > scr.WorkingArea.Right)
                {
                    x = scr.WorkingArea.Right - w - 4;
                }
                if (x < scr.WorkingArea.Left)
                {
                    x = scr.WorkingArea.Left + 4;
                }

                this.Size = new Size(w, h);
                this.Location = new Point(x, y);
                ShowWindow(this.Handle, 4);
                this.Invalidate();
            }
        }

        private void DisplayRich()
        {
            if (targetGame == null) return;

            int w = 390;
            int h = 230;

            int x = targetAnchor.Right + 10;
            int y = targetAnchor.Top - 8;

            Screen scr = Screen.FromRectangle(targetAnchor);
            if (x + w > scr.WorkingArea.Right)
            {
                x = targetAnchor.Left - w - 10;
            }
            if (x < scr.WorkingArea.Left)
            {
                x = Math.Max(scr.WorkingArea.Left + 8, targetAnchor.Left);
                y = targetAnchor.Bottom + 10;
            }
            if (y + h > scr.WorkingArea.Bottom)
            {
                y = scr.WorkingArea.Bottom - h - 8;
            }
            if (y < scr.WorkingArea.Top)
            {
                y = scr.WorkingArea.Top + 8;
            }

            mode = "rich";
            this.Size = new Size(w, h);
            this.Location = new Point(x, y);
            ShowWindow(this.Handle, 4);
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            if (mode == "mini")
            {
                RectangleF r = new RectangleF(1, 1, this.Width - 3, this.Height - 3);
                using (GraphicsPath gp = UITheme.CreateRoundRect(r, 6f))
                using (LinearGradientBrush lgb = new LinearGradientBrush(r, Color.FromArgb(44, 18, 22), Color.FromArgb(24, 10, 13), 90f))
                using (Pen p = new Pen(UITheme.NeonAmber, 1.2f))
                using (Font font = new Font("Segoe UI", 9F, FontStyle.Bold))
                using (Brush brush = new SolidBrush(Color.FromArgb(254, 226, 226)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.FillPath(lgb, gp);
                    g.DrawPath(p, gp);
                    g.DrawString(miniText, font, brush, r, sf);
                }
            }
            else if (mode == "rich" && targetGame != null)
            {
                RectangleF r = new RectangleF(2, 2, this.Width - 5, this.Height - 5);

                using (Pen aura = new Pen(Color.FromArgb(50, 255, 95, 60), 3f))
                using (GraphicsPath gpAura = UITheme.CreateRoundRect(r, 14f))
                {
                    g.DrawPath(aura, gpAura);
                }

                using (GraphicsPath gp = UITheme.CreateRoundRect(r, 12f))
                using (LinearGradientBrush lgb = new LinearGradientBrush(r, Color.FromArgb(36, 16, 20), Color.FromArgb(18, 8, 10), 90f))
                using (Pen p = new Pen(UITheme.BorderGlow, 1.4f))
                {
                    g.FillPath(lgb, gp);
                    g.DrawPath(p, gp);
                }

                using (Pen glass = new Pen(Color.FromArgb(45, 255, 255, 255), 1f))
                {
                    g.DrawLine(glass, r.X + 14, r.Y + 1, r.Right - 14, r.Y + 1);
                }

                RectangleF iconRect = new RectangleF(r.X + 14, r.Y + 14, 40, 40);
                using (GraphicsPath iconPath = UITheme.CreateRoundRect(iconRect, 8f))
                using (LinearGradientBrush ib = new LinearGradientBrush(iconRect, Color.FromArgb(60, 24, 28), Color.FromArgb(32, 12, 16), 90f))
                using (Pen ip = new Pen(UITheme.NeonAmber, 1.2f))
                {
                    g.FillPath(ib, iconPath);
                    g.DrawPath(ip, iconPath);

                    Image img = IconCache.GetIcon(targetGame, targetDriveRoot);
                    if (img != null)
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(img, new RectangleF(iconRect.X + 4, iconRect.Y + 4, 32, 32));
                    }
                    else
                    {
                        string iconStr = !string.IsNullOrEmpty(targetGame.icon) ? targetGame.icon : "🎮";
                        using (Font iconFont = new Font("Segoe UI Emoji", 14F))
                        using (Brush ibText = new SolidBrush(Color.White))
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            g.DrawString(iconStr, iconFont, ibText, iconRect, sf);
                        }
                    }
                }

                string name = !string.IsNullOrEmpty(targetGame.name) ? targetGame.name : "Ứng Dụng";
                RectangleF titleRect = new RectangleF(r.X + 62, r.Y + 13, r.Width - 76, 22);
                using (Font nameFont = new Font("Segoe UI", 11.5F, FontStyle.Bold))
                using (Brush nameBrush = new SolidBrush(Color.White))
                using (StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.NoWrap })
                {
                    g.DrawString(name, nameFont, nameBrush, titleRect, sf);
                }

                string ratingStr = "★ " + (targetGame.rating > 0 ? targetGame.rating.ToString("0.0") : "5.0");
                string genreStr = targetGame.genre ?? "Game / Tiện Ích";
                string subHeader = ratingStr + "  •  " + genreStr;
                RectangleF subRect = new RectangleF(r.X + 62, r.Y + 36, r.Width - 76, 18);
                using (Font subFont = new Font("Segoe UI", 8.8F, FontStyle.Regular))
                using (Brush subBrush = new SolidBrush(UITheme.NeonAmber))
                using (StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.NoWrap })
                {
                    g.DrawString(subHeader, subFont, subBrush, subRect, sf);
                }

                using (Pen divPen = new Pen(Color.FromArgb(70, 40, 44), 1f))
                {
                    g.DrawLine(divPen, r.X + 14, r.Y + 66, r.Right - 14, r.Y + 66);
                }

                string exeTarget = targetGame.exePath ?? "";
                bool fileReady = File.Exists(exeTarget) || (!string.IsNullOrEmpty(targetGame.fallbackPath) && File.Exists(targetGame.fallbackPath));
                string statusTxt = fileReady ? "● SẴN SÀNG TRÊN Ổ DI ĐỘNG" : "○ CHƯA TÌM THẤY TỆP (Cần quét)";
                Color statusColor = fileReady ? Color.FromArgb(74, 222, 128) : Color.FromArgb(248, 113, 113);

                using (Font stFont = new Font("Segoe UI", 8F, FontStyle.Bold))
                using (Brush stBrush = new SolidBrush(statusColor))
                {
                    g.DrawString(statusTxt, stFont, stBrush, r.X + 14, r.Y + 75);
                }

                RectangleF pathRect = new RectangleF(r.X + 14, r.Y + 93, r.Width - 28, 18);
                using (Font pathFont = new Font("Segoe UI", 8.2F, FontStyle.Regular))
                using (Brush pathBrush = new SolidBrush(UITheme.TextMuted))
                using (StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisPath, FormatFlags = StringFormatFlags.NoWrap })
                {
                    g.DrawString("📁 " + exeTarget, pathFont, pathBrush, pathRect, sf);
                }

                string desc = !string.IsNullOrEmpty(targetGame.desc) ? targetGame.desc : "Ứng dụng và trò chơi độc lập, lưu trữ dữ liệu an toàn trên ổ di động E:\\.";
                RectangleF descRect = new RectangleF(r.X + 14, r.Y + 115, r.Width - 28, 56);
                using (Font descFont = new Font("Segoe UI", 8.5F, FontStyle.Regular))
                using (Brush descBrush = new SolidBrush(Color.FromArgb(243, 232, 230)))
                using (StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisWord })
                {
                    g.DrawString("📝 " + desc, descFont, descBrush, descRect, sf);
                }

                RectangleF hintRect = new RectangleF(r.X + 10, r.Bottom - 36, r.Width - 20, 28);
                using (GraphicsPath hintPath = UITheme.CreateRoundRect(hintRect, 6f))
                using (SolidBrush hintBg = new SolidBrush(Color.FromArgb(26, 11, 14)))
                using (Pen hintPen = new Pen(Color.FromArgb(64, 26, 30), 1f))
                using (Font hintFont = new Font("Segoe UI", 8F, FontStyle.Bold))
                using (Brush hintBrush = new SolidBrush(UITheme.NeonAmber))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.FillPath(hintBg, hintPath);
                    g.DrawPath(hintPen, hintPath);
                    g.DrawString("💡 Click đúp: Chơi ngay  •  Chuột phải: Menu  •  Tick: Xóa hàng loạt", hintFont, hintBrush, hintRect, sf);
                }
            }
        }
    }

    public static class NeonMessageBox
    {
        public static DialogResult Show(IWin32Window owner, string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            using (NeonMessageBoxForm form = new NeonMessageBoxForm(message, title, buttons, icon))
            {
                return form.ShowDialog(owner);
            }
        }
    }

    public class NeonMessageBoxForm : Form
    {
        private string message;
        private string titleText;
        private MessageBoxButtons buttons;
        private MessageBoxIcon icon;
        private bool isDragging = false;
        private Point dragStart;

        public NeonMessageBoxForm(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            this.message = message;
            this.titleText = title;
            this.buttons = buttons;
            this.icon = icon;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(460, 210);
            this.BackColor = UITheme.BgMain;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false;
            this.TopMost = true;

            this.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left && e.Y < 45)
                {
                    isDragging = true;
                    dragStart = e.Location;
                }
            };
            this.MouseMove += (s, e) => {
                if (isDragging)
                {
                    Point diff = new Point(e.X - dragStart.X, e.Y - dragStart.Y);
                    this.Location = new Point(this.Location.X + diff.X, this.Location.Y + diff.Y);
                }
            };
            this.MouseUp += (s, e) => { isDragging = false; };

            InitButtons();
        }

        private void InitButtons()
        {
            int btnH = 34;
            if (buttons == MessageBoxButtons.YesNo)
            {
                NeonPillButton btnYes = new NeonPillButton
                {
                    Text = "Đồng Ý (Xóa)",
                    Width = 130,
                    Height = btnH,
                    Location = new Point(180, this.Height - btnH - 18),
                    NormalBg = UITheme.NeonFlame,
                    HoverBg = Color.FromArgb(255, 90, 45),
                    NormalBorder = UITheme.BorderGlow,
                    HoverBorder = Color.White,
                    ForeColor = Color.White
                };
                btnYes.Click += (s, e) => {
                    this.DialogResult = DialogResult.Yes;
                    this.Close();
                };

                NeonPillButton btnNo = new NeonPillButton
                {
                    Text = "Hủy Bỏ",
                    Width = 110,
                    Height = btnH,
                    Location = new Point(325, this.Height - btnH - 18),
                    NormalBg = Color.FromArgb(42, 18, 22),
                    HoverBg = Color.FromArgb(64, 26, 32),
                    NormalBorder = Color.FromArgb(80, 36, 42),
                    HoverBorder = UITheme.NeonAmber,
                    ForeColor = Color.FromArgb(230, 190, 185)
                };
                btnNo.Click += (s, e) => {
                    this.DialogResult = DialogResult.No;
                    this.Close();
                };

                this.Controls.Add(btnYes);
                this.Controls.Add(btnNo);
            }
            else
            {
                NeonPillButton btnOk = new NeonPillButton
                {
                    Text = "Đã Hiểu",
                    Width = 120,
                    Height = btnH,
                    Location = new Point(this.Width - 140, this.Height - btnH - 18),
                    NormalBg = UITheme.NeonFlame,
                    HoverBg = Color.FromArgb(255, 90, 45),
                    NormalBorder = UITheme.BorderGlow,
                    HoverBorder = Color.White,
                    ForeColor = Color.White
                };
                btnOk.Click += (s, e) => {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                this.Controls.Add(btnOk);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            RectangleF r = new RectangleF(2, 2, this.Width - 5, this.Height - 5);

            using (GraphicsPath gp = UITheme.CreateRoundRect(r, 12f))
            using (LinearGradientBrush lgb = new LinearGradientBrush(r, Color.FromArgb(36, 16, 20), Color.FromArgb(18, 8, 10), 90f))
            using (Pen p = new Pen(UITheme.BorderGlow, 1.4f))
            {
                g.FillPath(lgb, gp);
                g.DrawPath(p, gp);
            }

            string iconStr = (buttons == MessageBoxButtons.YesNo) ? "❓" : "ℹ️";
            if (icon == MessageBoxIcon.Warning) iconStr = "⚠️";

            using (Font iconFont = new Font("Segoe UI Emoji", 12F))
            using (Brush ib = new SolidBrush(UITheme.NeonAmber))
            {
                g.DrawString(iconStr, iconFont, ib, 16, 14);
            }

            using (Font titleFont = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (Brush tb = new SolidBrush(Color.White))
            {
                g.DrawString(titleText, titleFont, tb, 42, 14);
            }

            using (Pen divPen = new Pen(Color.FromArgb(70, 32, 38), 1f))
            {
                g.DrawLine(divPen, 16, 42, this.Width - 16, 42);
            }

            RectangleF msgRect = new RectangleF(20, 54, this.Width - 40, this.Height - 110);
            using (Font msgFont = new Font("Segoe UI", 9.5F, FontStyle.Regular))
            using (Brush mb = new SolidBrush(Color.FromArgb(245, 230, 226)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near })
            {
                g.DrawString(message, msgFont, mb, msgRect, sf);
            }
        }
    }

    public class GameCardPanel : Panel
    {
        public GameItem Game { get; private set; }
        public bool IsSelected { get; set; }
        private bool isHovered = false;
        private Action<GameItem> onPlay;
        private Action<GameItem> onEdit;
        private Action<GameItem> onDelete;
        private Action<GameItem> onOpenFolder;
        private Action<GameItem, bool> onToggleSelect;
        private string driveRoot;

        private System.Windows.Forms.Timer animTimer;
        private float hoverProgress = 0f;
        private float targetHover = 0f;
        private float shimmerX = -120f;

        private RectangleF checkboxRect;
        private RectangleF playBtnRect;
        private RectangleF folderBtnRect;
        private RectangleF editBtnRect;
        private RectangleF delBtnRect;

        private bool checkboxHovered = false;
        private bool playBtnHovered = false;
        private bool folderHovered = false;
        private bool editHovered = false;
        private bool delHovered = false;

        public GameCardPanel(
            GameItem game,
            Action<GameItem> play,
            Action<GameItem> edit,
            Action<GameItem> del,
            Action<GameItem> openFolder,
            Action<GameItem, bool> toggleSelect,
            bool isSelected,
            string driveRoot
        )
        {
            this.Game = game;
            this.onPlay = play;
            this.onEdit = edit;
            this.onDelete = del;
            this.onOpenFolder = openFolder;
            this.onToggleSelect = toggleSelect;
            this.IsSelected = isSelected;
            this.driveRoot = driveRoot;

            this.Size = new Size(295, 210);
            this.Margin = new Padding(8, 10, 8, 10);
            this.BackColor = UITheme.BgCardNormal;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Default;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 15;
            animTimer.Tick += (s, e) => {
                float step = 0.12f;
                if (Math.Abs(hoverProgress - targetHover) < 0.02f)
                {
                    hoverProgress = targetHover;
                    if (targetHover == 0f && shimmerX > this.Width + 100)
                    {
                        animTimer.Stop();
                    }
                }
                else if (hoverProgress < targetHover)
                {
                    hoverProgress += step;
                    if (hoverProgress > 1f) hoverProgress = 1f;
                }
                else
                {
                    hoverProgress -= step;
                    if (hoverProgress < 0f) hoverProgress = 0f;
                }

                if (hoverProgress > 0.01f && shimmerX < this.Width + 100)
                {
                    shimmerX += 18f;
                }

                this.Invalidate();
            };

            ContextMenu cm = new ContextMenu();
            cm.MenuItems.Add("▶  Chơi Ngay (Portable)", delegate { onPlay(this.Game); });
            cm.MenuItems.Add("📁  Mở Thư Mục Chứa File", delegate { onOpenFolder(this.Game); });
            cm.MenuItems.Add("✏  Sửa Tên & Phân Loại", delegate { onEdit(this.Game); });
            cm.MenuItems.Add("-");
            cm.MenuItems.Add("✕  Xóa Khỏi Menu", delegate { onDelete(this.Game); });
            this.ContextMenu = cm;

            this.MouseEnter += (s, e) => SetHover(true);
            this.MouseLeave += (s, e) => {
                Point clientPos = this.PointToClient(Cursor.Position);
                if (!this.ClientRectangle.Contains(clientPos))
                {
                    SetHover(false);
                    checkboxHovered = false;
                    playBtnHovered = false;
                    folderHovered = false;
                    editHovered = false;
                    delHovered = false;
                    NeonToolTipPopup.Instance.HidePopup();
                    this.Invalidate();
                }
            };

            this.MouseMove += (s, e) => {
                Point p = e.Location;
                bool oldPlay = playBtnHovered;
                bool oldFold = folderHovered;
                bool oldEdit = editHovered;
                bool oldDel = delHovered;
                bool oldCb = checkboxHovered;

                checkboxHovered = checkboxRect.Contains(p);
                delHovered = delBtnRect.Contains(p);
                editHovered = editBtnRect.Contains(p);
                folderHovered = folderBtnRect.Contains(p);
                playBtnHovered = playBtnRect.Contains(p);

                if (checkboxHovered || playBtnHovered || folderHovered || editHovered || delHovered)
                {
                    this.Cursor = Cursors.Hand;
                }
                else
                {
                    this.Cursor = Cursors.Default;
                }

                if (checkboxHovered)
                {
                    Rectangle r = this.RectangleToScreen(Rectangle.Round(checkboxRect));
                    NeonToolTipPopup.Instance.ShowMini("☑ Tick chọn thẻ để xóa hàng loạt", r);
                }
                else if (delHovered)
                {
                    Rectangle r = this.RectangleToScreen(Rectangle.Round(delBtnRect));
                    NeonToolTipPopup.Instance.ShowMini("✕ Xóa thẻ này khỏi Menu", r);
                }
                else if (editHovered)
                {
                    Rectangle r = this.RectangleToScreen(Rectangle.Round(editBtnRect));
                    NeonToolTipPopup.Instance.ShowMini("✏ Sửa thông tin & phân loại", r);
                }
                else if (folderHovered)
                {
                    Rectangle r = this.RectangleToScreen(Rectangle.Round(folderBtnRect));
                    NeonToolTipPopup.Instance.ShowMini("📁 Mở vị trí tệp trên ổ đĩa", r);
                }
                else if (playBtnHovered)
                {
                    Rectangle r = this.RectangleToScreen(Rectangle.Round(playBtnRect));
                    NeonToolTipPopup.Instance.ShowMini("▶ Khởi chạy ứng dụng (Portable)", r);
                }
                else
                {
                    Rectangle r = this.RectangleToScreen(this.ClientRectangle);
                    NeonToolTipPopup.Instance.QueueRich(this.Game, this.driveRoot, r);
                }

                if (oldPlay != playBtnHovered || oldFold != folderHovered || oldEdit != editHovered || oldDel != delHovered || oldCb != checkboxHovered)
                {
                    this.Invalidate();
                }
            };

            this.MouseClick += (s, e) => {
                NeonToolTipPopup.Instance.HidePopup();
                if (e.Button == MouseButtons.Left)
                {
                    Point p = e.Location;
                    if (checkboxRect.Contains(p))
                    {
                        IsSelected = !IsSelected;
                        if (onToggleSelect != null) onToggleSelect(this.Game, IsSelected);
                        this.Invalidate();
                        return;
                    }
                    else if (playBtnRect.Contains(p))
                    {
                        onPlay(this.Game);
                    }
                    else if (folderBtnRect.Contains(p))
                    {
                        onOpenFolder(this.Game);
                    }
                    else if (editBtnRect.Contains(p))
                    {
                        onEdit(this.Game);
                    }
                    else if (delBtnRect.Contains(p))
                    {
                        onDelete(this.Game);
                    }
                }
            };

            this.DoubleClick += (s, e) => {
                NeonToolTipPopup.Instance.HidePopup();
                onPlay(this.Game);
            };
        }

        private void SetHover(bool hover)
        {
            if (isHovered != hover)
            {
                isHovered = hover;
                targetHover = hover ? 1f : 0f;
                if (hover)
                {
                    shimmerX = -120f;
                }
                animTimer.Start();
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int lift = (int)(4f * hoverProgress);
            RectangleF cardRect = new RectangleF(3, 3 - lift, this.Width - 7, this.Height - 7);

            playBtnRect = new RectangleF(cardRect.X + 12, cardRect.Y + 155, cardRect.Width - 24, 38);
            float btnW = 28f;
            float btnH = 24f;
            float rightX = cardRect.Right - 10;
            delBtnRect = new RectangleF(rightX - btnW, cardRect.Y + 10, btnW, btnH);
            editBtnRect = new RectangleF(delBtnRect.X - btnW - 4, cardRect.Y + 10, btnW, btnH);
            folderBtnRect = new RectangleF(editBtnRect.X - btnW - 4, cardRect.Y + 10, btnW, btnH);

            checkboxRect = new RectangleF(cardRect.X + 10, cardRect.Y + 10, 22, 22);

            // 1. Multi-layer Neon Aura
            if (IsSelected || hoverProgress > 0.05f)
            {
                float factor = IsSelected ? Math.Max(0.75f, hoverProgress) : hoverProgress;
                int aura1 = (int)(20 * factor);
                int aura2 = (int)(45 * factor);
                int aura3 = (int)(70 * factor);

                using (Pen p1 = new Pen(Color.FromArgb(aura1, 255, 95, 60), 6f))
                using (GraphicsPath gp1 = UITheme.CreateRoundRect(cardRect, 16f))
                {
                    g.DrawPath(p1, gp1);
                }

                using (Pen p2 = new Pen(Color.FromArgb(aura2, 255, 110, 75), 3f))
                using (GraphicsPath gp2 = UITheme.CreateRoundRect(cardRect, 14f))
                {
                    g.DrawPath(p2, gp2);
                }

                using (Pen p3 = new Pen(Color.FromArgb(aura3, 255, 140, 90), 1.5f))
                using (GraphicsPath gp3 = UITheme.CreateRoundRect(cardRect, 13f))
                {
                    g.DrawPath(p3, gp3);
                }
            }

            // 2. Card Background Gradient
            Color topBg = UITheme.Blend(Color.FromArgb(32, 15, 18), Color.FromArgb(48, 22, 26), hoverProgress);
            Color botBg = UITheme.Blend(Color.FromArgb(20, 10, 12), Color.FromArgb(32, 14, 17), hoverProgress);

            using (GraphicsPath cardPath = UITheme.CreateRoundRect(cardRect, 13f))
            {
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(cardRect, topBg, botBg, 90f))
                {
                    g.FillPath(bgBrush, cardPath);
                }

                RectangleF bannerRect = new RectangleF(cardRect.X, cardRect.Y, cardRect.Width, 55);
                using (GraphicsPath bannerClip = UITheme.CreateRoundRect(cardRect, 13f))
                {
                    g.SetClip(bannerClip);
                    Color bTop = UITheme.Blend(Color.FromArgb(46, 20, 24), Color.FromArgb(64, 26, 31), hoverProgress);
                    Color bBot = Color.FromArgb(0, 32, 15, 18);
                    using (LinearGradientBrush bannerBrush = new LinearGradientBrush(bannerRect, bTop, bBot, 90f))
                    {
                        g.FillRectangle(bannerBrush, bannerRect);
                    }

                    if (shimmerX > -80f && shimmerX < cardRect.Width + 80f)
                    {
                        PointF[] shimmerPts = new PointF[] {
                            new PointF(cardRect.X + shimmerX - 25, cardRect.Y),
                            new PointF(cardRect.X + shimmerX + 25, cardRect.Y),
                            new PointF(cardRect.X + shimmerX + 5, cardRect.Bottom),
                            new PointF(cardRect.X + shimmerX - 45, cardRect.Bottom)
                        };
                        using (LinearGradientBrush shBrush = new LinearGradientBrush(
                            new RectangleF(cardRect.X + shimmerX - 45, cardRect.Y, 90, cardRect.Height),
                            Color.FromArgb(0, 255, 255, 255),
                            Color.FromArgb((int)(38 * hoverProgress), 255, 235, 210),
                            LinearGradientMode.Horizontal))
                        {
                            g.FillPolygon(shBrush, shimmerPts);
                        }
                    }

                    g.ResetClip();
                }

                using (Pen glassPen = new Pen(Color.FromArgb(45, 255, 255, 255), 1f))
                {
                    g.DrawLine(glassPen, cardRect.X + 14, cardRect.Y + 1, cardRect.Right - 14, cardRect.Y + 1);
                }

                Color curBorder = IsSelected ? Color.FromArgb(251, 146, 60) : UITheme.Blend(UITheme.BorderDim, UITheme.BorderGlow, hoverProgress);
                float borderWidth = IsSelected ? 2.2f : (1.2f + (0.8f * hoverProgress));
                using (Pen borderPen = new Pen(curBorder, borderWidth))
                {
                    g.DrawPath(borderPen, cardPath);
                }
            }

            // 3. Checkbox
            using (GraphicsPath cbPath = UITheme.CreateRoundRect(checkboxRect, 5f))
            {
                if (IsSelected)
                {
                    using (LinearGradientBrush cbBg = new LinearGradientBrush(checkboxRect, UITheme.NeonFlame, UITheme.NeonOrange, 90f))
                    {
                        g.FillPath(cbBg, cbPath);
                    }
                    using (Pen cbPen = new Pen(Color.White, 1.4f))
                    {
                        g.DrawPath(cbPen, cbPath);
                    }
                    using (Pen checkPen = new Pen(Color.White, 2f))
                    {
                        checkPen.StartCap = LineCap.Round;
                        checkPen.EndCap = LineCap.Round;
                        PointF p1 = new PointF(checkboxRect.X + 5, checkboxRect.Y + 11);
                        PointF p2 = new PointF(checkboxRect.X + 9, checkboxRect.Y + 15);
                        PointF p3 = new PointF(checkboxRect.X + 17, checkboxRect.Y + 6);
                        g.DrawLines(checkPen, new PointF[] { p1, p2, p3 });
                    }
                }
                else
                {
                    Color cbBgCol = checkboxHovered ? Color.FromArgb(120, 60, 24, 28) : Color.FromArgb(70, 36, 16, 18);
                    Color cbBorderCol = checkboxHovered ? UITheme.NeonAmber : Color.FromArgb(100, 70, 32, 36);
                    using (SolidBrush cbBg = new SolidBrush(cbBgCol))
                    using (Pen cbPen = new Pen(cbBorderCol, 1.2f))
                    {
                        g.FillPath(cbBg, cbPath);
                        g.DrawPath(cbPen, cbPath);
                    }
                }
            }

            // 4. Category Tag Pill (shifted right for checkbox)
            string catText = (Game.category ?? "GAME").ToUpper();
            Color tagBorderColor = Color.FromArgb(220, 38, 38);
            Color tagBgColor1 = Color.FromArgb(64, 22, 26);
            Color tagBgColor2 = Color.FromArgb(40, 14, 17);

            if (catText == "HOT")
            {
                catText = "🔥 HOT";
                tagBorderColor = Color.FromArgb(239, 68, 68);
                tagBgColor1 = Color.FromArgb(80, 24, 28);
            }
            else if (catText == "BROWSER")
            {
                catText = "🌐 TRÌNH DUYỆT";
                tagBorderColor = Color.FromArgb(59, 130, 246);
                tagBgColor1 = Color.FromArgb(28, 38, 68);
                tagBgColor2 = Color.FromArgb(18, 22, 42);
            }
            else if (catText == "TOOL")
            {
                catText = "🛠️ TIỆN ÍCH";
                tagBorderColor = Color.FromArgb(168, 85, 247);
                tagBgColor1 = Color.FromArgb(55, 24, 68);
                tagBgColor2 = Color.FromArgb(32, 16, 42);
            }
            else if (catText == "OFFLINE")
            {
                catText = "🎮 OFFLINE";
                tagBorderColor = Color.FromArgb(34, 197, 94);
                tagBgColor1 = Color.FromArgb(22, 54, 32);
                tagBgColor2 = Color.FromArgb(16, 34, 22);
            }
            else if (catText == "ONLINE")
            {
                catText = "⚡ ONLINE";
                tagBorderColor = Color.FromArgb(249, 115, 22);
                tagBgColor1 = Color.FromArgb(68, 34, 20);
                tagBgColor2 = Color.FromArgb(42, 20, 14);
            }

            using (Font tagFont = new Font("Segoe UI", 7.8F, FontStyle.Bold))
            {
                SizeF tagSize = g.MeasureString(catText, tagFont);
                RectangleF pillRect = new RectangleF(cardRect.X + 38, cardRect.Y + 10, tagSize.Width + 12, 22);

                using (GraphicsPath pillPath = UITheme.CreateRoundRect(pillRect, 8f))
                using (LinearGradientBrush pillBg = new LinearGradientBrush(pillRect, tagBgColor1, tagBgColor2, 90f))
                using (Pen pillPen = new Pen(tagBorderColor, 1f))
                using (Brush pillTxt = new SolidBrush(Color.White))
                {
                    g.FillPath(pillBg, pillPath);
                    g.DrawPath(pillPen, pillPath);
                    g.DrawString(catText, tagFont, pillTxt, pillRect.X + 6, pillRect.Y + 3);
                }
            }

            // 5. Quick Action Buttons
            int actionAlpha = (int)(70 + 185 * hoverProgress);
            if (actionAlpha > 255) actionAlpha = 255;

            DrawActionButton(g, folderBtnRect, "📁", folderHovered, actionAlpha, Color.FromArgb(251, 146, 60));
            DrawActionButton(g, editBtnRect, "✏", editHovered, actionAlpha, Color.FromArgb(251, 191, 36));
            DrawActionButton(g, delBtnRect, "✕", delHovered, actionAlpha, Color.FromArgb(248, 113, 113));

            // 6. Middle Content: Avatar Icon Ring + Title + Details
            float contentY = cardRect.Y + 46;
            RectangleF avatarRect = new RectangleF(cardRect.X + 12, contentY + 2, 44, 44);

            using (GraphicsPath avatarPath = UITheme.CreateRoundRect(avatarRect, 10f))
            using (LinearGradientBrush avBg = new LinearGradientBrush(avatarRect, Color.FromArgb(52, 22, 26), Color.FromArgb(28, 12, 15), 90f))
            using (Pen avPen = new Pen(UITheme.Blend(Color.FromArgb(110, 42, 45), UITheme.NeonAmber, hoverProgress), 1.5f))
            {
                g.FillPath(avBg, avatarPath);
                g.DrawPath(avPen, avatarPath);

                Image appIcon = IconCache.GetIcon(Game, driveRoot);
                if (appIcon != null)
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    RectangleF iconDest = new RectangleF(avatarRect.X + 6, avatarRect.Y + 6, 32, 32);
                    g.DrawImage(appIcon, iconDest);
                }
                else
                {
                    string iconStr = !string.IsNullOrEmpty(Game.icon) ? Game.icon : "🎮";
                    using (Font iconFont = new Font("Segoe UI Emoji", 15F, FontStyle.Regular))
                    using (Brush iconBrush = new SolidBrush(Color.White))
                    {
                        SizeF iconSize = g.MeasureString(iconStr, iconFont);
                        float ix = avatarRect.X + (avatarRect.Width - iconSize.Width) / 2f;
                        float iy = avatarRect.Y + (avatarRect.Height - iconSize.Height) / 2f;
                        g.DrawString(iconStr, iconFont, iconBrush, ix, iy);
                    }
                }
            }

            // Title Text
            string name = !string.IsNullOrEmpty(Game.name) ? Game.name : Path.GetFileNameWithoutExtension(Game.exePath);
            if (string.IsNullOrEmpty(name)) name = "Ứng Dụng";

            RectangleF titleRect = new RectangleF(cardRect.X + 66, contentY, cardRect.Width - 76, 24);
            using (Font nameFont = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (Brush nameBrush = new SolidBrush(Color.White))
            {
                StringFormat sf = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisWord,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(name, nameFont, nameBrush, titleRect, sf);
            }

            // Genre & Rating Stars
            string ratingStr = "★ " + (Game.rating > 0 ? Game.rating.ToString("0.0") : "5.0");
            string genreStr = Game.genre ?? "Game / Ứng Dụng";

            using (Font starFont = new Font("Segoe UI", 8.5F, FontStyle.Bold))
            using (Brush starBrush = new SolidBrush(Color.FromArgb(251, 191, 36)))
            {
                g.DrawString(ratingStr, starFont, starBrush, cardRect.X + 66, contentY + 24);
            }

            using (Font genreFont = new Font("Segoe UI", 8.5F, FontStyle.Regular))
            using (Brush genreBrush = new SolidBrush(UITheme.TextMuted))
            {
                float starWidth = g.MeasureString(ratingStr, new Font("Segoe UI", 8.5F, FontStyle.Bold)).Width;
                RectangleF gRect = new RectangleF(cardRect.X + 66 + starWidth + 6, contentY + 24, cardRect.Width - 76 - starWidth - 6, 18);
                StringFormat sf = new StringFormat { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString("• " + genreStr, genreFont, genreBrush, gRect, sf);
            }

            // Play Count / Status
            string playInfo = "🔥 " + (Game.playCount > 0 ? (Game.playCount >= 1000 ? (Game.playCount / 1000.0).ToString("0.#") + "k" : Game.playCount.ToString()) : "1.5k") + " lượt mở";
            if (catText.IndexOf("TIỆN ÍCH") >= 0 || catText.IndexOf("TRÌNH DUYỆT") >= 0)
            {
                playInfo = "⚡ Portable Ready • Ổ E:\\";
            }
            using (Font playCountFont = new Font("Segoe UI", 8F, FontStyle.Regular))
            using (Brush countBrush = new SolidBrush(Color.FromArgb(190, 140, 130)))
            {
                g.DrawString(playInfo, playCountFont, countBrush, cardRect.X + 66, contentY + 43);
            }

            // 7. Action Button: "CHƠI NGAY" / "KHỞI CHẠY"
            using (GraphicsPath btnPath = UITheme.CreateRoundRect(playBtnRect, 9f))
            {
                Color btnC1 = playBtnHovered ? Color.FromArgb(235, 60, 30) : UITheme.NeonFlame;
                Color btnC2 = playBtnHovered ? Color.FromArgb(255, 115, 45) : UITheme.NeonOrange;

                if (playBtnHovered || hoverProgress > 0.1f)
                {
                    int bGlowA = playBtnHovered ? 65 : (int)(30 * hoverProgress);
                    using (Pen bGlowPen = new Pen(Color.FromArgb(bGlowA, 255, 95, 60), 3f))
                    using (GraphicsPath bGlowPath = UITheme.CreateRoundRect(playBtnRect, 10f))
                    {
                        g.DrawPath(bGlowPen, bGlowPath);
                    }
                }

                using (LinearGradientBrush btnBrush = new LinearGradientBrush(playBtnRect, btnC1, btnC2, 90f))
                {
                    g.FillPath(btnBrush, btnPath);
                }

                using (Pen borderPen = new Pen(Color.FromArgb(100, 255, 255, 255), 1f))
                {
                    g.DrawLine(borderPen, playBtnRect.X + 8, playBtnRect.Y + 1, playBtnRect.Right - 8, playBtnRect.Y + 1);
                }

                string btnText = (catText.IndexOf("TIỆN ÍCH") >= 0 || catText.IndexOf("TRÌNH DUYỆT") >= 0) ? "▶   KHỞI CHẠY" : "▶   CHƠI NGAY";
                if (playBtnHovered)
                {
                    btnText += "  ➜";
                }

                using (Font btnFont = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.White))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(btnText, btnFont, textBrush, playBtnRect, sf);
                }
            }
        }

        private void DrawActionButton(Graphics g, RectangleF rect, string icon, bool isHovered, int alpha, Color highlight)
        {
            using (GraphicsPath path = UITheme.CreateRoundRect(rect, 6f))
            {
                Color bg = isHovered ? Color.FromArgb(alpha, 64, 26, 30) : Color.FromArgb(alpha / 2, 40, 16, 18);
                Color border = isHovered ? highlight : Color.FromArgb(alpha / 2, 80, 36, 40);

                using (SolidBrush sb = new SolidBrush(bg))
                using (Pen p = new Pen(border, 1f))
                {
                    g.FillPath(sb, path);
                    g.DrawPath(p, path);
                }

                Color textC = isHovered ? highlight : Color.FromArgb(alpha, 220, 180, 175);
                using (Font f = new Font("Segoe UI", 9F, FontStyle.Bold))
                using (Brush b = new SolidBrush(textC))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(icon, f, b, rect, sf);
                }
            }
        }
    }

    public class PmtClickHubPanel : Panel
    {
        private string driveRoot;
        private Label lblBannerBadge;
        private Label lblServerStatus;
        private Button btnToggleServer;
        private Button btnOpenWeb;
        private Button btnOpenApk;
        private PictureBox picQr;
        private TextBox txtDeviceId;
        private Button btnCopyId;
        private TextBox txtRemoteUrl;
        private Button btnCopyRemote;
        private TextBox txtWifiUrl;
        private Button btnCopyWifi;
        private TextBox txtApkUrl;
        private Button btnCopyApk;
        private TextBox txtLog;
        private CheckBox chkShowLog;
        private Button btnClearLog;
        private Button btnCopyLog;
        private Panel pnlContent;
        private Panel pnlLeft;
        private Panel pnlRight;
        private Panel cardServer;
        private Panel cardConnection;
        private Panel cardLog;
        private System.Windows.Forms.Timer statusTimer;

        public PmtClickHubPanel(string driveRoot)
        {
            this.driveRoot = driveRoot;
            this.Dock = DockStyle.Fill;
            this.BackColor = UITheme.BgMain;
            this.AutoScroll = true;
            this.Padding = new Padding(20, 14, 20, 18);

            InitHubUI();

            PmtClickManager.OnLogReceived += OnServerLogReceived;
            PmtClickManager.OnStatusChanged += OnServerStatusChanged;
            PmtClickManager.OnPublicUrlReady += OnServerPublicUrlReady;

            statusTimer = new System.Windows.Forms.Timer();
            statusTimer.Interval = 1200;
            statusTimer.Tick += delegate(object s, EventArgs e) {
                UpdateServerStatus();
            };
            statusTimer.Start();

            // Populate initial log history
            LoadInitialLogs();
            UpdateServerStatus();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PmtClickManager.OnLogReceived -= OnServerLogReceived;
                PmtClickManager.OnStatusChanged -= OnServerStatusChanged;
                PmtClickManager.OnPublicUrlReady -= OnServerPublicUrlReady;
                if (statusTimer != null)
                {
                    statusTimer.Stop();
                    statusTimer.Dispose();
                    statusTimer = null;
                }
            }
            base.Dispose(disposing);
        }

        private void OnServerLogReceived(string text)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                try { this.BeginInvoke(new Action<string>(OnServerLogReceived), text); } catch { }
                return;
            }

            if (txtLog != null && !txtLog.IsDisposed)
            {
                txtLog.AppendText(text + Environment.NewLine);
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.ScrollToCaret();
            }
        }

        private void OnServerStatusChanged()
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                try { this.BeginInvoke(new Action(OnServerStatusChanged)); } catch { }
                return;
            }
            UpdateServerStatus();
        }

        private void OnServerPublicUrlReady(string url)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                try { this.BeginInvoke(new Action<string>(OnServerPublicUrlReady), url); } catch { }
                return;
            }
            UpdateServerStatus();
        }

        private void LoadInitialLogs()
        {
            if (txtLog == null || txtLog.IsDisposed) return;
            txtLog.Clear();
            List<string> logs = PmtClickManager.GetLogHistory();
            if (logs != null && logs.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < logs.Count; i++)
                {
                    sb.AppendLine(logs[i]);
                }
                txtLog.Text = sb.ToString();
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.ScrollToCaret();
            }
            else
            {
                txtLog.Text = string.Format("[{0:HH:mm:ss}] ℹ️ Sẵn sàng. Nhấn [KHỞI ĐỘNG DỊCH VỤ] để bắt đầu máy chủ PMT Click.{1}", DateTime.Now, Environment.NewLine);
            }
        }

        private void InitHubUI()
        {
            // 1. Top Header Banner
            Panel banner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = Color.FromArgb(28, 12, 16),
                Padding = new Padding(16, 10, 16, 10)
            };
            banner.Paint += delegate(object s, PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF r = new RectangleF(1, 1, banner.Width - 3, banner.Height - 3);
                using (GraphicsPath gp = UITheme.CreateRoundRect(r, 12f))
                using (LinearGradientBrush lgb = new LinearGradientBrush(r, Color.FromArgb(46, 18, 24), Color.FromArgb(26, 10, 14), 90f))
                using (Pen p = new Pen(Color.FromArgb(160, 50, 40), 1.2f))
                {
                    g.FillPath(lgb, gp);
                    g.DrawPath(p, gp);
                }
            };

            Label lblBannerIcon = new Label
            {
                Text = "📱",
                Font = new Font("Segoe UI Emoji", 24F),
                Location = new Point(14, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            Label lblBannerTitle = new Label
            {
                Text = "PMT CLICK • ĐIỀU KHIỂN MÁY TÍNH TỪ ĐIỆN THOẠI",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = UITheme.NeonCoral,
                Location = new Point(66, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            Label lblBannerSub = new Label
            {
                Text = "Bàn phím cơ ảo & Chuột trackpad độ trễ cực thấp < 1ms • Điều khiển qua Wi-Fi LAN hoặc 4G/5G Internet.",
                Font = new Font("Segoe UI", 8.8F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Location = new Point(68, 39),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            lblBannerBadge = new Label
            {
                Text = "○ OFFLINE",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 160, 155),
                AutoSize = false,
                Size = new Size(190, 32),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(40, 16, 20)
            };
            lblBannerBadge.Paint += delegate(object s, PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF r = new RectangleF(1, 1, lblBannerBadge.Width - 3, lblBannerBadge.Height - 3);
                using (GraphicsPath gp = UITheme.CreateRoundRect(r, 8f))
                using (Pen p = new Pen(Color.FromArgb(120, 45, 40), 1f))
                {
                    g.DrawPath(p, gp);
                }
            };
            banner.Resize += delegate(object s, EventArgs e) {
                lblBannerBadge.Location = new Point(banner.Width - lblBannerBadge.Width - 18, 20);
            };

            banner.Controls.Add(lblBannerIcon);
            banner.Controls.Add(lblBannerTitle);
            banner.Controls.Add(lblBannerSub);
            banner.Controls.Add(lblBannerBadge);

            // 2. Responsive Content Container
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 12, 0, 0)
            };

            pnlLeft = new Panel
            {
                BackColor = Color.Transparent
            };

            pnlRight = new Panel
            {
                BackColor = Color.Transparent
            };

            // Build Left Column Cards
            BuildLeftColumn();

            // Build Right Column Terminal
            BuildRightColumn();

            pnlContent.Controls.Add(pnlLeft);
            pnlContent.Controls.Add(pnlRight);

            pnlContent.Resize += delegate(object s, EventArgs e) {
                LayoutPmtPanels();
            };

            this.Controls.Add(pnlContent);
            this.Controls.Add(banner);

            LayoutPmtPanels();
        }

        private void LayoutPmtPanels()
        {
            if (pnlContent == null || pnlLeft == null || pnlRight == null) return;

            int pad = 0;
            int gap = 12;
            int totalW = pnlContent.ClientSize.Width;
            int totalH = pnlContent.ClientSize.Height - 12;

            if (totalW >= 1060)
            {
                int leftW = 540;
                int rightW = Math.Max(420, totalW - leftW - gap);

                pnlLeft.Location = new Point(pad, 12);
                pnlLeft.Size = new Size(leftW, totalH);

                pnlRight.Location = new Point(leftW + gap, 12);
                pnlRight.Size = new Size(rightW, totalH);

                if (cardServer != null) cardServer.Size = new Size(leftW, 140);
                if (cardConnection != null)
                {
                    cardConnection.Location = new Point(0, 150);
                    cardConnection.Size = new Size(leftW, Math.Max(380, totalH - 150));
                }
            }
            else
            {
                int fullW = totalW;
                pnlLeft.Location = new Point(pad, 12);
                pnlLeft.Size = new Size(fullW, 530);

                if (cardServer != null) cardServer.Size = new Size(fullW, 140);
                if (cardConnection != null)
                {
                    cardConnection.Location = new Point(0, 150);
                    cardConnection.Size = new Size(fullW, 370);
                }

                int rightY = 554;
                int rightH = Math.Max(320, totalH - rightY);
                pnlRight.Location = new Point(pad, rightY);
                pnlRight.Size = new Size(fullW, rightH);
            }

            if (cardLog != null)
            {
                cardLog.Size = pnlRight.ClientSize;
            }
        }

        private void BuildLeftColumn()
        {
            // CARD 1: Server Control & Quick Actions
            cardServer = CreateCardPanel(540, 140);
            AddCardTitle(cardServer, "⚡ TRẠNG THÁI MÁY CHỦ (PMT CLICK ENGINE)", UITheme.NeonAmber);

            lblServerStatus = new Label
            {
                Text = "Đang kiểm tra trạng thái...",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(18, 42),
                Size = new Size(504, 38)
            };
            cardServer.Controls.Add(lblServerStatus);

            btnToggleServer = CreateButton("▶  KHỞI ĐỘNG DỊCH VỤ", UITheme.NeonFlame, Color.White, 18, 88, 195, 38);
            btnToggleServer.Click += delegate(object s, EventArgs e) {
                if (PmtClickManager.IsRunning())
                {
                    PmtClickManager.Stop();
                }
                else
                {
                    PmtClickManager.Start(driveRoot);
                }
                UpdateServerStatus();
            };
            cardServer.Controls.Add(btnToggleServer);

            btnOpenWeb = CreateButton("🌐  Web Controller", Color.FromArgb(46, 20, 26), UITheme.NeonAmber, 222, 88, 150, 38);
            btnOpenWeb.Click += delegate(object s, EventArgs e) {
                int port = PmtClickManager.ActivePort;
                string url = string.Format("http://localhost:{0}", port > 0 ? port : 5000);
                OpenUrlWithPortableBrowser(url);
            };
            cardServer.Controls.Add(btnOpenWeb);

            btnOpenApk = CreateButton("📥  Mở File APK", Color.FromArgb(42, 18, 22), Color.FromArgb(248, 113, 113), 380, 88, 140, 38);
            btnOpenApk.Click += delegate(object s, EventArgs e) {
                string apkPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PMT_Click.apk");
                if (!File.Exists(apkPath) && !string.IsNullOrEmpty(driveRoot))
                {
                    string p1 = Path.Combine(driveRoot.TrimEnd('\\') + "\\", @"code\phone_pc_keyboard\PMT_Click.apk");
                    string p2 = Path.Combine(driveRoot.TrimEnd('\\') + "\\", "PMT_Click.apk");
                    if (File.Exists(p1)) apkPath = p1;
                    else if (File.Exists(p2)) apkPath = p2;
                }

                if (File.Exists(apkPath))
                {
                    Process.Start("explorer.exe", "/select,\"" + apkPath + "\"");
                }
                else
                {
                    int port = PmtClickManager.ActivePort;
                    string url = string.Format("http://localhost:{0}/apk", port > 0 ? port : 5000);
                    try { Process.Start(url); } catch { }
                }
            };
            cardServer.Controls.Add(btnOpenApk);

            pnlLeft.Controls.Add(cardServer);

            // CARD 2: Connection Details & QR Code
            cardConnection = CreateCardPanel(540, 380);
            cardConnection.Location = new Point(0, 150);
            AddCardTitle(cardConnection, "📶 THÔNG TIN KẾT NỐI (LAN WI-FI & 4G/5G INTERNET)", UITheme.NeonCoral);

            // QR Code Box (Left side of Card 2)
            Panel qrContainer = new Panel
            {
                Location = new Point(18, 48),
                Size = new Size(144, 250),
                BackColor = Color.Transparent
            };

            picQr = new PictureBox
            {
                Location = new Point(0, 0),
                Size = new Size(144, 144),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };
            picQr.Paint += delegate(object s, PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, picQr.Width - 1, picQr.Height - 1);
                using (Pen p = new Pen(Color.FromArgb(180, 60, 50), 1.5f))
                {
                    g.DrawRectangle(p, r);
                }
            };
            qrContainer.Controls.Add(picQr);

            Label lblQrCaption = new Label
            {
                Text = "⚡ Quét Camera\nTải App & Kết nối",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 222, 128),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 150),
                Size = new Size(144, 36)
            };
            qrContainer.Controls.Add(lblQrCaption);

            Label lblQrSub = new Label
            {
                Text = "Hỗ trợ camera thường, Zalo, QR Scanner.",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(170, 130, 125),
                TextAlign = ContentAlignment.TopCenter,
                Location = new Point(0, 190),
                Size = new Size(144, 45)
            };
            qrContainer.Controls.Add(lblQrSub);

            cardConnection.Controls.Add(qrContainer);

            // Connection Fields (Right side of Card 2)
            int fieldX = 176;

            // Row 1: UltraViewer Device ID
            Label lblIdTag = CreateFieldLabel("🔑 MÃ ID KẾT NỐI (DEVICE ID 6 SỐ):", Color.FromArgb(251, 191, 36), fieldX, 46);
            cardConnection.Controls.Add(lblIdTag);

            txtDeviceId = new TextBox
            {
                Location = new Point(fieldX, 68),
                Size = new Size(170, 28),
                ReadOnly = true,
                BackColor = Color.FromArgb(14, 6, 8),
                ForeColor = Color.FromArgb(251, 191, 36),
                Font = new Font("Consolas", 12F, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                Text = "--- ---"
            };
            cardConnection.Controls.Add(txtDeviceId);

            btnCopyId = CreateSmallCopyButton("📋 Copy ID", Color.FromArgb(251, 191, 36), Color.Black, fieldX + 178, 68, 90, 27);
            btnCopyId.Click += delegate(object s, EventArgs e) {
                if (!string.IsNullOrEmpty(txtDeviceId.Text) && txtDeviceId.Text != "--- ---")
                {
                    try
                    {
                        Clipboard.SetText(txtDeviceId.Text.Replace(" ", ""));
                        MessageBox.Show("Đã sao chép mã ID: " + txtDeviceId.Text, "PMT Click", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch { }
                }
            };
            cardConnection.Controls.Add(btnCopyId);

            // Row 2: 4G/5G Remote Domain Link
            Label lblRemoteTag = CreateFieldLabel("🌍 KẾT NỐI TỪ XA 4G/5G (DOMAIN CHÍNH THỨC):", Color.FromArgb(251, 146, 60), fieldX, 106);
            cardConnection.Controls.Add(lblRemoteTag);

            txtRemoteUrl = new TextBox
            {
                Location = new Point(fieldX, 126),
                Size = new Size(185, 24),
                ReadOnly = true,
                BackColor = Color.FromArgb(18, 8, 11),
                ForeColor = Color.FromArgb(251, 146, 60),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = "https://www.quiniumthu.qd.je",
                Cursor = Cursors.Hand
            };
            txtRemoteUrl.DoubleClick += delegate(object s, EventArgs e) {
                if (!string.IsNullOrEmpty(txtRemoteUrl.Text) && txtRemoteUrl.Text.StartsWith("http"))
                {
                    OpenUrlWithPortableBrowser(txtRemoteUrl.Text);
                }
            };
            cardConnection.Controls.Add(txtRemoteUrl);

            btnCopyRemote = CreateSmallCopyButton("📋 Copy", Color.FromArgb(60, 24, 20), Color.FromArgb(251, 146, 60), fieldX + 190, 125, 74, 26);
            btnCopyRemote.Click += delegate(object s, EventArgs e) {
                if (!string.IsNullOrEmpty(txtRemoteUrl.Text) && txtRemoteUrl.Text.StartsWith("http"))
                {
                    try
                    {
                        Clipboard.SetText(txtRemoteUrl.Text);
                        MessageBox.Show("Đã sao chép đường dẫn 4G:\n" + txtRemoteUrl.Text, "PMT Click", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch { }
                }
            };
            cardConnection.Controls.Add(btnCopyRemote);

            Button btnToken = CreateSmallCopyButton("🔑 Token CF", Color.FromArgb(46, 22, 16), Color.FromArgb(251, 191, 36), fieldX + 268, 125, 78, 26);
            btnToken.Click += delegate(object s, EventArgs e) {
                string tokenPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cloudflare_token.txt");
                if (!File.Exists(tokenPath))
                {
                    File.WriteAllText(tokenPath, "# Dan Cloudflare Tunnel Token cua ban vao ben duoi roi luu lai (Ctrl+S):\n");
                }
                try { Process.Start("notepad.exe", tokenPath); } catch { }
                MessageBox.Show("Đã mở file cloudflare_token.txt.\n\nNếu bạn đã tạo Tunnel trên Cloudflare Zero Trust cho domain quiniumthu.qd.je, hãy dán Token vào file notepad vừa mở rồi bấm Lưu (Ctrl+S).\nSau đó Khởi Động Lại PMT Click để kết nối vĩnh viễn!", "Cấu Hình Cloudflare Token", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            cardConnection.Controls.Add(btnToken);

            // Row 3: Wi-Fi LAN
            Label lblWifiTag = CreateFieldLabel("🏠 LINK MẠNG WI-FI LAN (TRONG NHÀ):", Color.FromArgb(56, 189, 248), fieldX, 160);
            cardConnection.Controls.Add(lblWifiTag);

            txtWifiUrl = new TextBox
            {
                Location = new Point(fieldX, 180),
                Size = new Size(244, 24),
                ReadOnly = true,
                BackColor = Color.FromArgb(18, 8, 11),
                ForeColor = Color.FromArgb(56, 189, 248),
                Font = new Font("Segoe UI", 9F),
                Text = "http://127.0.0.1:5000",
                Cursor = Cursors.Hand
            };
            txtWifiUrl.DoubleClick += delegate(object s, EventArgs e) {
                if (!string.IsNullOrEmpty(txtWifiUrl.Text) && txtWifiUrl.Text.StartsWith("http"))
                {
                    OpenUrlWithPortableBrowser(txtWifiUrl.Text);
                }
            };
            cardConnection.Controls.Add(txtWifiUrl);

            btnCopyWifi = CreateSmallCopyButton("📋 Copy LAN", Color.FromArgb(24, 36, 50), Color.FromArgb(56, 189, 248), fieldX + 252, 179, 94, 26);
            btnCopyWifi.Click += delegate(object s, EventArgs e) {
                if (!string.IsNullOrEmpty(txtWifiUrl.Text) && txtWifiUrl.Text.StartsWith("http"))
                {
                    try
                    {
                        Clipboard.SetText(txtWifiUrl.Text);
                        MessageBox.Show("Đã sao chép link Wi-Fi LAN:\n" + txtWifiUrl.Text, "PMT Click", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch { }
                }
            };
            cardConnection.Controls.Add(btnCopyWifi);

            // Row 4: APK Download Link
            Label lblApkTag = CreateFieldLabel("🤖 LINK TẢI TRỰC TIẾP APP ANDROID (.APK):", Color.FromArgb(74, 222, 128), fieldX, 214);
            cardConnection.Controls.Add(lblApkTag);

            txtApkUrl = new TextBox
            {
                Location = new Point(fieldX, 234),
                Size = new Size(244, 24),
                ReadOnly = true,
                BackColor = Color.FromArgb(18, 8, 11),
                ForeColor = Color.FromArgb(74, 222, 128),
                Font = new Font("Segoe UI", 9F),
                Text = "https://www.quiniumthu.qd.je/apk",
                Cursor = Cursors.Hand
            };
            txtApkUrl.DoubleClick += delegate(object s, EventArgs e) {
                if (!string.IsNullOrEmpty(txtApkUrl.Text) && txtApkUrl.Text.StartsWith("http"))
                {
                    OpenUrlWithPortableBrowser(txtApkUrl.Text);
                }
            };
            cardConnection.Controls.Add(txtApkUrl);

            btnCopyApk = CreateSmallCopyButton("📋 Copy APK", Color.FromArgb(20, 42, 28), Color.FromArgb(74, 222, 128), fieldX + 252, 233, 94, 26);
            btnCopyApk.Click += delegate(object s, EventArgs e) {
                if (!string.IsNullOrEmpty(txtApkUrl.Text) && txtApkUrl.Text.StartsWith("http"))
                {
                    try
                    {
                        Clipboard.SetText(txtApkUrl.Text);
                        MessageBox.Show("Đã sao chép link tải APK:\n" + txtApkUrl.Text, "PMT Click", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch { }
                }
            };
            cardConnection.Controls.Add(btnCopyApk);

            // Help Hint
            Label lblHelpHint = new Label
            {
                Text = "💡 Mẹo: Mở camera điện thoại quét mã QR bên cạnh để mở ngay bàn phím & chuột hoặc tải bản APK Native.",
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 135, 130),
                Location = new Point(fieldX, 276),
                Size = new Size(346, 36)
            };
            cardConnection.Controls.Add(lblHelpHint);

            pnlLeft.Controls.Add(cardConnection);
        }

        private void BuildRightColumn()
        {
            // CARD 3: DEDICATED LIVE ACTIVITY LOG TERMINAL
            cardLog = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 7, 10),
                Padding = new Padding(12)
            };
            cardLog.Paint += delegate(object s, PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF r = new RectangleF(1, 1, cardLog.Width - 3, cardLog.Height - 3);
                using (GraphicsPath gp = UITheme.CreateRoundRect(r, 12f))
                using (LinearGradientBrush lgb = new LinearGradientBrush(r, Color.FromArgb(24, 9, 12), Color.FromArgb(14, 5, 7), 90f))
                using (Pen pen = new Pen(Color.FromArgb(120, 42, 48), 1.2f))
                {
                    g.FillPath(lgb, gp);
                    g.DrawPath(pen, gp);
                }
            };

            // Terminal Header Bar
            Panel termHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent
            };

            // Terminal Window Dots (Red, Yellow, Green)
            termHeader.Paint += delegate(object s, PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush bRed = new SolidBrush(Color.FromArgb(239, 68, 68)))
                using (SolidBrush bYel = new SolidBrush(Color.FromArgb(245, 158, 11)))
                using (SolidBrush bGrn = new SolidBrush(Color.FromArgb(34, 197, 94)))
                {
                    g.FillEllipse(bRed, 4, 14, 10, 10);
                    g.FillEllipse(bYel, 20, 14, 10, 10);
                    g.FillEllipse(bGrn, 36, 14, 10, 10);
                }
            };

            Label lblTermTitle = new Label
            {
                Text = "NHẬT KÝ HOẠT ĐỘNG THỜI GIAN THỰC (LIVE ACTIVITY LOG)",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UITheme.NeonAmber,
                Location = new Point(56, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            termHeader.Controls.Add(lblTermTitle);

            // Controls on the right of Terminal Header
            chkShowLog = new CheckBox
            {
                Text = "Log phím & chuột",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(220, 175, 170),
                AutoSize = true,
                Checked = PmtClickManager.ShowLog,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            chkShowLog.CheckedChanged += delegate(object s, EventArgs e) {
                PmtClickManager.ShowLog = chkShowLog.Checked;
            };

            btnClearLog = CreateSmallCopyButton("🗑️ Xóa", Color.FromArgb(46, 18, 22), Color.FromArgb(248, 113, 113), 0, 0, 70, 26);
            btnClearLog.Click += delegate(object s, EventArgs e) {
                if (txtLog != null) txtLog.Clear();
                PmtClickManager.ClearLogs();
            };

            btnCopyLog = CreateSmallCopyButton("📋 Copy", Color.FromArgb(36, 16, 20), Color.FromArgb(251, 191, 36), 0, 0, 75, 26);
            btnCopyLog.Click += delegate(object s, EventArgs e) {
                if (txtLog != null && !string.IsNullOrEmpty(txtLog.Text))
                {
                    try
                    {
                        Clipboard.SetText(txtLog.Text);
                        MessageBox.Show("Đã sao chép toàn bộ nhật ký hoạt động!", "PMT Click", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch { }
                }
            };

            termHeader.Resize += delegate(object s, EventArgs e) {
                int rightMargin = 6;
                btnCopyLog.Location = new Point(termHeader.Width - btnCopyLog.Width - rightMargin, 8);
                btnClearLog.Location = new Point(btnCopyLog.Left - btnClearLog.Width - 6, 8);
                chkShowLog.Location = new Point(btnClearLog.Left - chkShowLog.Width - 10, 12);
            };

            termHeader.Controls.Add(chkShowLog);
            termHeader.Controls.Add(btnClearLog);
            termHeader.Controls.Add(btnCopyLog);

            cardLog.Controls.Add(termHeader);

            // Terminal Screen Container
            Panel termScreen = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 4, 6),
                Padding = new Padding(8)
            };
            termScreen.Paint += delegate(object s, PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF r = new RectangleF(0, 0, termScreen.Width - 1, termScreen.Height - 1);
                using (GraphicsPath gp = UITheme.CreateRoundRect(r, 8f))
                using (Pen p = new Pen(Color.FromArgb(70, 24, 28), 1f))
                {
                    g.DrawPath(p, gp);
                }
            };

            txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(10, 4, 6),
                ForeColor = Color.FromArgb(245, 205, 198),
                Font = new Font("Consolas", 9.25F, FontStyle.Regular)
            };
            termScreen.Controls.Add(txtLog);

            cardLog.Controls.Add(termScreen);
            pnlRight.Controls.Add(cardLog);
        }

        private Label CreateFieldLabel(string title, Color color, int x, int y)
        {
            return new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = color,
                Location = new Point(x, y),
                AutoSize = true,
                BackColor = Color.Transparent
            };
        }

        private Panel CreateCardPanel(int width, int height)
        {
            Panel p = new Panel
            {
                Size = new Size(width, height),
                BackColor = Color.FromArgb(28, 14, 17)
            };
            p.Paint += delegate(object s, PaintEventArgs e) {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF r = new RectangleF(1, 1, p.Width - 3, p.Height - 3);
                using (GraphicsPath gp = UITheme.CreateRoundRect(r, 12f))
                using (LinearGradientBrush lgb = new LinearGradientBrush(r, Color.FromArgb(34, 15, 19), Color.FromArgb(22, 10, 13), 90f))
                using (Pen pen = new Pen(Color.FromArgb(90, 36, 42), 1.2f))
                {
                    g.FillPath(lgb, gp);
                    g.DrawPath(pen, gp);
                }
            };
            return p;
        }

        private void AddCardTitle(Panel p, string title, Color color)
        {
            Label lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = color,
                Location = new Point(18, 15),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            p.Controls.Add(lbl);
        }

        private void OpenUrlWithPortableBrowser(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            string root = !string.IsNullOrEmpty(driveRoot) ? driveRoot.TrimEnd('\\') + "\\" : "E:\\";

            string[] candidateExes = new string[]
            {
                Path.Combine(root, @"Browsers\Chrome\chơi_chrome.bat"),
                Path.Combine(root, @"Browsers\Chrome\Chrome.bat"),
                Path.Combine(root, @"Browsers\Chrome\App\chrome.exe"),
                Path.Combine(root, @"Browsers\CocCoc\chơi_coccoc.bat"),
                Path.Combine(root, @"Browsers\CocCoc\CocCoc.bat"),
                Path.Combine(root, @"Browsers\CocCoc\App\browser.exe"),
                Path.Combine(root, @"Browsers\Edge\chơi_edge.bat"),
                Path.Combine(root, @"Browsers\Edge\App\msedge.exe")
            };

            for (int i = 0; i < candidateExes.Length; i++)
            {
                string path = candidateExes[i];
                if (File.Exists(path))
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        if (path.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
                        {
                            psi.FileName = "cmd.exe";
                            psi.Arguments = "/c \"" + path + "\" \"" + url + "\"";
                            psi.CreateNoWindow = true;
                            psi.WindowStyle = ProcessWindowStyle.Hidden;
                        }
                        else
                        {
                            psi.FileName = path;
                            psi.Arguments = "\"" + url + "\"";
                        }
                        Process.Start(psi);
                        return;
                    }
                    catch { }
                }
            }

            try { Process.Start(url); } catch { }
        }

        private Button CreateButton(string text, Color bg, Color fg, int x, int y, int w, int h)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(140, 50, 40);
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        private Button CreateSmallCopyButton(string text, Color bg, Color fg, int x, int y, int w, int h)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(120, 48, 40);
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        public void UpdateServerStatus()
        {
            bool running = PmtClickManager.IsRunning();
            int port = PmtClickManager.ActivePort;

            if (running)
            {
                if (lblBannerBadge != null)
                {
                    lblBannerBadge.Text = string.Format("● ONLINE (PORT {0})", port);
                    lblBannerBadge.ForeColor = Color.FromArgb(74, 222, 128);
                }

                if (lblServerStatus != null)
                {
                    lblServerStatus.Text = string.Format("● DỊCH VỤ ĐANG HOẠT ĐỘNG (CỔNG {0})\nSẵn sàng nhận kết nối điều khiển từ điện thoại.", port);
                    lblServerStatus.ForeColor = Color.FromArgb(74, 222, 128);
                }

                if (btnToggleServer != null)
                {
                    btnToggleServer.Text = "⏹  DỪNG DỊCH VỤ";
                    btnToggleServer.BackColor = Color.FromArgb(185, 28, 28);
                }

                if (txtDeviceId != null) txtDeviceId.Text = PmtClickManager.FormattedDeviceId;
                if (txtWifiUrl != null) txtWifiUrl.Text = PmtClickManager.WifiUrl;
                if (txtRemoteUrl != null) txtRemoteUrl.Text = PmtClickManager.RemoteUrl;
                if (txtApkUrl != null) txtApkUrl.Text = PmtClickManager.ApkUrl;

                // QR Code rendering - Defaults to Wi-Fi LAN for instant phone connection
                string targetQrUrl = !string.IsNullOrEmpty(PmtClickManager.WifiUrl) ? PmtClickManager.WifiUrl : PmtClickManager.ApkUrl;
                if (!string.IsNullOrEmpty(targetQrUrl) && targetQrUrl.StartsWith("http"))
                {
                    try
                    {
                        Image qrImg = PmtClickManager.GenerateQr(targetQrUrl, 4);
                        if (qrImg != null)
                        {
                            Image old = picQr.Image;
                            picQr.Image = qrImg;
                            if (old != null) old.Dispose();
                        }
                    }
                    catch { }
                }
            }
            else
            {
                if (lblBannerBadge != null)
                {
                    lblBannerBadge.Text = "○ OFFLINE";
                    lblBannerBadge.ForeColor = Color.FromArgb(200, 160, 155);
                }

                if (lblServerStatus != null)
                {
                    lblServerStatus.Text = "○ DỊCH VỤ ĐANG TẠM DỪNG\nBấm nút [KHỞI ĐỘNG DỊCH VỤ] để bắt đầu máy chủ PMT Click.";
                    lblServerStatus.ForeColor = Color.FromArgb(200, 160, 155);
                }

                if (btnToggleServer != null)
                {
                    btnToggleServer.Text = "▶  KHỞI ĐỘNG DỊCH VỤ";
                    btnToggleServer.BackColor = UITheme.NeonFlame;
                }

                if (txtDeviceId != null) txtDeviceId.Text = "--- ---";
                if (txtWifiUrl != null) txtWifiUrl.Text = "http://127.0.0.1:5000";
                if (txtRemoteUrl != null) txtRemoteUrl.Text = "Dịch vụ đang tạm dừng...";
                if (txtApkUrl != null) txtApkUrl.Text = "Dịch vụ đang tạm dừng...";

                if (picQr != null && picQr.Image != null)
                {
                    Image old = picQr.Image;
                    picQr.Image = null;
                    if (old != null) old.Dispose();
                }
            }
        }
    }

    public class EditGameForm : Form
    {
        public GameItem Item { get; private set; }
        private bool isNew;

        private TextBox txtName;
        private ComboBox cboCategory;
        private TextBox txtGenre;
        private TextBox txtExePath;
        private TextBox txtIcon;

        public EditGameForm(GameItem item, bool isNewItem, string driveRoot)
        {
            this.Item = item;
            this.isNew = isNewItem;

            this.Text = isNew ? "Thêm Ứng Dụng Mới Vào Menu" : "Chỉnh Sửa: " + (item.name ?? "Ứng Dụng");
            this.Size = new Size(560, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(24, 11, 13);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            int y = 20;

            Label lblHeader = new Label
            {
                Text = isNew ? "⚡ THÊM GAME / ỨNG DỤNG MỚI" : "✏ CẬP NHẬT THÔNG TIN",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = UITheme.NeonCoral,
                Location = new Point(25, y),
                AutoSize = true
            };
            this.Controls.Add(lblHeader);
            y += 42;

            AddLabel("Tên Hiển Thị:", 25, y);
            txtName = AddTextBox(item.name ?? "", 25, y + 22, 490);
            y += 62;

            AddLabel("Phân Loại:", 25, y);
            cboCategory = new ComboBox
            {
                Location = new Point(25, y + 22),
                Width = 235,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(40, 18, 22),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F)
            };
            cboCategory.Items.AddRange(new object[] { "hot", "offline", "online", "browser", "tool" });
            cboCategory.SelectedItem = string.IsNullOrEmpty(item.category) ? "offline" : item.category.ToLower();
            this.Controls.Add(cboCategory);

            AddLabel("Icon Emoji:", 280, y);
            txtIcon = AddTextBox(item.icon ?? "🎮", 280, y + 22, 235);
            y += 62;

            AddLabel("Thể Loại (VD: Nhập vai / Sinh tồn / FPS):", 25, y);
            txtGenre = AddTextBox(item.genre ?? "Game Hành Động", 25, y + 22, 490);
            y += 62;

            AddLabel("Đường Dẫn File Thực Thi (.exe hoặc .bat):", 25, y);
            txtExePath = AddTextBox(item.exePath ?? "", 25, y + 22, 400);

            Button btnBrowse = new Button
            {
                Text = "Chọn File...",
                Location = new Point(435, y + 20),
                Size = new Size(80, 28),
                BackColor = Color.FromArgb(48, 22, 26),
                ForeColor = UITheme.NeonAmber,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderColor = UITheme.NeonOrange;
            btnBrowse.Click += (s, e) => {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Chương trình (*.exe;*.bat;*.lnk)|*.exe;*.bat;*.lnk|Tất cả files (*.*)|*.*";
                ofd.InitialDirectory = driveRoot;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtExePath.Text = ofd.FileName;
                    if (string.IsNullOrEmpty(txtName.Text))
                    {
                        txtName.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
                    }
                }
            };
            this.Controls.Add(btnBrowse);
            y += 75;

            Button btnSave = new Button
            {
                Text = "✔ Lưu Vào Menu",
                Location = new Point(140, y),
                Size = new Size(130, 38),
                BackColor = UITheme.NeonFlame,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => {
                if (string.IsNullOrEmpty(txtExePath.Text.Trim()))
                {
                    MessageBox.Show("Vui lòng nhập đường dẫn file thực thi!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                item.name = txtName.Text.Trim();
                item.category = cboCategory.SelectedItem != null ? cboCategory.SelectedItem.ToString() : "offline";
                item.genre = txtGenre.Text.Trim();
                item.exePath = txtExePath.Text.Trim();
                item.icon = string.IsNullOrEmpty(txtIcon.Text.Trim()) ? "🎮" : txtIcon.Text.Trim();
                if (string.IsNullOrEmpty(item.id))
                {
                    item.id = Guid.NewGuid().ToString("N");
                    item.rating = 5.0;
                    item.playCount = 100;
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            Button btnCancel = new Button
            {
                Text = "Hủy Bỏ",
                Location = new Point(290, y),
                Size = new Size(110, 38),
                BackColor = Color.FromArgb(42, 18, 20),
                ForeColor = Color.FromArgb(200, 160, 150),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(70, 30, 32);
            btnCancel.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
        }

        private void AddLabel(string text, int x, int y)
        {
            Label lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(lbl);
        }

        private TextBox AddTextBox(string text, int x, int y, int width)
        {
            TextBox tb = new TextBox
            {
                Text = text,
                Location = new Point(x, y),
                Width = width,
                BackColor = Color.FromArgb(36, 16, 18),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F)
            };
            this.Controls.Add(tb);
            return tb;
        }
    }

    public class DestinyKeyVerificationForm : Form
    {
        private enum ViewState
        {
            Searching,
            Found,
            NotFound
        }

        private ViewState currentState = ViewState.Searching;
        private int remainingSeconds = 30;
        private System.Windows.Forms.Timer countdownTimer;
        private System.Windows.Forms.Timer scanTimer;
        private System.Windows.Forms.Timer pulseIconTimer;
        private float heartPulseScale = 1.0f;
        private bool heartPulseGrowing = true;

        private Label lblIcon;
        private Label lblTitle;
        private Label lblMessage;
        private Label lblCountdown;
        private Panel progressBarBg;
        private Panel progressBarFill;
        private Button btnAction;
        private const int TotalSeconds = 30;

        public DestinyKeyVerificationForm(bool isRuntimeReplug = false)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(540, 370);
            this.BackColor = Color.FromArgb(18, 12, 18);
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.Opacity = 0.0f;

            InitUI();
            SetState(ViewState.Searching);

            // Giữ nhịp đập tìm kiếm 1.2 giây để người dùng trải nghiệm hiệu ứng tìm kiếm
            System.Windows.Forms.Timer initialSearchTimer = new System.Windows.Forms.Timer { Interval = 1200 };
            initialSearchTimer.Tick += (s, e) => {
                initialSearchTimer.Stop();
                initialSearchTimer.Dispose();
                string foundRoot;
                if (MainForm.FindPortableDriveWithTag(out foundRoot))
                {
                    SetState(ViewState.Found);
                }
                else
                {
                    StartContinuousScan();
                }
            };
            initialSearchTimer.Start();

            // Hiệu ứng Fade-in mờ dần hiện lên khi mở modal
            this.Shown += (s, e) => {
                System.Windows.Forms.Timer fadeIn = new System.Windows.Forms.Timer { Interval = 15 };
                fadeIn.Tick += (s2, e2) => {
                    if (this.Opacity < 0.98f)
                    {
                        this.Opacity += 0.08f;
                    }
                    else
                    {
                        this.Opacity = 1.0f;
                        fadeIn.Stop();
                        fadeIn.Dispose();
                    }
                };
                fadeIn.Start();
            };
        }

        private void InitUI()
        {
            lblIcon = new Label
            {
                Text = "💖",
                Font = new Font("Segoe UI Emoji", 38F),
                ForeColor = UITheme.NeonCoral,
                Size = new Size(120, 75),
                Location = new Point((this.Width - 120) / 2, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            lblTitle = new Label
            {
                Text = "ĐANG TÌM KHOÁ ĐỊNH MỆNH...",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = UITheme.NeonCoral,
                Size = new Size(500, 32),
                Location = new Point(20, 98),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            lblMessage = new Label
            {
                Text = "Đang kết nối trái tim và quét tìm khoá định mệnh [anhyeuempmt.tag] trên các ổ đĩa...",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(240, 205, 215),
                Size = new Size(480, 52),
                Location = new Point(30, 134),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            lblCountdown = new Label
            {
                Text = "30s",
                Font = new Font("Segoe UI", 32F, FontStyle.Bold),
                ForeColor = UITheme.NeonAmber,
                Size = new Size(200, 55),
                Location = new Point((this.Width - 200) / 2, 185),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            progressBarBg = new Panel
            {
                Size = new Size(460, 6),
                Location = new Point(40, 250),
                BackColor = Color.FromArgb(44, 20, 28)
            };

            progressBarFill = new Panel
            {
                Size = new Size(460, 6),
                Location = new Point(0, 0),
                BackColor = UITheme.NeonAmber
            };
            progressBarBg.Controls.Add(progressBarFill);

            btnAction = new Button
            {
                Text = "Hủy & Thoát",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(254, 215, 226),
                BackColor = Color.FromArgb(46, 18, 24),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(220, 42),
                Location = new Point((this.Width - 220) / 2, 280),
                Cursor = Cursors.Hand
            };
            btnAction.FlatAppearance.BorderColor = Color.FromArgb(120, 44, 52);
            btnAction.Click += BtnAction_Click;

            this.Controls.Add(lblIcon);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblMessage);
            this.Controls.Add(lblCountdown);
            this.Controls.Add(progressBarBg);
            this.Controls.Add(btnAction);

            // Hiệu ứng nhịp đập trái tim đang tìm kiếm
            pulseIconTimer = new System.Windows.Forms.Timer { Interval = 75 };
            pulseIconTimer.Tick += (s, e) => {
                if (currentState == ViewState.Searching)
                {
                    if (heartPulseGrowing)
                    {
                        heartPulseScale += 0.035f;
                        if (heartPulseScale >= 1.18f) heartPulseGrowing = false;
                    }
                    else
                    {
                        heartPulseScale -= 0.035f;
                        if (heartPulseScale <= 0.92f) heartPulseGrowing = true;
                    }
                    float baseSize = 38f * heartPulseScale;
                    lblIcon.Font = new Font("Segoe UI Emoji", baseSize);
                }
            };
            pulseIconTimer.Start();
        }

        private void StartContinuousScan()
        {
            if (currentState != ViewState.Searching) return;
            countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            countdownTimer.Tick += CountdownTimer_Tick;
            countdownTimer.Start();

            scanTimer = new System.Windows.Forms.Timer { Interval = 350 };
            scanTimer.Tick += ScanTimer_Tick;
            scanTimer.Start();
        }

        private void SetState(ViewState state)
        {
            currentState = state;
            if (state == ViewState.Found)
            {
                StopTimers();
                lblIcon.Font = new Font("Segoe UI Emoji", 44F);
                lblIcon.Text = "🥰";
                lblTitle.Text = "ĐÃ TÌM THẤY KHOÁ ĐỊNH MỆNH! 💕";
                lblTitle.ForeColor = Color.FromArgb(255, 120, 160);

                lblMessage.Text = "Đã tìm thấy khoá định mệnh rồi 💕\nChúc bạn sử dụng vui vẻ bên Minh Thư! 🌸✨";
                lblMessage.ForeColor = Color.FromArgb(255, 230, 240);
                lblMessage.Location = new Point(30, 145);
                lblMessage.Size = new Size(480, 75);

                lblCountdown.Visible = false;
                progressBarBg.Visible = false;

                btnAction.Text = "OK 💕";
                btnAction.BackColor = Color.FromArgb(70, 22, 42);
                btnAction.FlatAppearance.BorderColor = Color.FromArgb(245, 95, 140);
                btnAction.ForeColor = Color.FromArgb(255, 235, 245);
                btnAction.Size = new Size(220, 44);
                btnAction.Location = new Point((this.Width - 220) / 2, 265);
            }
            else if (state == ViewState.NotFound)
            {
                StopTimers();
                lblIcon.Font = new Font("Segoe UI Emoji", 44F);
                lblIcon.Text = "😢";
                lblTitle.Text = "KHÔNG TÌM THẤY KHOÁ ĐỊNH MỆNH...";
                lblTitle.ForeColor = Color.FromArgb(248, 113, 113);

                lblMessage.Text = "Không tìm thấy khoá định mệnh [anhyeuempmt.tag]... 💔\nỨng dụng sẽ tự huỷ và dọn sạch dấu vết trên máy tính để bảo mật.";
                lblMessage.ForeColor = Color.FromArgb(254, 202, 202);
                lblMessage.Location = new Point(30, 145);
                lblMessage.Size = new Size(480, 75);

                lblCountdown.Visible = false;
                progressBarBg.Visible = false;

                btnAction.Text = "OK 😢";
                btnAction.BackColor = Color.FromArgb(50, 18, 22);
                btnAction.FlatAppearance.BorderColor = Color.FromArgb(180, 40, 50);
                btnAction.ForeColor = Color.FromArgb(254, 205, 215);
                btnAction.Size = new Size(220, 44);
                btnAction.Location = new Point((this.Width - 220) / 2, 265);
            }
            else // Searching
            {
                lblIcon.Text = "💖";
                lblTitle.Text = "ĐANG TÌM KHOÁ ĐỊNH MỆNH...";
                lblTitle.ForeColor = UITheme.NeonCoral;
                lblMessage.Text = "Đang kết nối trái tim và quét tìm khoá định mệnh [anhyeuempmt.tag] trên các ổ đĩa...";
                lblMessage.ForeColor = Color.FromArgb(240, 205, 215);
                lblMessage.Location = new Point(30, 134);
                lblMessage.Size = new Size(480, 52);

                lblCountdown.Visible = true;
                progressBarBg.Visible = true;

                btnAction.Text = "Hủy & Thoát";
                btnAction.BackColor = Color.FromArgb(46, 18, 24);
                btnAction.FlatAppearance.BorderColor = Color.FromArgb(120, 44, 52);
                btnAction.Size = new Size(200, 40);
                btnAction.Location = new Point((this.Width - 200) / 2, 285);

                remainingSeconds = TotalSeconds;
            }

            this.Invalidate();
        }

        private void StopTimers()
        {
            if (countdownTimer != null) { countdownTimer.Stop(); countdownTimer.Dispose(); countdownTimer = null; }
            if (scanTimer != null) { scanTimer.Stop(); scanTimer.Dispose(); scanTimer = null; }
        }

        private void BtnAction_Click(object sender, EventArgs e)
        {
            if (currentState == ViewState.Found)
            {
                StartFadeOutAndClose(DialogResult.OK);
            }
            else if (currentState == ViewState.NotFound)
            {
                StartFadeOutAndClose(DialogResult.Abort);
            }
            else // Searching: user pressed "Hủy & Thoát"
            {
                SetState(ViewState.NotFound);
            }
        }

        private void StartFadeOutAndClose(DialogResult result)
        {
            btnAction.Enabled = false;
            if (pulseIconTimer != null) { pulseIconTimer.Stop(); pulseIconTimer.Dispose(); pulseIconTimer = null; }
            System.Windows.Forms.Timer fadeTimer = new System.Windows.Forms.Timer { Interval = 15 };
            fadeTimer.Tick += (s, e) => {
                if (this.Opacity > 0.05f)
                {
                    this.Opacity -= 0.06f;
                }
                else
                {
                    fadeTimer.Stop();
                    fadeTimer.Dispose();
                    this.DialogResult = result;
                    this.Close();
                }
            };
            fadeTimer.Start();
        }

        private void ScanTimer_Tick(object sender, EventArgs e)
        {
            string foundRoot;
            if (MainForm.FindPortableDriveWithTag(out foundRoot))
            {
                SetState(ViewState.Found);
            }
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            remainingSeconds--;
            if (remainingSeconds < 0) remainingSeconds = 0;

            lblCountdown.Text = remainingSeconds + "s";

            float pct = (float)remainingSeconds / TotalSeconds;
            int newWidth = (int)(460 * pct);
            if (newWidth < 0) newWidth = 0;
            progressBarFill.Width = newWidth;

            if (remainingSeconds <= 10)
            {
                lblCountdown.ForeColor = UITheme.NeonCoral;
                progressBarFill.BackColor = UITheme.NeonCoral;
            }

            if (remainingSeconds <= 0)
            {
                SetState(ViewState.NotFound);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color borderColor = Color.FromArgb(180, 255, 75, 95);
            if (currentState == ViewState.Found)
                borderColor = Color.FromArgb(200, 255, 120, 160);
            else if (currentState == ViewState.NotFound)
                borderColor = Color.FromArgb(200, 248, 113, 113);

            using (Pen borderPen = new Pen(borderColor, 2f))
            {
                g.DrawRectangle(borderPen, 1, 1, this.Width - 2, this.Height - 2);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopTimers();
                if (pulseIconTimer != null) { pulseIconTimer.Dispose(); pulseIconTimer = null; }
            }
            base.Dispose(disposing);
        }
    }

    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        private string appDir;
        private string driveRoot;
        private string portableDataDir;
        private List<GameItem> allGames = new List<GameItem>();
        private string currentCategory = "all";
        private JavaScriptSerializer serializer = new JavaScriptSerializer();
        private HashSet<string> selectedGameIds = new HashSet<string>();
        private List<GameItem> currentFilteredGames = new List<GameItem>();

        private Panel cardsContainer;
        private DoubleBufferedFlowLayoutPanel flowPanel;
        private NeonScrollBar neonScrollBar;
        private PmtClickHubPanel pmtHubPanel;

        private TextBox searchBox;
        private Label lblCount;
        private Label lblStatus;
        private HeaderActionButton btnScan;
        private HeaderActionButton btnAddApp;
        private HeaderActionButton btnSelectAll;
        private HeaderActionButton btnBatchDelete;
        private List<NeonPillButton> categoryButtons = new List<NeonPillButton>();
        private System.Windows.Forms.Timer pulseTimer;
        private bool ledState = true;
        private string driveSpaceInfo = "";
        private Image appLogo = null;

        public MainForm()
        {
            ResolvePortableRoots();
            LoadAppIcon();

            this.Text = "QuinGM luv Mthu Menu (PORTABLE DESKTOP APP)";
            this.Size = new Size(1280, 830);
            this.MinimumSize = new Size(980, 620);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = UITheme.BgMain;
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.Opacity = 0.0;

            // Hiệu ứng Fade-in mượt mà khi mở Menu chính
            this.Shown += (s, e) => {
                System.Windows.Forms.Timer fadeInTimer = new System.Windows.Forms.Timer { Interval = 15 };
                fadeInTimer.Tick += (s2, e2) => {
                    if (this.Opacity < 0.98)
                    {
                        this.Opacity += 0.08;
                    }
                    else
                    {
                        this.Opacity = 1.0;
                        fadeInTimer.Stop();
                        fadeInTimer.Dispose();
                    }
                };
                fadeInTimer.Start();
            };

            this.Deactivate += (s, e) => NeonToolTipPopup.Instance.HidePopup();
            this.Move += (s, e) => NeonToolTipPopup.Instance.HidePopup();

            this.FormClosed += (s, e) => {
                Environment.Exit(0);
            };

            UpdateDriveInfo();
            InitUI();
            LoadGamesData();

            // Auto-start PMT Click server so user does not need to manually press start
            ThreadPool.QueueUserWorkItem(delegate(object state) {
                try {
                    Thread.Sleep(300);
                    PmtClickManager.Start(driveRoot);
                } catch { }
            });

            pulseTimer = new System.Windows.Forms.Timer();
            pulseTimer.Interval = 900;
            pulseTimer.Tick += (s, e) => {
                ledState = !ledState;
                string dot = ledState ? "●" : "○";
                if (PmtClickManager.IsRunning())
                {
                    lblStatus.Text = dot + " PMT CLICK: ONLINE (Cổng 5000) • Ổ " + driveRoot + driveSpaceInfo;
                    lblStatus.ForeColor = Color.FromArgb(74, 222, 128);
                }
                else
                {
                    lblStatus.Text = dot + " CHẾ ĐỘ PORTABLE: Ổ " + driveRoot + driveSpaceInfo + " (Dữ liệu cô lập 100% trên ổ di động)";
                    lblStatus.ForeColor = UITheme.NeonAmber;
                }

                // Kiểm tra liên tục: nếu ổ di động bị rút ra đột ngột
                if (!IsMyPortableDrive(driveRoot))
                {
                    pulseTimer.Stop();
                    using (DestinyKeyVerificationForm countdownForm = new DestinyKeyVerificationForm(true))
                    {
                        if (countdownForm.ShowDialog() == DialogResult.OK)
                        {
                            string newRoot;
                            if (FindPortableDriveWithTag(out newRoot)) driveRoot = newRoot;
                            pulseTimer.Start();
                        }
                        else
                        {
                            ExecuteSelfDestruct();
                            return;
                        }
                    }
                }
            };
            pulseTimer.Start();
        }

        private void LoadAppIcon()
        {
            try
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (Stream stream = asm.GetManifestResourceStream("app.ico"))
                {
                    if (stream != null)
                    {
                        this.Icon = new Icon(stream);
                    }
                }
                using (Stream logoStream = asm.GetManifestResourceStream("logo.png"))
                {
                    if (logoStream != null)
                    {
                        appLogo = Image.FromStream(logoStream);
                    }
                }
            }
            catch { }
        }

        private void UpdateDriveInfo()
        {
            try
            {
                DriveInfo di = new DriveInfo(driveRoot);
                if (di.IsReady)
                {
                    double freeGb = di.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    double totalGb = di.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    driveSpaceInfo = string.Format(" [Trống {0:0.0} GB / {1:0.0} GB]", freeGb, totalGb);
                }
            }
            catch { }
        }

        private void InitUI()
        {
            // 1. Header
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = UITheme.BgHeader,
                Padding = new Padding(18, 8, 18, 8)
            };

            PictureBox picLogo = new PictureBox
            {
                Size = new Size(42, 42),
                Location = new Point(18, 16),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            if (appLogo != null)
            {
                picLogo.Image = appLogo;
            }
            else
            {
                picLogo.Visible = false;
            }

            int titleX = picLogo.Visible ? 68 : 20;

            Label lblTitle = new Label
            {
                Text = "⚡ QuinGM luv Mthu Menu",
                Font = new Font("Segoe UI", 15.5F, FontStyle.Bold),
                ForeColor = UITheme.NeonCoral,
                AutoSize = true,
                Location = new Point(titleX, 13)
            };

            lblStatus = new Label
            {
                Text = "● CHẾ ĐỘ PORTABLE: Ổ " + driveRoot + driveSpaceInfo + " (Dữ liệu cô lập 100% trên ổ di động - Không ghi ổ C)",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UITheme.NeonAmber,
                AutoSize = true,
                Location = new Point(titleX + 2, 45)
            };

            header.Controls.Add(picLogo);
            header.Controls.Add(lblTitle);
            header.Controls.Add(lblStatus);

            FlowLayoutPanel headerRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 19, 14, 0),
                WrapContents = false
            };

            btnSelectAll = new HeaderActionButton
            {
                IconSymbol = "☑",
                ButtonText = "Chọn Tất Cả",
                Width = 124,
                Height = 36,
                NormalBg1 = Color.FromArgb(44, 18, 24),
                NormalBg2 = Color.FromArgb(28, 10, 15),
                HoverBg1 = Color.FromArgb(68, 26, 34),
                HoverBg2 = Color.FromArgb(42, 16, 22),
                NormalBorder = Color.FromArgb(100, 44, 52),
                HoverBorder = UITheme.NeonAmber,
                ForeColor = Color.FromArgb(254, 215, 226),
                Margin = new Padding(0, 0, 8, 0)
            };
            btnSelectAll.Click += (s, e) => ToggleSelectAll();

            btnBatchDelete = new HeaderActionButton
            {
                IconSymbol = "🗑️",
                ButtonText = "Xóa Hàng Loạt",
                Width = 142,
                Height = 36,
                NormalBg1 = Color.FromArgb(36, 16, 20),
                NormalBg2 = Color.FromArgb(24, 10, 14),
                HoverBg1 = Color.FromArgb(52, 22, 28),
                HoverBg2 = Color.FromArgb(34, 14, 18),
                NormalBorder = Color.FromArgb(80, 36, 42),
                HoverBorder = Color.FromArgb(140, 55, 65),
                ForeColor = Color.FromArgb(160, 110, 115),
                Margin = new Padding(0, 0, 8, 0)
            };
            btnBatchDelete.Click += (s, e) => BatchDeleteSelected();

            btnScan = new HeaderActionButton
            {
                IconSymbol = "⚡",
                ButtonText = "Quét Ổ Cứng",
                Width = 126,
                Height = 36,
                NormalBg1 = Color.FromArgb(46, 20, 24),
                NormalBg2 = Color.FromArgb(28, 12, 14),
                HoverBg1 = Color.FromArgb(68, 28, 34),
                HoverBg2 = Color.FromArgb(44, 18, 22),
                NormalBorder = Color.FromArgb(160, 60, 32),
                HoverBorder = UITheme.NeonAmber,
                ForeColor = UITheme.NeonAmber,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnScan.Click += (s, e) => TriggerScan();

            btnAddApp = new HeaderActionButton
            {
                IconSymbol = "＋",
                ButtonText = "Thêm Ứng Dụng",
                Width = 140,
                Height = 36,
                NormalBg1 = Color.FromArgb(50, 18, 22),
                NormalBg2 = Color.FromArgb(32, 10, 14),
                HoverBg1 = Color.FromArgb(76, 26, 32),
                HoverBg2 = Color.FromArgb(48, 16, 20),
                NormalBorder = Color.FromArgb(200, 36, 46),
                HoverBorder = Color.FromArgb(248, 113, 113),
                ForeColor = Color.FromArgb(254, 205, 211),
                Margin = new Padding(0, 0, 8, 0)
            };
            btnAddApp.Click += (s, e) => OpenAddDialog();

            Panel searchContainer = new Panel
            {
                Width = 215,
                Height = 36,
                BackColor = Color.FromArgb(32, 15, 17),
                Padding = new Padding(8, 7, 8, 7),
                Margin = new Padding(0, 0, 0, 0)
            };
            searchContainer.Paint += (s, e) => {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF sr = new RectangleF(1, 1, searchContainer.Width - 3, searchContainer.Height - 3);
                using (GraphicsPath gp = UITheme.CreateRoundRect(sr, 9f))
                using (Pen p = new Pen(Color.FromArgb(90, 42, 46), 1.2f))
                {
                    g.DrawPath(p, gp);
                }
            };

            Label searchIcon = new Label
            {
                Text = "🔍",
                AutoSize = true,
                ForeColor = UITheme.TextMuted,
                Font = new Font("Segoe UI", 9.5F),
                Location = new Point(6, 8)
            };

            searchBox = new TextBox
            {
                Width = 175,
                Location = new Point(30, 8),
                BackColor = Color.FromArgb(32, 15, 17),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F)
            };
            searchBox.TextChanged += (s, e) => FilterGames();

            searchContainer.Controls.Add(searchIcon);
            searchContainer.Controls.Add(searchBox);

            HeaderActionButton btnInstallDesktop = new HeaderActionButton
            {
                IconSymbol = "📌",
                ButtonText = "Cài Ra Desktop",
                Width = 142,
                Height = 36,
                NormalBg1 = Color.FromArgb(40, 20, 48),
                NormalBg2 = Color.FromArgb(26, 12, 32),
                HoverBg1 = Color.FromArgb(64, 30, 78),
                HoverBg2 = Color.FromArgb(42, 18, 52),
                NormalBorder = Color.FromArgb(130, 60, 180),
                HoverBorder = UITheme.NeonAmber,
                ForeColor = Color.FromArgb(235, 215, 255),
                Margin = new Padding(0, 0, 8, 0)
            };
            btnInstallDesktop.Click += (s, e) => InstallToDesktop();

            headerRight.Controls.Add(btnSelectAll);
            headerRight.Controls.Add(btnBatchDelete);
            headerRight.Controls.Add(btnScan);
            headerRight.Controls.Add(btnAddApp);
            headerRight.Controls.Add(btnInstallDesktop);
            headerRight.Controls.Add(searchContainer);
            header.Controls.Add(headerRight);

            // 2. Filter Bar
            Panel filterBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = UITheme.BgFilterBar,
                Padding = new Padding(20, 8, 20, 8)
            };

            string[] catKeys = new string[] { "all", "hot", "offline", "online", "browser", "tool", "pmt_click" };
            string[] catNames = new string[] { "⭐ Tất Cả", "🔥 Game Hot", "🎮 Game Offline", "⚡ Game Online", "🌐 Trình Duyệt", "🛠️ Tiện Ích", "📱 PMT Click" };

            int leftPos = 20;
            for (int i = 0; i < catKeys.Length; i++)
            {
                int btnW = (i == catKeys.Length - 1) ? 140 : 138;
                NeonPillButton catBtn = new NeonPillButton
                {
                    Text = catNames[i],
                    Tag = catKeys[i],
                    Width = btnW,
                    Height = 35,
                    Location = new Point(leftPos, 8),
                    CornerRadius = 14f,
                    NormalBg = Color.FromArgb(36, 16, 18),
                    HoverBg = Color.FromArgb(54, 24, 28),
                    ActiveBg = (i == catKeys.Length - 1) ? Color.FromArgb(192, 38, 100) : UITheme.NeonFlame,
                    NormalBorder = Color.FromArgb(64, 28, 32),
                    HoverBorder = (i == catKeys.Length - 1) ? Color.FromArgb(244, 114, 182) : UITheme.NeonOrange,
                    ActiveBorder = (i == catKeys.Length - 1) ? Color.FromArgb(244, 114, 182) : UITheme.BorderGlow,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
                    IsActive = (i == 0)
                };

                string currentKey = catKeys[i];
                catBtn.Click += (s, e) => SelectCategory(currentKey, (NeonPillButton)s);
                filterBar.Controls.Add(catBtn);
                categoryButtons.Add(catBtn);
                leftPos += btnW + 8;
            }

            lblCount = new Label
            {
                Text = "Đang tải...",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UITheme.NeonAmber,
                AutoSize = true,
                Dock = DockStyle.Right,
                Padding = new Padding(0, 15, 14, 0)
            };
            filterBar.Controls.Add(lblCount);

            // 3. Body Container with Sleek Neon ScrollBar
            cardsContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.BgMain
            };

            neonScrollBar = new NeonScrollBar
            {
                Dock = DockStyle.Right,
                Width = 10,
                Visible = false
            };
            neonScrollBar.ValueChanged += (s, e) => {
                flowPanel.Top = -neonScrollBar.Value;
            };

            flowPanel = new DoubleBufferedFlowLayoutPanel
            {
                Dock = DockStyle.None,
                AutoScroll = false, // Hides native WinForms gray scrollbar forever!
                BackColor = UITheme.BgMain,
                Padding = new Padding(18, 12, 18, 20),
                Location = new Point(0, 0),
                Size = new Size(cardsContainer.Width - 10, cardsContainer.Height)
            };

            cardsContainer.Resize += (s, e) => UpdateScrollLayout();
            cardsContainer.MouseWheel += (s, e) => HandleWheel(e);
            flowPanel.MouseWheel += (s, e) => HandleWheel(e);

            cardsContainer.Controls.Add(neonScrollBar);
            cardsContainer.Controls.Add(flowPanel);

            // 4. PMT Click Integrated Hub Panel
            pmtHubPanel = new PmtClickHubPanel(driveRoot)
            {
                Visible = false
            };

            this.Controls.Add(cardsContainer);
            this.Controls.Add(pmtHubPanel);
            this.Controls.Add(filterBar);
            this.Controls.Add(header);
        }

        private void HandleWheel(MouseEventArgs e)
        {
            if (neonScrollBar.Visible)
            {
                int scrollStep = 55;
                int delta = (e.Delta > 0) ? -scrollStep : scrollStep;
                neonScrollBar.Value += delta;
            }
        }

        private void AdjustCardsLayout()
        {
            if (cardsContainer == null || flowPanel == null) return;

            int scrollW = (neonScrollBar != null && neonScrollBar.Visible) ? neonScrollBar.Width : 0;
            int containerW = cardsContainer.ClientSize.Width - scrollW;
            flowPanel.Width = containerW;

            int availW = containerW - flowPanel.Padding.Horizontal;
            if (availW <= 100) return;

            int cardMargin = 8;
            int minCardW = 280;
            int cols = Math.Max(1, availW / (minCardW + cardMargin * 2));
            int cardW = (availW / cols) - (cardMargin * 2);
            if (cardW < 240) cardW = 240;

            flowPanel.SuspendLayout();
            foreach (Control c in flowPanel.Controls)
            {
                GameCardPanel card = c as GameCardPanel;
                if (card != null)
                {
                    if (card.Width != cardW || card.Margin.Left != cardMargin)
                    {
                        card.Margin = new Padding(cardMargin, 10, cardMargin, 10);
                        card.Width = cardW;
                    }
                }
            }
            flowPanel.ResumeLayout(true);
        }

        private void UpdateScrollLayout()
        {
            if (cardsContainer == null || flowPanel == null || neonScrollBar == null) return;

            AdjustCardsLayout();

            int totalH = 0;
            foreach (Control c in flowPanel.Controls)
            {
                if (c.Bottom + c.Margin.Bottom > totalH)
                {
                    totalH = c.Bottom + c.Margin.Bottom;
                }
            }
            totalH += flowPanel.Padding.Bottom + 20;
            flowPanel.Height = Math.Max(cardsContainer.Height, totalH);

            int maxScroll = Math.Max(0, totalH - cardsContainer.Height);
            if (maxScroll > 0)
            {
                neonScrollBar.Visible = true;
                neonScrollBar.Maximum = maxScroll;
                neonScrollBar.LargeChange = cardsContainer.Height / 2;
            }
            else
            {
                neonScrollBar.Visible = false;
                neonScrollBar.Value = 0;
            }

            flowPanel.Top = -neonScrollBar.Value;
        }

        private void SwitchToCategory(string cat)
        {
            foreach (var b in categoryButtons)
            {
                string tag = b.Tag != null ? b.Tag.ToString() : "";
                if (tag == cat)
                {
                    SelectCategory(cat, b);
                    break;
                }
            }
        }

        private void SelectCategory(string cat, NeonPillButton activeBtn)
        {
            currentCategory = cat;
            foreach (var b in categoryButtons)
            {
                b.IsActive = (b == activeBtn);
            }

            if (cat == "pmt_click")
            {
                cardsContainer.Visible = false;
                pmtHubPanel.Visible = true;
                pmtHubPanel.BringToFront();
                pmtHubPanel.Focus();
                pmtHubPanel.UpdateServerStatus();
                lblCount.Text = "📱 Chức năng tích hợp";

                if (btnSelectAll != null) btnSelectAll.Visible = false;
                if (btnBatchDelete != null) btnBatchDelete.Visible = false;
                if (btnScan != null) btnScan.Visible = false;
                if (btnAddApp != null) btnAddApp.Visible = false;
            }
            else
            {
                pmtHubPanel.Visible = false;
                cardsContainer.Visible = true;
                cardsContainer.BringToFront();

                if (btnSelectAll != null) btnSelectAll.Visible = true;
                if (btnBatchDelete != null) btnBatchDelete.Visible = true;
                if (btnScan != null) btnScan.Visible = true;
                if (btnAddApp != null) btnAddApp.Visible = true;

                FilterGames();
            }
        }

        private void UpdateCategoryBadges()
        {
            foreach (var btn in categoryButtons)
            {
                string tag = btn.Tag != null ? btn.Tag.ToString() : "all";
                if (tag == "pmt_click")
                {
                    btn.BadgeText = PmtClickManager.IsRunning() ? "(ON)" : "(OFF)";
                    btn.Invalidate();
                    continue;
                }

                int count = 0;
                foreach (var g in allGames)
                {
                    if (tag == "all") count++;
                    else if (tag == "hot" && (g.isHot || string.Equals(g.category, "hot", StringComparison.OrdinalIgnoreCase))) count++;
                    else if (string.Equals(g.category, tag, StringComparison.OrdinalIgnoreCase)) count++;
                }
                btn.BadgeText = "(" + count + ")";
                btn.Invalidate();
            }
        }

        public static bool IsMyPortableDrive(string root)
        {
            if (string.IsNullOrEmpty(root)) return false;
            try
            {
                // Chữ ký định danh độc quyền BẮT BUỘC: anhyeuempmt.tag
                string[] checkPaths = new string[]
                {
                    Path.Combine(root, "anhyeuempmt.tag"),
                    Path.Combine(root, "QuinGM", "anhyeuempmt.tag"),
                    Path.Combine(Path.GetPathRoot(root), "anhyeuempmt.tag")
                };

                foreach (string loveTag in checkPaths)
                {
                    if (File.Exists(loveTag))
                    {
                        try
                        {
                            string content = File.ReadAllText(loveTag, Encoding.UTF8);
                            if (content.IndexOf("Anh_Yeu_Em_Pham_Minh_Thu_Ksenia_Rin_Luv_U", StringComparison.OrdinalIgnoreCase) >= 0)
                                return true;
                        }
                        catch { }
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static bool FindPortableDriveWithTag(out string foundRoot)
        {
            foundRoot = null;
            try
            {
                string appDir = Path.GetDirectoryName(Application.ExecutablePath);
                if (IsMyPortableDrive(appDir))
                {
                    foundRoot = Path.GetPathRoot(appDir);
                    return true;
                }

                string currentRoot = Path.GetPathRoot(appDir);
                if (!string.IsNullOrEmpty(currentRoot) && IsMyPortableDrive(currentRoot))
                {
                    foundRoot = currentRoot;
                    return true;
                }

                string[] preferredDrives = new string[] { "E:\\", "D:\\", "F:\\", "G:\\", "H:\\", "I:\\", "J:\\" };
                foreach (string root in preferredDrives)
                {
                    if (IsMyPortableDrive(root))
                    {
                        foundRoot = root;
                        return true;
                    }
                }

                foreach (DriveInfo d in DriveInfo.GetDrives())
                {
                    if (!d.IsReady) continue;
                    string root = d.RootDirectory.FullName;
                    if (string.Equals(root, currentRoot, StringComparison.OrdinalIgnoreCase)) continue;

                    if (IsMyPortableDrive(root))
                    {
                        foundRoot = root;
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private void ResolvePortableRoots()
        {
            string exePath = Application.ExecutablePath;
            appDir = Path.GetDirectoryName(exePath);
            if (string.IsNullOrEmpty(appDir)) appDir = "E:\\";

            if (appDir.EndsWith("bin\\", StringComparison.OrdinalIgnoreCase) || appDir.EndsWith("bin/", StringComparison.OrdinalIgnoreCase))
            {
                string parent = Directory.GetParent(appDir.TrimEnd('\\', '/')).FullName;
                if (File.Exists(Path.Combine(parent, "games.json")))
                {
                    appDir = parent;
                }
            }

            string foundRoot;
            if (FindPortableDriveWithTag(out foundRoot))
            {
                driveRoot = foundRoot;
                appDir = foundRoot;
            }
            else
            {
                driveRoot = Path.GetPathRoot(appDir);
                if (string.IsNullOrEmpty(driveRoot)) driveRoot = "E:\\";
            }

            portableDataDir = Path.Combine(driveRoot, "data");
        }

        public void InstallToDesktop()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (string.IsNullOrEmpty(desktopPath) || !Directory.Exists(desktopPath))
                {
                    desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");
                }

                string exePath = Application.ExecutablePath;
                if (!string.IsNullOrEmpty(driveRoot))
                {
                    string rootExe = Path.Combine(driveRoot, "QuinGM luv Mthu Menu.exe");
                    if (File.Exists(rootExe)) exePath = rootExe;
                }

                string shortcutPath = Path.Combine(desktopPath, "QuinGM luv Mthu Menu.lnk");

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = exePath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                    shortcut.IconLocation = exePath + ",0";
                    shortcut.Description = "QuinGM luv Mthu Menu (Portable Gaming & PMT Click)";
                    shortcut.Save();
                }

                MessageBox.Show(
                    "ĐÃ CÀI ĐẶT RA DESKTOP THÀNH CÔNG!\n\n" +
                    "• Lối tắt: " + shortcutPath + "\n" +
                    "• Ổ đĩa di động: " + driveRoot + "\n\n" +
                    "Bạn có thể mở game trực tiếp từ màn hình Desktop bất cứ lúc nào.\n" +
                    "Lưu ý: Luôn cắm ổ cứng di động chứa thẻ [anhyeuempmt.tag] khi sử dụng!",
                    "Cài Đặt Thành Công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tạo lối tắt Desktop: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void ExecuteSelfDestruct()
        {
            try
            {
                // 1. Delete Desktop shortcut from all possible desktop locations
                string[] possibleDesktopPaths = new string[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop"),
                    @"C:\Users\trung\Desktop",
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
                };

                foreach (string dPath in possibleDesktopPaths)
                {
                    if (string.IsNullOrEmpty(dPath) || !Directory.Exists(dPath)) continue;
                    try
                    {
                        foreach (string sc in Directory.GetFiles(dPath, "*QuinGM*.lnk"))
                        {
                            try { File.Delete(sc); } catch { }
                        }
                    }
                    catch { }
                }

                // 2. If running directly from Desktop or C:\ drive, trigger self-delete script
                string currentExe = Application.ExecutablePath;
                string exeRoot = Path.GetPathRoot(currentExe);
                bool isSystemDrive = string.Equals(exeRoot, "C:\\", StringComparison.OrdinalIgnoreCase);

                if (isSystemDrive)
                {
                    try
                    {
                        string appDataRoaming = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuinGM");
                        if (Directory.Exists(appDataRoaming)) Directory.Delete(appDataRoaming, true);
                    }
                    catch { }

                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = "cmd.exe";
                        psi.Arguments = string.Format("/c ping 127.0.0.1 -n 2 >nul & del /f /q \"{0}\"", currentExe);
                        psi.WindowStyle = ProcessWindowStyle.Hidden;
                        psi.CreateNoWindow = true;
                        Process.Start(psi);
                    }
                    catch { }
                }
            }
            catch { }
            Environment.Exit(0);
        }

        private string GetGamesJsonPath()
        {
            if (!string.IsNullOrEmpty(appDir))
            {
                string p1 = Path.Combine(appDir, "games.json");
                if (File.Exists(p1)) return p1;

                string p2 = Path.Combine(appDir, "code", "code", "games.json");
                if (File.Exists(p2)) return p2;
            }

            if (!string.IsNullOrEmpty(driveRoot))
            {
                string p3 = Path.Combine(driveRoot, "games.json");
                if (File.Exists(p3)) return p3;

                string p4 = Path.Combine(driveRoot, "code", "code", "games.json");
                if (File.Exists(p4)) return p4;
            }

            return !string.IsNullOrEmpty(driveRoot) ? Path.Combine(driveRoot, "games.json") : "E:\\games.json";
        }

        private void LoadGamesData()
        {
            string jsonPath = GetGamesJsonPath();
            List<GameItem> loaded = new List<GameItem>();

            if (File.Exists(jsonPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonPath, Encoding.UTF8);
                    var list = serializer.Deserialize<List<GameItem>>(json);
                    if (list != null) loaded = list;
                }
                catch { }
            }

            List<GameItem> validList = new List<GameItem>();
            foreach (var g in loaded)
            {
                // pmt_click is now integrated as core feature, skip it from game cards
                if (string.Equals(g.id, "pmt_click", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(g.name, "pmt_click", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string target = RebaseDrive(g.exePath);
                if (File.Exists(target) || (!string.IsNullOrEmpty(g.fallbackPath) && File.Exists(RebaseDrive(g.fallbackPath))))
                {
                    g.exePath = target;
                    if (string.IsNullOrEmpty(g.name))
                    {
                        g.name = Path.GetFileNameWithoutExtension(target);
                    }
                    validList.Add(g);
                }
            }

            allGames = validList;

            if (allGames.Count == 0)
            {
                ScanExternalDriveInternal();
            }
            else
            {
                SaveGamesData();
            }

            UpdateCategoryBadges();
            FilterGames();
        }

        private void SaveGamesData()
        {
            try
            {
                string newJson = serializer.Serialize(allGames);
                string jsonPath = GetGamesJsonPath();
                File.WriteAllText(jsonPath, newJson, Encoding.UTF8);
                // If we are at root, keep code/code/games.json in sync as well
                string subPath = Path.Combine(appDir, "code", "code", "games.json");
                if (File.Exists(subPath) && !string.Equals(jsonPath, subPath, StringComparison.OrdinalIgnoreCase))
                {
                    try { File.WriteAllText(subPath, newJson, Encoding.UTF8); } catch { }
                }
            }
            catch { }
        }

        private void TriggerScan()
        {
            btnScan.ButtonText = "Đang quét...";
            btnScan.Enabled = false;

            ThreadPool.QueueUserWorkItem(delegate {
                ScanExternalDriveInternal();
                this.Invoke((MethodInvoker)delegate {
                    btnScan.ButtonText = "Quét Ổ Cứng";
                    btnScan.Enabled = true;
                    UpdateDriveInfo();
                    UpdateCategoryBadges();
                    FilterGames();
                    MessageBox.Show("Đã quét xong toàn bộ ổ " + driveRoot + "!\nHiện có " + allGames.Count + " ứng dụng thực tế trên ổ cứng.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            });
        }

        private void ScanExternalDriveInternal()
        {
            List<GameItem> validList = new List<GameItem>();
            foreach (var g in allGames)
            {
                if (string.Equals(g.id, "pmt_click", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(g.name, "pmt_click", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string p = RebaseDrive(g.exePath);
                if (File.Exists(p))
                {
                    g.exePath = p;
                    validList.Add(g);
                }
            }
            allGames = validList;

            List<string> foundExes = new List<string>();
            HashSet<string> excludedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "$RECYCLE.BIN", "System Volume Information", "Recovery", "$WinREAgent",
                "node_modules", ".git", ".github", ".svn", ".vscode",
                "bin", "obj", "target", "debug", "release",
                ".android_sdk", ".jdk", "jre", "jdk", "runtime",
                "downloading", "resources\\app", "_PortableData",
                "Windows", "WindowsApps", "ProgramData",
                "phone_pc_keyboard" // PMT Click is integrated core feature!
            };

            HashSet<string> excludedExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "unins000", "uninstall", "UnityCrashHandler", "CrashReport", "crashpad",
                "vcredist", "dxsetup", "inno_updater", "NativeUpdater", "updater",
                "kzonewkshelper", "MenuGame", "MenuGameNative", "QuinGM luv Mthu Menu", "GM Menu", "GMMenu", "cloudflared",
                "rcedit", "rg.exe", "esbuild", "tsgo", "winpty", "OpenConsole",
                "Compil32", "ISCC", "islzma", "backend", "language_server", "DriverHelper", "AudioConnect",
                "installer", "setup", "pmt_click"
            };

            ScanDir(driveRoot, foundExes, excludedDirs, excludedExes, 0, 7);

            foreach (string exe in foundExes)
            {
                bool exists = false;
                foreach (var g in allGames)
                {
                    if (string.Equals(Normalize(g.exePath), Normalize(exe), StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    GameItem newItem = CreateItem(exe);
                    if (newItem != null) allGames.Insert(0, newItem);
                }
            }

            SaveGamesData();
        }

        private void ScanDir(string dir, List<string> found, HashSet<string> exDirs, HashSet<string> exExes, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;
            try
            {
                string[] subDirs = Directory.GetDirectories(dir);
                foreach (string sd in subDirs)
                {
                    string name = Path.GetFileName(sd);
                    if (!exDirs.Contains(name))
                    {
                        ScanDir(sd, found, exDirs, exExes, depth + 1, maxDepth);
                    }
                }

                string[] exes = Directory.GetFiles(dir, "*.exe");
                foreach (string exe in exes)
                {
                    string name = Path.GetFileNameWithoutExtension(exe);
                    if (!exExes.Contains(name))
                    {
                        found.Add(exe);
                    }
                }
            }
            catch { }
        }

        private GameItem CreateItem(string exe)
        {
            string name = Path.GetFileNameWithoutExtension(exe);
            string lower = name.ToLower();
            string cat = "offline";
            string icon = "🎮";
            string genre = "Game / Tiện Ích";

            if (lower.Contains("steam"))
            {
                name = "Steam (Nền Tảng Game)";
                cat = "hot";
                icon = "🎮";
                genre = "Nền Tảng Game Bản Quyền";
            }
            else if (lower.Contains("minecraft"))
            {
                name = "Minecraft";
                cat = "hot";
                icon = "⛏️";
                genre = "Thế Giới Mở / Sinh Tồn";
            }
            else if (lower.Contains("chrome") || lower.Contains("edge") || lower.Contains("coccoc") || lower.Contains("browser"))
            {
                cat = "browser";
                icon = "🌐";
                genre = "Trình Duyệt Web";
            }
            else if (lower.Contains("discord") || lower.Contains("tool") || lower.Contains("antigravity"))
            {
                cat = "tool";
                icon = "⚡";
                genre = "Tiện Ích / Công Cụ";
            }

            return new GameItem
            {
                id = Guid.NewGuid().ToString("N"),
                name = name,
                category = cat,
                genre = genre,
                rating = 5.0,
                playCount = 120,
                isHot = (cat == "hot"),
                exePath = exe,
                icon = icon
            };
        }

        private void FilterGames()
        {
            if (currentCategory == "pmt_click") return;

            string query = (searchBox.Text ?? "").Trim().ToLower();
            List<GameItem> filtered = new List<GameItem>();

            foreach (var g in allGames)
            {
                bool matchCat = (currentCategory == "all") ||
                                (currentCategory == "hot" && (g.isHot || string.Equals(g.category, "hot", StringComparison.OrdinalIgnoreCase))) ||
                                string.Equals(g.category, currentCategory, StringComparison.OrdinalIgnoreCase);

                bool matchSearch = string.IsNullOrEmpty(query) ||
                                   (g.name != null && g.name.ToLower().Contains(query)) ||
                                   (g.genre != null && g.genre.ToLower().Contains(query)) ||
                                   (g.exePath != null && g.exePath.ToLower().Contains(query));

                if (matchCat && matchSearch)
                {
                    filtered.Add(g);
                }
            }

            RenderCards(filtered);
        }

        private string GetGameKey(GameItem g)
        {
            if (g == null) return "";
            if (!string.IsNullOrEmpty(g.id)) return g.id;
            if (!string.IsNullOrEmpty(g.exePath)) return g.exePath;
            return g.name ?? "";
        }

        private void ToggleSelectAll()
        {
            bool allSelected = currentFilteredGames.Count > 0 && currentFilteredGames.TrueForAll(g => selectedGameIds.Contains(GetGameKey(g)));

            if (allSelected)
            {
                foreach (var g in currentFilteredGames)
                {
                    selectedGameIds.Remove(GetGameKey(g));
                }
            }
            else
            {
                foreach (var g in currentFilteredGames)
                {
                    selectedGameIds.Add(GetGameKey(g));
                }
            }

            UpdateBatchActionsUI();
            foreach (Control c in flowPanel.Controls)
            {
                GameCardPanel card = c as GameCardPanel;
                if (card != null)
                {
                    card.IsSelected = selectedGameIds.Contains(GetGameKey(card.Game));
                    card.Invalidate();
                }
            }
        }

        private void BatchDeleteSelected()
        {
            if (selectedGameIds.Count == 0)
            {
                NeonMessageBox.Show(this, "Vui lòng tick chọn ít nhất 1 thẻ để thực hiện thao tác xóa hàng loạt.", "Chưa Chọn Thẻ Nào", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int count = selectedGameIds.Count;
            DialogResult dr = NeonMessageBox.Show(this, string.Format("Bạn có chắc chắn muốn xóa {0} ứng dụng đã tick chọn khỏi Menu không?\n\n(Thao tác này chỉ xóa liên kết trong Menu, tệp gốc trên ổ đĩa sẽ KHÔNG bị xóa)", count), "Xác Nhận Xóa Hàng Loạt", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                allGames.RemoveAll(g => selectedGameIds.Contains(GetGameKey(g)));
                selectedGameIds.Clear();
                SaveGamesData();
                UpdateCategoryBadges();
                UpdateBatchActionsUI();
                FilterGames();
            }
        }

        private void UpdateBatchActionsUI()
        {
            if (btnBatchDelete == null || btnSelectAll == null) return;

            int count = selectedGameIds.Count;
            if (count > 0)
            {
                btnBatchDelete.ButtonText = "Xóa Đã Chọn (" + count + ")";
                btnBatchDelete.NormalBg1 = Color.FromArgb(80, 22, 28);
                btnBatchDelete.NormalBg2 = Color.FromArgb(50, 14, 18);
                btnBatchDelete.HoverBg1 = Color.FromArgb(120, 30, 38);
                btnBatchDelete.HoverBg2 = Color.FromArgb(70, 18, 24);
                btnBatchDelete.NormalBorder = Color.FromArgb(239, 68, 68);
                btnBatchDelete.HoverBorder = Color.FromArgb(248, 113, 113);
                btnBatchDelete.ForeColor = Color.FromArgb(254, 202, 202);
            }
            else
            {
                btnBatchDelete.ButtonText = "Xóa Hàng Loạt";
                btnBatchDelete.NormalBg1 = Color.FromArgb(36, 16, 20);
                btnBatchDelete.NormalBg2 = Color.FromArgb(24, 10, 14);
                btnBatchDelete.HoverBg1 = Color.FromArgb(52, 22, 28);
                btnBatchDelete.HoverBg2 = Color.FromArgb(34, 14, 18);
                btnBatchDelete.NormalBorder = Color.FromArgb(80, 36, 42);
                btnBatchDelete.HoverBorder = Color.FromArgb(140, 55, 65);
                btnBatchDelete.ForeColor = Color.FromArgb(160, 110, 115);
            }
            btnBatchDelete.Invalidate();

            bool allSelected = currentFilteredGames.Count > 0 && currentFilteredGames.TrueForAll(g => selectedGameIds.Contains(GetGameKey(g)));
            btnSelectAll.ButtonText = allSelected ? "☐ Bỏ Chọn Tất Cả" : "☑ Chọn Tất Cả";
            btnSelectAll.Invalidate();
        }

        private void RenderCards(List<GameItem> list)
        {
            currentFilteredGames = list;
            flowPanel.SuspendLayout();
            flowPanel.Controls.Clear();
            lblCount.Text = list.Count + " ứng dụng hiển thị";

            int scrollW = (neonScrollBar != null && neonScrollBar.Visible) ? neonScrollBar.Width : 0;
            int containerW = cardsContainer.ClientSize.Width - scrollW;
            int availW = containerW - flowPanel.Padding.Horizontal;
            int cardMargin = 8;
            int minCardW = 280;
            int cols = Math.Max(1, availW / (minCardW + cardMargin * 2));
            int cardW = (availW / cols) - (cardMargin * 2);
            if (cardW < 240) cardW = 240;

            if (list.Count == 0)
            {
                Panel emptyPanel = new Panel
                {
                    Size = new Size(600, 180),
                    Margin = new Padding((availW - 600) / 2 > 0 ? (availW - 600) / 2 : 20, 50, 0, 0),
                    BackColor = Color.FromArgb(24, 11, 13)
                };

                Label lblEmptyIcon = new Label
                {
                    Text = "🔍",
                    Font = new Font("Segoe UI Emoji", 28F),
                    ForeColor = UITheme.NeonAmber,
                    AutoSize = true,
                    Location = new Point(270, 20)
                };

                Label lblEmptyText = new Label
                {
                    Text = "Không tìm thấy game hoặc ứng dụng phù hợp!\nThử gõ từ khóa khác hoặc bấm '+ Thêm Ứng Dụng'.",
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    ForeColor = UITheme.TextMuted,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Size = new Size(580, 50),
                    Location = new Point(10, 85)
                };

                emptyPanel.Controls.Add(lblEmptyIcon);
                emptyPanel.Controls.Add(lblEmptyText);
                flowPanel.Controls.Add(emptyPanel);
            }
            else
            {
                foreach (var item in list)
                {
                    var game = item;
                    string key = GetGameKey(game);
                    bool isSel = selectedGameIds.Contains(key);

                    GameCardPanel card = new GameCardPanel(
                        game,
                        g => LaunchNativePortable(g),
                        g => OpenEditDialog(g),
                        g => DeleteGame(g),
                        g => OpenFolder(g),
                        (g, isChecked) => {
                            string k = GetGameKey(g);
                            if (isChecked) selectedGameIds.Add(k);
                            else selectedGameIds.Remove(k);
                            UpdateBatchActionsUI();
                        },
                        isSel,
                        driveRoot
                    );

                    card.Size = new Size(cardW, 210);
                    card.Margin = new Padding(cardMargin, 10, cardMargin, 10);
                    card.MouseWheel += (s, e) => HandleWheel(e);
                    flowPanel.Controls.Add(card);
                }
            }

            flowPanel.ResumeLayout();
            UpdateScrollLayout();
            UpdateBatchActionsUI();
        }

        private void OpenAddDialog()
        {
            GameItem newItem = new GameItem
            {
                category = "offline",
                genre = "Game / Ứng Dụng",
                icon = "🎮",
                rating = 5.0,
                playCount = 10
            };
            EditGameForm form = new EditGameForm(newItem, true, driveRoot);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                allGames.Insert(0, newItem);
                SaveGamesData();
                UpdateCategoryBadges();
                FilterGames();
            }
        }

        private void OpenEditDialog(GameItem game)
        {
            EditGameForm form = new EditGameForm(game, false, driveRoot);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                SaveGamesData();
                UpdateCategoryBadges();
                FilterGames();
            }
        }

        private void DeleteGame(GameItem game)
        {
            DialogResult dr = NeonMessageBox.Show(this, "Bạn có chắc chắn muốn xóa '" + game.name + "' khỏi Menu không?\n\n(Thao tác này chỉ xóa liên kết trong Menu, tệp gốc trên ổ đĩa sẽ KHÔNG bị xóa)", "Xác Nhận Xóa Ứng Dụng", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr == DialogResult.Yes)
            {
                selectedGameIds.Remove(GetGameKey(game));
                allGames.Remove(game);
                SaveGamesData();
                UpdateCategoryBadges();
                UpdateBatchActionsUI();
                FilterGames();
            }
        }

        private void OpenFolder(GameItem game)
        {
            try
            {
                string target = RebaseDrive(game.exePath);
                if (File.Exists(target))
                {
                    Process.Start("explorer.exe", "/select,\"" + target + "\"");
                }
                else
                {
                    string dir = Path.GetDirectoryName(target);
                    if (Directory.Exists(dir)) Process.Start("explorer.exe", dir);
                }
            }
            catch { }
        }

        private void LaunchNativePortable(GameItem game)
        {
            try
            {
                string targetPath = RebaseDrive(game.exePath);
                if (!File.Exists(targetPath) && !string.IsNullOrEmpty(game.fallbackPath))
                {
                    targetPath = RebaseDrive(game.fallbackPath);
                }

                if (!File.Exists(targetPath) && !Directory.Exists(targetPath))
                {
                    MessageBox.Show("Không tìm thấy file chạy tại:\n" + targetPath + "\n\nỨng dụng này có thể đã bị di chuyển hoặc xóa.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string appFolder = Path.GetDirectoryName(targetPath);
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.WorkingDirectory = appFolder;
                psi.UseShellExecute = false;

                string appDataRoaming = Path.Combine(portableDataDir, "Roaming");
                string appDataLocal = Path.Combine(portableDataDir, "Local");
                string userProfile = Path.Combine(portableDataDir, "UserProfile");

                if (game.name.IndexOf("minecraft", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    targetPath.IndexOf("minecraft", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    Directory.Exists(Path.Combine(appFolder, ".minecraft")))
                {
                    psi.EnvironmentVariables["APPDATA"] = appFolder;
                    psi.EnvironmentVariables["LOCALAPPDATA"] = appFolder;
                    psi.EnvironmentVariables["USERPROFILE"] = userProfile;
                    psi.EnvironmentVariables["HOME"] = userProfile;
                }
                else if (game.name.IndexOf("discord", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         targetPath.IndexOf("discord", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string discordData = Path.Combine(driveRoot + "\\", "Discord", "data");
                    if (!Directory.Exists(discordData)) discordData = appDataRoaming;
                    psi.EnvironmentVariables["DISCORD_USER_DATA_DIR"] = discordData;
                    psi.EnvironmentVariables["APPDATA"] = discordData;
                    psi.EnvironmentVariables["LOCALAPPDATA"] = Path.Combine(driveRoot + "\\", "Discord", "local");
                    psi.EnvironmentVariables["USERPROFILE"] = userProfile;
                    psi.EnvironmentVariables["HOME"] = userProfile;
                }
                else if (Directory.Exists(Path.Combine(appFolder, "data")))
                {
                    string localData = Path.Combine(appFolder, "data");
                    psi.EnvironmentVariables["APPDATA"] = localData;
                    psi.EnvironmentVariables["LOCALAPPDATA"] = localData;
                    psi.EnvironmentVariables["USERPROFILE"] = localData;
                    psi.EnvironmentVariables["HOME"] = localData;
                }
                else
                {
                    psi.EnvironmentVariables["APPDATA"] = appDataRoaming;
                    psi.EnvironmentVariables["LOCALAPPDATA"] = appDataLocal;
                    psi.EnvironmentVariables["USERPROFILE"] = userProfile;
                    psi.EnvironmentVariables["HOME"] = userProfile;
                }

                if (targetPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    psi.FileName = "cmd.exe";
                    psi.Arguments = "/c start \"\" \"" + targetPath + "\"";
                }
                else if (targetPath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
                {
                    psi.FileName = "cmd.exe";
                    psi.Arguments = "/c \"" + targetPath + "\"";
                    psi.CreateNoWindow = true;
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                }
                else
                {
                    psi.FileName = targetPath;
                }

                Process.Start(psi);
                lblStatus.Text = "🚀 ĐÃ KHỞI CHẠY: " + game.name + " (Chế độ Portable: Ổ " + driveRoot + ")";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi chạy: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string RebaseDrive(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (path.Length >= 3 && path[1] == ':' && path[2] == '\\')
            {
                return driveRoot.TrimEnd('\\') + path.Substring(2);
            }
            return path;
        }

        private string Normalize(string p)
        {
            if (string.IsNullOrEmpty(p)) return "";
            try { return Path.GetFullPath(p).TrimEnd('\\', '/'); } catch { return p; }
        }

        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "QuinGM_luv_Mthu_Menu_Mutex", out createdNew))
            {
                if (!createdNew)
                {
                    try
                    {
                        Process current = Process.GetCurrentProcess();
                        bool restored = false;
                        foreach (Process p in Process.GetProcessesByName(current.ProcessName))
                        {
                            if (p.Id != current.Id && p.MainWindowHandle != IntPtr.Zero)
                            {
                                ShowWindowAsync(p.MainWindowHandle, 9);
                                SetForegroundWindow(p.MainWindowHandle);
                                restored = true;
                                break;
                            }
                        }
                        if (restored) return;

                        foreach (Process p in Process.GetProcessesByName(current.ProcessName))
                        {
                            if (p.Id != current.Id)
                            {
                                try { p.Kill(); } catch { }
                            }
                        }
                    }
                    catch { }
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                AppDomain.CurrentDomain.UnhandledException += (s, ev) => {
                    try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), ev.ExceptionObject.ToString()); } catch {}
                };
                Application.ThreadException += (s, ev) => {
                    try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), ev.Exception.ToString()); } catch {}
                };

                // Modal xác thực Khoá định mệnh (trái tim nhịp đập, mặt cười, mờ dần chuyển cảnh, tự huỷ)
                using (DestinyKeyVerificationForm verifyForm = new DestinyKeyVerificationForm())
                {
                    if (verifyForm.ShowDialog() != DialogResult.OK)
                    {
                        MainForm.ExecuteSelfDestruct();
                        return;
                    }
                }

                try
                {
                    Application.Run(new MainForm());
                }
                catch (Exception ex)
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), ex.ToString());
                }
                finally
                {
                    Environment.Exit(0);
                }
            }
        }
    }
}
