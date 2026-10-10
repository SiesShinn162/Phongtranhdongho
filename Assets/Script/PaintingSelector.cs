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

        // Tương tác bằng Chuột trái hoặc phím E
        if (dangNhinTranh && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E)))
        {
            if (tranhDangNhin != null && tranhDangNhin.data != null)
            {
                Select(tranhDangNhin.data);
            }
        }
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
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, khoangCachToiDa, layerTranh))
        {
            PaintingInfo info = hit.collider.GetComponentInParent<PaintingInfo>() ?? hit.collider.GetComponentInChildren<PaintingInfo>();
            if (info != null && info.data != null)
            {
                dangNhinTranh = true;
                tranhDangNhin = info;
                return;
            }
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

            string ten = tranhDangNhin.data.tenTranh;
            float w = 260;
            Rect r = new Rect((Screen.width - w) / 2, Screen.height / 2 + 35, w, 32);

            GUI.Box(r, "");
            GUI.Label(r, "Bấm [E / Click] Xem tranh", promptStyle);
        }
    }
}
