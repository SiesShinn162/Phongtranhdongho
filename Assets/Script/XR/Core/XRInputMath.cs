using UnityEngine;

public static class XRInputMath
{
    public static bool ShouldApplyMouseLook(bool hmdActive)
    {
        return !hmdActive;
    }

    public static Vector3 FlatMove(Vector2 keys, Vector3 forward, Vector3 right)
    {
        Vector3 direction = Vector3.ProjectOnPlane(forward, Vector3.up).normalized * keys.y
            + Vector3.ProjectOnPlane(right, Vector3.up).normalized * keys.x;
        return Vector3.ClampMagnitude(direction, 1f);
    }

    public static Collider FirstHit(Ray ray, float distance, int layers)
    {
        return Physics.Raycast(ray, out RaycastHit hit, distance, layers, QueryTriggerInteraction.Ignore)
            ? hit.collider : null;
    }
}
