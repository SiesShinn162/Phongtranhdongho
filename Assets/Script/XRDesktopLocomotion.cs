using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class XRDesktopLocomotion : MonoBehaviour
{
    [SerializeField] private Transform view;
    [SerializeField] private ContinuousMoveProvider xrMove;
    [SerializeField, Min(0f)] private float walkSpeed = 2f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 2f;
    [SerializeField] private bool desktopMouseLookEnabled;

    private CharacterController character;
    private float pitch;
    private float fallingSpeed;
    private bool ownsCursorLock;
    private XRDeviceSimulator configuredSimulator;
    private float originalSimulatorXSpeed;
    private float originalSimulatorYSpeed;
    private float originalSimulatorZSpeed;
    private bool simulatorKeyboardTranslationDisabled;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        if (view == null && Camera.main != null) view = Camera.main.transform;
    }

    private void Update()
    {
        bool hmdActive = XRSettings.isDeviceActive;
        // The simulator uses the desktop keyboard/mouse to drive a tracked HMD.
        // Running desktop locomotion as well moves the rig a second time.
        var classicSimulator = XRDeviceSimulator.instance;
        bool simulatorActive = classicSimulator != null && classicSimulator.isActiveAndEnabled;
        if (!hmdActive && !simulatorActive)
        {
            var interactionSimulator = FindAnyObjectByType<XRInteractionSimulator>();
            simulatorActive = interactionSimulator != null && interactionSimulator.isActiveAndEnabled;
        }
        bool desktopMode = XRInputMath.ShouldUseDesktopLocomotion(hmdActive, simulatorActive);
        bool simulatorFpsMode = XRInputMath.ShouldMoveSimulatorBody(
            hmdActive, simulatorActive, classicSimulator != null && classicSimulator.manipulatingFPS);
        ConfigureSimulatorTranslation(classicSimulator, simulatorFpsMode);
        if (xrMove != null && xrMove.enabled == desktopMode) xrMove.enabled = !desktopMode;

        if (desktopMode && XRInputMath.ShouldApplyMouseLook(hmdActive, desktopMouseLookEnabled))
        {
            if (ownsCursorLock && Cursor.lockState != CursorLockMode.Locked) ownsCursorLock = false;
            if (Input.GetKeyDown(KeyCode.Escape)) ReleaseCursorLock();
            if (view != null && Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                ownsCursorLock = true;
            }

            if (view != null && ownsCursorLock && Cursor.lockState == CursorLockMode.Locked)
            {
                transform.Rotate(0f, Input.GetAxis("Mouse X") * mouseSensitivity, 0f);
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -85f, 85f);
                view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }
        else
        {
            ReleaseCursorLock();
        }

        if ((desktopMode || simulatorFpsMode) && character.enabled)
        {
            Vector2 keys;
            if (simulatorFpsMode && Keyboard.current != null)
            {
                // Simulator controls use the Input System even when the legacy
                // Input Manager is disabled in project settings.
                var keyboard = Keyboard.current;
                keys = new Vector2(
                    (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            }
            else
            {
                keys = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }
            Vector3 move = XRInputMath.FlatMove(keys, view != null ? view.forward : transform.forward, transform.right);
            fallingSpeed = character.isGrounded ? -1f : fallingSpeed + Physics.gravity.y * Time.deltaTime;
            character.Move((move * walkSpeed + Vector3.up * fallingSpeed) * Time.deltaTime);
        }
        else fallingSpeed = 0f;

        // The head moves independently inside the rig. Keep the capsule over the head,
        // but never write to the tracked camera's pose while a headset is connected.
        if (hmdActive && view != null)
        {
            Vector3 localHead = transform.InverseTransformPoint(view.position);
            float headHeight = Mathf.Clamp(localHead.y, 1f, 2.3f);
            character.height = headHeight;
            character.center = new Vector3(localHead.x, headHeight * 0.5f, localHead.z);
        }
    }

    private void OnDisable()
    {
        RestoreSimulatorTranslation();
        ReleaseCursorLock();
    }

    private void ConfigureSimulatorTranslation(XRDeviceSimulator simulator, bool simulatorFpsMode)
    {
        if (configuredSimulator != simulator)
        {
            RestoreSimulatorTranslation();
            configuredSimulator = simulator;
            if (configuredSimulator != null)
            {
                originalSimulatorXSpeed = configuredSimulator.keyboardXTranslateSpeed;
                originalSimulatorYSpeed = configuredSimulator.keyboardYTranslateSpeed;
                originalSimulatorZSpeed = configuredSimulator.keyboardZTranslateSpeed;
            }
        }

        if (configuredSimulator == null || simulatorKeyboardTranslationDisabled == simulatorFpsMode)
            return;

        if (simulatorFpsMode)
        {
            // Keyboard FPS travel normally changes the simulated HMD pose, which
            // bypasses every world collider. Let CharacterController.Move do it.
            configuredSimulator.keyboardXTranslateSpeed = 0f;
            configuredSimulator.keyboardYTranslateSpeed = 0f;
            configuredSimulator.keyboardZTranslateSpeed = 0f;
            simulatorKeyboardTranslationDisabled = true;
        }
        else
        {
            RestoreSimulatorTranslation();
        }
    }

    private void RestoreSimulatorTranslation()
    {
        if (configuredSimulator != null && simulatorKeyboardTranslationDisabled)
        {
            configuredSimulator.keyboardXTranslateSpeed = originalSimulatorXSpeed;
            configuredSimulator.keyboardYTranslateSpeed = originalSimulatorYSpeed;
            configuredSimulator.keyboardZTranslateSpeed = originalSimulatorZSpeed;
        }
        simulatorKeyboardTranslationDisabled = false;
    }

    private void ReleaseCursorLock()
    {
        if (!ownsCursorLock) return;
        ownsCursorLock = false;
        if (Cursor.lockState == CursorLockMode.Locked) Cursor.lockState = CursorLockMode.None;
    }
}
