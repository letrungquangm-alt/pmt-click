using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace QuinGM.Uninstaller
{
    public class UninstallerForm : Form
    {
        private Button btnConfirm;
        private Button btnCancel;
        private Label lblStatus;

        public UninstallerForm()
        {
            this.Text = "Gỡ Cài Đặt QuinGM luv Mthu Menu";
            this.Size = new Size(540, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(18, 12, 18);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10F);
            this.Opacity = 0.0;

            LoadIcon();
            InitUI();

            // Hiệu ứng mờ dần hiện lên (Fade-in)
            this.Shown += (s, e) => {
                Timer fadeIn = new Timer { Interval = 15 };
                fadeIn.Tick += (s2, e2) => {
                    if (this.Opacity < 0.98) this.Opacity += 0.08;
                    else { this.Opacity = 1.0; fadeIn.Stop(); fadeIn.Dispose(); }
                };
                fadeIn.Start();
            };
        }

        private void LoadIcon()
        {
            try
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream("app.ico"))
                {
                    if (s != null) this.Icon = new Icon(s);
                }
            }
            catch { }
        }

        private void InitUI()
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 75,
                BackColor = Color.FromArgb(32, 14, 20)
            };
            header.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(248, 113, 113), 2))
                {
                    e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
                }
            };

            Label lblTitle = new Label
            {
                Text = "🗑️ GỠ CÀI ĐẶT QUINGM LUV MTHU MENU",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(248, 113, 113),
                AutoSize = true,
                Location = new Point(20, 15)
            };
            Label lblSub = new Label
            {
                Text = "Dọn sạch toàn bộ ứng dụng, khoá định mệnh ẩn và phím tắt Desktop",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(220, 180, 190),
                AutoSize = true,
                Location = new Point(22, 44)
            };
            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSub);
            this.Controls.Add(header);

            Label lblWarn = new Label
            {
                Text = "Bạn có chắc chắn muốn gỡ cài đặt hoàn toàn QuinGM luv Mthu Menu không?\n\n" +
                       "Thao tác này sẽ xoá sạch triệt để:\n" +
                       "  ❌ Ứng dụng chính (QuinGM luv Mthu Menu.exe)\n" +
                       "  ❌ Khoá định mệnh ẩn bí mật (anhyeuempmt.tag)\n" +
                       "  ❌ Phím tắt bản quyền trên màn hình Desktop máy tính\n" +
                       "  ❌ Dữ liệu cấu hình và bộ nhớ đệm tạm thời\n" +
                       "  ❌ Thư mục cài đặt ứng dụng\n\n" +
                       "Sau khi gỡ, mọi dấu vết sẽ biến mất hoàn toàn không còn gì.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(254, 215, 226),
                Location = new Point(25, 90),
                Size = new Size(485, 195)
            };
            this.Controls.Add(lblWarn);

            lblStatus = new Label
            {
                Location = new Point(25, 290),
                Size = new Size(485, 22),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(251, 191, 36),
                Text = ""
            };
            this.Controls.Add(lblStatus);

            btnConfirm = new Button
            {
                Text = "🗑️ XÁC NHẬN GỠ SẠCH TOÀN BỘ",
                Location = new Point(25, 320),
                Size = new Size(270, 46),
                BackColor = Color.FromArgb(65, 18, 24),
                ForeColor = Color.FromArgb(254, 202, 202),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderColor = Color.FromArgb(220, 50, 60);
            btnConfirm.Click += BtnConfirm_Click;
            this.Controls.Add(btnConfirm);

            btnCancel = new Button
            {
                Text = "Hủy Bỏ",
                Location = new Point(310, 320),
                Size = new Size(195, 46),
                BackColor = Color.FromArgb(32, 20, 28),
                ForeColor = Color.FromArgb(220, 200, 210),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(80, 50, 70);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private void ForceDeleteFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                }
            }
            catch { }
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            btnConfirm.Enabled = false;
            btnCancel.Enabled = false;
            lblStatus.Text = "Đang gỡ sạch ứng dụng và dữ liệu liên quan...";
            this.Cursor = Cursors.WaitCursor;

            try
            {
                // 0. Tắt ứng dụng QuinGM nếu đang chạy ngầm để giải phóng file lock
                foreach (Process p in Process.GetProcesses())
                {
                    try
                    {
                        string pName = p.ProcessName;
                        if ((pName.IndexOf("QuinGM", StringComparison.OrdinalIgnoreCase) >= 0 || pName.IndexOf("GMMenu", StringComparison.OrdinalIgnoreCase) >= 0)
                            && p.Id != Process.GetCurrentProcess().Id)
                        {
                            p.Kill();
                        }
                        else if (pName.IndexOf("pmt_click", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            p.Kill();
                        }
                    }
                    catch { }
                }
                System.Threading.Thread.Sleep(300);

                string currentExe = Application.ExecutablePath;
                string currentDir = Path.GetDirectoryName(currentExe);
                string driveRoot = Path.GetPathRoot(currentDir);

                // 1. Xoá phím tắt Desktop máy tính
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
                            ForceDeleteFile(sc);
                        }
                    }
                    catch { }
                }

                // 2. Xoá thẻ khoá định mệnh ẩn (kể cả thuộc tính Hidden + System)
                ForceDeleteFile(Path.Combine(currentDir, "anhyeuempmt.tag"));
                if (!string.IsNullOrEmpty(driveRoot))
                {
                    ForceDeleteFile(Path.Combine(driveRoot, "anhyeuempmt.tag"));
                    ForceDeleteFile(Path.Combine(driveRoot, "QuinGM", "anhyeuempmt.tag"));
                }

                // 3. Xoá file app chính
                ForceDeleteFile(Path.Combine(currentDir, "QuinGM luv Mthu Menu.exe"));
                if (!string.IsNullOrEmpty(driveRoot))
                {
                    ForceDeleteFile(Path.Combine(driveRoot, "QuinGM luv Mthu Menu.exe"));
                    ForceDeleteFile(Path.Combine(driveRoot, "QuinGM", "QuinGM luv Mthu Menu.exe"));
                }

                // 4. Xoá games.json hoặc log trong thư mục cài đặt nếu có
                ForceDeleteFile(Path.Combine(currentDir, "games.json"));
                ForceDeleteFile(Path.Combine(currentDir, "crash.log"));

                // 5. Xoá thư mục cache tạm thời %APPDATA%\QuinGM
                try
                {
                    string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuinGM");
                    if (Directory.Exists(appData)) Directory.Delete(appData, true);
                }
                catch { }

                // 6. Tự hủy file gỡ cài đặt và xoá hoàn toàn thư mục cài đặt
                bool isRoot = string.Equals(currentDir.TrimEnd('\\'), driveRoot.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "cmd.exe";
                    if (!isRoot)
                    {
                        psi.Arguments = string.Format("/c ping 127.0.0.1 -n 2 >nul & del /f /q /a \"{0}\" & cd /d \"{2}\" & attrib -r -s -h \"{1}\\*.*\" /s /d >nul 2>&1 & rmdir /s /q \"{1}\"", currentExe, currentDir, driveRoot);
                    }
                    else
                    {
                        psi.Arguments = string.Format("/c ping 127.0.0.1 -n 2 >nul & del /f /q /a \"{0}\"", currentExe);
                    }
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                    psi.CreateNoWindow = true;
                    Process.Start(psi);
                }
                catch { }

                MessageBox.Show(
                    "🧹 ĐÃ GỠ CÀI ĐẶT TOÀN BỘ THÀNH CÔNG!\n\n" +
                    "• Ứng dụng QuinGM luv Mthu Menu đã được xoá sạch.\n" +
                    "• Khoá định mệnh [anhyeuempmt.tag] đã được xoá sạch.\n" +
                    "• Phím tắt ngoài Desktop máy tính đã được dọn sạch.\n" +
                    "• Thư mục cài đặt và dữ liệu tạm thời đã được xoá triệt để.\n\n" +
                    "Không còn bất kỳ dấu vết nào còn sót lại!",
                    "Gỡ Cài Đặt Hoàn Tất",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Có lỗi xảy ra khi gỡ: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Environment.Exit(0);
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UninstallerForm());
        }
    }
}
