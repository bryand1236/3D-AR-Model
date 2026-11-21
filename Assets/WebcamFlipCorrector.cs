using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Vuforia;
#endif

/// <summary>
/// Corrects webcam video background mirroring/flipping in Unity Play Mode when using Vuforia Engine,
/// and synchronizes all AR 3D models and ImageTargets so they accurately track the flipped camera feed.
/// Webcams identified as front-facing/selfie cameras default to mirrored rendering in Vuforia.
/// This component flips the video background horizontally (and vertically if needed) and reflects
/// the 3D target coordinates so that AR models, UI text, and interactable buttons remain perfectly aligned
/// over the physical cards on screen.
///
/// Designed specifically for Unity Editor Play Mode:
/// Automatically disables itself on mobile/standalone builds to avoid altering native camera feeds.
/// </summary>
[DisallowMultipleComponent]
public class WebcamFlipCorrector : MonoBehaviour
{
    [Header("Webcam Video Background Flip Settings")]
    [Tooltip("Flip video background horizontally to un-mirror front-facing/selfie webcams.")]
    public bool flipHorizontal = true;

    [Tooltip("Flip video background vertically if the camera feed appears upside-down.")]
    public bool flipVertical = false;

    [Header("Play Mode Interactive Hotkeys")]
    [Tooltip("Allow pressing hotkeys 'H' (horizontal) and 'V' (vertical) to toggle flips in real time.")]
    public bool enableHotkeys = true;

    [Tooltip("Display a small unobtrusive status overlay in the bottom corner during Play Mode.")]
    public bool showOnScreenStatus = true;

    private Transform videoBackgroundTransform;
    private Camera targetCamera;

#if UNITY_EDITOR
    private const string PREF_FLIP_H = "WebcamFlipCorrector_FlipH";
    private const string PREF_FLIP_V = "WebcamFlipCorrector_FlipV";

    // Track raw pose from Vuforia per target to allow instant flip toggling and prevent double-flipping
    private class TargetTrackingData
    {
        public Vector3 rawWorldPosition;
        public Quaternion rawWorldRotation;
        public bool hasRawPose;
    }

    private readonly Dictionary<Transform, TargetTrackingData> targetDataMap = new Dictionary<Transform, TargetTrackingData>();
    private ObserverBehaviour[] cachedObservers;
    private bool vuforiaHooked = false;
#endif

    void Awake()
    {
#if !UNITY_EDITOR
        // Deactivate on mobile/standalone builds so native camera is 100% untouched
        enabled = false;
        return;
#else
        targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // Restore saved editor preferences if available
        if (EditorPrefs.HasKey(PREF_FLIP_H))
        {
            flipHorizontal = EditorPrefs.GetBool(PREF_FLIP_H, flipHorizontal);
        }
        if (EditorPrefs.HasKey(PREF_FLIP_V))
        {
            flipVertical = EditorPrefs.GetBool(PREF_FLIP_V, flipVertical);
        }
#endif
    }

    void Start()
    {
#if UNITY_EDITOR
        RegisterVuforiaCallbacks();
        RefreshObservers();
#endif
    }

    void OnEnable()
    {
#if UNITY_EDITOR
        RegisterVuforiaCallbacks();
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnregisterVuforiaCallbacks();
#endif
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
        UnregisterVuforiaCallbacks();
#endif
    }

#if UNITY_EDITOR
    private void RegisterVuforiaCallbacks()
    {
        if (vuforiaHooked) return;

        if (VuforiaBehaviour.Instance != null && VuforiaBehaviour.Instance.World != null)
        {
            VuforiaBehaviour.Instance.World.OnStateUpdated += OnVuforiaStateUpdated;
            vuforiaHooked = true;
        }
        else
        {
            VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;
        }
    }

    private void UnregisterVuforiaCallbacks()
    {
        if (vuforiaHooked && VuforiaBehaviour.Instance != null && VuforiaBehaviour.Instance.World != null)
        {
            VuforiaBehaviour.Instance.World.OnStateUpdated -= OnVuforiaStateUpdated;
            vuforiaHooked = false;
        }
        VuforiaApplication.Instance.OnVuforiaStarted -= OnVuforiaStarted;
    }

    private void OnVuforiaStarted()
    {
        if (!vuforiaHooked && VuforiaBehaviour.Instance != null && VuforiaBehaviour.Instance.World != null)
        {
            VuforiaBehaviour.Instance.World.OnStateUpdated += OnVuforiaStateUpdated;
            vuforiaHooked = true;
        }
        RefreshObservers();
    }

    public void RefreshObservers()
    {
        cachedObservers = Object.FindObjectsByType<ObserverBehaviour>(FindObjectsInactive.Include);
        foreach (var obs in cachedObservers)
        {
            if (obs != null && !targetDataMap.ContainsKey(obs.transform))
            {
                targetDataMap[obs.transform] = new TargetTrackingData();
            }
        }
    }

    private void OnVuforiaStateUpdated()
    {
        // Vuforia has just finished calculating raw poses for this frame!
        UpdateAllTargetPoses(isRawUpdateFromVuforia: true);
    }

    void LateUpdate()
    {
        HandleHotkeyInput();
        ApplyVideoBackgroundFlip();

        // Ensure Vuforia callback is registered
        if (!vuforiaHooked)
        {
            RegisterVuforiaCallbacks();
        }

        // Keep targets aligned if Vuforia didn't dispatch OnStateUpdated this frame
        UpdateAllTargetPoses(isRawUpdateFromVuforia: false);
    }

    private void HandleHotkeyInput()
    {
        if (!enableHotkeys) return;

        bool toggled = false;
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.hKey.wasPressedThisFrame)
            {
                flipHorizontal = !flipHorizontal;
                EditorPrefs.SetBool(PREF_FLIP_H, flipHorizontal);
                Debug.Log($"[WebcamFlipCorrector] Horizontal Flip toggled: {(flipHorizontal ? "ON (Un-mirrored)" : "OFF (Mirrored)")}");
                toggled = true;
            }
            if (keyboard.vKey.wasPressedThisFrame)
            {
                flipVertical = !flipVertical;
                EditorPrefs.SetBool(PREF_FLIP_V, flipVertical);
                Debug.Log($"[WebcamFlipCorrector] Vertical Flip toggled: {(flipVertical ? "ON" : "OFF")}");
                toggled = true;
            }
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.H))
        {
            flipHorizontal = !flipHorizontal;
            EditorPrefs.SetBool(PREF_FLIP_H, flipHorizontal);
            Debug.Log($"[WebcamFlipCorrector] Horizontal Flip toggled: {(flipHorizontal ? "ON (Un-mirrored)" : "OFF (Mirrored)")}");
            toggled = true;
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            flipVertical = !flipVertical;
            EditorPrefs.SetBool(PREF_FLIP_V, flipVertical);
            Debug.Log($"[WebcamFlipCorrector] Vertical Flip toggled: {(flipVertical ? "ON" : "OFF")}");
            toggled = true;
        }
#endif

        if (toggled)
        {
            ApplyVideoBackgroundFlip();
            // Force re-applying pose using stored raw poses
            UpdateAllTargetPoses(isRawUpdateFromVuforia: false, forceReapply: true);
        }
    }

    public void ToggleHorizontalFlip()
    {
        flipHorizontal = !flipHorizontal;
        ApplyVideoBackgroundFlip();
#if UNITY_EDITOR
        UpdateAllTargetPoses(isRawUpdateFromVuforia: false, forceReapply: true);
#endif
    }

    public void ApplyVideoBackgroundFlip()
    {
        if (videoBackgroundTransform == null)
        {
            if (targetCamera != null)
            {
                var vb = targetCamera.transform.Find("VideoBackground");
                if (vb != null)
                {
                    videoBackgroundTransform = vb;
                }
            }

            if (videoBackgroundTransform == null)
            {
                var vbObj = GameObject.Find("VideoBackground");
                if (vbObj != null)
                {
                    videoBackgroundTransform = vbObj.transform;
                }
            }
        }

        if (videoBackgroundTransform != null)
        {
            Vector3 s = videoBackgroundTransform.localScale;
            float absX = Mathf.Abs(s.x);
            float absY = Mathf.Abs(s.y);

            float targetX = flipHorizontal ? -absX : absX;
            float targetY = flipVertical ? -absY : absY;

            if (Mathf.Abs(s.x - targetX) > 0.0001f || Mathf.Abs(s.y - targetY) > 0.0001f)
            {
                videoBackgroundTransform.localScale = new Vector3(targetX, targetY, s.z);
            }
        }
    }

    private void UpdateAllTargetPoses(bool isRawUpdateFromVuforia, bool forceReapply = false)
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;
        }

        if (cachedObservers == null || cachedObservers.Length == 0)
        {
            RefreshObservers();
            if (cachedObservers == null || cachedObservers.Length == 0) return;
        }

        float sx = flipHorizontal ? -1f : 1f;
        float sy = flipVertical ? -1f : 1f;
        bool isFlipping = !Mathf.Approximately(sx, 1f) || !Mathf.Approximately(sy, 1f);

        for (int i = 0; i < cachedObservers.Length; i++)
        {
            var obs = cachedObservers[i];
            if (obs == null) continue;

            // Only adjust target when it is actively tracked or extended tracked
            var status = obs.TargetStatus.Status;
            bool isTracked = status == Status.TRACKED || status == Status.EXTENDED_TRACKED;
            if (!isTracked) continue;

            Transform t = obs.transform;
            if (!targetDataMap.TryGetValue(t, out var data))
            {
                data = new TargetTrackingData();
                targetDataMap[t] = data;
            }

            if (isRawUpdateFromVuforia)
            {
                // Capture the pristine raw pose straight from Vuforia's tracking solver
                data.rawWorldPosition = t.position;
                data.rawWorldRotation = t.rotation;
                data.hasRawPose = true;
            }
            else if (!data.hasRawPose)
            {
                // Fallback initial capture
                data.rawWorldPosition = t.position;
                data.rawWorldRotation = t.rotation;
                data.hasRawPose = true;
            }

            if (!data.hasRawPose) continue;

            if (!isFlipping)
            {
                // Restore original raw pose if flip is turned off
                if (forceReapply || isRawUpdateFromVuforia)
                {
                    t.position = data.rawWorldPosition;
                    t.rotation = data.rawWorldRotation;
                }
                continue;
            }

            // Apply reflected pose derived from the raw pose
            ReflectTargetPose(t, data.rawWorldPosition, data.rawWorldRotation, sx, sy);
        }
    }

    private void ReflectTargetPose(Transform target, Vector3 rawPos, Quaternion rawRot, float sx, float sy)
    {
        // 1. Transform raw world pose into camera space
        Vector3 camPos = targetCamera.transform.InverseTransformPoint(rawPos);
        Quaternion camRot = Quaternion.Inverse(targetCamera.transform.rotation) * rawRot;

        // 2. Reflected camera space position
        Vector3 mirroredCamPos = new Vector3(sx * camPos.x, sy * camPos.y, camPos.z);
        target.position = targetCamera.transform.TransformPoint(mirroredCamPos);

        // 3. Reflected camera space rotation
        Vector3 forward = camRot * Vector3.forward;
        Vector3 up = camRot * Vector3.up;

        Vector3 mirroredForward = new Vector3(sx * forward.x, sy * forward.y, forward.z);
        Vector3 mirroredUp = new Vector3(sx * up.x, sy * up.y, up.z);

        if (mirroredForward.sqrMagnitude > 0.0001f && mirroredUp.sqrMagnitude > 0.0001f)
        {
            Quaternion mirroredCamRot = Quaternion.LookRotation(mirroredForward, mirroredUp);
            target.rotation = targetCamera.transform.rotation * mirroredCamRot;
        }
    }

    void OnGUI()
    {
        if (!showOnScreenStatus) return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 11
        };
        style.normal.textColor = Color.white;

        string hStatus = flipHorizontal ? "ON (Un-mirrored)" : "OFF (Mirrored)";
        string vStatus = flipVertical ? "ON" : "OFF";
        string text = $"Webcam Feed: [H] Flip-X: {hStatus} | [V] Flip-Y: {vStatus} (3D AR Model Aligned)";

        GUI.Box(new Rect(10, Screen.height - 35, 450, 25), text, style);
    }

    void OnValidate()
    {
        ApplyVideoBackgroundFlip();
    }
#endif
}
