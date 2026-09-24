using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    public AudioClip nhacNen;
    [Range(0f, 1f)] public float amLuong = 0.3f;

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
}