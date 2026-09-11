using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;

namespace QuinGM.Installer
{
    public class InstallerForm : Form
    {
        private ComboBox cboDrives;
        private Label lblTagStatus;
        private Label lblDesktopStatus;
        private CheckBox chkCreateTag;
        private CheckBox chkCreateShortcut;
        private Panel progressBg;
        private Panel progressFill;
        private Label lblProgressText;
        private Button btnInstall;
        private Button btnUninstall;
        private Button btnLaunch;
        private Label lblHeaderTitle;
        private Label lblHeaderSub;
        private Panel cardBox;

        public InstallerForm()
        {
            this.Text = "Trình Cài Đặt QuinGM luv Mthu Menu";
            this.Size = new Size(620, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.BackColor = Color.FromArgb(14, 10, 14);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 10F);
            this.Opacity = 0.0;

            LoadIcon();
            InitUI();
            RefreshDrives();
            CheckDesktopStatus();

            // Hiệu ứng mờ dần hiện lên (Fade-in)
            this.Shown += (s, e) => {
                Timer fadeIn = new Timer { Interval = 15 };
                fadeIn.Tick += (s2, e2) => {
                    if (this.Opacity < 0.98)
                    {
                        this.Opacity += 0.08;
                    }
                    else
                    {
                        this.Opacity = 1.0;
                        fadeIn.Stop();
                        fadeIn.Dispose();
                    }
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
            // Header banner
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(24, 14, 22)
            };
            header.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(245, 95, 140), 2))
                {
                    e.Graphics.DrawLine(p, 0, header.Height - 1, header.Width, header.Height - 1);
                }
            };

            lblHeaderTitle = new Label
            {
                Text = "💖 TRÌNH CÀI ĐẶT QUINGM LUV MTHU MENU",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 95, 140),
                AutoSize = true,
                Location = new Point(20, 18)
            };
            lblHeaderSub = new Label
            {
                Text = "Đóng gói trọn bộ: Cài đặt ứng dụng vào ổ đĩa di động & tạo phím tắt Desktop",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 180, 195),
                AutoSize = true,
                Location = new Point(22, 48)
            };
            header.Controls.Add(lblHeaderTitle);
            header.Controls.Add(lblHeaderSub);
            this.Controls.Add(header);

            // Card container
            cardBox = new Panel
            {
                Location = new Point(25, 98),
                Size = new Size(555, 270),
                BackColor = Color.FromArgb(24, 16, 24)
            };
            cardBox.Paint += (s, e) => {
                using (Pen p = new Pen(Color.FromArgb(60, 30, 48), 1.5f))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, cardBox.Width - 1, cardBox.Height - 1);
                }
            };

            Label lblStep1 = new Label
            {
                Text = "📁 Bước 1: Chọn ổ đĩa đích để cài đặt app (USB / Ổ di động):",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 210, 225),
                Location = new Point(18, 14),
                AutoSize = true
            };
            cardBox.Controls.Add(lblStep1);

            cboDrives = new ComboBox
            {
                Location = new Point(22, 42),
                Size = new Size(395, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(36, 20, 32),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F)
            };
            cboDrives.SelectedIndexChanged += (s, e) => CheckSelectedDriveTag();
            cardBox.Controls.Add(cboDrives);

            Button btnRefresh = new Button
            {
                Text = "🔄 Quét lại",
                Location = new Point(425, 41),
                Size = new Size(110, 30),
                BackColor = Color.FromArgb(45, 25, 40),
                ForeColor = Color.FromArgb(245, 180, 205),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(120, 60, 90);
            btnRefresh.Click += (s, e) => { RefreshDrives(); CheckDesktopStatus(); };
            cardBox.Controls.Add(btnRefresh);

            lblTagStatus = new Label
            {
                Location = new Point(22, 80),
                Size = new Size(510, 38),
                Font = new Font("Segoe UI", 9F),
                Text = "Đang kiểm tra ổ đĩa..."
            };
            cardBox.Controls.Add(lblTagStatus);

            // Options
            chkCreateTag = new CheckBox
            {
                Text = "Tạo khoá định mệnh [anhyeuempmt.tag] trên ổ đĩa nếu chưa có",
                Checked = true,
                Location = new Point(24, 122),
                Size = new Size(500, 26),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(240, 215, 230)
            };
            cardBox.Controls.Add(chkCreateTag);

            chkCreateShortcut = new CheckBox
            {
                Text = "Tạo phím tắt ngoài màn hình Desktop máy tính này",
                Checked = true,
                Location = new Point(24, 150),
                Size = new Size(500, 26),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(240, 215, 230)
            };
            cardBox.Controls.Add(chkCreateShortcut);

            // Progress bar
            progressBg = new Panel
            {
                Location = new Point(22, 188),
                Size = new Size(510, 8),
                BackColor = Color.FromArgb(44, 20, 32),
                Visible = false
            };
            progressFill = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(0, 8),
                BackColor = Color.FromArgb(245, 95, 140)
            };
            progressBg.Controls.Add(progressFill);
            cardBox.Controls.Add(progressBg);

            lblProgressText = new Label
            {
                Location = new Point(22, 202),
                Size = new Size(510, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(250, 200, 215),
                Text = "Sẵn sàng cài đặt ứng dụng vào ổ đĩa được chọn."
            };
            cardBox.Controls.Add(lblProgressText);

            lblDesktopStatus = new Label
            {
                Location = new Point(22, 232),
                Size = new Size(510, 28),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = "Đang kiểm tra trạng thái Desktop..."
            };
            cardBox.Controls.Add(lblDesktopStatus);

            this.Controls.Add(cardBox);

            // Action Buttons
            btnInstall = new Button
            {
                Text = "🚀 BẮT ĐẦU CÀI ĐẶT APP",
                Location = new Point(25, 385),
                Size = new Size(250, 50),
                BackColor = Color.FromArgb(80, 22, 50),
                ForeColor = Color.FromArgb(255, 230, 242),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderColor = Color.FromArgb(245, 95, 140);
            btnInstall.Click += BtnInstall_Click;
            this.Controls.Add(btnInstall);

            btnUninstall = new Button
            {
                Text = "🗑️ Gỡ Khỏi Desktop",
                Location = new Point(285, 385),
                Size = new Size(155, 50),
                BackColor = Color.FromArgb(40, 18, 24),
                ForeColor = Color.FromArgb(254, 202, 202),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnUninstall.FlatAppearance.BorderColor = Color.FromArgb(160, 40, 60);
            btnUninstall.Click += BtnUninstall_Click;
            this.Controls.Add(btnUninstall);

            btnLaunch = new Button
            {
                Text = "🎮 Mở Game",
                Location = new Point(450, 385),
                Size = new Size(130, 50),
                BackColor = Color.FromArgb(32, 20, 32),
                ForeColor = Color.FromArgb(240, 200, 220),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLaunch.FlatAppearance.BorderColor = Color.FromArgb(140, 60, 90);
            btnLaunch.Click += (s, e) => LaunchApp();
            this.Controls.Add(btnLaunch);
        }

        private class DriveItem
        {
            public string Root { get; set; }
            public string Display { get; set; }
            public bool HasTag { get; set; }
            public bool HasApp { get; set; }
            public override string ToString() { return Display; }
        }

        private void RefreshDrives()
        {
            cboDrives.Items.Clear();
            int selectedIdx = 0;
            int currentIdx = 0;

            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                if (!d.IsReady) continue;
                string root = d.RootDirectory.FullName;
                bool hasTag = CheckTagFile(root);
                bool hasApp = File.Exists(Path.Combine(root, "QuinGM luv Mthu Menu.exe"));

                string labelName = d.VolumeLabel;
                if (string.IsNullOrEmpty(labelName)) labelName = "Ổ đĩa";

                string stateDesc = "";
                if (hasTag && hasApp) stateDesc = "💖 ĐÃ CÀI ĐẶT APP & CÓ KHOÁ ĐỊNH MỆNH";
                else if (hasTag) stateDesc = "💖 Có thẻ khoá định mệnh";
                else if (hasApp) stateDesc = "Đã có file app";
                else stateDesc = "Chưa cài đặt";

                string display = string.Format("{0} [{1}] ({2})", root, labelName, stateDesc);
                DriveItem item = new DriveItem { Root = root, Display = display, HasTag = hasTag, HasApp = hasApp };
                cboDrives.Items.Add(item);

                if (hasTag && selectedIdx == 0) selectedIdx = currentIdx;
                currentIdx++;
            }

            if (cboDrives.Items.Count > 0)
            {
                cboDrives.SelectedIndex = selectedIdx;
            }
            CheckSelectedDriveTag();
        }

        private bool CheckTagFile(string root)
        {
            try
            {
                string tagPath = Path.Combine(root, "anhyeuempmt.tag");
                if (File.Exists(tagPath))
                {
                    string txt = File.ReadAllText(tagPath);
                    if (txt.IndexOf("Anh_Yeu_Em_Pham_Minh_Thu_Ksenia_Rin_Luv_U", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            catch { }
            return false;
        }

        private void CheckSelectedDriveTag()
        {
            DriveItem item = cboDrives.SelectedItem as DriveItem;
            if (item == null)
            {
                lblTagStatus.Text = "Vui lòng chọn ổ đĩa.";
                lblTagStatus.ForeColor = Color.Gray;
                btnInstall.Enabled = false;
                return;
            }

            string appPath = Path.Combine(item.Root, "QuinGM luv Mthu Menu.exe");
            bool appExists = File.Exists(appPath);

            if (item.HasTag && appExists)
            {
                lblTagStatus.Text = "✅ Ổ đĩa này đã cài đặt app và có khoá định mệnh hợp lệ!\nBạn có thể bấm Cài đặt lại để cập nhật bản mới nhất hoặc Mở Game ngay.";
                lblTagStatus.ForeColor = Color.FromArgb(74, 222, 128);
                btnInstall.Text = "🔄 CÀI ĐẶT / CẬP NHẬT APP";
            }
            else if (item.HasTag)
            {
                lblTagStatus.Text = "💖 Đã có khoá định mệnh [anhyeuempmt.tag]!\nBấm Cài Đặt bên dưới để giải nén ứng dụng vào ổ đĩa này.";
                lblTagStatus.ForeColor = Color.FromArgb(245, 95, 140);
                btnInstall.Text = "🚀 BẮT ĐẦU CÀI ĐẶT APP";
            }
            else
            {
                lblTagStatus.Text = "💡 Ổ đĩa sẵn sàng cài đặt. Trình cài đặt sẽ tự động tạo thẻ định danh và giải nén app.";
                lblTagStatus.ForeColor = Color.FromArgb(200, 190, 210);
                btnInstall.Text = "🚀 BẮT ĐẦU CÀI ĐẶT APP";
            }
            btnInstall.Enabled = true;
        }

        private string GetDesktopShortcutPath()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrEmpty(desktopPath) || !Directory.Exists(desktopPath))
            {
                desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");
            }
            return Path.Combine(desktopPath, "QuinGM luv Mthu Menu.lnk");
        }

        private void CheckDesktopStatus()
        {
            string shortcutPath = GetDesktopShortcutPath();
            if (File.Exists(shortcutPath))
            {
                lblDesktopStatus.Text = "💻 Màn hình Desktop máy này: ĐÃ CÓ PHÍM TẮT BẢN QUYỀN ✅";
                lblDesktopStatus.ForeColor = Color.FromArgb(74, 222, 128);
                btnUninstall.Enabled = true;
            }
            else
            {
                lblDesktopStatus.Text = "💻 Màn hình Desktop máy này: CHƯA CÓ PHÍM TẮT";
                lblDesktopStatus.ForeColor = Color.FromArgb(251, 191, 36);
                btnUninstall.Enabled = false;
            }
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            DriveItem item = cboDrives.SelectedItem as DriveItem;
            if (item == null) return;

            string driveRoot = item.Root;
            btnInstall.Enabled = false;
            btnLaunch.Enabled = false;
            progressBg.Visible = true;
            progressFill.Width = 10;
            lblProgressText.Text = "Đang khởi tạo gói cài đặt...";

            Timer animTimer = new Timer { Interval = 20 };
            int step = 0;
            animTimer.Tick += (s, ev) => {
                step++;
                if (step == 10)
                {
                    progressFill.Width = 120;
                    lblProgressText.Text = "Đang giải nén QuinGM luv Mthu Menu.exe vào ổ " + driveRoot + "...";
                }
                else if (step == 25)
                {
                    // Trích xuất file thực thi chính từ resource
                    try
                    {
                        string targetExe = Path.Combine(driveRoot, "QuinGM luv Mthu Menu.exe");
                        ExtractPayload(targetExe);
                    }
                    catch (Exception ex)
                    {
                        animTimer.Stop();
                        animTimer.Dispose();
                        MessageBox.Show("Lỗi khi ghi file app: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnInstall.Enabled = true;
                        return;
                    }

                    progressFill.Width = 280;
                    lblProgressText.Text = "Đang kiểm tra và tạo khoá định mệnh [anhyeuempmt.tag]...";
                }
                else if (step == 38)
                {
                    if (chkCreateTag.Checked)
                    {
                        try
                        {
                            string tagPath = Path.Combine(driveRoot, "anhyeuempmt.tag");
                            File.WriteAllText(tagPath, "Anh_Yeu_Em_Pham_Minh_Thu_Ksenia_Rin_Luv_U", System.Text.Encoding.UTF8);
                        }
                        catch { }
                    }

                    progressFill.Width = 420;
                    lblProgressText.Text = "Đang tạo lối tắt bản quyền ra màn hình Desktop...";
                }
                else if (step == 48)
                {
                    if (chkCreateShortcut.Checked)
                    {
                        try
                        {
                            string targetExe = Path.Combine(driveRoot, "QuinGM luv Mthu Menu.exe");
                            string shortcutPath = GetDesktopShortcutPath();

                            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                            if (shellType != null)
                            {
                                dynamic shell = Activator.CreateInstance(shellType);
                                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                                shortcut.TargetPath = targetExe;
                                shortcut.WorkingDirectory = Path.GetDirectoryName(targetExe);
                                shortcut.IconLocation = targetExe + ",0";
                                shortcut.Description = "QuinGM luv Mthu Menu (Portable Gaming & PMT Click)";
                                shortcut.Save();
                            }
                        }
                        catch { }
                    }

                    progressFill.Width = 510;
                    lblProgressText.Text = "🎉 CÀI ĐẶT HOÀN TẤT THÀNH CÔNG 100%!";
                    lblProgressText.ForeColor = Color.FromArgb(74, 222, 128);
                }
                else if (step >= 55)
                {
                    animTimer.Stop();
                    animTimer.Dispose();

                    btnInstall.Enabled = true;
                    btnLaunch.Enabled = true;
                    RefreshDrives();
                    CheckDesktopStatus();

                    MessageBox.Show(
                        "🎉 ĐÃ CÀI ĐẶT ỨNG DỤNG THÀNH CÔNG!\n\n" +
                        "• Ứng dụng đã cài đặt tại: " + Path.Combine(driveRoot, "QuinGM luv Mthu Menu.exe") + "\n" +
                        "• Khoá định mệnh [anhyeuempmt.tag] đã sẵn sàng trên ổ " + driveRoot + "\n" +
                        "• Phím tắt Desktop đã được tạo thành công!\n\n" +
                        "Bây giờ bạn có thể mở game chơi ngay lập tức!",
                        "Cài Đặt Hoàn Tất",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            };
            animTimer.Start();
        }

        private void ExtractPayload(string targetPath)
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream("app_payload.bin"))
            {
                if (s != null)
                {
                    using (FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                    {
                        byte[] buffer = new byte[8192];
                        int read;
                        while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fs.Write(buffer, 0, read);
                        }
                    }
                    return;
                }
            }

            // Fallback nếu chạy trực tiếp từ repo hoặc folder
            string localExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QuinGM luv Mthu Menu.exe");
            if (File.Exists(localExe))
            {
                File.Copy(localExe, targetPath, true);
                return;
            }

            string binExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "GMMenu.exe");
            if (File.Exists(binExe))
            {
                File.Copy(binExe, targetPath, true);
                return;
            }

            throw new FileNotFoundException("Không tìm thấy gói cài đặt ứng dụng nhúng trong file!");
        }

        private void BtnUninstall_Click(object sender, EventArgs e)
        {
            DialogResult confirm = MessageBox.Show(
                "Bạn có chắc muốn gỡ phím tắt QuinGM luv Mthu Menu khỏi màn hình Desktop máy tính này không?\n(Dữ liệu game trên ổ di động vẫn được bảo toàn nguyên vẹn 100%)",
                "Xác Nhận Gỡ Bỏ",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
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

                try
                {
                    string appDataRoaming = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuinGM");
                    if (Directory.Exists(appDataRoaming)) Directory.Delete(appDataRoaming, true);
                }
                catch { }

                CheckDesktopStatus();

                MessageBox.Show(
                    "🧹 ĐÃ DỌN SẠCH DẤU VẾT THÀNH CÔNG!\n\n" +
                    "• Đã gỡ bỏ phím tắt trên Desktop máy tính.\n" +
                    "• Đã dọn sạch bộ nhớ đệm tạm thời.\n\n" +
                    "Máy tính đã hoàn toàn sạch sẽ an toàn!",
                    "Đã Gỡ Bỏ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi gỡ: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LaunchApp()
        {
            DriveItem item = cboDrives.SelectedItem as DriveItem;
            string driveRoot = (item != null) ? item.Root : "E:\\";
            string exePath = Path.Combine(driveRoot, "QuinGM luv Mthu Menu.exe");

            if (!File.Exists(exePath))
            {
                string localExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QuinGM luv Mthu Menu.exe");
                if (File.Exists(localExe)) exePath = localExe;
            }

            if (File.Exists(exePath))
            {
                try
                {
                    Process.Start(exePath);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khởi chạy: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Chưa cài đặt app vào ổ " + driveRoot + "!\nVui lòng bấm 'CÀI ĐẶT APP' trước khi mở.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }
}
