using UnityEngine;

/// <summary>
/// Sets up optimal mobile AR performance parameters on application launch:
/// - Locks target frame rate to 60 FPS for fluid AR tracking without thermal throttling.
/// - Disables screen sleep timeout so the AR experience does not dim or sleep while aiming at cards.
/// </summary>
public class ARAppOptimizer : MonoBehaviour
{
    [Header("Frame Rate & Power Settings")]
    [Tooltip("Target frame rate for mobile AR. 60 FPS provides smooth camera tracking.")]
    public int targetFrameRate = 60;

    [Tooltip("Prevent device from sleeping during AR tracking.")]
    public bool preventScreenSleep = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        QualitySettings.vSyncCount = 0; // Required on mobile platforms for targetFrameRate to take effect
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void Awake()
    {
        ApplySettings();
    }

    public void ApplySettings()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;

        if (preventScreenSleep)
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
