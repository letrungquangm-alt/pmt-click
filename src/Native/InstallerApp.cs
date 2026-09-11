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
        private Label lblHint;
        private Label lblTagStatus;
        private Label lblDesktopStatus;
        private CheckBox chkCreateTag;
        private CheckBox chkCreateShortcut;
        private Panel progressBg;
        private Panel progressFill;
        private Label lblProgressText;
        private Button btnInstall;
        private Button btnOpenFolder;
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
                Text = "Tự động phát hiện ổ đĩa di động • Cài đặt app • Tạo khoá định mệnh ẩn",
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
                Size = new Size(555, 290),
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
                Text = "🔍 BƯỚC 1: HỆ THỐNG ĐÃ TỰ QUÉT & CHỌN Ổ ĐĨA ĐỀ XUẤT:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 210, 225),
                Location = new Point(18, 14),
                AutoSize = true
            };
            cardBox.Controls.Add(lblStep1);

            cboDrives = new ComboBox
            {
                Location = new Point(22, 40),
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
                Location = new Point(425, 39),
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

            lblHint = new Label
            {
                Text = "👉 Nếu ĐÚNG ổ bạn muốn: Bấm [OK - BẮT ĐẦU CÀI ĐẶT APP] bên dưới.\n👉 Nếu SAI: Bạn bấm menu ở trên để tự chọn lại ổ đĩa khác theo ý muốn.",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(254, 202, 202),
                Location = new Point(22, 76),
                Size = new Size(515, 34)
            };
            cardBox.Controls.Add(lblHint);

            lblTagStatus = new Label
            {
                Location = new Point(22, 114),
                Size = new Size(515, 36),
                Font = new Font("Segoe UI", 9F),
                Text = "Đang kiểm tra ổ đĩa..."
            };
            cardBox.Controls.Add(lblTagStatus);

            // Options
            chkCreateTag = new CheckBox
            {
                Text = "🔒 Tự động tạo khoá định mệnh [anhyeuempmt.tag] ở chế độ ẨN (Tàng hình)",
                Checked = true,
                Location = new Point(24, 154),
                Size = new Size(510, 25),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 180, 205)
            };
            cardBox.Controls.Add(chkCreateTag);

            chkCreateShortcut = new CheckBox
            {
                Text = "🖥️ Tạo phím tắt bản quyền ngoài màn hình Desktop máy tính này",
                Checked = true,
                Location = new Point(24, 180),
                Size = new Size(510, 25),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(230, 215, 225)
            };
            cardBox.Controls.Add(chkCreateShortcut);

            // Progress bar
            progressBg = new Panel
            {
                Location = new Point(22, 215),
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
                Location = new Point(22, 228),
                Size = new Size(510, 24),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(250, 200, 215),
                Text = "Sẵn sàng cài đặt ứng dụng vào ổ đĩa được chọn."
            };
            cardBox.Controls.Add(lblProgressText);

            lblDesktopStatus = new Label
            {
                Location = new Point(22, 256),
                Size = new Size(510, 26),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = "Đang kiểm tra trạng thái Desktop..."
            };
            cardBox.Controls.Add(lblDesktopStatus);

            this.Controls.Add(cardBox);

            // Action Buttons
            btnInstall = new Button
            {
                Text = "✅ OK - BẮT ĐẦU CÀI ĐẶT APP 💕",
                Location = new Point(25, 405),
                Size = new Size(265, 52),
                BackColor = Color.FromArgb(85, 22, 55),
                ForeColor = Color.FromArgb(255, 230, 245),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderColor = Color.FromArgb(245, 95, 140);
            btnInstall.Click += BtnInstall_Click;
            this.Controls.Add(btnInstall);

            btnOpenFolder = new Button
            {
                Text = "📁 Mở Thư Mục",
                Location = new Point(300, 405),
                Size = new Size(135, 52),
                BackColor = Color.FromArgb(34, 20, 36),
                ForeColor = Color.FromArgb(240, 215, 235),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOpenFolder.FlatAppearance.BorderColor = Color.FromArgb(120, 60, 100);
            btnOpenFolder.Click += (s, e) => OpenInstallFolder();
            this.Controls.Add(btnOpenFolder);

            btnLaunch = new Button
            {
                Text = "🎮 Mở Game",
                Location = new Point(445, 405),
                Size = new Size(135, 52),
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
            public bool IsRemovable { get; set; }
            public override string ToString() { return Display; }
        }

        private void RefreshDrives()
        {
            cboDrives.Items.Clear();
            int selectedIdx = 0;
            int currentIdx = 0;
            int removableIdx = -1;

            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                if (!d.IsReady) continue;
                string root = d.RootDirectory.FullName;
                string installFolder = Path.Combine(root, "QuinGM");
                bool hasTag = CheckTagFile(root) || CheckTagFile(installFolder);
                bool hasApp = File.Exists(Path.Combine(installFolder, "QuinGM luv Mthu Menu.exe")) || File.Exists(Path.Combine(root, "QuinGM luv Mthu Menu.exe"));
                bool isRemovable = (d.DriveType == DriveType.Removable || (!root.StartsWith("C:", StringComparison.OrdinalIgnoreCase) && root.Length >= 2));

                string labelName = d.VolumeLabel;
                if (string.IsNullOrEmpty(labelName)) labelName = "Ổ đĩa";

                string stateDesc = "";
                if (hasTag && hasApp) stateDesc = "💖 Đã có App & Khoá định mệnh";
                else if (hasTag) stateDesc = "💖 Đã có Khoá định mệnh";
                else if (hasApp) stateDesc = "Đã có file App";
                else stateDesc = "Sẵn sàng cài đặt";

                string display = string.Format("{0} [{1}] ({2})", root, labelName, stateDesc);
                DriveItem item = new DriveItem { Root = root, Display = display, HasTag = hasTag, HasApp = hasApp, IsRemovable = isRemovable };
                cboDrives.Items.Add(item);

                // Ưu tiên chọn: ổ có tag trước, sau đó ổ E:\ hoặc ổ Removable
                if (hasTag && selectedIdx == 0) selectedIdx = currentIdx;
                else if (removableIdx == -1 && root.StartsWith("E:", StringComparison.OrdinalIgnoreCase)) removableIdx = currentIdx;
                else if (removableIdx == -1 && isRemovable) removableIdx = currentIdx;

                currentIdx++;
            }

            if (cboDrives.Items.Count > 0)
            {
                int finalIdx = 0;
                if (selectedIdx > 0 || (cboDrives.Items[0] as DriveItem).HasTag) finalIdx = selectedIdx;
                else if (removableIdx >= 0) finalIdx = removableIdx;

                cboDrives.SelectedIndex = finalIdx;
            }
            CheckSelectedDriveTag();
        }

        private bool CheckTagFile(string pathOrDir)
        {
            try
            {
                string tagPath = pathOrDir.EndsWith(".tag", StringComparison.OrdinalIgnoreCase) ? pathOrDir : Path.Combine(pathOrDir, "anhyeuempmt.tag");
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
                btnOpenFolder.Enabled = false;
                return;
            }

            string installDir = Path.Combine(item.Root, "QuinGM");
            string appPath = Path.Combine(installDir, "QuinGM luv Mthu Menu.exe");
            bool appExists = File.Exists(appPath) || File.Exists(Path.Combine(item.Root, "QuinGM luv Mthu Menu.exe"));
            btnOpenFolder.Enabled = Directory.Exists(installDir) || appExists;

            if (item.HasTag && appExists)
            {
                lblTagStatus.Text = "✅ Ổ đĩa " + item.Root + " đã có app & khoá định mệnh ẩn hợp lệ!\nBấm [CẬP NHẬT APP] nếu muốn cập nhật lại bản mới nhất.";
                lblTagStatus.ForeColor = Color.FromArgb(74, 222, 128);
                btnInstall.Text = "🔄 OK - CẬP NHẬT APP 💕";
            }
            else if (item.HasTag)
            {
                lblTagStatus.Text = "💖 Ổ đĩa " + item.Root + " đã có khoá định mệnh ẩn sẵn sàng.\nBấm [CÀI ĐẶT APP] để giải nén ứng dụng.";
                lblTagStatus.ForeColor = Color.FromArgb(245, 95, 140);
                btnInstall.Text = "✅ OK - BẮT ĐẦU CÀI ĐẶT APP 💕";
            }
            else
            {
                lblTagStatus.Text = "💡 Ổ đĩa " + item.Root + " sẵn sàng cài đặt. Trình cài đặt sẽ tự động tạo khoá định mệnh ẩn.";
                lblTagStatus.ForeColor = Color.FromArgb(200, 190, 210);
                btnInstall.Text = "✅ OK - BẮT ĐẦU CÀI ĐẶT APP 💕";
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
            }
            else
            {
                lblDesktopStatus.Text = "💻 Màn hình Desktop máy này: CHƯA CÓ PHÍM TẮT";
                lblDesktopStatus.ForeColor = Color.FromArgb(251, 191, 36);
            }
        }

        private void CreateHiddenTagFile(string tagPath)
        {
            try
            {
                if (File.Exists(tagPath))
                {
                    File.SetAttributes(tagPath, FileAttributes.Normal);
                }
                File.WriteAllText(tagPath, "Anh_Yeu_Em_Pham_Minh_Thu_Ksenia_Rin_Luv_U", System.Text.Encoding.UTF8);
                // Đặt thuộc tính Ẩn bí mật và Tàng hình (Hidden + System)
                File.SetAttributes(tagPath, FileAttributes.Hidden | FileAttributes.System);
            }
            catch { }
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            DriveItem item = cboDrives.SelectedItem as DriveItem;
            if (item == null) return;

            string driveRoot = item.Root;
            string installDir = Path.Combine(driveRoot, "QuinGM");

            btnInstall.Enabled = false;
            btnLaunch.Enabled = false;
            btnOpenFolder.Enabled = false;
            progressBg.Visible = true;
            progressFill.Width = 10;
            lblProgressText.Text = "Đang khởi tạo thư mục cài đặt: " + installDir + "...";

            Timer animTimer = new Timer { Interval = 20 };
            int step = 0;
            animTimer.Tick += (s, ev) => {
                step++;
                if (step == 8)
                {
                    try
                    {
                        if (!Directory.Exists(installDir)) Directory.CreateDirectory(installDir);
                    }
                    catch { }

                    progressFill.Width = 110;
                    lblProgressText.Text = "Đang giải nén QuinGM luv Mthu Menu.exe...";
                }
                else if (step == 22)
                {
                    // 1. Trích xuất file thực thi chính từ resource vào thư mục cài đặt QuinGM
                    try
                    {
                        string targetExe = Path.Combine(installDir, "QuinGM luv Mthu Menu.exe");
                        ExtractPayload("app_payload.bin", targetExe);
                    }
                    catch (Exception ex)
                    {
                        animTimer.Stop();
                        animTimer.Dispose();
                        MessageBox.Show("Lỗi khi ghi file app: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnInstall.Enabled = true;
                        return;
                    }

                    progressFill.Width = 240;
                    lblProgressText.Text = "Đang tạo công cụ Gỡ Cài Đặt bên trong thư mục cài đặt...";
                }
                else if (step == 34)
                {
                    // 2. Trích xuất file Gỡ Cài Đặt bên trong thư mục cài đặt QuinGM
                    try
                    {
                        string targetUninstaller = Path.Combine(installDir, "Gỡ Cài Đặt QuinGM.exe");
                        ExtractPayload("uninstall_payload.bin", targetUninstaller);
                    }
                    catch { }

                    progressFill.Width = 360;
                    lblProgressText.Text = "Đang tạo khoá định mệnh [anhyeuempmt.tag] ở chế độ ẨN (Hidden)...";
                }
                else if (step == 42)
                {
                    // 3. Tạo khoá định mệnh ẩn
                    if (chkCreateTag.Checked)
                    {
                        CreateHiddenTagFile(Path.Combine(installDir, "anhyeuempmt.tag"));
                        CreateHiddenTagFile(Path.Combine(driveRoot, "anhyeuempmt.tag"));
                    }

                    progressFill.Width = 440;
                    lblProgressText.Text = "Đang tạo lối tắt bản quyền ra màn hình Desktop...";
                }
                else if (step == 50)
                {
                    // 4. Tạo lối tắt Desktop
                    if (chkCreateShortcut.Checked)
                    {
                        try
                        {
                            string targetExe = Path.Combine(installDir, "QuinGM luv Mthu Menu.exe");
                            string shortcutPath = GetDesktopShortcutPath();

                            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                            if (shellType != null)
                            {
                                dynamic shell = Activator.CreateInstance(shellType);
                                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                                shortcut.TargetPath = targetExe;
                                shortcut.WorkingDirectory = installDir;
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
                else if (step >= 56)
                {
                    animTimer.Stop();
                    animTimer.Dispose();

                    btnInstall.Enabled = true;
                    btnLaunch.Enabled = true;
                    btnOpenFolder.Enabled = true;
                    RefreshDrives();
                    CheckDesktopStatus();

                    MessageBox.Show(
                        "🎉 ĐÃ CÀI ĐẶT ỨNG DỤNG THÀNH CÔNG!\n\n" +
                        "• Thư mục cài đặt: " + installDir + "\n" +
                        "• File ứng dụng: QuinGM luv Mthu Menu.exe\n" +
                        "• File gỡ cài đặt: Gỡ Cài Đặt QuinGM.exe (nằm bên trong thư mục cài đặt để bạn gỡ sạch bất kỳ lúc nào)\n" +
                        "• Khoá định mệnh [anhyeuempmt.tag] đã được tạo ở CHẾ ĐỘ ẨN (Hidden) để bảo mật tuyệt đối!\n" +
                        "• Phím tắt Desktop đã được tạo sẵn sàng!\n\n" +
                        "Bây giờ bạn có thể bấm 'Mở Game' để trải nghiệm ngay!",
                        "Cài Đặt Hoàn Tất",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            };
            animTimer.Start();
        }

        private void ExtractPayload(string resourceName, string targetPath)
        {
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
            using (Stream s = asm.GetManifestResourceStream(resourceName))
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

            // Fallback nếu chạy trực tiếp từ repo hoặc thư mục biên dịch
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (resourceName == "app_payload.bin")
            {
                string localExe = Path.Combine(baseDir, "QuinGM luv Mthu Menu.exe");
                if (File.Exists(localExe)) { File.Copy(localExe, targetPath, true); return; }

                string binExe = Path.Combine(baseDir, "bin", "GMMenu.exe");
                if (File.Exists(binExe)) { File.Copy(binExe, targetPath, true); return; }
            }
            else if (resourceName == "uninstall_payload.bin")
            {
                string localUn = Path.Combine(baseDir, "Gỡ Cài Đặt QuinGM.exe");
                if (File.Exists(localUn)) { File.Copy(localUn, targetPath, true); return; }

                string binUn = Path.Combine(baseDir, "bin", "Uninstall.exe");
                if (File.Exists(binUn)) { File.Copy(binUn, targetPath, true); return; }
            }

            throw new FileNotFoundException("Không tìm thấy tài nguyên nhúng: " + resourceName);
        }

        private void OpenInstallFolder()
        {
            DriveItem item = cboDrives.SelectedItem as DriveItem;
            string driveRoot = (item != null) ? item.Root : "E:\\";
            string installDir = Path.Combine(driveRoot, "QuinGM");

            if (!Directory.Exists(installDir))
            {
                installDir = driveRoot;
            }

            try
            {
                Process.Start("explorer.exe", installDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở thư mục: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LaunchApp()
        {
            DriveItem item = cboDrives.SelectedItem as DriveItem;
            string driveRoot = (item != null) ? item.Root : "E:\\";
            string installDir = Path.Combine(driveRoot, "QuinGM");
            string exePath = Path.Combine(installDir, "QuinGM luv Mthu Menu.exe");

            if (!File.Exists(exePath))
            {
                exePath = Path.Combine(driveRoot, "QuinGM luv Mthu Menu.exe");
            }
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
                MessageBox.Show("Chưa cài đặt app vào ổ " + driveRoot + "!\nVui lòng bấm 'OK - BẮT ĐẦU CÀI ĐẶT APP' trước khi mở.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
