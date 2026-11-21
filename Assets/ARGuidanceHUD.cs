using UnityEngine;
using Vuforia;

/// <summary>
/// Provides user-facing AR guidance and mobile camera controls:
/// - Fades in an elegant guidance hint when no Wayang card is in view ("Arahkan kamera ke kartu Wayang").
/// - Displays a brief confirmation toast when a card is recognized ("Wayang [Name] Terdeteksi").
/// - Provides a camera flip toggle button in the corner to switch camera mirroring / direction.
/// </summary>
[DisallowMultipleComponent]
public class ARGuidanceHUD : MonoBehaviour
{
    private static ARGuidanceHUD instance;
    public static ARGuidanceHUD Instance => instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (instance == null && Object.FindAnyObjectByType<ARGuidanceHUD>() == null)
        {
            var go = new GameObject("ARGuidanceHUD");
            go.AddComponent<ARGuidanceHUD>();
            DontDestroyOnLoad(go);
        }
    }

    [Header("Guidance Settings")]
    [Tooltip("Show the guidance banner when looking for cards.")]
    public bool showTrackingHint = true;

    [Tooltip("Show the top-corner camera flip toggle button.")]
    public bool showCameraToggleButton = true;

    private float hintAlpha = 1f;
    private float targetHintAlpha = 1f;
    private float cardDetectedTimer = 0f;
    private string currentTrackedName = "";
    private bool hasTrackedCard = false;

    private ObserverBehaviour[] observers;
    private float observerRefreshTimer = 0f;

    private GUIStyle hintBoxStyle;
    private GUIStyle iconStyle;
    private GUIStyle camBtnStyle;
    private Texture2D pillBgTex;
    private Texture2D btnBgTex;

    void Awake()
    {
        instance = this;
        CreateTextures();
    }

    private void CreateTextures()
    {
        pillBgTex = new Texture2D(1, 1);
        pillBgTex.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.12f, 0.78f));
        pillBgTex.Apply();

        btnBgTex = new Texture2D(1, 1);
        btnBgTex.SetPixel(0, 0, new Color(0.12f, 0.12f, 0.18f, 0.85f));
        btnBgTex.Apply();
    }

    void Start()
    {
        RefreshObservers();
    }

    private void RefreshObservers()
    {
        observers = Object.FindObjectsByType<ObserverBehaviour>(FindObjectsSortMode.None);
    }

    void Update()
    {
        observerRefreshTimer += Time.deltaTime;
        if (observerRefreshTimer > 1.5f || observers == null || observers.Length == 0)
        {
            observerRefreshTimer = 0f;
            RefreshObservers();
        }

        // Check if any card is currently tracked
        bool anyTracked = false;
        string detectedCard = "";

        if (observers != null)
        {
            for (int i = 0; i < observers.Length; i++)
            {
                var obs = observers[i];
                if (obs != null && (obs.TargetStatus.Status == Status.TRACKED || obs.TargetStatus.Status == Status.EXTENDED_TRACKED))
                {
                    anyTracked = true;
                    detectedCard = obs.TargetName;
                    if (string.IsNullOrEmpty(detectedCard))
                    {
                        detectedCard = obs.gameObject.name;
                    }
                    break;
                }
            }
        }

        if (anyTracked)
        {
            if (!hasTrackedCard)
            {
                // Newly detected!
                currentTrackedName = detectedCard;
                cardDetectedTimer = 2.2f;
                hasTrackedCard = true;
            }
        }
        else
        {
            hasTrackedCard = false;
        }

        if (cardDetectedTimer > 0f)
        {
            cardDetectedTimer -= Time.deltaTime;
            targetHintAlpha = 1f; // Show confirmation toast
        }
        else
        {
            // If card is tracked, hide hint. If no card, show guidance hint.
            targetHintAlpha = anyTracked ? 0f : 1f;
        }

        hintAlpha = Mathf.MoveTowards(hintAlpha, targetHintAlpha, Time.deltaTime * 3.5f);
    }

    void OnGUI()
    {
        if (pillBgTex == null) CreateTextures();

        float scale = Screen.dpi > 0 ? Mathf.Clamp(Screen.dpi / 160f, 1f, 3f) : 1f;

        InitStyles(scale);

        // 1. Guidance / Status Pill Banner
        if (showTrackingHint && hintAlpha > 0.01f)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, hintAlpha);

            string message;
            if (cardDetectedTimer > 0f)
            {
                message = "★  " + currentTrackedName + " Terdeteksi";
            }
            else
            {
                message = "✦  Arahkan kamera ke kartu Wayang";
            }

            float bannerWidth = Mathf.Min(Screen.width * 0.88f, 380f * scale);
            float bannerHeight = 44f * scale;
            float bannerX = (Screen.width - bannerWidth) * 0.5f;
            float bannerY = Screen.height - bannerHeight - (28f * scale);

            Rect bannerRect = new Rect(bannerX, bannerY, bannerWidth, bannerHeight);
            GUI.Box(bannerRect, GUIContent.none, hintBoxStyle);

            Rect labelRect = new Rect(bannerX, bannerY, bannerWidth, bannerHeight);
            GUI.Label(labelRect, message, iconStyle);

            GUI.color = oldColor;
        }

        // 2. Camera Flip / Mirroring Toggle Button
        if (showCameraToggleButton)
        {
            float btnSize = 46f * scale;
            float margin = 16f * scale;
            Rect btnRect = new Rect(Screen.width - btnSize - margin, margin, btnSize, btnSize);

            if (GUI.Button(btnRect, "↻", camBtnStyle))
            {
                OnCameraToggleButtonClicked();
            }
        }
    }

    private void OnCameraToggleButtonClicked()
    {
        if (ARAudioManager.Instance != null)
        {
            ARAudioManager.Instance.PlayButtonClick();
        }

        var flipCorrector = Object.FindAnyObjectByType<WebcamFlipCorrector>();
        if (flipCorrector != null)
        {
            flipCorrector.ToggleHorizontalFlip();
        }
    }

    private void InitStyles(float scale)
    {
        if (hintBoxStyle == null)
        {
            hintBoxStyle = new GUIStyle(GUI.skin.box);
            hintBoxStyle.normal.background = pillBgTex;
            hintBoxStyle.border = new RectOffset(4, 4, 4, 4);
        }

        if (iconStyle == null)
        {
            iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.alignment = TextAnchor.MiddleCenter;
            iconStyle.fontStyle = FontStyle.Bold;
            iconStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        }
        iconStyle.fontSize = Mathf.RoundToInt(14f * scale);

        if (camBtnStyle == null)
        {
            camBtnStyle = new GUIStyle(GUI.skin.button);
            camBtnStyle.normal.background = btnBgTex;
            camBtnStyle.active.background = pillBgTex;
            camBtnStyle.normal.textColor = Color.white;
            camBtnStyle.alignment = TextAnchor.MiddleCenter;
            camBtnStyle.fontStyle = FontStyle.Bold;
        }
        camBtnStyle.fontSize = Mathf.RoundToInt(20f * scale);
    }
}
