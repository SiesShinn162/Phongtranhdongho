using UnityEngine;

public class DebugPaintingUI : MonoBehaviour
{
    private PaintingData trangThaiHienTai;

    void OnEnable ()
    {
        PaintingSelector.OnPaintingSelected += CapNhatThongTin;
    }
    void OnDisable ()
    {
        PaintingSelector.OnPaintingSelected -= CapNhatThongTin;
    }
    void CapNhatThongTin(PaintingData tranh)
    {
        trangThaiHienTai = tranh;
    }
    void OnGUI()
    {
        if (UnityEngine.XR.XRSettings.isDeviceActive) return;
        if (trangThaiHienTai == null) return;

        GUIStyle style = new GUIStyle();
        style.fontSize =24;
        style.normal.textColor = Color.white;

        GUI.Box(new Rect(20, 20, 400, 100),"");
        GUI.Label(new Rect(30, 30, 380, 40),"Tên:"+ trangThaiHienTai.tenTranh, style);

        style.fontSize = 16;
        GUI.Label(new Rect(30, 70, 380, 60), trangThaiHienTai.moTa, style);
    }

}
