using TMPro;
using UnityEngine;

public class XRPaintingPanel : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text description;
    [SerializeField] private Transform view;
    [SerializeField, Min(0.5f)] private float readingDistance = 1.5f;

    private void Awake()
    {
        if (canvas == null) canvas = GetComponent<Canvas>();
    }

    private void OnEnable()
    {
        PaintingSelector.OnPaintingSelected += Show;
        PaintingSelector.OnPaintingCleared += Hide;
        Hide();
    }

    private void OnDisable()
    {
        PaintingSelector.OnPaintingSelected -= Show;
        PaintingSelector.OnPaintingCleared -= Hide;
    }

    private void Show(PaintingData painting)
    {
        if (!UnityEngine.XR.XRSettings.isDeviceActive || painting == null || canvas == null) return;
        if (title != null) title.text = painting.tenTranh;
        if (description != null) description.text = painting.moTa;
        canvas.enabled = true;
    }

    private void Hide()
    {
        if (canvas != null) canvas.enabled = false;
    }

    private void LateUpdate()
    {
        if (canvas == null || !canvas.enabled) return;
        Transform cameraTransform = view != null ? view : Camera.main != null ? Camera.main.transform : null;
        if (cameraTransform == null) return;
        transform.position = cameraTransform.position + cameraTransform.forward * readingDistance - cameraTransform.up * 0.12f;
        transform.rotation = Quaternion.LookRotation(cameraTransform.forward, Vector3.up);
    }
}
