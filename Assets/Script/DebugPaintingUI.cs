using UnityEngine;
using UnityEngine.UI;

// Bảng thông tin tranh kiểu giấy dó (uGUI, dựng hoàn toàn bằng code).
// Giữ tên class DebugPaintingUI để không phải gắn lại component trong scene.
public class DebugPaintingUI : MonoBehaviour
{
    public static bool DangMo { get; private set; }
    public static int KhungHinhDong = -10;   // frame vừa đóng bảng, để PaintingSelector không mở lại ngay

    [Header("Font (để trống = tự chọn Georgia / Times New Roman)")]
    public Font fontChu;

    [Header("Kích thước bảng (theo canvas 1920x1080)")]
    public Vector2 kichThuocBang = new Vector2(1500, 860);

    // Bảng màu
    static readonly Color MauGiay    = new Color(0.93f, 0.87f, 0.74f);
    static readonly Color MauNenTranh = new Color(0.82f, 0.74f, 0.58f);
    static readonly Color MauVienNau = new Color(0.30f, 0.19f, 0.11f);
    static readonly Color MauNavy    = new Color(0.10f, 0.17f, 0.31f);
    static readonly Color MauNavyTren = new Color(0.17f, 0.27f, 0.47f);
    static readonly Color MauChuNau  = new Color(0.27f, 0.19f, 0.12f);
    static readonly Color MauVang    = new Color(0.78f, 0.62f, 0.33f);

    GameObject root;
    Image anhTranh;
    RectTransform content, viewport;
    RectTransform nutNgheRect, nutDongRect;
    Image nutNgheImg, nutDongImg;
    Text nutNgheText;
    Font fontMacDinh;
    static Sprite boTron;

    PaintingData hienTai;
    StarterAssets.FirstPersonController nguoiChoi;
    StarterAssets.StarterAssetsInputs nguoiChoiInput;
    bool inputCoCursorLocked;
    int khungMo = -1;
    float cuon;

    public bool IsShowing => hienTai != null;

    // ---------------------------------------------------------------- vòng đời
    void Awake()
    {
        BuildUI();
        root.SetActive(false);
    }

    void OnEnable()
    {
        PaintingSelector.OnPaintingSelected += HienBang;
        PaintingSelector.OnPaintingCleared += Dong;
    }

    void OnDisable()
    {
        PaintingSelector.OnPaintingSelected -= HienBang;
        PaintingSelector.OnPaintingCleared -= Dong;
        if (hienTai != null)
        {
            hienTai = null;
            DangMo = false;
            if (root != null) root.SetActive(false);
            KhoaNguoiChoi(false);
        }
    }

    // ---------------------------------------------------------------- hiển thị
    void HienBang(PaintingData tranh)
    {
        if (tranh == null || UnityEngine.XR.XRSettings.isDeviceActive) return;

        hienTai = tranh;
        DangMo = true;
        khungMo = Time.frameCount;
        cuon = 0f;

        anhTranh.sprite = tranh.anhTranh;
        anhTranh.enabled = tranh.anhTranh != null;

        root.SetActive(true);
        DungNoiDung(tranh);

        KhoaNguoiChoi(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Dong()
    {
        if (hienTai == null) return;
        hienTai = null;
        DangMo = false;
        KhungHinhDong = Time.frameCount;
        if (root != null) root.SetActive(false);

        var am = LayAm();
        if (am != null) am.DungPhat();

        KhoaNguoiChoi(false);

        if (!UnityEngine.XR.XRSettings.isDeviceActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void DungNoiDung(PaintingData t)
    {
        // Xóa nội dung cũ
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var c = content.GetChild(i);
            c.SetParent(null, false);
            Destroy(c.gameObject);
        }

        ThemChu("TÊN TÁC PHẨM", 26, new Color(0.55f, 0.38f, 0.20f), FontStyle.Bold);
        ThemChu(string.IsNullOrEmpty(t.tenTranh) ? "(Chưa có tên)" : t.tenTranh, 46, MauNavy, FontStyle.Bold);
        ThemDuongKe();

        bool coYNghia = !string.IsNullOrWhiteSpace(t.yNghia);
        bool coNguon = !string.IsNullOrWhiteSpace(t.nguonGoc);

        if (coYNghia) ThemMuc("Ý NGHĨA", t.yNghia);
        if (coYNghia && coNguon) ThemDuongKe();
        if (coNguon) ThemMuc("NGUỒN GỐC", t.nguonGoc);

        if (!coYNghia && !coNguon)
        {
            string mt = string.IsNullOrWhiteSpace(t.moTa) ? "Chưa có mô tả cho bức tranh này." : t.moTa;
            ThemMuc("MÔ TẢ", mt);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        content.anchoredPosition = Vector2.zero;
    }

    void ThemMuc(string tieuDe, string noiDung)
    {
        ThemChu(tieuDe, 34, MauNavy, FontStyle.Bold);
        ThemChu(noiDung, 30, MauChuNau, FontStyle.Normal);
    }

    void ThemChu(string s, int size, Color mau, FontStyle kieu)
    {
        var rt = NewRect("Chu", content);
        Chu(rt, s, size, mau, kieu, TextAnchor.UpperLeft);
    }

    void ThemDuongKe()
    {
        var rt = NewRect("DuongKe", content);
        Anh(rt, MauVang, false);
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 3; le.preferredHeight = 3;
    }

    // ---------------------------------------------------------------- cập nhật
    void Update()
    {
        if (hienTai == null) return;
        if (Time.frameCount == khungMo) return; // bỏ qua input của chính lần click mở bảng

        var am = LayAm();
        bool dangPhat = am != null && am.IsPlaying;
        bool coAm = hienTai.amThanhThuyetMinh != null;
        nutNgheText.text = !coAm ? "Chưa có thuyết minh" : (dangPhat ? "Dừng nghe   [T]" : "Nghe thuyết minh   [T]");

        Vector2 chuot = Input.mousePosition;
        bool tren_nghe = RectTransformUtility.RectangleContainsScreenPoint(nutNgheRect, chuot, null);
        bool tren_dong = RectTransformUtility.RectangleContainsScreenPoint(nutDongRect, chuot, null);
        nutNgheImg.color = tren_nghe ? MauNavyTren : MauNavy;
        nutDongImg.color = tren_dong ? MauNavyTren : MauNavy;

        // Phím tắt
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q)) { Dong(); return; }
        if (Input.GetKeyDown(KeyCode.T)) ToggleThuyetMinh();

        // Click nút
        if (Input.GetMouseButtonDown(0))
        {
            if (tren_nghe) ToggleThuyetMinh();
            else if (tren_dong) { Dong(); return; }
        }

        // Cuộn chuột khi nội dung dài
        float delta = Input.mouseScrollDelta.y;
        if (Mathf.Abs(delta) > 0.01f)
        {
            float toiDa = Mathf.Max(0f, content.rect.height - viewport.rect.height);
            cuon = Mathf.Clamp(cuon - delta * 70f, 0f, toiDa);
            content.anchoredPosition = new Vector2(0f, cuon);
        }
    }

    void ToggleThuyetMinh()
    {
        var am = LayAm();
        if (am == null) return;
        if (am.IsPlaying) am.DungPhat();
        else am.PhatThuyetMinh();
    }

    // Khi bảng mở: dừng xoay camera và đi lại để chuột tự do không làm view xoay
    void KhoaNguoiChoi(bool khoa)
    {
        if (khoa)
        {
            nguoiChoi = FindAnyObjectByType<StarterAssets.FirstPersonController>();
            if (nguoiChoi == null) return;
            nguoiChoiInput = nguoiChoi.GetComponent<StarterAssets.StarterAssetsInputs>();
            if (nguoiChoiInput != null)
            {
                inputCoCursorLocked = nguoiChoiInput.cursorLocked;
                nguoiChoiInput.cursorLocked = false;   // tránh bị khóa chuột lại khi quay lại cửa sổ game
                nguoiChoiInput.look = Vector2.zero;
                nguoiChoiInput.move = Vector2.zero;
            }
            nguoiChoi.enabled = false;
        }
        else
        {
            if (nguoiChoi != null) nguoiChoi.enabled = true;
            if (nguoiChoiInput != null)
            {
                nguoiChoiInput.cursorLocked = inputCoCursorLocked;
                nguoiChoiInput.look = Vector2.zero;
                nguoiChoiInput.move = Vector2.zero;
            }
            nguoiChoi = null;
            nguoiChoiInput = null;
        }
    }

    AudioManager LayAm()
    {
        if (AudioManager.Instance != null) return AudioManager.Instance;
        return FindAnyObjectByType<AudioManager>();
    }

    // ---------------------------------------------------------------- dựng UI
    void BuildUI()
    {
        var cgo = new GameObject("PaintingInfoCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        cgo.transform.SetParent(transform, false);
        root = cgo;

        var canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = cgo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Nền mờ
        var dim = NewRect("NenMo", cgo.transform);
        Gian(dim, 0, 0, 0, 0);
        Anh(dim, new Color(0, 0, 0, 0.6f), false);

        // Khung ngoài (viền nâu)
        var bang = NewRect("Bang", dim);
        bang.anchorMin = bang.anchorMax = bang.pivot = new Vector2(0.5f, 0.5f);
        bang.sizeDelta = kichThuocBang;
        Anh(bang, MauVienNau, true);

        // Nền giấy
        var giay = NewRect("Giay", bang);
        Gian(giay, 12, 12, 12, 12);
        Anh(giay, MauGiay, true);

        // --- Khung tranh bên trái
        var khung = NewRect("KhungTranh", giay);
        khung.anchorMin = new Vector2(0, 0); khung.anchorMax = new Vector2(0, 1);
        khung.offsetMin = new Vector2(40, 150); khung.offsetMax = new Vector2(500, -40);
        Anh(khung, MauVienNau, false);

        var nenTranh = NewRect("NenTranh", khung);
        Gian(nenTranh, 10, 10, 10, 10);
        Anh(nenTranh, MauNenTranh, false);

        var anhRt = NewRect("AnhTranh", nenTranh);
        Gian(anhRt, 18, 18, 18, 18);
        anhTranh = anhRt.gameObject.AddComponent<Image>();
        anhTranh.preserveAspect = true;
        anhTranh.raycastTarget = false;

        // --- Vùng nội dung bên phải
        viewport = NewRect("Viewport", giay);
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(540, 150); viewport.offsetMax = new Vector2(-40, -40);
        viewport.gameObject.AddComponent<RectMask2D>();

        content = NewRect("NoiDung", viewport);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 0, 20);
        vlg.spacing = 14;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        var csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // --- Thanh nút bên dưới
        var thanh = NewRect("Thanh", giay);
        thanh.anchorMin = new Vector2(0, 0); thanh.anchorMax = new Vector2(1, 0);
        thanh.offsetMin = new Vector2(40, 30); thanh.offsetMax = new Vector2(-40, 120);
        var thanhImg = Anh(thanh, MauVienNau, true);

        var thanhTrong = NewRect("ThanhTrong", thanh);
        Gian(thanhTrong, 5, 5, 5, 5);
        Anh(thanhTrong, new Color(0.07f, 0.12f, 0.24f), true);

        TaoNut(thanh, "NutNghe", 0f, 0.62f, new Vector2(18, 14), new Vector2(-8, -14),
               "Nghe thuyết minh   [T]", out nutNgheRect, out nutNgheImg, out nutNgheText);
        TaoNut(thanh, "NutDong", 0.62f, 1f, new Vector2(8, 14), new Vector2(-18, -14),
               "Đóng   [Q / Esc]", out nutDongRect, out nutDongImg, out _);
    }

    void TaoNut(RectTransform cha, string ten, float xMin, float xMax, Vector2 offMin, Vector2 offMax,
                string nhan, out RectTransform rt, out Image img, out Text txt)
    {
        rt = NewRect(ten, cha);
        rt.anchorMin = new Vector2(xMin, 0); rt.anchorMax = new Vector2(xMax, 1);
        rt.offsetMin = offMin; rt.offsetMax = offMax;
        img = Anh(rt, MauNavy, true);
        var vien = rt.gameObject.AddComponent<Outline>();
        vien.effectColor = MauVang;
        vien.effectDistance = new Vector2(2, -2);

        var chuRt = NewRect("Nhan", rt);
        Gian(chuRt, 10, 0, 10, 0);
        txt = Chu(chuRt, nhan, 32, new Color(0.96f, 0.92f, 0.82f), FontStyle.Bold, TextAnchor.MiddleCenter);
    }

    // ---------------------------------------------------------------- tiện ích
    RectTransform NewRect(string ten, Transform cha)
    {
        var go = new GameObject(ten, typeof(RectTransform));
        go.transform.SetParent(cha, false);
        return (RectTransform)go.transform;
    }

    static void Gian(RectTransform rt, float l, float b, float r, float t)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
    }

    static Image Anh(RectTransform rt, Color mau, bool tron)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = mau;
        img.raycastTarget = false;
        if (tron) { img.sprite = BoTron(); img.type = Image.Type.Sliced; }
        return img;
    }

    Text Chu(RectTransform rt, string s, int size, Color mau, FontStyle kieu, TextAnchor can)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = LayFont();
        t.text = s;
        t.fontSize = size;
        t.color = mau;
        t.fontStyle = kieu;
        t.alignment = can;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.lineSpacing = 1.15f;
        t.supportRichText = false;
        t.raycastTarget = false;
        return t;
    }

    Font LayFont()
    {
        if (fontChu != null) return fontChu;
        if (fontMacDinh == null)
        {
            fontMacDinh = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "Arial" }, 32);
            if (fontMacDinh == null) fontMacDinh = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return fontMacDinh;
    }

    // Sprite bo góc 9-slice tạo bằng code (không cần file ảnh)
    static Sprite BoTron()
    {
        if (boTron != null) return boTron;
        const int n = 96; const float r = 28f;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float dx = Mathf.Max(r - fx, fx - (n - r), 0f);
                float dy = Mathf.Max(r - fy, fy - (n - r), 0f);
                float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        boTron = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
                               SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        return boTron;
    }
}