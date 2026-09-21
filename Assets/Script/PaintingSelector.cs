using UnityEngine;
using UnityEngine.XR;

public class PaintingSelector : MonoBehaviour
{
    public delegate void PaintingSelectedHandler(PaintingData painting);
    public static event PaintingSelectedHandler OnPaintingSelected;

    [Header("Cấu hình")]
    public float khoangCachToiDa = 5f;
    public LayerMask layerTranh;

    private bool nutDangNhan = false;

    void Update()
    {
        bool nutTayCamDuocNhan = KiemTraNutTrigger();

        // Vẫn giữ chuột để lỡ cần test nhanh không đeo kính
        bool nutChuotDuocNhan = Input.GetMouseButtonDown(0);

        if ((nutTayCamDuocNhan && !nutDangNhan) || nutChuotDuocNhan)
        {
            KiemTraChonTranh();
        }

        nutDangNhan = nutTayCamDuocNhan;
    }

    bool KiemTraNutTrigger()
    {
        InputDevice tayPhai = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        bool dangNhan;
        if (tayPhai.TryGetFeatureValue(CommonUsages.triggerButton, out dangNhan))
        {
            return dangNhan;
        }
        return false;
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