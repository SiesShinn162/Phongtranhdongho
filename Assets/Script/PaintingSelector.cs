using UnityEngine;

public class PaintingSelector : MonoBehaviour
{
    public delegate void PaintingSelectedHandler(PaintingData painting);
    public static event PaintingSelectedHandler OnPaintingSelected;
    public static event System.Action OnPaintingCleared;

    public static void Select(PaintingData painting)
    {
        if (painting != null) OnPaintingSelected?.Invoke(painting);
    }

    public static void ClearSelection()
    {
        OnPaintingCleared?.Invoke();
    }

    [Header("Cấu hình tương tác")]
    public float khoangCachToiDa = 5f;
    public LayerMask layerTranh;

    [Header("Dung sai tranh sát tường")]
    public float dungSaiVatCan = 0.5f;

    private bool dangNhinTranh = false;
    private PaintingInfo tranhDangNhin = null;

    void Start()
    {
        // Tự động thêm Layer Tranh và Default để quét trúng mọi tranh
        int tranhLayer = LayerMask.NameToLayer("Tranh");
        if (tranhLayer != -1)
        {
            layerTranh |= (1 << tranhLayer);
        }
        layerTranh |= (1 << 0); // Bao gồm cả Default
    }

    void Update()
    {
        if (UnityEngine.XR.XRSettings.isDeviceActive)
        {
            dangNhinTranh = false;
            return;
        }

        KiemTraTamNhinTranh();


// Không tương tác khi bảng đang mở / vừa đóng
        if (DebugPaintingUI.DangMo || Time.frameCount <= DebugPaintingUI.KhungHinhDong) return;

// Tương tác bằng Chuột trái hoặc phím E
        if (dangNhinTranh && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E)))
        // Tương tác bằng Chuột trái hoặc phím E
        if (dangNhinTranh && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E)))
        {
            if (tranhDangNhin != null && tranhDangNhin.data != null)
            {
                Select(tranhDangNhin.data);
            }
        }
    }

    // Tìm tranh đang nhìn: bắn tia riêng vào layer Tranh (bỏ qua tường),
    // rồi kiểm tra có vật cản thật sự phía trước tranh hay không.
    public static PaintingInfo TimTranh(Ray ray, float maxDist, int mask, float dungSai)
    {
        int tranhLayer = LayerMask.NameToLayer("Tranh");
        if (tranhLayer == -1) return null;
        int maskTranh = 1 << tranhLayer;

        if (!Physics.Raycast(ray, out RaycastHit hitTranh, maxDist, maskTranh,
                             QueryTriggerInteraction.Collide))
            return null;

        PaintingInfo info = hitTranh.collider.GetComponentInParent<PaintingInfo>();
        if (info == null) info = hitTranh.collider.GetComponentInChildren<PaintingInfo>();
        if (info == null || info.data == null) return null;

        // Có vật cản (người, cột, tường phòng khác) nằm trước tranh không?
        float khoangCachKiemTra = hitTranh.distance - dungSai;
        if (khoangCachKiemTra > 0f)
        {
            int maskVatCan = mask & ~maskTranh;
            if (Physics.Raycast(ray, khoangCachKiemTra, maskVatCan,
                                QueryTriggerInteraction.Ignore))
                return null;
        }
        return info;
    }

    void KiemTraTamNhinTranh()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            dangNhinTranh = false;
            tranhDangNhin = null;
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        PaintingInfo info = TimTranh(ray, khoangCachToiDa, layerTranh, dungSaiVatCan);

        if (info != null)
        {
            dangNhinTranh = true;
            tranhDangNhin = info;
            return;
        }

        dangNhinTranh = false;
        tranhDangNhin = null;
    }

    void OnGUI()
    {
        if (UnityEngine.XR.XRSettings.isDeviceActive) return;

        // Nếu DebugPaintingUI đang mở hộp thoại tranh thì không hiện prompt nhắm
        var debugUI = GetComponent<DebugPaintingUI>() ?? FindAnyObjectByType<DebugPaintingUI>();
        if (debugUI != null && debugUI.IsShowing) return;

        // Tâm ngắm và gợi ý khi nhìn vào tranh
        if (dangNhinTranh && tranhDangNhin != null && tranhDangNhin.data != null)
        {
            GUIStyle promptStyle = new GUIStyle();
            promptStyle.fontSize = 18;
            promptStyle.normal.textColor = Color.white;
            promptStyle.alignment = TextAnchor.MiddleCenter;

            float w = 260;
            Rect r = new Rect((Screen.width - w) / 2, Screen.height / 2 + 35, w, 32);

            GUI.Box(r, "");
            GUI.Label(r, "Bấm [E / Click] Xem tranh", promptStyle);
        }
    }
}