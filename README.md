# ⚡ QuinGM luv Mthu Menu (Portable Gaming & Utility Launcher)

> **MENU TRÒ CHƠI & CÔNG CỤ DI ĐỘNG CAO CẤP DÀNH CHO Ổ CỨNG / USB DI ĐỘNG**  
> **100% ĐỘC LẬP • KHÔNG CẦN CÀI ĐẶT • ZERO GHI VÀO Ổ C: • TÍCH HỢP CHỨC NĂNG PMT CLICK • THANH CUỘN NEON ĐẲNG CẤP**

---

## 📍 1. Vị Trí Cố Định Của Dự Án

Toàn bộ dự án này được lưu trữ và thực thi cố định tại thư mục:
```
E:\code\code\
```

### Danh Mục Các File Chính Trong Thư Mục `E:\code\code\`:
| Tên File / Thư Mục | Ý Nghĩa / Chức Năng |
| :--- | :--- |
| 🚀 **`QuinGM luv Mthu Menu.exe`** | **File chạy chính của Menu** (Click đúp để mở ngay, dùng chung icon với PMT Click). |
| 📱 **`PMT Click Hub`** | **Chức năng tích hợp chính trong Menu** (Điều khiển PC từ điện thoại, chạy nền liên tục). |
| 📋 **`games.json`** | File cấu hình chứa danh sách tất cả các Game và Ứng dụng. |
| 📖 **`README.md`** | Hướng dẫn sử dụng và tài liệu chi tiết (File này). |
| 📄 **`HuongDanSuDung.txt`** | Hướng dẫn nhanh dạng text dễ đọc trên Notepad. |
| 🎨 **`app.ico`** | Icon nhận diện chính thống của ứng dụng (dùng chung với PMT Click). |
| 🛠️ **`build.bat`** | File script biên dịch lại Menu từ mã nguồn C# thuần (.NET 4.0). |
| 💻 **`src\Native\NativeApp.cs`** | Toàn bộ mã nguồn C# thuần (GDI+ 60 FPS, Theme Neon Auburn, Custom NeonScrollBar, PMT Click Manager). |
| 📁 **`bin\`** | Thư mục chứa file binary sau khi biên dịch. |
| 📁 **`data\`** | Thư mục cô lập dữ liệu người dùng (`Roaming`, `Local`, `UserProfile`) để không chạm ổ C. |

---

## 📱 2. Chức Năng Tích Hợp: PMT Click

Dự án `E:\code\phone_pc_keyboard` đã được chuyển đổi thành **chức năng cốt lõi (Built-in Feature)** bên trong `QuinGM luv Mthu Menu`:
- **Gọn gàng trên thanh điều hướng**: Đã dọn nút thừa trên header; tab **`📱 PMT Click`** chính thống nằm ngay trên thanh bộ lọc (Filter Bar).
- **Chạy nền liên tục (Persistent Background)**: Server WebSocket & HTTP chạy ngầm cổng `5000`, chuyển tab chơi game thoải mái không lo ngắt kết nối.
- **Tính năng đầy đủ**: Bật/tắt server, mở Web Controller, mở giao diện nâng cao, sao chép link Wi-Fi LAN và mở file cài `PMT_Click.apk`.

---

## 🎨 3. Giao Diện Responsive & Hệ Thống Tooltip / Modal Neon Tùy Biến

1. **Bố Cục Căn Đều Tuyệt Đối (Responsive Grid)**:
   - Thẻ game tự động co giãn theo chiều rộng cửa sổ.
   - Luôn lấp đầy 100% không gian hàng, **xóa bỏ triệt để khoảng trống thừa bên phải**.
2. **Hộp Kiểm (Checkbox) & Xóa Thẻ Hàng Loạt**:
   - Ô tick Neon ở góc trên bên trái từng thẻ, sáng viền cam rực rỡ khi được chọn.
   - Nút **`☑ Chọn Tất Cả`** và **`🗑️ Xóa Đã Chọn (N)`** tích hợp trực tiếp trên Header.
   - Hộp thoại xác nhận tùy biến **`NeonMessageBox`** phong cách Auburn sang trọng.
3. **Hệ Thống Tooltip & Modal Hover Cao Cấp (`NeonToolTipPopup`)**:
   - Thay thế toàn bộ ToolTip xám mặc định của Windows Forms bằng cửa sổ nổi kính mờ Auburn viền Neon.
   - Rê chuột vào thân thẻ: Hiển thị bảng chi tiết gồm Icon, Tên, Trạng thái tệp trên ổ E:, Thể loại, Mô tả, Lượt mở và Phím tắt hướng dẫn.
   - Rê chuột vào 3 nút chức năng ở góc trên phải (`📁 Mở vị trí tệp`, `✏ Sửa thẻ`, `✕ Xóa thẻ`) hoặc ô tick / nút chạy: Hiển thị tooltip mini riêng biệt, chuẩn xác và đồng bộ phong cách Neon.
4. **Thanh Cuộn Neon (`NeonScrollBar`)**:
   - Thanh cuộn siêu mảnh (10px) màu Auburn `#10080A` phát sáng Neon cam ánh san hô.

---

## 🚀 4. Cách Sử Dụng Nhanh

1. Mở ổ đĩa di động `E:\`.
2. Vào thư mục `E:\code\code\`.
3. Click đúp vào file `QuinGM luv Mthu Menu.exe`.
4. Nếu cần điều khiển PC bằng điện thoại: Bấm vào **`📱 PMT Click Hub`** và nhấn **`Khởi Động Dịch Vụ`**.
5. Muốn quay lại chơi game: Bấm chọn bất kỳ danh mục nào (`⭐ Tất Cả`, `🔥 Game Hot`, `🎮 Game Offline`...) — PMT Click vẫn chạy ngầm mượt mà!

---
*QuinGM luv Mthu Menu — Bản quyền thuộc về QuinGM & Mthu.*
