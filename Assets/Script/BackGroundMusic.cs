using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    public AudioClip nhacNen;

    [Header("Âm lượng")]
    [Range(0f, 1f)] public float amLuong = 0.3f;          // Âm lượng bình thường
    [Range(0f, 1f)] public float amLuongKhiThuyetMinh = 0.05f; // Âm lượng khi đang nghe thuyết minh

    [Header("Tốc độ chuyển")]
    public float tocDoGiam = 3f;   // Số càng lớn thì nhỏ lại càng nhanh
    public float tocDoTang = 1.5f; // Số càng lớn thì to lại càng nhanh

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = nhacNen;
        audioSource.loop = true;
        audioSource.volume = amLuong;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (nhacNen != null)
        {
            audioSource.Play();
        }
    }

    void Update()
    {
        // Có đang phát thuyết minh không?
        bool dangThuyetMinh = (AudioManager.Instance != null && AudioManager.Instance.IsPlaying) || NPCInteract.DangNoi;

        float mucTieu = dangThuyetMinh ? amLuongKhiThuyetMinh : amLuong;
        float tocDo = dangThuyetMinh ? tocDoGiam : tocDoTang;

        // Đổi âm lượng từ từ cho êm tai, không bị giật
        audioSource.volume = Mathf.MoveTowards(audioSource.volume, mucTieu, tocDo * 0.3f * Time.deltaTime);
    }
}