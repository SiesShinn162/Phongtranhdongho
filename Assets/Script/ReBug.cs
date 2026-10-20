using UnityEngine;

public class ReBug : MonoBehaviour
{
    public LayerMask layerTranh;
    public float khoangCach = 5f; // bằng giá trị trên object chọn tranh

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        Camera cam = Camera.main;
        if (cam == null) { Debug.Log("[RayDebug] Không có Camera.main"); return; }
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit all, 100f))
            Debug.Log("[RayDebug] Trúng gần nhất: " + all.collider.name
                + " | layer=" + LayerMask.LayerToName(all.collider.gameObject.layer)
                + " | cách " + all.distance.ToString("F1") + "m");
        else
            Debug.Log("[RayDebug] Tia không trúng collider nào");

        if (Physics.Raycast(ray, out RaycastHit tranh, khoangCach, layerTranh))
        {
            var info = tranh.collider.GetComponent<PaintingInfo>();
            Debug.Log("[RayDebug] Selector thấy: " + tranh.collider.name
                + " | có PaintingInfo=" + (info != null)
                + " | có Data=" + (info != null && info.data != null));
        }
        else
            Debug.Log("[RayDebug] Selector KHÔNG thấy gì");
    }
}