using NUnit.Framework;
using UnityEngine;
using System.Reflection;

public class XRInputTests
{
    [Test]
    public void DesktopMovementYieldsToBothSimulatorAndHeadset()
    {
        // This contract prevents WASD from moving the rig while the simulator
        // uses those same keys to move the tracked head.
        var decision = typeof(XRInputMath).GetMethod("ShouldUseDesktopLocomotion", BindingFlags.Public | BindingFlags.Static);
        Assert.That(decision, Is.Not.Null, "Desktop locomotion needs an explicit XR/simulator gate");
        Assert.That(decision.Invoke(null, new object[] { false, false }), Is.EqualTo(true));
        Assert.That(decision.Invoke(null, new object[] { false, true }), Is.EqualTo(false));
        Assert.That(decision.Invoke(null, new object[] { true, false }), Is.EqualTo(false));
    }

    [Test]
    public void SimulatorFpsKeyboardMovementUsesCollidableRigInsteadOfTrackedHead()
    {
        var decision = typeof(XRInputMath).GetMethod("ShouldMoveSimulatorBody", BindingFlags.Public | BindingFlags.Static);
        Assert.That(decision, Is.Not.Null, "FPS simulator travel needs a CharacterController route");
        Assert.That(decision.Invoke(null, new object[] { false, true, true }), Is.EqualTo(true));
        Assert.That(decision.Invoke(null, new object[] { false, true, false }), Is.EqualTo(false),
            "Controller manipulation must still use the simulator's device controls");
        Assert.That(decision.Invoke(null, new object[] { true, true, true }), Is.EqualTo(false),
            "A real headset must keep its own tracking");
        Assert.That(decision.Invoke(null, new object[] { false, false, false }), Is.EqualTo(false));
    }

    [Test]
    public void DesktopMouseLookIsOffByDefaultWithoutAHeadset()
    {
        Assert.That(XRInputMath.ShouldApplyMouseLook(true), Is.False);
        Assert.That(XRInputMath.ShouldApplyMouseLook(false), Is.False);
    }

    [Test]
    public void DesktopMouseLookRequiresOptInAndNoActiveHeadset()
    {
        Assert.That(XRInputMath.ShouldApplyMouseLook(false, true), Is.True);
        Assert.That(XRInputMath.ShouldApplyMouseLook(true, true), Is.False);
        Assert.That(XRInputMath.ShouldApplyMouseLook(false, false), Is.False);
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
