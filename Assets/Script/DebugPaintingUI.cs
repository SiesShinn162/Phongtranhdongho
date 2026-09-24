using UnityEngine;

public class DebugPaintingUI : MonoBehaviour
{
    private PaintingData trangThaiHienTai;
    private Vector2 cuonTrang = Vector2.zero;

    void OnEnable() { PaintingSelector.OnPaintingSelected += CapNhatThongTin; }
    void OnDisable() { PaintingSelector.OnPaintingSelected -= CapNhatThongTin; }

    void CapNhatThongTin(PaintingData tranh)
    {
        trangThaiHienTai = tranh;
        cuonTrang = Vector2.zero; // mỗi tranh mới thì cuộn về đầu trang
    }

    void OnGUI()
    {
        if (trangThaiHienTai == null) return;

        float rongBang = 400;
        float caoBang = 190;

        GUI.Box(new Rect(20, 20, rongBang, caoBang), "");

        GUIStyle kieuTen = new GUIStyle();
        kieuTen.fontSize = 24;
        kieuTen.normal.textColor = Color.white;
        GUI.Label(new Rect(30, 30, rongBang - 20, 30), "Tên: " + trangThaiHienTai.tenTranh, kieuTen);

        GUIStyle kieuMoTa = new GUIStyle();
        kieuMoTa.fontSize = 16;
        kieuMoTa.normal.textColor = Color.white;
        kieuMoTa.wordWrap = true; // ← chốt sửa lỗi tràn chữ

        Rect vungHienThi = new Rect(30, 65, rongBang - 20, 75); // khung cố định, cao 75px
        float caoNoiDungThat = kieuMoTa.CalcHeight(new GUIContent(trangThaiHienTai.moTa), vungHienThi.width - 20);
        Rect vungNoiDung = new Rect(0, 0, vungHienThi.width - 20, caoNoiDungThat);

        cuonTrang = GUI.BeginScrollView(vungHienThi, cuonTrang, vungNoiDung);
        GUI.Label(vungNoiDung, trangThaiHienTai.moTa, kieuMoTa);
        GUI.EndScrollView();

        if (GUI.Button(new Rect(30, 150, 150, 35), "▶ Nghe thuyết minh"))
        {
            AudioManager.Instance.PhatThuyetMinh();
        }
        if (GUI.Button(new Rect(190, 150, 80, 35), "Đóng"))
        {
            trangThaiHienTai = null;
            AudioManager.Instance.DungPhat();
        }
    }
}