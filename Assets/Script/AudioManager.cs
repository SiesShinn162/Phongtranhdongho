using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    private AudioSource audioSource;
    private PaintingData tranhHienTai;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
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

        Debug.Log("Đang xử lý tranh: " + tranhHienTai.tenTranh + 
                   " | File audio: " + (tranhHienTai.amThanhThuyetMinh != null ? tranhHienTai.amThanhThuyetMinh.name : "KHÔNG CÓ"));

        if (tranhHienTai.amThanhThuyetMinh == null)
        {
            Debug.Log("Tranh " + tranhHienTai.tenTranh + " chưa có file âm thanh.");
            return;
        }

        audioSource.Stop();
        audioSource.clip = tranhHienTai.amThanhThuyetMinh;
        audioSource.Play();
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