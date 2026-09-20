# Vietnamese Roof & 3-Part Yard Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Dùng `superpowers:executing-plans` để thực thi tuần tự từng task có checkpoint rõ ràng. **KHÔNG** dispatch nhiều subagent chạy song song cùng sửa file Scene `PhongTrienLam.unity` để tránh conflict ghi đè file scene. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xây dựng hệ thống mái ngói 4 dốc xếp lớp truyền thống Việt Nam có đầu đao cong và phân chia mặt sân thành 3 dải gạch PBR chân thực (2 bên gạch đỏ, giữa đá xám 7m) cho phòng triển lãm tranh Đông Hồ.

**Architecture:** Mái nhà được phân tầng rõ ràng thành 3 lớp độc lập: (1) Underlay kín chống lọt sáng/Z-fighting, (2) Lớp ngói 3D xếp chồng mí được gom cụm/strip tối ưu hiệu năng, và (3) Hệ bờ dải, bờ nóc cùng 4 đầu đao vuốt cong dựng bằng ProBuilder. Mặt sân gồm 3 mesh visual ghép khít nhưng dùng 1 Collider liên tục thống nhất để tránh vấp mí chuyển động.

**Tech Stack:** Unity 6000.5.9f1 URP, ProBuilder, ReflectorNet / Unity C# API, URP Lit PBR Shader.

**Spec:** [`docs/superpowers/specs/2026-09-21-vietnamese-roof-and-yard-design.md`](file:///d:/UNITY-PROJECT/My%20project%20(1)/docs/superpowers/specs/2026-09-21-vietnamese-roof-and-yard-design.md)

## Global Constraints

- **Tọa độ & Kích thước động:** Tất cả kích thước trong spec chỉ là baseline; Phase 0 phải đo đạc chính xác từ runtime scene (`BuildingBounds`, `YardBounds`, `EntranceCenter`, `GroundY`). Tuyệt đối không hard-code các giá trị vị trí.
- **Hình học mái chính xác:**
  - Không khóa đồng thời cả góc dốc $\theta$ và chiều cao nóc.
  - Công thức hình học:
    - $HalfSpan = EaveWidth / 2$
    - $RoofRise = HalfSpan \times \tan(\theta)$ (với góc dốc $\theta \approx 30^\circ - 35^\circ$)
    - $RidgeTopY = WallTopY + RoofRise$
    - $RidgeLength = EaveLength - 2 \times HalfSpan = EaveLength - EaveWidth$
- **Chống lọt sáng & Z-Fighting:** Khung Underlay phải khép kín hoàn toàn nóc phòng tranh và đỉnh bờ nóc của Underlay phải thấp hơn ngói: $UnderlayTopY = RidgeTopY - 0.08\text{ m}$.
- **Performance Budget toàn diện:** Không chỉ đo Draw calls / Batches đơn lẻ mà phải kiểm soát đồng thời 4 chỉ số:
  1. `GameObject count`: Dưới 100 objects cho toàn bộ hệ mái (gom cụm strip/cluster, không sinh hàng ngàn viên lẻ).
  2. `Renderer count`: Giữ ở mức tối thiểu cần thiết.
  3. `Triangles count`: Tương thích với năng lực render của URP trong scene.
  4. `Batches / Draw Calls`: Giữ mức tăng draw call nhỏ nhất thông qua material instancing / batching.
- **Định dạng Normal Map:** Normal map sinh từ Albedo Grayscale chỉ là giải pháp xấp xỉ (*approximation*); ưu tiên số 1 là Normal map thật nếu có. Bắt buộc lưu ở định dạng lossless **PNG** hoặc **TGA** (như `san_gach_Normal.png`, `san_da_Normal.png`), tuyệt đối **KHÔNG** dùng JPEG vì thuật toán nén 8x8 block của JPEG sẽ phá hỏng các vector pháp tuyến bề mặt.
- **Cổng phê duyệt bắt buộc (Human Approval Gate):** Chỉ chuyển từ 1 đầu đao sang 3 đầu đao còn lại sau khi người dùng trực tiếp xem screenshot và phê duyệt dáng cong.
- **Collider mặt sân:** 3 Mesh visual riêng biệt nhưng dùng 1 BoxCollider phẳng liên tục duy nhất tại cao độ `GroundY` đo từ Phase 0.
- **Quy tắc dừng (STOP Condition):** Mỗi Phase bắt buộc có tiêu chí dừng rõ ràng; nếu checkpoint thất bại, không được chuyển sang Phase tiếp theo.

---

### Task 0: Phase 0 - Đo Đạc Bounds & Trắc Đạc Kích Thước Thực Tế Từ Scene (Read-Only)

**Files:**
- Create: `Assets/Script/Editor/MeasureSceneBounds.cs` (Tool đo đạc tạm thời)

**Interfaces:**
- Consumes: Scene `PhongTrienLam.unity` đang mở.
- Produces: Biến số đo thực tế runtime:
  - `BuildingBounds`: Center, Size, Min, Max của phòng tranh (loại trừ mái cũ).
  - `WallTopY`: $BuildingBounds.max.y$.
  - `YardBounds`: Bounds của mặt sân hiện tại (`Plane`).
  - `GroundY`: Cao độ mặt sân thực tế ($Plane.transform.position.y$).
  - `EntranceCenter`: Tọa độ $X, Z$ của khu vực cửa chính/tiền sảnh dẫn vào phòng tranh.
  - `RoofPrefabStats`: ChildCount, RendererCount, VertexCount của `VietnameseAsianRoof.prefab`.

- [ ] **Step 1: Viết script Editor đo đạc toàn diện kích thước hình học**

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

        var plane = GameObject.Find("Plane");
        Bounds yardBounds = plane != null ? plane.GetComponent<Renderer>().bounds : new Bounds();
        float groundY = plane != null ? plane.transform.position.y : 0f;

        Vector3 entrancePos = Vector3.zero;
        foreach (Transform t in gallery.transform)
        {
            if (t.name.ToLower().Contains("cua") || t.name.ToLower().Contains("door") || t.name.ToLower().Contains("lintel"))
            {
                entrancePos = t.position;
                break;
            }
        }
        if (entrancePos == Vector3.zero)
        {
            entrancePos = new Vector3(b.max.x, groundY, b.center.z);
        }

        var roofPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gallery/RoofNative/VietnameseAsianRoof.prefab");
        int prefabVertices = 0;
        int prefabRenderers = 0;
        int prefabChildren = 0;
        if (roofPrefab != null)
        {
            prefabChildren = roofPrefab.transform.childCount;
            var rList = roofPrefab.GetComponentsInChildren<Renderer>();
            prefabRenderers = rList.Length;
            foreach (var mf in roofPrefab.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh != null) prefabVertices += mf.sharedMesh.vertexCount;
            }
        }

        return $"BuildingBounds: Center={b.center}, Size={b.size}, Min={b.min}, Max={b.max} | " +
               $"Yard: Bounds={yardBounds}, GroundY={groundY:F3} | " +
               $"Entrance: Pos={entrancePos} | " +
               $"RoofPrefab: Children={prefabChildren}, Renderers={prefabRenderers}, Verts={prefabVertices}";
    }
}
```

- [ ] **Step 2: Chạy script qua `script-execute` và tính toán hình học động**
  - Đọc các kết quả:
    - $FootprintWidth = Size.x$, $FootprintLength = Size.z$, $WallTopY = Max.y$.
    - $EaveOverhang = 0.4\text{ m}$.
    - $EaveWidth = FootprintWidth + 2 \times EaveOverhang$.
    - $EaveLength = FootprintLength + 2 \times EaveOverhang$.
    - $HalfSpan = EaveWidth / 2$.
    - $RoofRise = HalfSpan \times \tan(32^\circ) \approx HalfSpan \times 0.6249$.
    - $RidgeTopY = WallTopY + RoofRise$.
    - $RidgeLength = EaveLength - 2 \times HalfSpan = EaveLength - EaveWidth$.
    - $YardBounds, GroundY, EntranceCenter$ lưu trữ cho Phase 7.

- [ ] **Step 3: Đánh giá Performance Budget**
  - Dựa trên `prefabVertices` và `prefabRenderers`: thiết kế cấu trúc nhóm dải (strip) ngói sao cho toàn bộ 4 mặt mái có tổng `GameObject count < 100` và `Renderer count` tối ưu.

- [ ] **Step 4: Xóa script đo đạc tạm thời**
  - Xóa `Assets/Script/Editor/MeasureSceneBounds.cs`.

- [ ] **Step 5: Kiểm tra STOP condition của Phase 0**
  - **STOP condition:** Nếu không xác định được `BuildingBounds`, `YardBounds`, hoặc `VietnameseAsianRoof.prefab` $\rightarrow$ DỪNG ngay lập tức và báo cáo.

---

### Task 1: Phase 1 - Dựng Khung Đỡ Mái Kín (Underlay) Chống Lọt Sáng & Chống Z-Fighting

**Files:**
- Modify: Scene `Assets/Scenes/PhongTrienLam.unity` (GameObject `RoofUnderlay`)

**Interfaces:**
- Consumes: $FootprintWidth, FootprintLength, WallTopY, RidgeLength, RidgeTopY$ từ Phase 0.
- Produces: Khung chóp cụt `RoofUnderlay` kín 4 dốc với đỉnh đặt ở $UnderlayTopY = RidgeTopY - 0.08\text{ m}$.

- [ ] **Step 1: Cấu trúc Mesh hình chóp cụt 4 dốc cho `RoofUnderlay`**
  - Đáy: Khớp với chu vi đỉnh tường ($Width = FootprintWidth$, $Length = FootprintLength$) tại cao độ $Y = WallTopY$.
  - Đỉnh nóc: Dài $RidgeLength$ tại $Y = UnderlayTopY$ (thấp hơn ngói $0.08\text{ m}$ để triệt tiêu Z-fighting).
  - Đáy khép kín mép tường bao để không lọt $100\%$ ánh sáng.

- [ ] **Step 2: Gán Material và cấu hình bóng đổ**
  - Material: `Assets/Gallery/Materials/Tran_NauDo.mat`.
  - Thiết lập: `Cast Shadows = Two Sided`, `Receive Shadows = true`.

- [ ] **Step 3: Verification kiểm tra lọt sáng từ trong phòng tranh**
  - Đặt Camera trong phòng tranh nhìn lên trần nhà.
  - Chụp ảnh kiểm tra bằng `screenshot-camera`.
  - Xác nhận không có tia sáng Directional Light nào xuyên qua.

- [ ] **Step 4: Kiểm tra STOP condition của Phase 1**
  - **STOP condition:** Nếu phát hiện lọt sáng vào phòng tranh hoặc Underlay nhô cao hơn $RidgeTopY$ $\rightarrow$ DỪNG.

---

### Task 2: Phase 2 - Lợp Ngói 3D Mặt Mái Thử Nghiệm Số 1 (1 Tile Face Checkpoint)

**Files:**
- Modify: Scene `Assets/Scenes/PhongTrienLam.unity` (Tạo `Roof_Face_Front` dưới `MaiNgoi`)

**Interfaces:**
- Consumes: Tọa độ mép hiên trước, bờ nóc chính, prefab `VietnameseAsianRoof`.
- Produces: Mặt dốc trước lợp ngói xếp chồng mí $15 - 20\%$.

- [ ] **Step 1: Tính toán phân bổ hàng ngói theo chiều dốc mặt trước**
  - Chiều dài dốc: $L_{slope} = \sqrt{HalfSpan^2 + RoofRise^2}$.
  - Xác định số hàng ngói (4 - 5 hàng gối mí nhau $15 - 20\%$).
  - Hàng mép hiên dưới nhô ra ngoài $0.4\text{ m}$.

- [ ] **Step 2: Sinh các mảng ngói gom cụm và gán Material `Roof_Brick.mat`**
  - Bố trí các mảng ngói trong `Roof_Face_Front`.
  - Thiết lập `_Smoothness = 0.2` trên `Roof_Brick.mat`.

- [ ] **Step 3: Verification kiểm tra toàn diện 4 chỉ số Performance & Độ khít**
  - Đo đạc:
    1. `GameObject count`: Ghi nhận số object của `Roof_Face_Front` (yêu cầu $\le 20$).
    2. `Renderer count`: Ghi nhận số lượng MeshRenderer.
    3. `Triangles`: Kiểm tra tổng triangle count trong ngưỡng an toàn.
    4. `Batches / Draw calls`: Kiểm tra số Batches tăng thêm từ Scene Stats / Profiler.
  - Visual: Chụp ảnh bằng `screenshot-scene-view`, xác nhận không hở khe nhìn thấy Underlay.

- [ ] **Step 4: Kiểm tra STOP condition của Phase 2**
  - **STOP condition:** Nếu mặt ngói bị hở khe, Z-fighting, hoặc 4 chỉ số performance vượt ngưỡng ngân sách $\rightarrow$ DỪNG.

---

### Task 3: Phase 3 - Lợp Ngói 3D Hoàn Thiện Cả 4 Mặt Mái (All 4 Tile Faces Checkpoint)

**Files:**
- Modify: Scene `Assets/Scenes/PhongTrienLam.unity` (Tạo `Roof_Face_Back`, `Roof_Face_Left`, `Roof_Face_Right`)

**Interfaces:**
- Consumes: Quy cách xếp ngói chuẩn hóa từ Phase 2.
- Produces: 4 mặt dốc ngói phủ kín toàn bộ mái.

- [ ] **Step 1: Sinh mặt dốc sau (`Roof_Face_Back`)**
  - Lấy đối xứng từ `Roof_Face_Front` qua trục bờ nóc.

- [ ] **Step 2: Sinh 2 mặt chái hồi trái & phải (`Roof_Face_Left`, `Roof_Face_Right`)**
  - Dốc hình tam giác từ 2 đầu bờ nóc xuống 2 mép hiên hồi.
  - Thu gọn mảng ngói về phía đỉnh nóc để tạo dáng chái hồi cân xứng.

- [ ] **Step 3: Verification kiểm tra toàn diện 4 mặt & Performance**
  - Chụp ảnh 4 hướng (Front, Back, Left, Right) bằng `screenshot-scene-view`.
  - Kiểm tra 4 chỉ số: Tổng `GameObject count < 100`, `Renderer count`, `Triangles`, `Batches` ổn định.

- [ ] **Step 4: Kiểm tra STOP condition của Phase 3**
  - **STOP condition:** Nếu mép hiên không đều $0.4\text{ m}$, không đối xứng, hoặc xuất hiện khe nứt lớn tại các đường chéo góc chái $\rightarrow$ DỪNG.

---

### Task 4: Phase 4 - Dựng Bờ Nóc (Ridge) & 4 Bờ Dải (Hip Ridges) Bằng ProBuilder

**Files:**
- Modify: Scene `Assets/Scenes/PhongTrienLam.unity` (Tạo `Roof_RidgeCap`, `Roof_HipRidge_01..04`)

**Interfaces:**
- Consumes: Tọa độ bờ nóc ($RidgeLength$, $RidgeTopY$) và 4 góc mép hiên mái.
- Produces: Hệ bờ dải và bờ nóc che kín hoàn toàn 4 đường giao nhau giữa các mặt ngói.

- [ ] **Step 1: Dựng thanh bờ nóc chính (Ridge Cap)**
  - Dùng ProBuilder tạo khối nẹp viền chạy dọc đỉnh bờ nóc tại $Y = RidgeTopY$.
  - Gán Material vữa trát cổ xám trắng.

- [ ] **Step 2: Dựng 4 thanh bờ dải (Hip Ridges)**
  - Nẹp từ 2 đầu bờ nóc chéo xuống 4 góc mép hiên mái, ôm sát bề mặt giao nhau của ngói.

- [ ] **Step 3: Verification kiểm tra Z-fighting & Độ khít**
  - Kiểm tra các cạnh tiếp xúc giữa bờ dải và ngói lợp: Tuyệt đối không có hiện tượng nhấp nháy đa giác (Z-fighting).

- [ ] **Step 4: Kiểm tra STOP condition của Phase 4**
  - **STOP condition:** Nếu bờ dải bị lún chìm vào trong ngói hoặc hở khoảng trống nhìn thấy Underlay $\rightarrow$ DỪNG.

---

### Task 5: Phase 5 - Dựng Đầu Đao Thử Nghiệm Số 1 & Trình Duyệt Phê Duyệt (Human Approval Gate)

**Files:**
- Modify: Scene `Assets/Scenes/PhongTrienLam.unity` (Tạo `Curved_Eave_Corner_01`)

**Interfaces:**
- Consumes: Mũi bờ dải tại Góc 1 (trước - trái).
- Produces: Chi tiết đầu đao cong vút nhẹ mẫu tại Góc 1.

- [ ] **Step 1: Dựng chi tiết đầu đao cong tại Góc 1 bằng ProBuilder**
  - Nối tiếp mũi bờ dải tại góc hiên mái trước - trái.
  - Vuốt cong mềm mại vểnh lên trên với độ nâng cao $\approx 0.35\text{ m}$, vuốt thon nhẹ ở mũi đao theo đúng Mục 4 `tutor.png`.

- [ ] **Step 2: Chụp ảnh cận cảnh kiểm tra dáng cong**
  - Chụp ảnh cận cảnh độ vểnh và đường cong của đầu đao Góc 1 bằng `screenshot-isolated` hoặc `screenshot-camera`.

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
  - Góc 2: Trước - Phải.
  - Góc 3: Sau - Phải.
  - Góc 4: Sau - Trái.

- [ ] **Step 2: Tổ chức Hierarchy sạch sẽ**
  - Gom toàn bộ vào `MaiNgoi/DauDao_4Goc`.

- [ ] **Step 3: Verification kiểm tra toàn cảnh 4 góc**
  - Chụp ảnh từ trên cao bằng `screenshot-scene-view` bao quát cả 4 góc mái.
  - Xác nhận 4 đầu đao cong đều tăm tắp, cân xứng hoàn hảo.

- [ ] **Step 4: Kiểm tra STOP condition của Phase 6**
  - **STOP condition:** Nếu có góc bị ngược hướng cong hoặc sai lệch khớp nối bờ dải $\rightarrow$ DỪNG.

---

### Task 7: Phase 7 - Tách Mặt Sân 3 Mesh Visual & 1 Collider Thống Nhất Dựa Trên Tọa Độ Runtime

**Files:**
- Modify: Scene `Assets/Scenes/PhongTrienLam.unity` (Thay thế `Plane` bằng nhóm `Ground_Yard`)

**Interfaces:**
- Consumes: `YardBounds`, `GroundY`, `EntranceCenter` đo đạc thực tế từ Phase 0.
- Produces: 3 Mesh visual phẳng ghép khít + 1 BoxCollider liên tục phủ trọn `YardBounds`.

- [ ] **Step 1: Tạo Lối Đi Giữa (`Yard_Center_Walkway`)**
  - Căn giữa theo $Z = EntranceCenter.z$.
  - Bề rộng: Đúng $7.0\text{ m}$ (từ $Z = EntranceCenter.z - 3.5\text{ m}$ đến $Z = EntranceCenter.z + 3.5\text{ m}$).
  - Chiều dài: Từ mép thềm sảnh ($X = EntranceCenter.x$) ra mép ngoài của `YardBounds`.
  - Cao độ: $Y = GroundY$.
  - Gán Material `San_Da_Xam.mat`. Tắt Collider trên mesh này.

- [ ] **Step 2: Tạo Sân Gạch Đỏ Hai Bên (`Yard_Left_RedBrick`, `Yard_Right_RedBrick`)**
  - Sân trái: Kéo từ mép trái lối đi giữa ($Z = EntranceCenter.z + 3.5\text{ m}$) ra biên trái của `YardBounds`.
  - Sân phải: Kéo từ mép phải lối đi giữa ($Z = EntranceCenter.z - 3.5\text{ m}$) ra biên phải của `YardBounds`.
  - Ghép sát khít mép lối đi giữa (sai số $0.0\text{ m}$, không kẽ hở).
  - Cao độ: $Y = GroundY$.
  - Gán Material `San_Gach_Do.mat`. Tắt Collider trên các mesh này.

- [ ] **Step 3: Tạo 1 Collider phẳng liên tục duy nhất (`Yard_Continuous_Collider`)**
  - Gắn 1 `BoxCollider` bao trọn diện tích `YardBounds` tại cao độ $Y = GroundY$ (bề dày $0.1\text{ m}$).
  - Đảm bảo người chơi khi di chuyển qua lại giữa gạch đỏ và đá xám không bao giờ bị vấp mí ron va chạm.

- [ ] **Step 4: Verification kiểm tra va chạm & Liền mạch**
  - Điều khiển `PlayerCapsule` di chuyển ngang qua ranh giới giữa 2 loại sân.
  - Xác nhận camera êm ru, không bị kênh/vấp chân vật lý.

- [ ] **Step 5: Kiểm tra STOP condition của Phase 7**
  - **STOP condition:** Nếu xuất hiện khe hở giữa 3 mesh hoặc người chơi bị kẹt bước chân tại mép nối $\rightarrow$ DỪNG.

---

### Task 8: Phase 8 - Xử Lý Texture Gạch Thật & Normal Maps Lossless PBR

**Files:**
- Create: `Assets/Gallery/Textures/san_gach_Normal.png`, `Assets/Gallery/Textures/san_da_Normal.png` (Định dạng PNG lossless)
- Create: `Assets/Gallery/Materials/San_Gach_Do.mat`
- Modify: `Assets/Gallery/Materials/San_Da_Xam.mat`

**Interfaces:**
- Consumes: `san_gach.jpg`, `san da.jpg`.
- Produces: Normal map định dạng PNG lossless cấu hình TextureImporter + Material PBR mờ lì tự nhiên.

- [ ] **Step 1: Kiểm tra xem có Normal map gốc hay không; nếu không, tạo Normal map từ Grayscale xuất ra PNG**
  - Kiểm tra project xem có normal map thực tế cho gạch/đá không.
  - Nếu không, nhân bản albedo và xuất sang định dạng lossless **PNG** (`san_gach_Normal.png`, `san_da_Normal.png`) để tránh lỗi nén block của JPEG.
  - Cấu hình TextureImporter qua C# API:
    - `textureType = TextureImporterType.NormalMap`
    - `convertToNormalMap = true`
    - `normalStrength = 0.35`
    - `Apply()`

- [ ] **Step 2: Thiết lập Material `San_Gach_Do.mat`**
  - Shader: `Universal Render Pipeline/Lit`.
  - `_BaseMap`: `san_gach.jpg`.
  - `_BumpMap`: `san_gach_Normal.png` với `_BumpScale = 1.0`.
  - `_Smoothness`: $0.18$ (bề mặt lì mộc của gạch nung Bát Tràng).
  - Tiling UV: Căn chỉnh tỉ lệ viên gạch $\approx 0.35\text{ m} \times 0.35\text{ m}$.

- [ ] **Step 3: Thiết lập Material `San_Da_Xam.mat`**
  - `_BaseMap`: `san da.jpg`.
  - `_BumpMap`: `san_da_Normal.png` với `_BumpScale = 1.0`.
  - `_Smoothness`: $0.22$.
  - Tiling UV: Căn chỉnh kích thước phiến đá $\approx 0.5\text{ m} \times 0.5\text{ m}$.

- [ ] **Step 4: Verification kiểm tra bề mặt nổi khối**
  - Chiếu ánh sáng Directional Light xiên góc.
  - Chụp ảnh kiểm tra độ nổi khối của rãnh ron gạch và độ sần hạt của đá.

- [ ] **Step 5: Kiểm tra STOP condition của Phase 8**
  - **STOP condition:** Nếu Normal map bị đảo ngược hoặc vật liệu bị bóng lóa bất thường $\rightarrow$ DỪNG.

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
  4. Performance: Kiểm tra toàn diện 4 chỉ số (GameObjects < 100, Renderers, Triangles, Batches).
  5. Materials: Ngói đỏ đất nung, bờ dải vữa xám, sân gạch đỏ và đá xám chuẩn PBR.
  6. Collision: Player di chuyển trơn tru trên 1 collider phẳng liên tục.
  7. Multi-angle screenshots đầy đủ.

- [ ] **Step 3: Lưu scene và lập báo cáo `walkthrough.md`**
  - Lưu scene `Assets/Scenes/PhongTrienLam.unity`.
  - Cập nhật `walkthrough.md`.
