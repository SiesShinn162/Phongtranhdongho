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

    [Header("Cấu hình")]
    public float khoangCachToiDa = 5f;
    public LayerMask layerTranh;

    void Update()
    {
        if (!UnityEngine.XR.XRSettings.isDeviceActive && Input.GetMouseButtonDown(0))
        {
            KiemTraChonTranh();
        }
    }

    void KiemTraChonTranh()
    {
        if (Camera.main == null) return;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, khoangCachToiDa, layerTranh))
        {
            PaintingInfo info = hit.collider.GetComponentInParent<PaintingInfo>();

            if (info != null)
            {
                Select(info.data);
            }
        }
    }
}
