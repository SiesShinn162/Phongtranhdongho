using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class PlayerRigSwitcher : MonoBehaviour
{
    [Header("Rigs")]
    [Tooltip("The VR Rig (XR ORIGIN) containing VR Cameras, Tracked Controllers, etc.")]
    [SerializeField] private GameObject vrRig;

    [Tooltip("The Desktop Rig containing StarterAssets FirstPersonController, Cinemachine Camera, etc.")]
    [SerializeField] private GameObject desktopRig;

    [Tooltip("Optional standalone camera that should be disabled when rigs are active.")]
    [SerializeField] private GameObject fallbackCamera;

    [Header("Switching Options")]
    [Tooltip("Automatically check for active VR headset on Start.")]
    [SerializeField] private bool autoDetectOnStart = true;

    [Tooltip("Allow manual toggling between VR and Desktop via keyboard shortcut.")]
    [SerializeField] private bool allowManualToggle = true;

    [Tooltip("Key to toggle between VR and Desktop rigs.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    [Header("Runtime State")]
    [SerializeField] private bool isVRActive = false;

    public bool IsVRActive => isVRActive;

    private void Awake()
    {
        // Auto-assign if not explicitly wired
        if (vrRig == null) vrRig = GameObject.Find("XR ORIGIN");
        if (desktopRig == null)
        {
            var fpc = FindAnyObjectByType<StarterAssets.FirstPersonController>();
            if (fpc != null)
            {
                desktopRig = fpc.transform.parent != null && fpc.transform.parent.name.Contains("NestedParent") 
                    ? fpc.transform.parent.gameObject 
                    : fpc.gameObject;
            }
            if (desktopRig == null)
            {
                var np = GameObject.Find("NestedParent_Unpack");
                if (np != null) desktopRig = np;
            }
        }
        if (fallbackCamera == null)
        {
            var cam = GameObject.Find("Main Camera");
            if (cam != null && (desktopRig == null || !cam.transform.IsChildOf(desktopRig.transform)))
            {
                fallbackCamera = cam;
            }
        }
    }

    private void Start()
    {
        if (autoDetectOnStart)
        {
            bool realVR = DetectRealVR();
            SetMode(realVR, syncPosition: false);
        }
        else
        {
            SetMode(isVRActive, syncPosition: false);
        }
    }

    private void Update()
    {
        if (allowManualToggle && Input.GetKeyDown(toggleKey))
        {
            ToggleMode();
        }
    }

    public void ToggleMode()
    {
        SetMode(!isVRActive, syncPosition: true);
    }

    public void SetMode(bool enableVR, bool syncPosition = true)
    {
        Vector3 currentPos = Vector3.zero;
        Quaternion currentRot = Quaternion.identity;
        bool hasPosition = false;

        if (syncPosition)
        {
            if (isVRActive && vrRig != null)
            {
                currentPos = vrRig.transform.position;
                currentRot = vrRig.transform.rotation;
                hasPosition = true;
            }
            else if (!isVRActive && desktopRig != null)
            {
                // If desktop rig is NestedParent_Unpack, the player movement is on PlayerCapsule child
                var fpc = desktopRig.GetComponentInChildren<StarterAssets.FirstPersonController>();
                if (fpc != null)
                {
                    currentPos = fpc.transform.position;
                    currentRot = fpc.transform.rotation;
                }
                else
                {
                    currentPos = desktopRig.transform.position;
                    currentRot = desktopRig.transform.rotation;
                }
                hasPosition = true;
            }
        }

        isVRActive = enableVR;

        if (isVRActive)
        {
            // Activate VR
            if (desktopRig != null) desktopRig.SetActive(false);
            if (vrRig != null)
            {
                if (hasPosition)
                {
                    vrRig.transform.position = currentPos;
                    vrRig.transform.rotation = currentRot;
                }
                vrRig.SetActive(true);
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[PlayerRigSwitcher] Switched to VR Mode (XR ORIGIN active).");
        }
        else
        {
            // Activate Desktop (StarterAssets)
            if (vrRig != null) vrRig.SetActive(false);
            if (desktopRig != null)
            {
                if (hasPosition)
                {
                    var fpc = desktopRig.GetComponentInChildren<StarterAssets.FirstPersonController>();
                    if (fpc != null)
                    {
                        var cc = fpc.GetComponent<CharacterController>();
                        if (cc != null) cc.enabled = false;
                        fpc.transform.position = currentPos;
                        fpc.transform.rotation = currentRot;
                        if (cc != null) cc.enabled = true;
                    }
                    else
                    {
                        desktopRig.transform.position = currentPos;
                        desktopRig.transform.rotation = currentRot;
                    }
                }
                desktopRig.SetActive(true);
            }
            Debug.Log("[PlayerRigSwitcher] Switched to Desktop Mode (StarterAssets active). Press F1 to toggle.");
        }

        if (fallbackCamera != null)
        {
            fallbackCamera.SetActive(false);
        }
    }

    public static bool DetectRealVR()
    {
        if (!XRSettings.isDeviceActive) return false;

        var displaySubsystems = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displaySubsystems);
        foreach (var ds in displaySubsystems)
        {
            if (ds.running) return true;
        }

        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, devices);
        return devices.Count > 0;
    }
}
