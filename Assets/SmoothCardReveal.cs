using UnityEngine;

/// <summary>
/// Plays a smooth, juicy pop-in scale animation when an AR card/model is detected by Vuforia.
/// Scales smoothly from 0 -> 1.05 -> 1.0 using an elastic spring curve over 0.35 seconds,
/// and triggers the gamelan discovery chime sound.
/// </summary>
public class SmoothCardReveal : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("Duration of the pop-in entrance animation in seconds.")]
    public float duration = 0.35f;

    [Tooltip("Peak overshoot factor (e.g. 1.06 gives a lively elastic bounce).")]
    public float overshoot = 1.06f;

    [Header("Audio Feedback")]
    [Tooltip("Play the authentic gamelan chime when this card is revealed.")]
    public bool playChimeOnReveal = true;

    private Vector3 baseScale;
    private bool hasBaseScale = false;
    private Renderer targetRenderer;
    private bool wasVisible = false;
    private float animationTimer = 1f;

    void Awake()
    {
        EnsureBaseScale();
        CacheRenderer();
    }

    private void EnsureBaseScale()
    {
        if (!hasBaseScale)
        {
            baseScale = transform.localScale;
            if (baseScale.sqrMagnitude < 0.00001f)
            {
                baseScale = Vector3.one;
            }
            hasBaseScale = true;
        }
    }

    private void CacheRenderer()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>(true);
            }
        }
    }

    void OnEnable()
    {
        EnsureBaseScale();
        TriggerReveal();
    }

    public void TriggerReveal()
    {
        animationTimer = 0f;
        transform.localScale = Vector3.zero;
        wasVisible = true;

        if (playChimeOnReveal && ARAudioManager.Instance != null)
        {
            ARAudioManager.Instance.PlayCardDiscoveryChime();
        }
    }

    void Update()
    {
        CacheRenderer();
        bool isCurrentlyVisible = targetRenderer == null || targetRenderer.enabled;

        // Detect when Vuforia marks the target visible
        if (isCurrentlyVisible && !wasVisible)
        {
            TriggerReveal();
        }
        else if (!isCurrentlyVisible && wasVisible)
        {
            wasVisible = false;
            animationTimer = 1f;
            transform.localScale = Vector3.zero;
        }

        if (animationTimer < 1f)
        {
            animationTimer += Time.deltaTime / Mathf.Max(0.01f, duration);
            float t = Mathf.Clamp01(animationTimer);

            // Elastic bounce curve: 0 to overshoot to 1
            float scaleMultiplier;
            if (t < 1f)
            {
                float curve = Mathf.Sin(t * Mathf.PI);
                scaleMultiplier = t + (overshoot - 1f) * curve * 2f;
            }
            else
            {
                scaleMultiplier = 1f;
            }

            transform.localScale = baseScale * scaleMultiplier;
        }
    }
}
