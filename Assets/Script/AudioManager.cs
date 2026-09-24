using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
   
    private AudioSource audioSource;
    private PaintingData tranhHienTai;

    void Awake()
    {
        if (Instance == null) {Instance = this;}       
        else { Destroy(gameObject);return;}
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
void GhiNhoTranh(PaintingData tranh)
{
    tranhHienTai = tranh;
}

    public void PhatThuyetMinh()
    {
        if (tranhHienTai.amThanhThuyetMinh == null)
        {
            Debug.Log("Tranh"+ tranhHienTai.tenTranh +"chưa có file âm thanh.");
            return;
        }
        audioSource.Stop();
        audioSource.clip = tranhHienTai.amThanhThuyetMinh;
        audioSource.Play();
    }
    public void DungPhat()
{
    audioSource.Stop();
}
}