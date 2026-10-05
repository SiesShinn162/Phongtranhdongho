using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    public string[] cacCauNoi;
    public float khoangCachToiDa = 3f;
    public LayerMask layerNPC;

    private string cauDangNoi = "";
    private float thoiGianConLai = 0f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            KiemTraChonNPC();
        }

        if (thoiGianConLai > 0) thoiGianConLai -= Time.deltaTime;
    }

    void KiemTraChonNPC()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, khoangCachToiDa, layerNPC))
        {
            if (cacCauNoi.Length == 0) return;
            int idx = Random.Range(0, cacCauNoi.Length);
            cauDangNoi = cacCauNoi[idx];
            thoiGianConLai = 4f;
        }
    }

    void OnGUI()
    {
        if (thoiGianConLai <= 0) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.yellow;
        style.alignment = TextAnchor.MiddleCenter;
        style.wordWrap = true;

        GUI.Box(new Rect(Screen.width/2 - 220, Screen.height - 110, 440, 70), "");
        GUI.Label(new Rect(Screen.width/2 - 210, Screen.height - 100, 420, 50), cauDangNoi, style);
    }
}