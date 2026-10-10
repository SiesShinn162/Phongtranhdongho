using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class XRDesktopLocomotion : MonoBehaviour
{
    [SerializeField] private Transform view;
    [SerializeField] private ContinuousMoveProvider xrMove;
    [SerializeField, Min(0f)] private float walkSpeed = 2f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 2f;
    [SerializeField] private bool desktopMouseLookEnabled = true;

    private CharacterController character;
    private float pitch;
    private float fallingSpeed;
    private bool ownsCursorLock;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        if (view == null && Camera.main != null) view = Camera.main.transform;
    }

    private void Update()
    {
        bool hmdActive = XRSettings.isDeviceActive;
        bool desktopMode = !hmdActive;

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

        if (desktopMode && character.enabled)
        {
            Vector2 keys = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector3 move = XRInputMath.FlatMove(keys, view != null ? view.forward : transform.forward, transform.right);
            fallingSpeed = character.isGrounded ? -1f : fallingSpeed + Physics.gravity.y * Time.deltaTime;
            character.Move((move * walkSpeed + Vector3.up * fallingSpeed) * Time.deltaTime);
        }
        else
        {
            fallingSpeed = 0f;
        }

        // Keep capsule over head if HMD active
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
        ReleaseCursorLock();
    }

    private void ReleaseCursorLock()
    {
        if (!ownsCursorLock) return;
        ownsCursorLock = false;
        if (Cursor.lockState == CursorLockMode.Locked) Cursor.lockState = CursorLockMode.None;
    }
}
