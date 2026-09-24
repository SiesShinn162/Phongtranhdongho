using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    private AudioSource audioSource;

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
        PaintingSelector.OnPaintingSelected += PhatThuyetMinh;
    }
    void OnDisable()
    {
        PaintingSelector.OnPaintingSelected -= PhatThuyetMinh;
    }
    void PhatThuyetMinh(PaintingData tranh)
    {
        if (tranh.amThanhThuyetMinh == null)
        {
            Debug.Log("Tranh"+ tranh.tenTranh +"chưa có file âm thanh.");
            return;
        }
        audioSource.Stop();
        audioSource.clip = tranh.amThanhThuyetMinh;
        audioSource.Play();
    }
}