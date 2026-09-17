using UnityEngine;

[CreateAssetMenu(fileName = "NewPainting", menuName = "PhongTranh/Painting Data")]
public class PaintingData : ScriptableObject
{
    public string tenTranh;
    [TextArea] public string moTa;
    public Sprite anhTranh;
    public AudioClip amThanhThuyetMinh;
}
