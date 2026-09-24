using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

[RequireComponent(typeof(CharacterController))]
public class XRDesktopLocomotion : MonoBehaviour
{
    [SerializeField] private Transform view;
    [SerializeField] private ContinuousMoveProvider xrMove;
    [SerializeField, Min(0f)] private float walkSpeed = 2f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 2f;

    private CharacterController character;
    private float pitch;
    private float fallingSpeed;

    private void Awake()
    {
        character = GetComponent<CharacterController>();
        if (view == null && Camera.main != null) view = Camera.main.transform;
    }

    private void Update()
    {
        bool hmdActive = XRSettings.isDeviceActive;
        if (xrMove != null && xrMove.enabled != hmdActive) xrMove.enabled = hmdActive;

        if (XRInputMath.ShouldApplyMouseLook(hmdActive))
        {
            if (Input.GetKeyDown(KeyCode.Escape)) Cursor.lockState = CursorLockMode.None;
            if (Input.GetMouseButtonDown(0)) Cursor.lockState = CursorLockMode.Locked;

            if (view != null && Cursor.lockState == CursorLockMode.Locked)
            {
                transform.Rotate(0f, Input.GetAxis("Mouse X") * mouseSensitivity, 0f);
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -85f, 85f);
                view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }
        else if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
        }

        Vector2 keys = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 move = XRInputMath.FlatMove(keys, view != null ? view.forward : transform.forward, transform.right);
        if (!hmdActive)
        {
            fallingSpeed = character.isGrounded ? -1f : fallingSpeed + Physics.gravity.y * Time.deltaTime;
        }
        else fallingSpeed = 0f;

        if (!character.enabled) return;
        character.Move((move * walkSpeed + Vector3.up * fallingSpeed) * Time.deltaTime);

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
}
