using NUnit.Framework;
using UnityEngine;

public class XRInputTests
{
    [Test]
    public void MouseLookOnlyRunsWithoutAnActiveHeadset()
    {
        Assert.That(XRInputMath.ShouldApplyMouseLook(true), Is.False);
        Assert.That(XRInputMath.ShouldApplyMouseLook(false), Is.True);
    }

    [Test]
    public void FlatMoveNeverAddsVerticalMotionFromHeadTilt()
    {
        Vector3 movement = XRInputMath.FlatMove(new Vector2(1, 1),
            new Vector3(0, 0.8f, 0.6f), Vector3.right);
        Assert.That(movement.y, Is.EqualTo(0).Within(0.0001f));
        Assert.That(movement.magnitude, Is.EqualTo(1).Within(0.0001f));
        Assert.That(movement.x, Is.GreaterThan(0));
        Assert.That(movement.z, Is.GreaterThan(0));
    }

    [Test]
    public void PointerStopsAtFirstColliderInsteadOfSelectingThroughWalls()
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var painting = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            wall.transform.position = new Vector3(0, 0, 1);
            painting.transform.position = new Vector3(0, 0, 3);
            Physics.SyncTransforms();
            var collider = XRInputMath.FirstHit(new Ray(Vector3.zero, Vector3.forward), 5f, ~0);
            Assert.That(collider, Is.EqualTo(wall.GetComponent<Collider>()));
        }
        finally
        {
            Object.DestroyImmediate(wall);
            Object.DestroyImmediate(painting);
        }
    }
}
