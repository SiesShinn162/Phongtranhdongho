# 🏛️ Không Gian Triển Lãm Tranh Dân Gian Đông Hồ

Dự án mô phỏng không gian bảo tàng triển lãm nghệ thuật tranh dân gian Đông Hồ truyền thống Việt Nam, được xây dựng trên nền tảng **Unity 6 (6000.5.9f1)** 

---

## 📋 Yêu Cầu Hệ Thống

* **Unity Hub:** Phiên bản mới nhất.
* **Phiên bản Unity:** **Unity 6 (6000.5.9f1)** hoặc các bản **Unity 6000.x** tương đương.
* **Render Pipeline:** Universal Render Pipeline (URP) - Đã được cấu hình sẵn trong project.

---

## 📥 Hướng Dẫn Tải Dự Án Từ GitHub

Bạn có thể lựa chọn 1 trong 2 cách sau để tải project về máy tính:

### Cách 1: Dùng Git CLI (Khuyên dùng)
1. Mở **Terminal** / **Git Bash** / **Command Prompt** trên máy tính.
2. Di chuyển đến thư mục bạn muốn lưu dự án và chạy lệnh:
```bash
git clone https://github.com/SiesShinn162/Phongtranhdongho.git
```

### Cách 2: Tải trực tiếp file ZIP
1. Truy cập trang repository: [https://github.com/SiesShinn162/Phongtranhdongho](https://github.com/SiesShinn162/Phongtranhdongho).
2. Nhấn vào nút màu xanh **`<> Code`** ở góc trên bên phải.
3. Chọn **Download ZIP**.
4. Sau khi tải về, giải nén file `.zip` vào một thư mục trên máy tính (ví dụ: `D:\UnityProjects\Phongtranhdongho`).

---

## 🚀 Cách Thêm Dự Án Vào Unity Hub & Khởi Chạy

### Bước 1: Thêm dự án vào Unity Hub
1. Khởi động ứng dụng **Unity Hub**.
2. Tại mục **Projects**, nhấn vào mũi tên cạnh nút **Add** (hoặc bấm nút **Add**) ở góc trên bên phải.
3. Chọn **Add project from disk**.
4. Điều hướng và chọn đúng thư mục chứa dự án vừa tải về (thư mục chứa các thư mục con `Assets`, `Packages`, `ProjectSettings`).
5. Nhấn **Add Project**. Dự án sẽ xuất hiện trong danh sách của Unity Hub.

### Bước 2: Kiểm tra phiên bản Unity Editor
* Tại cột **Editor Version** của dự án trên Unity Hub, hãy đảm bảo chọn phiên bản **Unity 6 (6000.5.9f1)** hoặc phiên bản Unity 6 đã cài trên máy bạn.
* Nếu chưa có Unity 6, Unity Hub sẽ hiện biểu tượng cảnh báo màu vàng, bạn chỉ cần nhấn vào và chọn **Install** để Unity Hub tự động tải bản phù hợp.

### Bước 3: Mở dự án
* Nhấp chuột vào tên dự án trên Unity Hub để mở bằng Unity Editor.
* *Lưu ý:* Trong lần đầu tiên mở dự án, Unity sẽ mất khoảng **2 - 5 phút** để tải các Package URP, import asset và biên dịch Shader. Hãy kiên nhẫn đợi cho đến khi giao diện Editor tải xong hoàn toàn.

### Bước 4: Mở Scene triển lãm và bấm Play
1. Trong cửa sổ **Project** (ở góc dưới giao diện Unity), điều hướng theo đường dẫn:
   ```
   Assets > Scenes
   ```
2. Nhấp đúp chuột vào file Scene chính: **`PhongTrienLam.unity`** (hoặc `PhongTrienLam`).
3. Nhấn nút **Play** (biểu tượng hình tam giác ▶️ ở giữa thanh công cụ trên cùng của Unity Editor) để bắt đầu trải nghiệm!

---

## 🎮 Hướng Dẫn Cách Chơi & Tương Tác

Dự án hỗ trợ song song 2 cơ chế trải nghiệm: **Màn hình máy tính (3D Desktop)** và **Kính thực tế ảo (VR)**. Game tự động nhận diện thiết bị khi khởi chạy hoặc cho phép chuyển đổi tức thì bằng phím tắt.

### 🖥️ 1. Chế Độ 3D Máy Tính (Desktop PC / Laptop)

Dành cho người dùng trải nghiệm bằng chuột và bàn phím thông thường:

| Thao tác | Phím / Chuột | Chức năng |
| :--- | :---: | :--- |
| **Di chuyển** | W, A, S, D | Đi tới, lùi, di chuyển sang trái, sang phải |
| **Xoay góc nhìn** | Di chuyển Chuột | Xoay góc nhìn 360 độ tự do trong không gian phòng tranh |
| **Khóa / Mở chuột** | Click Chuột Trái / ESC | Nhấp chuột để khóa tâm nhìn vào màn hình; bấm ESC để hiện lại con trỏ chuột |
| **Xem chi tiết tranh** | Phím E hoặc Click Chuột | Hướng tâm nhìn vào bức tranh và nhấn phím để mở bảng thông tin giấy dó |
| **Nghe thuyết minh** | Click nút Nghe thuyết minh | Phát giọng đọc thuyết minh và giới thiệu ý nghĩa văn hóa của tác phẩm |
| **Đóng bảng tranh** | Phím ESC hoặc Nút Đóng | Đóng bảng thông tin và tiếp tục di chuyển tham quan |
| **Đổi chế độ thủ công** | Phím F1 | Chuyển đổi qua lại giữa chế độ Desktop và chế độ VR |

---

### 🥽 2. Chế Độ Thực Tế Ảo (Virtual Reality - VR)

Dành cho người chơi sử dụng kính thực tế ảo (Meta Quest 2, Quest 3, Quest 3S, Quest Pro qua Quest Link / AirLink hoặc các kính PCVR qua SteamVR / OpenXR):

* **Kết nối & Nhận diện:**
  * Bật phần mềm kết nối kính (Oculus App / Quest Link / SteamVR) trước khi bấm Play trong Unity hoặc chạy file build .exe.
  * Hệ thống tích hợp sẵn script tự động phát hiện kính VR và kích hoạt hệ thống **XR ORIGIN**.

| Thao tác trên Tay Cầm VR | Phím / Nút bấm | Chức năng |
| :--- | :---: | :--- |
| **Di chuyển không gian** | Cần gạt Analog Trái (Left Thumbstick) | Di chuyển tiến/lùi và sang ngang mượt mà trong phòng tranh |
| **Xoay chuyển hướng** | Cần gạt Analog Phải (Right Thumbstick) | Xoay hướng nhìn nhanh (Snap/Smooth Turn) mà không gây chóng mặt |
| **Ngắm chọn tranh** | Tia Laser Tay Phải (Controller Ray) | Chỉ tia laser từ tay cầm phải vào mặt khung tranh để chọn tranh |
| **Phát thuyết minh** | Nút Cò Ngón Trỏ Phải (Right Trigger) | Bóp cò ngón trỏ để kích hoạt ngay bài thuyết minh âm thanh của tranh |
| **Nhạc nền hòa tấu** | Tự động phát 2D Stereo | Âm hưởng nhạc cụ truyền thống dân tộc nhẹ nhàng bao trùm toàn không gian |

---

## 🏛️ Nội Dung Trưng Bày Trong Triển Lãm

* **49 tác phẩm tranh Đông Hồ tiêu biểu** thuộc 3 chủ đề đặc sắc:
  * 🎋 **Chúc tụng:** *Tiến Tài, Tiến Lộc, Gà Đại Cát, Đàn Lợn Âm Dương, Em Bé Ôm Gà (Vinh Hoa), Em Bé Ôm Vịt (Phú Quý), Tam Dương Khai Thái...*
  * 🌾 **Sinh hoạt dân gian:** *Đám Cưới Chuột, Hứng Dừa, Đánh Ghen, Hội Đu, Chăn Trâu Thổi Sáo, Kéo Co, Tố Nữ...*
  * ⚔️ **Lịch sử & Huyền thoại:** *Bà Triệu Cưỡi Voi, Thánh Gióng, Vinh Quy Bái Tổ, Ngô Quyền, Quang Trung...*
* **23 file âm thanh thuyết minh bản ngữ** giúp người xem hiểu sâu sắc ý nghĩa biểu trưng, ước vọng thái bình, thịnh vượng và mỹ cảm văn hóa dân gian Việt Nam.

