using UnityEngine;
using UnityEngine.XR;

public class XRPaintingPointer : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float maxDistance = 5f;
    [SerializeField] private Transform pointerOrigin;
    [SerializeField] private LayerMask collisionLayers = ~0;
    [SerializeField] private LineRenderer rayLine;

    private PaintingInfo current;

    private void Awake()
    {
        if (pointerOrigin == null) pointerOrigin = transform;
    }

    private void OnDisable()
    {
        if (current != null) PaintingSelector.ClearSelection();
        current = null;
        if (rayLine != null) rayLine.enabled = false;
    }

    private void Update()
    {
        bool active = XRSettings.isDeviceActive;
        if (rayLine != null) rayLine.enabled = active;
        if (!active)
        {
            if (current != null) PaintingSelector.ClearSelection();
            current = null;
            return;
        }

        var origin = pointerOrigin != null ? pointerOrigin : transform;
        Collider hit = XRInputMath.FirstHit(new Ray(origin.position, origin.forward), maxDistance, collisionLayers);
        var painting = hit != null ? hit.GetComponentInParent<PaintingInfo>() : null;
        if (rayLine != null)
        {
            rayLine.positionCount = 2;
            rayLine.SetPosition(0, origin.position);
            rayLine.SetPosition(1, hit != null ? hit.ClosestPoint(origin.position) : origin.position + origin.forward * maxDistance);
        }
        if (painting == current) return;
        current = painting;
        if (painting != null && painting.data != null) PaintingSelector.Select(painting.data);
        else PaintingSelector.ClearSelection();
    }
}
