ê# ⚡ PMT Click - Phone to PC Keyboard, Mouse & Screen Remote (100% Standalone Portable)

<p align="center">
  <img src="logo.png" width="96" height="96" alt="PMT Click Logo">
  <br>
  <b>PMT Click</b> - Ứng dụng biến điện thoại (Android / iOS) thành <b>Bàn phím cơ</b>, <b>Bàn di chuột Trackpad đa điểm</b> và <b>Màn hình phụ mini 60 FPS</b> điều khiển máy tính Windows siêu mượt qua Wi-Fi hoặc Internet (4G/5G).
</p>

---

## 🌟 Tính năng nổi bật

### 1. 🔑 Mã ID Kết Nối 6 Số (Phong cách UltraViewer)
- Mỗi khi bật server trên PC, ứng dụng tự động sinh một **Mã ID 6 số ngẫu nhiên** (ví dụ: `159 676`).
- Người dùng chỉ cần nhập 6 số này vào điện thoại là có thể kết nối ngay lập tức qua mạng 4G/5G hoặc bất cứ đâu trên thế giới.

### 2. 📶 Tự Động Quét & Dò  Tìm PC Trong Mạng Wi-Fi (LAN Discovery)
- Tích hợp công nghệ UDP Beacon ngầm: Khi điện thoại mở app/web trong cùng mạng Wi-Fi, máy tính sẽ **tự động hiện lên danh sách kết nối** chỉ với **1 chạm**.

### 3. 📥 1-Click Tải File APK Về Điện Thoại Thông Minh
- **Mã QR Tải APK trực tiếp**: Quét mã QR trên tool PC bằng camera điện thoại để tự động tải `PMT_Click.apk` về máy.
- **Link tải tự động**: Truy cập `http://<ip-máy-tính>:5000/apk` trên trình duyệt điện thoại để tải ngay file APK bản mới nhất (chống lưu đệm cache tuyệt đối).

### 4. 🤖 Ứng Dụng Android Native APK Hoàn Chỉnh (`pmt_click_android/`)
- Khóa xoay ngang và chế độ toàn màn hình không viền (**Immersive Mode**).
- **Giữ sáng màn hình tuyệt đối (`FLAG_KEEP_SCREEN_ON`)**: Không bao giờ bị tắt màn hình khi đang làm việc hoặc chơi game.
- **Rung phản hồi phần cứng (Haptic Feedback)**: Cảm giác gõ chân thực như bàn phím cơ.
- **Tự động nhắc cập nhật trong App (Auto Updater)**: Tự nhận diện phiên bản mới từ PC và cập nhật chỉ với 1 chạm.

### 5. ⚡ Phản Hồi Tức Thì (< 1ms) & Truyền Màn Hình 60 FPS
- Sử dụng Native Win32 API (`SendInput`, `mouse_event`, `Direct StretchBlt`) và luồng WebSocket nhị phân tối ưu hóa độ trễ về gần như bằng 0.
- Truyền hình ảnh màn hình thời gian thực mượt mà với GPU canvas tăng tốc phần cứng.

### 6. ⌨️ Bàn Phím PC Đầy Đủ (Hacker's Keyboard) & Trackpad Đa Điểm
- Bàn phím QWERTY chuẩn, hàng số, phím điều hướng mũi tên (▲ ▼ ◀ ▶), Esc, Tab, Caps, Enter, Delete, PrtSc.
- Hàng phím chức năng mở rộng **F1 - F12**, Home, End, PgUp, PgDn, Ins.
- **Phím dính (Sticky Modifiers)**: Hỗ trợ ấn `Ctrl`, `Alt`, `Shift`, `Win` 1 chạm để gõ tổ hợp phím dễ dàng.
- **Phím tắt nhanh**: Nút bấm 1 chạm cho `Ctrl+C`, `Ctrl+V`, `Ctrl+Z`, `Ctrl+A`, `Alt+Tab`, `Win+D`.
- **Trackpad cảm ứng**: 1 ngón di chuột / chạm chuột trái, 2 ngón chạm chuột phải, vuốt 2 ngón cuộn trang siêu mượt.
- **Chế độ Ẩn 100% (Stealth Mode)**: Ẩn toàn bộ phím/chuột để xem trọn vẹn màn hình PC, chạm icon con mắt `👁️` để hiện lại ngay.

---

## 🚀 Hướng dẫn sử dụng cực nhanh

### Cách 1: Sử Dụng App Android APK (Khuyên Dùng Cho Trải Nghiệm Mượt Nhất)
1. Trên PC, nhấp đúp vào **`pmt_click.exe`** để mở phần mềm.
2. Dùng **Camera điện thoại quét Mã QR** trên giao diện PC -> File `PMT_Click.apk` sẽ tự động tải về máy -> Mở và chọn **Cài đặt**.
3. Mở app **PMT Click** trên điện thoại:
   - **Cùng mạng Wi-Fi**: App sẽ tự động quét và hiện tên máy tính -> Bấm **`👉 KẾT NỐI`**.
   - **Từ xa (4G/5G)**: Nhập **Mã ID 6 số** (hiển thị trên PC) -> Bấm **`🚀 KẾT NỐI TỪ XA`**.

### Cách 2: Sử Dụng Trực Tiếp Trên Trình Duyệt Web (iOS / Android / Không Cần Cài App)
1. Mở **`pmt_click.exe`** trên máy tính.
2. Nhập địa chỉ IP Wi-Fi (hoặc Link Public 4G/5G) vào trình duyệt điện thoại.
3. Xoay ngang màn hình điện thoại và bắt đầu điều khiển máy tính.

---

## 📁 Cấu trúc thư mục dự án

```text
phone_pc_keyboard/
├── pmt_click.exe         # Ứng dụng Desktop chạy ngay 1-click (Đã nhúng sẵn HTML/CSS/JS/APK)
├── App.cs                # Mã nguồn C# All-in-One (WebServer, WebSocket, Win32 Input, QR, GUI)
├── PMT_Click.apk         # Bộ cài đặt Android APK hoàn chỉnh
├── app.ico               # Icon chính thức của PMT Click
├── logo.png              # Logo chính thức của PMT Click
├── cloudflared.exe       # Công cụ tạo đường truyền kết nối từ xa qua 4G/5G
├── static/               # Giao diện Web Frontend & Trang tải APK
│   ├── index.html        # Giao diện bàn phím, trackpad & Connection Hub
│   ├── style.css         # Phong cách Cyberpunk Dark Mode hiện đại
│   ├── script.js         # Xử lý cảm ứng Touch đa điểm, WebSocket & Auto-discovery
│   ├── apk_download.html # Trang tự động tải APK cho điện thoại
│   ├── manifest.json     # Cấu hình PWA Web App
│   ├── favicon.ico       # Favicon web
│   └── logo.png          # Logo web
├── pmt_click_android/    # Dự án Android Studio Native
│   ├── app/src/main/     # Mã nguồn Java (MainActivity, LanDiscovery, AutoUpdater)
│   └── build.gradle      # Cấu hình build Android Gradle
└── README.md             # Tài liệu hướng dẫn sử dụng chi tiết
```

---

## 🛠️ Hướng dẫn Biên dịch (Dành cho Lập trình viên)

Lệnh biên dịch file EXE độc lập từ mã nguồn C# và tài nguyên nhúng:

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /t:winexe /o+ /win32icon:app.ico /res:app.ico,app.ico /res:static\logo.png,logo.png /res:static\favicon.ico,favicon.ico /res:static\index.html,index.html /res:static\style.css,style.css /res:static\script.js,script.js /res:static\manifest.json,manifest.json /res:static\apk_download.html,apk_download.html /res:PMT_Click.apk,PMT_Click.apk /out:pmt_click.exe App.cs
```

---

## 🛡️ Khắc phục sự cố thường gặp (Troubleshooting)

1. **Điện thoại không vào được link Wi-Fi:**
   - Đảm bảo điện thoại và máy tính kết nối chung mạng Wi-Fi (hoặc phát Hotspot từ điện thoại sang PC).
   - Hoặc chuyển sang dùng **Mã ID Kết Nối Từ Xa 6 Số / Link Public (4G/5G)** trên ứng dụng.
2. **Một số game / phần mềm không nhận phím:**
   - Nhấp chuột phải vào `pmt_click.exe` và chọn **"Run as Administrator"** để cấp đủ quyền gửi phím cho các game và ứng dụng chạy quyền Admin.
