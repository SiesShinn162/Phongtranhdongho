# Thiết Kế Kiến Trúc Mái Ngói Chùa Việt Nam & Mặt Sân Gạch 3 Phần (Phòng Triển Lãm Tranh Đông Hồ)

**Ngày lập:** 2026-09-21  
**Môi trường:** Unity 6000.5.9f1 (Unity 6) URP, Scene `Assets/Scenes/PhongTrienLam.unity`  
**Tài liệu tham chiếu:** `tutor.png`  

---

## 1. Tổng Quan & Mục Tiêu

Nâng cấp diện mạo ngoại thất của phòng triển lãm tranh Đông Hồ theo đúng phong cách kiến trúc đình/chùa truyền thống Việt Nam dựa trên tài liệu tham khảo `tutor.png`, bao gồm hai hạng mục chính:
1. **Hệ thống Mái ngói 4 dốc xếp lớp (Layered Roof) & Đầu đao cong:**
   - Sử dụng model 3D mảng ngói `VietnameseAsianRoof` xếp chồng lớp (overlap) mí nhau từ đỉnh bờ nóc xuống chân mép mái theo 4 hướng dốc.
   - Dựng bờ dải viền mái và vuốt cong nhẹ 4 đầu đao ở góc bằng ProBuilder.
   - Mép mái nhô ra ngoài bờ tường $0.4\text{ m}$ tạo hiên che truyền thống.
2. **Phân chia mặt sân (Ground Plane) thành 3 phần & Xử lý Texture gạch PBR chân thực:**
   - Tách mặt sân thành 3 tấm mesh phẳng ghép khít: 2 bên là sân gạch đỏ Bát Tràng, ở giữa là lối đi lát đá xám rộng $7.0\text{ m}$ dẫn thẳng vào cửa chính.
   - Tạo Normal map bằng tính năng tích hợp sẵn của Unity (*Create from Grayscale*), cân chỉnh Tiling và Smoothness để hiển thị rõ khối từng viên gạch thật và khe rãnh mạch vữa.

---

## 2. Thiết Kế Chi Tiết Hạng Mục 1: Mái Ngói 4 Dốc & Đầu Đao Cong

### 2.1. Cấu Trúc Hình Học & Tọa Độ
- **Kích thước khung nhà hiện tại:** Rộng $X = 24.5\text{ m}$, Dài $Z = 36.5\text{ m}$, cao độ đỉnh tường $Y = 8.82\text{ m}$.
- **Dáng mái:** Mái 4 dốc úp truyền thống (2 dốc chính trước/sau theo trục $Z$, 2 chái hồi hai đầu theo trục $X$).
  - Góc nghiêng dốc: $\approx 32^\circ$.
  - Cao độ bờ nóc (đỉnh mái): $Y \approx 13.5 - 14.0\text{ m}$.
  - Độ nhô mép hiên mái: $0.4\text{ m}$ vượt ra ngoài mép tường bao (kích thước phủ bì mép mái: rộng $X \approx 25.3\text{ m}$, dài $Z \approx 37.3\text{ m}$, cao độ mép hiên $Y \approx 8.5\text{ m}$).
- **Khung đỡ mái (Roof Underlay):** Điều chỉnh `RoofUnderlay` làm bệ đỡ kín phía dưới các lớp ngói, ngăn lọt sáng vào bên trong phòng tranh.

### 2.2. Xếp Lớp Ngói 3D (Layered Roof Tiles)
- **Asset sử dụng:** Prefab `Assets/Gallery/RoofNative/VietnameseAsianRoof.prefab` (chứa các chi tiết ngói vảy cá / ngói mũi tên chuẩn kiến trúc Việt).
- **Quy tắc xếp chồng (Overlap):**
  - Bố trí các hàng ngói theo từng bậc từ đỉnh nóc xuống chân hiên theo từng hướng dốc.
  - Hàng trên đè nhẹ lên mép hàng dưới ($\approx 15 - 20\%$ chiều dài mảng ngói) tạo độ gối mí tự nhiên, vừa che kín mái vừa tạo cảm giác xếp lớp dày dặn, đổ bóng rõ từng hàng ngói khi ánh sáng chiếu vào.

### 2.3. Bờ Dải, Bờ Nóc & Đầu Đao Cong (ProBuilder)
- **Bờ nóc chính (Ridge Cap):** Thanh gờ nóc thẳng chạy dọc đỉnh mái theo trục $Z$.
- **4 Bờ dải (Corner Hip Ridges):** Chạy chéo từ góc bờ nóc xuống 4 góc mép hiên mái.
- **Đầu đao cong ở 4 góc:**
  - Tại 4 góc mép hiên, thanh bờ dải được vuốt cong nhẹ vểnh lên trên (bán kính cong thoải, nâng cao mũi đao $\approx 0.35\text{ m}$ so với mép hiên).
  - Sử dụng ProBuilder để tạo mesh dải viền và bo góc.
- **Vật liệu bờ dải & đầu đao:** Gán material vữa trát cổ màu xám trắng nhẹ (tương phản thẩm mỹ với ngói đỏ nung theo đúng hình 4 của `tutor.png`).

### 2.4. Material Ngói Lợp (`Roof_Brick.mat`)
- Tông màu: Đỏ cam đất nung ấm áp.
- Normal Map & Độ nhám: Bổ sung normal map sần nhẹ, thiết lập `_Smoothness = 0.2` để bắt sáng tự nhiên, không bóng nhựa.

---

## 3. Thiết Kế Chi Tiết Hạng Mục 2: Mặt Sân 3 Phần & Texture Gạch PBR

### 3.1. Phân Chia Mặt Sân (3 Meshes Ghép Khít)
Thay thế đối tượng `Plane` đơn lẻ hiện tại bằng nhóm GameObject `Ground_Yard` tại cao độ $Y = 3.74\text{ m}$:
1. **Lối Đi Giữa (`Yard_Center_Walkway`):**
   - Vị trí: Căn giữa theo cửa chính phòng tranh (tâm $Z = 0$, trải dài từ thềm cửa $X \approx 12.25\text{ m}$ ra mép ngoài sân $X \approx 51.9\text{ m}$).
   - Bề rộng: $7.0\text{ m}$ (từ $Z = -3.5\text{ m}$ đến $Z = +3.5\text{ m}$).
   - Material: `San_Da_Xam.mat` (đá xám tự nhiên).
2. **Sân Gạch Đỏ Trái (`Yard_Left_RedBrick`):**
   - Trải rộng từ mép trái của lối đi giữa ($Z = +3.5\text{ m}$) ra hết biên sân bên trái ($Z \approx +49.7\text{ m}$).
   - Material: `San_Gach_Do.mat` (gạch nung đỏ Bát Tràng).
3. **Sân Gạch Đỏ Phải (`Yard_Right_RedBrick`):**
   - Trải rộng từ mép phải của lối đi giữa ($Z = -3.5\text{ m}$) ra hết biên sân bên phải ($Z \approx -44.1\text{ m}$).
   - Material: `San_Gach_Do.mat`.

### 3.2. Xử Lý Texture Gạch Thật (Unity Built-in Workflow)
- **Tạo Normal Map từ Grayscale:**
  - Nhân bản `Assets/Gallery/Textures/san_gach.jpg` $\rightarrow$ `san_gach_Normal.jpg`.
  - Nhân bản `Assets/Gallery/Textures/san da.jpg` $\rightarrow$ `san_da_Normal.jpg`.
  - Thiết lập trong `TextureImporter`:
    - `textureType = TextureImporterType.NormalMap`
    - `convertToNormalMap = true` (Create from Grayscale)
    - `normalStrength = 0.35` (tạo độ sâu rõ rệt cho rãnh vữa và bề mặt hạt gạch/đá)
    - `apply()`
- **Cấu hình Material URP Lit:**
  - `San_Gach_Do.mat`:
    - `_BaseMap`: `san_gach.jpg`
    - `_BumpMap`: `san_gach_Normal.jpg`
    - `_BumpScale`: $1.0$
    - `_Smoothness`: $0.18$ (bề mặt lì, sần nhẹ của gạch nung)
    - Tiling UV: Căn tỉ lệ kích thước viên gạch ngoài đời thực $\approx 0.35\text{ m} \times 0.35\text{ m}$.
  - `San_Da_Xam.mat`:
    - `_BaseMap`: `san da.jpg`
    - `_BumpMap`: `san_da_Normal.jpg`
    - `_BumpScale`: $1.0$
    - `_Smoothness`: $0.22$
    - Tiling UV: Căn tỉ lệ kích thước phiến đá lát ngoài đời thực $\approx 0.5\text{ m} \times 0.5\text{ m}$.

---

## 4. Kế Hoạch Kiểm Thử & Nghiệm Thu (Verification)

1. **Kiểm tra hình ảnh trực quan (Visual Inspection):**
   - Dùng MCP `screenshot-scene-view`, `screenshot-game-view` và `screenshot-camera` để kiểm tra từ nhiều góc nhìn (góc nhìn phối cảnh từ trên cao như hình 1 `tutor.png`, góc nhìn chính diện sảnh vào, và góc nhìn từ góc mái kiểm tra đầu đao).
2. **Kiểm tra độ gối mí của ngói:**
   - Đảm bảo các hàng ngói không bị hở khe, không thấy phần bệ bên dưới xuyên qua, các mép ngói xếp chồng liên tục từ đỉnh xuống chân hiên.
3. **Kiểm tra mặt sân & gạch:**
   - 3 tấm plane ghép khít $100\%$, không có kẽ hở giữa lối đi đá xám và sân gạch đỏ.
   - Dưới ánh sáng Directional Light, các rãnh vữa gạch và mặt đá nổi khối 3D rõ ràng, không còn hiện tượng phẳng lì như giấy dán.
4. **Kiểm tra di chuyển (Player Controller):**
   - PlayerCapsule di chuyển mượt mà trên cả 3 phần sân, không bị vướng va chạm (Collider phẳng đều ở $Y = 3.74$).
