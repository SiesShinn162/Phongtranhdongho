using UnityEngine;

public class NPCInteract : MonoBehaviour
{
    [Header("Cấu hình hội thoại")]
    public string tenNPC = "NPC Hướng Dẫn";
    public string[] cacCauNoi = new string[]
    {
        "Chào mừng bạn đến với Phòng Tranh Dân Gian Đông Hồ!",
        "Tranh Đông Hồ được in từ các bản khắc gỗ trên giấy điệp truyền thống.",
        "Màu sắc trong tranh hoàn toàn tự nhiên: màu đỏ từ sỏi son, màu vàng từ hoa hòe...",
        "Bạn hãy tiến lại gần các bức tranh và bấm [E] hoặc Click chuột để xem chi tiết và nghe thuyết minh nhé!",
        "Bức tranh Đám Cưới Chuột là một trong những tác phẩm nổi tiếng và châm biếm sâu cay nhất.",
        "Chúc bạn có một buổi tham quan triển lãm thật thú vị và bổ ích!"
    };

    [Header("Giọng nói (file audio)")]
    [Tooltip("Thứ tự phải khớp với danh sách câu nói: Element 0 là giọng của câu 0, v.v.")]
    public AudioClip[] giongNoi;
    [Range(0f, 1f)] public float amLuongGiong = 1f;
    public bool hienPhuDe = true;

    [Header("Khoảng cách & Layer")]
    public float khoangCachToiDa = 5f;
    public LayerMask layerNPC;

    private string cauDangNoi = "";
    private float thoiGianConLai = 0f;
    private bool dangNhinNPC = false;
    private Collider myCollider;
    private AudioSource audioSource;
    private int cauTruoc = -1;

    // Script khác (nhạc nền) hỏi xem NPC có đang nói không
    private static AudioSource giongDangPhat;
    public static bool DangNoi => giongDangPhat != null && giongDangPhat.isPlaying;

    void Awake()
    {
        myCollider = GetComponent<Collider>();
        if (myCollider == null)
        {
            myCollider = gameObject.AddComponent<CapsuleCollider>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D để đứng đâu cũng nghe rõ
    }

    void Start()
    {
        // Tự động gán layerNPC nếu chưa cấu hình
        if (layerNPC.value == 0)
        {
            int npcLayer = LayerMask.NameToLayer("NPC");
            if (npcLayer != -1)
            {
                layerNPC = 1 << npcLayer;
            }
            else
            {
                layerNPC = ~0; // Mặc định nhận diện mọi layer
            }
        }
    }

    private bool wasVrTrigger = false;

    void Update()
    {
        KiemTraTamNhin();

        bool vrTrigger = false;
        if (UnityEngine.XR.XRSettings.isDeviceActive)
        {
            var rightHand = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (rightHand.isValid && rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool pressed))
            {
                vrTrigger = pressed;
            }
        }

        if (dangNhinNPC && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E) || (vrTrigger && !wasVrTrigger)))
        {
            NoiChuyen();
        }
        wasVrTrigger = vrTrigger;

        if (thoiGianConLai > 0)
        {
            thoiGianConLai -= Time.deltaTime;
        }
    }

    void KiemTraTamNhin()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            dangNhinNPC = false;
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, khoangCachToiDa, layerNPC))
        {
            if (hit.collider == myCollider || hit.collider.transform.IsChildOf(transform) || transform.IsChildOf(hit.collider.transform))
            {
                dangNhinNPC = true;
                return;
            }
        }

        dangNhinNPC = false;
    }

    public void NoiChuyen()
    {
        if (cacCauNoi == null || cacCauNoi.Length == 0) return;

        // Chọn ngẫu nhiên, tránh lặp lại đúng câu vừa nói
        int idx = Random.Range(0, cacCauNoi.Length);
        if (cacCauNoi.Length > 1)
        {
            while (idx == cauTruoc) idx = Random.Range(0, cacCauNoi.Length);
        }
        cauTruoc = idx;

        cauDangNoi = cacCauNoi[idx];
        thoiGianConLai = 5f;

        // Phát giọng nói nếu câu này có file audio
        AudioClip clip = (giongNoi != null && idx < giongNoi.Length) ? giongNoi[idx] : null;
        if (clip != null)
        {
            // Dừng thuyết minh tranh để không bị đè tiếng
            if (AudioManager.Instance != null) AudioManager.Instance.DungPhat();

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.volume = amLuongGiong;
            audioSource.Play();
            giongDangPhat = audioSource;

            thoiGianConLai = clip.length + 0.5f; // Phụ đề hiện đến hết câu nói
        }
    }

    void OnGUI()
    {
        if (UnityEngine.XR.XRSettings.isDeviceActive) return;

        // Gợi ý tương tác khi đang nhìn vào NPC ở cự ly gần
        if (dangNhinNPC && thoiGianConLai <= 0)
        {
            GUIStyle promptStyle = new GUIStyle();
            promptStyle.fontSize = 18;
            promptStyle.normal.textColor = Color.white;
            promptStyle.alignment = TextAnchor.MiddleCenter;

            GUI.Box(new Rect(Screen.width / 2 - 120, Screen.height / 2 + 40, 240, 35), "");
            GUI.Label(new Rect(Screen.width / 2 - 110, Screen.height / 2 + 42, 220, 30), "Bấm [E / Click] Nói chuyện", promptStyle);
        }

        // Phụ đề khi NPC đang nói
        if (hienPhuDe && thoiGianConLai > 0)
        {
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.textColor = Color.yellow;
            boxStyle.fontSize = 18;
            boxStyle.alignment = TextAnchor.MiddleCenter;
            boxStyle.wordWrap = true;

            float width = 560;
            float height = 90;
            Rect boxRect = new Rect((Screen.width - width) / 2, Screen.height - height - 40, width, height);

            GUI.Box(boxRect, "");
            GUI.Label(new Rect(boxRect.x + 15, boxRect.y + 10, width - 30, height - 20), "<b>" + tenNPC + ":</b> \"" + cauDangNoi + "\"", boxStyle);
        }
    }
}