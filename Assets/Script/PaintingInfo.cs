using UnityEngine;

public class PaintingInfo : MonoBehaviour
{
    public PaintingData data;

    void Start()
    {
        if (data != null && data.anhTranh != null)
        {
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.SetTexture("_BaseMap", data.anhTranh.texture);
                rend.material.SetTexture("_MainTex", data.anhTranh.texture);
            }
        }
    }
}