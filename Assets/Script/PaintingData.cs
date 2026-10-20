using UnityEngine;

[CreateAssetMenu(fileName = "NewPainting", menuName = "PhongTranh/Painting Data")]
public class PaintingData : ScriptableObject
{
    public string tenTranh;

    [Header("Nội dung hiển thị trên bảng (mới)")]
    [TextArea(3, 8)] public string yNghia;
    [TextArea(3, 8)] public string nguonGoc;

    [Header("Mô tả chung (dùng cho VR và làm dự phòng)")]
    [TextArea] public string moTa;

    public Sprite anhTranh;
    public AudioClip amThanhThuyetMinh;
}