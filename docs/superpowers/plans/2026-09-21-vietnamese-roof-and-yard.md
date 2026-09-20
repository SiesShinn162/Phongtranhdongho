# Vietnamese Roof & 3-Part Yard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xây dựng hệ thống mái ngói 4 dốc xếp lớp truyền thống Việt Nam có đầu đao cong và phân chia mặt sân thành 3 dải gạch PBR chân thực (2 bên gạch đỏ, giữa đá xám 7m) cho phòng triển lãm tranh Đông Hồ.

**Architecture:** Mái nhà được phân tầng rõ ràng thành 3 lớp độc lập: (1) Underlay kín chống lọt sáng/Z-fighting, (2) Lớp ngói 3D xếp chồng mí được gom cụm/strip tối ưu hiệu năng, và (3) Hệ bờ dải, bờ nóc cùng 4 đầu đao vuốt cong dựng bằng ProBuilder. Mặt sân gồm 3 mesh visual ghép khít nhưng dùng 1 Collider liên tục thống nhất để tránh vấp mí chuyển động.

**Tech Stack:** Unity 6000.5.9f1 URP, ProBuilder, ReflectorNet / Unity C# API, URP Lit PBR Shader.

**Spec:** [`docs/superpowers/specs/2026-09-21-vietnamese-roof-and-yard-design.md`](file:///d:/UNITY-PROJECT/My%20project%20(1)/docs/superpowers/specs/2026-09-21-vietnamese-roof-and-yard-design.md)

## Global Constraints

- **Tọa độ & Kích thước:** Tất cả kích thước trong spec là baseline; Phase 0 phải đo đạc chính xác từ runtime scene.
- **Hình học mái:** Không khóa đồng thời cả góc dốc $32^\circ$ và chiều cao nóc; chiều cao nóc $H$ phải được tính toán linh hoạt theo footprint thực tế và góc dốc: $H = (EaveWidth / 2) \times \tan(\theta)$.
- **Chống lọt sáng & Z-Fighting:** Khung Underlay phải khép kín hoàn toàn nóc phòng tranh và đặt thấp hơn mặt đáy của ngói từ $0.05\text{ m} - 0.1\text{ m}$.
- **Tối ưu hiệu năng:** Tuyệt đối không sinh ra hàng nghìn GameObject ngói rời rạc; gom cụm theo dải (strips) hoặc sử dụng Prefab cluster để kiểm soát số lượng draw calls / batches.
- **Cổng phê duyệt bắt buộc (Human Approval Gate):** Chỉ chuyển từ 1 đầu đao sang 3 đầu đao còn lại sau khi người dùng trực tiếp xem screenshot và phê duyệt dáng cong.
- **Collider mặt sân:** 3 Mesh visual riêng biệt nhưng dùng 1 Collider phẳng liên tục duy nhất tại $Y = 3.74\text{ m}$.
- **Quy tắc dừng (STOP Condition):** Mỗi Phase bắt buộc có tiêu chí dừng rõ ràng; nếu checkpoint thất bại, không được chuyển sang Phase tiếp theo.

---

### Task 0: Phase 0 - Đo Đạc Bounds & Trắc Đạc Kích Thước Thực Tế Từ Scene (Read-Only)

**Files:**
- Create: `Assets/Script/Editor/MeasureSceneBounds.cs` (Tool đo đạc tạm thời)

**Interfaces:**
- Consumes: Scene `PhongTrienLam.unity` đang mở.
- Produces: Báo cáo số liệu thực tế về Footprint tường, cao độ đỉnh tường, vị trí cửa, cấu trúc prefab `VietnameseAsianRoof`.

- [ ] **Step 1: Viết script Editor đo đạc chính xác kích thước tường và prefab ngói**

```csharp
using UnityEngine;
using UnityEditor;

public class MeasureSceneBounds
{
    public static string Run()
    {
        var gallery = GameObject.Find("PhongTrienLam_Gallery");
        if (gallery == null) return "ERROR: PhongTrienLam_Gallery not found";

        var renderers = gallery.GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds(gallery.transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in renderers)
        {
            if (r.gameObject.name.Contains("Roof") || r.gameObject.name.Contains("Ngoi") || r.gameObject.name.Contains("Plane"))
                continue;
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }

        var roofPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gallery/RoofNative/VietnameseAsianRoof.prefab");
        int prefabVertexCount = 0;
        int prefabChildCount = 0;
        if (roofPrefab != null)
        {
            prefabChildCount = roofPrefab.transform.childCount;
            foreach (var mf in roofPrefab.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh != null) prefabVertexCount += mf.sharedMesh.vertexCount;
            }
        }

        return $"BuildingBounds: Center={b.center}, Size={b.size}, Min={b.min}, Max={b.max} | " +
               $"RoofPrefab: Children={prefabChildCount}, Vertices={prefabVertexCount}";
    }
}
```

- [ ] **Step 2: Chạy script qua `script-execute` và ghi nhận kích thước thực tế**
  - Chạy `MeasureSceneBounds.Run()`.
  - Tính toán các thông số hình học động:
    - $FootprintWidth = Size.x$, $FootprintLength = Size.z$, $WallTopY = Max.y$.
    - $EaveOverhang = 0.4\text{ m}$.
    - $EaveWidth = FootprintWidth + 0.8\text{ m}$, $EaveLength = FootprintLength + 0.8\text{ m}$.
    - $RidgeLength = EaveLength - EaveWidth$.
    - Chiều cao nóc: $RidgeHeight = (EaveWidth / 2) \times \tan(32^\circ) \approx (EaveWidth / 2) \times 0.625$.
    - Cao độ đỉnh nóc: $RidgeTopY = WallTopY + RidgeHeight$.

- [ ] **Step 3: Đánh giá số lượng mảng ngói cần thiết để tối ưu hiệu năng**
  - Tính toán số hàng ngói theo chiều dốc (mỗi dốc khoảng 4 - 5 hàng gối mí).
  - Đảm bảo tổng số GameObject ngói sinh ra dưới 100 đối tượng thay vì hàng ngàn viên lẻ.

- [ ] **Step 4: Xóa script đo đạc tạm thời**
  - Xóa `Assets/Script/Editor/MeasureSceneBounds.cs`.

- [ ] **Step 5: Kiểm tra STOP condition của Phase 0**
  - **STOP condition:** Nếu không xác định được `BuildingBounds` khép kín hoặc không tìm thấy `VietnameseAsianRoof.prefab` $\rightarrow$ DỪNG ngay lập tức và báo cáo người dùng.

---

### Task 1: Phase 1 - Dựng Khung Đỡ Mái Kín (Underlay) Chống Lọt Sáng & Chống Z-Fighting

**Files:**
- Modify: Scene `Assets/Scenes/PhongTrienLam.unity` (GameObject `RoofUnderlay`)

**Interfaces:**
- Consumes: Kích thước $EaveWidth, EaveLength, RidgeLength, RidgeTopY, WallTopY$ từ Phase 0.
- Produces: GameObject `RoofUnderlay` kín 4 dốc với material `Tran_NauDo.mat`.

- [ ] **Step 1: Tạo hoặc tái cấu trúc Mesh hình chóp cụt 4 dốc cho `RoofUnderlay`**
  - Đáy hình chữ nhật: $Width = FootprintWidth$, $Length = FootprintLength$ nằm khớp trên đỉnh tường ở $Y = WallTopY$.
  - Đỉnh bờ nóc: dài $RidgeLength$ tại $Y = RidgeTopY - 0.08\text{ m}$ (thấp hơn mặt ngói $0.08\text{ m}$ để triệt tiêu Z-fighting).
  - Mép đáy kéo kín vào tim tường để ngăn $100\%$ ánh sáng chiếu xuyên qua trần.

- [ ] **Step 2: Gán Material và cấu hình bóng đổ**
  - Gán Material `Assets/Gallery/Materials/Tran_NauDo.mat` (màu gỗ nâu tối).
  - Bật `Cast Shadows = Two Sided` và `Receive Shadows = true`.

- [ ] **Step 3: Verification kiểm tra lọt sáng từ trong phòng tranh**
  - Đặt Camera bên trong phòng tranh nhìn lên trần nhà.
  - Chụp ảnh kiểm tra bằng `screenshot-camera`.
  - Xác nhận không có bất kỳ tia sáng nào lọt qua khe giữa tường và trần.

- [ ] **Step 4: Kiểm tra STOP condition của Phase 1**
  - **STOP condition:** Nếu nhìn từ trong phòng tranh thấy ánh sáng Directional Light lọt vào hoặc Underlay nhô cao hơn cao độ mặt ngói dự kiến $\rightarrow$ DỪNG, chỉnh lại hình học đáy/đỉnh của Underlay.

---

### Task 2: Phase 2 - Lợp Ngói 3D Mặt Mái Thử Nghiệm Số 1 (1 Tile Face Checkpoint)

**Files:**
- Modify: Scene `Assets/Scenes/PhongTrienLam.unity` (Tạo nhóm `Roof_Face_Front` dưới `MaiNgoi`)

**Interfaces:**
- Consumes: Mép hiên trước ($Z = +FootprintLength/2 + 0.4\text{ m}$), bờ nóc chính, prefab `VietnameseAsianRoof`.
- Produces: Mặt dốc ngói trước hoàn chỉnh với các hàng ngói gối mí nhau $15 - 20\%$.

- [ ] **Step 1: Tính toán phân bổ hàng ngói theo chiều dốc mặt trước**
  - Chiều dài dốc mặt trước: $L_{slope} = \sqrt{(EaveWidth/2)^2 + RidgeHeight^2}$.
  - Xác định số hàng ngói (ví dụ 4 hàng gối mí): Hàng $N$ xếp đè lên mép hàng $N-1$ khoảng $15\%$.
  - Hàng mép hiên dưới cùng nhô ra ngoài $0.4\text{ m}$ so với tường.

- [ ] **Step 2: Sinh các mảng ngói và gán Material `Roof_Brick.mat`**
  - Khởi tạo các mảng ngói dọc theo mặt dốc trước.
  - Đặt toàn bộ trong GameObject `Roof_Face_Front`.
  - Tinh chỉnh `_Smoothness = 0.2` trên `Roof_Brick.mat` để có độ mờ lì của đất nung.

- [ ] **Step 3: Verification kiểm tra độ phủ & Draw Calls**
  - Dùng `screenshot-scene-view` chụp cận cảnh mặt mái trước.
  - Kiểm tra độ khít: Không hở khe nhìn thấy lớp Underlay bên dưới.
  - Kiểm tra Draw Calls / Triangle Count thông qua Profiler/Stats.

- [ ] **Step 4: Kiểm tra STOP condition của Phase 2**
  - **STOP condition:** Nếu mặt ngói bị lệch góc nghiêng, bị hở khe nhìn thấy Underlay, hoặc số draw calls tăng đột biến (> 20 batches cho 1 mặt) $\rightarrow$ DỪNG, căn chỉnh lại bước gối mí và scale của mảng ngói.

---

### Task 3: Phase 3 - Lợp Ngói 3D Hoàn Thiện Cả 4 Mặt Mái (All 4 Tile Faces Checkpoint)

**Files:**
- Modify: Scene `Assets/Scenes/PhongTrienLam.unity` (Tạo `Roof_Face_Back`, `Roof_Face_Left`, `Roof_Face_Right`)

**Interfaces:**
- Consumes: Quy cách xếp ngói đã chuẩn hóa từ Phase 2.
- Produces: Đầy đủ 4 mặt mái ngói phủ kín toàn bộ 4 dốc.

- [ ] **Step 1: Sinh mặt dốc sau (`Roof_Face_Back`)**
  - Lấy đối xứng hình học từ `Roof_Face_Front` qua trục bờ nóc $X = 0$.

- [ ] **Step 2: Sinh 2 mặt chái hồi trái & phải (`Roof_Face_Left`, `Roof_Face_Right`)**
  - Hai mặt hồi hình tam giác dốc từ 2 đầu bờ nóc xuống 2 mép hiên hồi $Z = \pm (FootprintLength/2 + 0.4\text{ m})$.
  - Cắt chỉnh các mảng ngói thu hẹp dần về phía đỉnh nóc để tạo dáng chái hồi gọn gàng.

- [ ] **Step 3: Verification kiểm tra 4 hướng nhìn**
  - Chụp ảnh 4 hướng (Front, Back, Left, Right) bằng `screenshot-scene-view`.
  - Xác nhận 4 mặt mái che phủ đồng đều, khoảng cách nhô mép hiên đều đặn $0.4\text{ m}$ ở cả 4 cạnh.

- [ ] **Step 4: Kiểm tra STOP condition của Phase 3**
  - **STOP condition:** Nếu có mặt dốc bị lệch mép hiên, không đối xứng, hoặc xuất hiện khe nứt lớn dọc theo 4 đường chéo góc chái $\rightarrow$ DỪNG.

---

### Task 4: Phase 4 - Dựng Bờ Nóc (Ridge) & 4 Bờ Dải (Hip Ridges) Bằng ProBuilder

**Files:**
- Modify: Scene `Assets/Scenes/PhongTrienLam.unity` (Tạo `Roof_RidgeCap`, `Roof_HipRidge_01..04`)

**Interfaces:**
- Consumes: Tọa độ bờ nóc và 4 góc mép hiên mái.
- Produces: Hệ bờ dải và bờ nóc che kín hoàn toàn 4 đường giao nhau giữa các mặt ngói.

- [ ] **Step 1: Dựng thanh bờ nóc chính (Ridge Cap)**
  - Dùng ProBuilder tạo khối nẹp viền chạy dọc đỉnh bờ nóc dài $RidgeLength$.
  - Gán Material vữa trát cổ xám trắng hoặc đá nung.

- [ ] **Step 2: Dựng 4 thanh bờ dải (Hip Ridges)**
  - Chạy từ 2 đầu của bờ nóc chéo xuống 4 góc mép hiên mái.
  - Tiết diện gờ chỉ nẹp ôm sát trên bề mặt tiếp giáp của các hàng ngói, che kín $100\%$ đường cắt giữa các mặt mái.

- [ ] **Step 3: Verification kiểm tra Z-fighting và độ khít**
  - Kiểm tra tại các giao điểm tiếp xúc giữa bờ dải và ngói lợp: Không có hiện tượng nhấp nháy đa giác (Z-fighting).

- [ ] **Step 4: Kiểm tra STOP condition của Phase 4**
  - **STOP condition:** Nếu bờ dải bị lún chìm vào trong ngói hoặc hở khoảng trống nhìn thấy Underlay $\rightarrow$ DỪNG, chỉnh lại cao độ và độ dày của bờ dải.

---

### Task 5: Phase 5 - Dựng Đầu Đao Thử Nghiệm Số 1 & Trình Duyệt Phê Duyệt (Human Approval Gate)

**Files:**
- Modify: Scene `Assets/Scenes/PhongTrienLam.unity` (Tạo `Curved_Eave_Corner_01`)

**Interfaces:**
- Consumes: Mũi bờ dải tại Góc 1 (trước - trái).
- Produces: Chi tiết đầu đao cong vút nhẹ mẫu tại Góc 1.

- [ ] **Step 1: Dựng chi tiết đầu đao cong tại Góc 1 bằng ProBuilder**
  - Nối tiếp mũi bờ dải tại góc hiên mái trước - trái.
  - Uốn lượn cong mềm mại vểnh lên trên với độ nâng cao $\approx 0.35\text{ m}$, vuốt thon nhẹ ở mũi đao theo đúng Mục 4 `tutor.png`.

- [ ] **Step 2: Chụp ảnh cận cảnh kiểm tra dáng cong**
  - Đặt góc Camera chụp cận cảnh độ vểnh và đường cong của đầu đao Góc 1.
  - Sử dụng `screenshot-isolated` hoặc `screenshot-camera`.

- [ ] **Step 3: CỔNG PHÊ DUYỆT BẮT BUỘC (HUMAN APPROVAL GATE - STOP)**
  - **STOP CONDITION:** Dừng lại, trình ảnh chụp đầu đao Góc 1 cho người dùng duyệt.
  - Không được tự ý chuyển sang Phase 6 nếu người dùng chưa xác nhận hài lòng với dáng cong.

---

### Task 6: Phase 6 - Hoàn Thiện 3 Đầu Đao Còn Lại (Remaining 3 Curved Eaves)

**Files:**
- Modify: Scene `Assets/Scenes/PhongTrienLam.unity` (Tạo `Curved_Eave_Corner_02..04`)

**Interfaces:**
- Consumes: Mẫu đầu đao Góc 1 đã được người dùng phê duyệt từ Task 5.
- Produces: Toàn bộ 4 góc mái đều có đầu đao cong đồng bộ chuẩn mực.

- [ ] **Step 1: Nhân bản đối xứng đầu đao sang 3 góc còn lại**
  - Góc 2: Trước - Phải (đối xứng qua trục $Z$).
  - Góc 3: Sau - Phải (đối xứng qua tâm).
  - Góc 4: Sau - Trái (đối xứng qua trục $X$).

- [ ] **Step 2: Gộp nhóm và tổ chức Hierarchy sạch sẽ**
  - Gom toàn bộ vào `MaiNgoi/DauDao_4Goc`.

- [ ] **Step 3: Verification kiểm tra toàn cảnh 4 góc**
  - Chụp ảnh từ trên cao bằng `screenshot-scene-view` bao quát cả 4 góc mái.
  - Xác nhận 4 đầu đao cong đều tăm tắp, cân xứng hoàn hảo.

- [ ] **Step 4: Kiểm tra STOP condition của Phase 6**
  - **STOP condition:** Nếu có góc bị ngược hướng cong hoặc sai lệch khớp nối bờ dải $\rightarrow$ DỪNG.

---

### Task 7: Phase 7 - Tách Mặt Sân 3 Mesh Visual & 1 Collider Thống Nhất (Yard Plane & Continuous Collider)

**Files:**
- Modify: Scene `Assets/Scenes/PhongTrienLam.unity` (Thay thế `Plane` bằng nhóm `Ground_Yard`)

**Interfaces:**
- Consumes: Vị trí sảnh cửa chính ($Z = 0$, $X \approx 12.25\text{ m}$), cao độ $Y = 3.74\text{ m}$, biên sân rộng $94\text{ m} \times 68\text{ m}$.
- Produces: 3 Mesh visual phẳng ghép khít + 1 BoxCollider liên tục phủ trọn sân.

- [ ] **Step 1: Tạo Lối Đi Giữa (`Yard_Center_Walkway`)**
  - Bề rộng: Đúng $7.0\text{ m}$ (từ $Z = -3.5\text{ m}$ đến $Z = +3.5\text{ m}$).
  - Chiều dài: Từ mép thềm cửa $X = 12.25\text{ m}$ ra mép ngoài sân $X = 51.9\text{ m}$.
  - Gán Material `San_Da_Xam.mat`. Tắt Collider trên mesh này.

- [ ] **Step 2: Tạo Sân Gạch Đỏ Hai Bên (`Yard_Left_RedBrick`, `Yard_Right_RedBrick`)**
  - Sân trái: Kéo từ $Z = +3.5\text{ m}$ đến biên trái $Z \approx +49.7\text{ m}$.
  - Sân phải: Kéo từ $Z = -3.5\text{ m}$ đến biên phải $Z \approx -44.1\text{ m}$.
  - Cả hai ghép sát mép lối đi giữa (sai số $0.0\text{ m}$, không kẽ hở).
  - Gán Material `San_Gach_Do.mat`. Tắt Collider trên các mesh này.

- [ ] **Step 3: Tạo 1 Collider phẳng liên tục duy nhất (`Yard_Continuous_Collider`)**
  - Gắn 1 `BoxCollider` bao trọn diện tích toàn bộ sân $68\text{ m} \times 94\text{ m}$ tại cao độ $Y = 3.74\text{ m}$ (bề dày $0.1\text{ m}$).
  - Đảm bảo người chơi khi di chuyển qua lại giữa gạch đỏ và đá xám không bao giờ bị vấp mí ron va chạm.

- [ ] **Step 4: Verification kiểm tra va chạm & liền mạch**
  - Điều khiển `PlayerCapsule` di chuyển ngang qua ranh giới giữa 2 loại sân.
  - Xác nhận camera êm ru, không bị kênh/vấp chân vật lý.

- [ ] **Step 5: Kiểm tra STOP condition của Phase 7**
  - **STOP condition:** Nếu xuất hiện khe hở giữa 3 mesh hoặc người chơi bị kẹt bước chân tại mép nối $\rightarrow$ DỪNG.

---

### Task 8: Phase 8 - Xử Lý Texture Gạch Thật & Normal Maps PBR

**Files:**
- Create: `Assets/Gallery/Textures/san_gach_Normal.jpg`, `Assets/Gallery/Textures/san_da_Normal.jpg`
- Create: `Assets/Gallery/Materials/San_Gach_Do.mat`
- Modify: `Assets/Gallery/Materials/San_Da_Xam.mat`

**Interfaces:**
- Consumes: `san_gach.jpg`, `san da.jpg`.
- Produces: Normal map cấu hình chuẩn Unity Importer + Material PBR mờ lì tự nhiên.

- [ ] **Step 1: Kiểm tra xem có Normal map gốc hay không; nếu không, tạo Normal map từ Grayscale**
  - Nhân bản `san_gach.jpg` $\rightarrow$ `san_gach_Normal.jpg`.
  - Nhân bản `san da.jpg` $\rightarrow$ `san_da_Normal.jpg`.
  - Cấu hình TextureImporter qua C# API:
    - `textureType = TextureImporterType.NormalMap`
    - `convertToNormalMap = true`
    - `normalStrength = 0.35`
    - `Apply()`

- [ ] **Step 2: Thiết lập Material `San_Gach_Do.mat`**
  - Shader: `Universal Render Pipeline/Lit`.
  - `_BaseMap`: `san_gach.jpg`.
  - `_BumpMap`: `san_gach_Normal.jpg` với `_BumpScale = 1.0`.
  - `_Smoothness`: $0.18$ (bề mặt lì mộc của gạch nung).
  - Tiling UV: Căn chỉnh tỉ lệ viên gạch $\approx 0.35\text{ m} \times 0.35\text{ m}$.

- [ ] **Step 3: Thiết lập Material `San_Da_Xam.mat`**
  - `_BaseMap`: `san da.jpg`.
  - `_BumpMap`: `san_da_Normal.jpg` với `_BumpScale = 1.0`.
  - `_Smoothness`: $0.22$.
  - Tiling UV: Căn chỉnh kích thước phiến đá $\approx 0.5\text{ m} \times 0.5\text{ m}$.

- [ ] **Step 4: Verification kiểm tra bề mặt nổi khối**
  - Chiếu ánh sáng Directional Light xiên góc.
  - Chụp ảnh kiểm tra độ nổi khối của rãnh ron gạch và độ sần hạt của đá.

- [ ] **Step 5: Kiểm tra STOP condition của Phase 8**
  - **STOP condition:** Nếu Normal map bị đảo ngược (mạch vữa lồi lên thay vì chìm xuống) hoặc vật liệu bị bóng lóa bất thường $\rightarrow$ DỪNG, đảo kênh Normal và hạ Smoothness.

---

### Task 9: Phase 9 - Nghiệm Thu Toàn Diện & Chụp Ảnh Báo Cáo (Final Verification)

**Files:**
- Create: `C:\Users\sies\.gemini\antigravity-ide\brain\ca4f4703-b61d-424a-a88d-c8d7ba1a9736\walkthrough.md`

**Interfaces:**
- Consumes: Toàn bộ thành quả từ Phase 0 đến Phase 8.
- Produces: Báo cáo nghiệm thu hoàn chỉnh kèm ảnh chụp đa góc.

- [ ] **Step 1: Chụp bộ ảnh nghiệm thu đa góc (Multi-angle Screenshots)**
  - Góc 1: Toàn cảnh phối cảnh từ trên cao (tương đương Hình 1 trong `tutor.png`).
  - Góc 2: Chính diện sảnh đón tiếp nhìn rõ mái ngói và lối đi đá xám 7m.
  - Góc 3: Cận cảnh chi tiết đầu đao cong vút ở góc mái.
  - Góc 4: Cận cảnh mặt sân gạch đỏ bắt sáng nổi khối rãnh vữa.
  - Góc 5: Từ bên trong phòng tranh nhìn lên trần (chứng minh $100\%$ không lọt sáng).

- [ ] **Step 2: Đánh giá tiêu chuẩn nghiệm thu 7 điểm**
  1. Geometry: Mái 4 dốc cân đối, nhô mép $0.4\text{ m}$, 4 đầu đao cong thanh thoát.
  2. Gaps & Light Leaks: Không lọt sáng.
  3. Z-Fighting: Tuyệt đối không nhấp nháy.
  4. Performance: Draw calls trong ngưỡng an toàn.
  5. Materials: Ngói đỏ đất nung, bờ dải vữa xám, sân gạch đỏ và đá xám chuẩn PBR.
  6. Collision: Player di chuyển trơn tru trên 1 collider phẳng.
  7. Multi-angle screenshots đầy đủ.

- [ ] **Step 3: Lưu scene và lập báo cáo `walkthrough.md`**
  - Lưu scene `Assets/Scenes/PhongTrienLam.unity`.
  - Cập nhật `walkthrough.md`.
