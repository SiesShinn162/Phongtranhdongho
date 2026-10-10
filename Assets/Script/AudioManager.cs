using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioSource audioSource;
    private PaintingData tranhHienTai;

    public AudioSource CurrentAudioSource
    {
        get
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            return audioSource;
        }
    }

    public bool IsPlaying => audioSource != null && audioSource.isPlaying;
    public PaintingData TranhHienTai => tranhHienTai;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        var src = CurrentAudioSource;
        src.spatialBlend = 0f; // 2D Stereo để người chơi ở bất kỳ đâu cũng nghe rõ
        src.volume = 1f;
        src.mute = false;
        src.playOnAwake = false;
        // Tắt Play On Awake không dừng clip đã tự phát khi scene được nạp.
        src.Stop();
        src.clip = null;
    }

    void OnEnable()
    {
        PaintingSelector.OnPaintingSelected += GhiNhoTranh;
    }

    void OnDisable()
    {
        PaintingSelector.OnPaintingSelected -= GhiNhoTranh;
    }

    public void GhiNhoTranh(PaintingData tranh)
    {
        tranhHienTai = tranh;
    }

    public void PhatThuyetMinh()
    {
        if (tranhHienTai == null) return;

        var src = CurrentAudioSource;
        if (src == null) return;

        Debug.Log("[AudioManager] Đang xử lý tranh: " + tranhHienTai.tenTranh +
                   " | File audio: " + (tranhHienTai.amThanhThuyetMinh != null ? tranhHienTai.amThanhThuyetMinh.name : "KHÔNG CÓ"));

        if (tranhHienTai.amThanhThuyetMinh == null)
        {
            Debug.LogWarning("[AudioManager] Tranh '" + tranhHienTai.tenTranh + "' chưa có file âm thanh.");
            return;
        }

        src.Stop();
        src.clip = tranhHienTai.amThanhThuyetMinh;
        src.volume = 1f;
        src.spatialBlend = 0f; // Luôn phát 2D toàn dải rõ nét
        src.mute = false;
        src.Play();
    }

    public void PhatThuyetMinh(PaintingData tranh)
    {
        GhiNhoTranh(tranh);
        PhatThuyetMinh();
    }

    public void DungPhat()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}
