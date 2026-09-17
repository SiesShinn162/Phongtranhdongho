using UnityEngine;

public class PaintingSelector : MonoBehaviour
{
    public delegate void PaintingSelectedHandler(PaintingData painting);
    public static event PaintingSelectedHandler OnPaintingSelected;

    [Header("Cấu hình")]
    public float khoangCachToiDa = 5f;
    public LayerMask layerTranh;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            KiemTraChonTranh();
        }
    }

    void KiemTraChonTranh()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, khoangCachToiDa, layerTranh))
        {
            PaintingInfo info = hit.collider.GetComponent<PaintingInfo>();

            if (info != null)
            {
                OnPaintingSelected?.Invoke(info.data);
            }
        }
    }
}