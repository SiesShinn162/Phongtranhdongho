using UnityEngine;

public class DebugPaintingUI : MonoBehaviour
{
    private PaintingData trangThaiHienTai;
    private Vector2 cuonTrang = Vector2.zero;

    public bool IsShowing => trangThaiHienTai != null;

    void OnEnable()
    {
        PaintingSelector.OnPaintingSelected += CapNhatThongTin;
        PaintingSelector.OnPaintingCleared += Dong;
    }

    void OnDisable()
    {
        PaintingSelector.OnPaintingSelected -= CapNhatThongTin;
        PaintingSelector.OnPaintingCleared -= Dong;
    }

    void Update()
    {
        if (trangThaiHienTai != null)
        {
            // Bấm Escape hoặc Q để đóng bảng tranh
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
            {
                Dong();
                return;
            }

            // Phím tắt T để nghe / dừng thuyết minh
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleThuyetMinh();
            }
        }
    }

    void CapNhatThongTin(PaintingData tranh)
    {
        trangThaiHienTai = tranh;
        cuonTrang = Vector2.zero;

        if (tranh != null && !UnityEngine.XR.XRSettings.isDeviceActive)
        {
            // Mở khóa chuột để người dùng click nút xem/nghe trên UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Dong()
    {
        trangThaiHienTai = null;
        var am = AudioManager.Instance ?? FindAnyObjectByType<AudioManager>();
        if (am != null)
        {
            am.DungPhat();
        }

        if (!UnityEngine.XR.XRSettings.isDeviceActive)
        {
            // Khóa lại chuột cho bộ điều khiển góc nhìn thứ nhất
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void ToggleThuyetMinh()
    {
        var am = AudioManager.Instance ?? FindAnyObjectByType<AudioManager>();
        if (am == null) return;

        if (am.IsPlaying)
        {
            am.DungPhat();
        }
        else
        {
            am.PhatThuyetMinh();
        }
    }

    void OnGUI()
    {
        if (UnityEngine.XR.XRSettings.isDeviceActive) return;
        if (trangThaiHienTai == null) return;

        var am = AudioManager.Instance ?? FindAnyObjectByType<AudioManager>();
        bool isPlaying = am != null && am.IsPlaying;
        bool hasAudio = trangThaiHienTai.amThanhThuyetMinh != null;

        // Chiều rộng rộng rãi, vừa vặn màn hình
        float rongBang = Mathf.Min(680f, Screen.width - 60f);

        // Style chữ mô tả
        GUIStyle kieuMoTa = new GUIStyle(GUI.skin.label);
        kieuMoTa.fontSize = 16;
        kieuMoTa.normal.textColor = Color.white;
        kieuMoTa.wordWrap = true;

        float chieuRongVanBan = rongBang - 45f;
        string textMoTa = string.IsNullOrEmpty(trangThaiHienTai.moTa) ? "Chưa có mô tả cho bức tranh này." : trangThaiHienTai.moTa;
        float caoNoiDungThat = kieuMoTa.CalcHeight(new GUIContent(textMoTa), chieuRongVanBan);

        // Chiều cao tự động thích ứng với độ dài văn bản để không phải cuộn chuột
        float maxChoPhep = Mathf.Max(200f, Screen.height - 230f);
        float caoVungHienThi = Mathf.Clamp(caoNoiDungThat + 12f, 150f, maxChoPhep);

        float caoBang = caoVungHienThi + 150f;
        Rect khungBang = new Rect(30, 30, rongBang, caoBang);

        // Hộp nền
        GUI.Box(khungBang, "");

        // Tên tranh
        GUIStyle kieuTen = new GUIStyle();
        kieuTen.fontSize = 22;
        kieuTen.fontStyle = FontStyle.Bold;
        kieuTen.normal.textColor = Color.yellow;
        GUI.Label(new Rect(48, 45, rongBang - 35, 32), trangThaiHienTai.tenTranh, kieuTen);

        // Vùng hiển thị mô tả
        Rect vungHienThi = new Rect(48, 85, chieuRongVanBan, caoVungHienThi);
        Rect vungNoiDung = new Rect(0, 0, chieuRongVanBan - 15f, Mathf.Max(caoNoiDungThat, caoVungHienThi));

        cuonTrang = GUI.BeginScrollView(vungHienThi, cuonTrang, vungNoiDung);
        GUI.Label(vungNoiDung, textMoTa, kieuMoTa);
        GUI.EndScrollView();

        // Vị trí thanh điều khiển ở đáy bảng
        float yNut = khungBang.y + caoBang - 52f;

        // Nút Nghe / Dừng thuyết minh
        string btnText = isPlaying ? "⏹ Dừng nghe [T]" : "▶ Nghe thuyết minh [T]";
        if (GUI.Button(new Rect(48, yNut, 210, 38), btnText))
        {
            ToggleThuyetMinh();
        }

        // Nhãn trạng thái âm thanh
        GUIStyle kieuTrangThai = new GUIStyle();
        kieuTrangThai.fontSize = 14;
        kieuTrangThai.alignment = TextAnchor.MiddleLeft;

        if (isPlaying)
        {
            kieuTrangThai.normal.textColor = Color.green;
            GUI.Label(new Rect(270, yNut + 5, 230, 28), "♫ Đang phát âm thanh...", kieuTrangThai);
        }
        else if (hasAudio)
        {
            kieuTrangThai.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            GUI.Label(new Rect(270, yNut + 5, 230, 28), "Có bản thuyết minh", kieuTrangThai);
        }
        else
        {
            kieuTrangThai.normal.textColor = Color.gray;
            GUI.Label(new Rect(270, yNut + 5, 230, 28), "Chưa có file âm thanh", kieuTrangThai);
        }

        // Nút Đóng
        float xDong = khungBang.x + rongBang - 120f;
        if (GUI.Button(new Rect(xDong, yNut, 100, 38), "Đóng [Q/Esc]"))
        {
            Dong();
        }
    }
}