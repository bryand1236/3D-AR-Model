using UnityEngine;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Toggles description and title between three languages (Indonesian, English, and Aksara Jawa) when clicked or tapped in 3D.
/// Flow: Indonesian (Button: English) -> English (Button: Aksara Jawa) -> Aksara Jawa (Button: Bahasa Indonesia) -> repeats.
/// </summary>
public class LanguageSwitcher : MonoBehaviour
{
    [System.Serializable]
    public struct LanguagePreset
    {
        public string languageName;       // e.g., "Indonesian", "English", "Aksara Jawa"
        public TMP_FontAsset fontAsset;   // Font to apply to title and description
        
        [TextArea(3, 8)]
        public string descriptionText;
        public string titleText;
        public string buttonText;
    }

    [Header("Target Text Components")]
    public TextMeshPro descriptionTextMesh;
    public TextMeshPro titleTextMesh;
    public TextMeshPro buttonText;

    [Header("Fonts")]
    public TMP_FontAsset language1Font; // Indonesian (Latin)
    public TMP_FontAsset language2Font; // Aksara Jawa (Noto Sans Javanese)
    public TMP_FontAsset language3Font; // English (Latin)

    [Header("Language 1 (Primary / Indonesian)")]
    [TextArea(3, 8)]
    public string language1Text;
    public string language1Title;

    [Header("Language 2 (Secondary / Aksara Jawa)")]
    [TextArea(3, 8)]
    public string language2Text;
    public string language2Title;

    [Header("Language 3 (Tertiary / English)")]
    [TextArea(3, 8)]
    public string language3Text;
    public string language3Title;

    [Header("Button Text")]
    public TextMeshPro buttonTextMesh;
    public string buttonTextLang1 = "English";
    public string buttonTextLang2 = "Aksara Jawa";
    public string buttonTextLang3 = "Bahasa Indonesia";

    [Header("State")]
    public int currentLanguageIndex = 0; // 0 = Indonesian, 1 = English, 2 = Aksara Jawa

    public bool isLanguage2 => currentLanguageIndex == 2; // Aksara Jawa active

    private Camera cachedCamera;
    private Renderer cachedRenderer;
    private bool hasCachedRenderer = false;
    private static readonly RaycastHit[] s_RaycastHits = new RaycastHit[16];

    private bool IsVisible()
    {
        if (!hasCachedRenderer)
        {
            cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer == null)
            {
                cachedRenderer = GetComponentInChildren<Renderer>(true);
            }
            hasCachedRenderer = true;
        }
        return cachedRenderer == null || cachedRenderer.enabled;
    }
    private TMP_FontAsset originalTitleFont;
    private TMP_FontAsset originalDescFont;
    private TMP_FontAsset originalButtonFont;

    void Awake()
    {
        if (titleTextMesh != null) originalTitleFont = titleTextMesh.font;
        if (descriptionTextMesh != null) originalDescFont = descriptionTextMesh.font;
        if (buttonTextMesh != null) originalButtonFont = buttonTextMesh.font;
    }

    void Start()
    {
        if (originalTitleFont == null && titleTextMesh != null) originalTitleFont = titleTextMesh.font;
        if (originalDescFont == null && descriptionTextMesh != null) originalDescFont = descriptionTextMesh.font;
        if (originalButtonFont == null && buttonTextMesh != null) originalButtonFont = buttonTextMesh.font;

        if (language1Font == null)
        {
            language1Font = originalDescFont != null ? originalDescFont : originalTitleFont;
        }

        // Auto-capture initial text from target components if not pre-assigned
        if (descriptionTextMesh != null && string.IsNullOrEmpty(language1Text))
        {
            language1Text = descriptionTextMesh.text;
        }
        if (titleTextMesh != null && string.IsNullOrEmpty(language1Title))
        {
            language1Title = titleTextMesh.text;
        }

        // Also wire UI Button click if present
        var btn = GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(ToggleLanguage);
        }

        UpdateCamera();
        ApplyLanguage(currentLanguageIndex);
    }

    private void UpdateCamera()
    {
        if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
        {
            cachedCamera = Camera.main;
            if (cachedCamera == null)
            {
                cachedCamera = Object.FindAnyObjectByType<Camera>();
            }
        }
    }

    void Update()
    {
        if (!IsVisible()) return;

#if ENABLE_INPUT_SYSTEM
        bool pressed = false;
        Vector2 tapPosition = Vector2.zero;

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            pressed = true;
            tapPosition = Pointer.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pressed = true;
            tapPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressed = true;
            tapPosition = Mouse.current.position.ReadValue();
        }

        if (pressed)
        {
            CheckRaycastHit(tapPosition);
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            CheckRaycastHit(Input.mousePosition);
        }
#endif
    }

    private void CheckRaycastHit(Vector2 screenPoint)
    {
        if (!IsVisible()) return;

        UpdateCamera();
        if (cachedCamera == null) return;

        Ray ray = cachedCamera.ScreenPointToRay(screenPoint);
        int hitCount = Physics.RaycastNonAlloc(ray, s_RaycastHits, 50f);
        if (hitCount <= 0) return;

        // In-place insertion sort by distance to avoid GC allocation
        for (int i = 1; i < hitCount; i++)
        {
            RaycastHit key = s_RaycastHits[i];
            int j = i - 1;
            while (j >= 0 && s_RaycastHits[j].distance > key.distance)
            {
                s_RaycastHits[j + 1] = s_RaycastHits[j];
                j--;
            }
            s_RaycastHits[j + 1] = key;
        }

        for (int i = 0; i < hitCount; i++)
        {
            if (s_RaycastHits[i].transform == transform || s_RaycastHits[i].transform.IsChildOf(transform))
            {
                ToggleLanguage();
                break;
            }
        }
    }

    private void OnMouseDown()
    {
        ToggleLanguage();
    }

    public void ToggleLanguage()
    {
        TriggerButtonFeedback();
        currentLanguageIndex = (currentLanguageIndex + 1) % 3;
        ApplyLanguage(currentLanguageIndex);
    }

    private Coroutine pressCoroutine;
    private Vector3 initialButtonScale;
    private bool hasInitialButtonScale = false;

    private void EnsureInitialScale()
    {
        if (!hasInitialButtonScale)
        {
            initialButtonScale = transform.localScale;
            hasInitialButtonScale = true;
        }
    }

    void OnDisable()
    {
        if (hasInitialButtonScale)
        {
            transform.localScale = initialButtonScale;
        }
        pressCoroutine = null;
    }

    private void TriggerButtonFeedback()
    {
        EnsureInitialScale();

        // 1. Audio feedback
        if (ARAudioManager.Instance != null)
        {
            ARAudioManager.Instance.PlayButtonClick();
        }

        // 2. Mobile haptic feedback
#if UNITY_ANDROID || UNITY_IOS
        try
        {
            Handheld.Vibrate();
        }
        catch {}
#endif

        // 3. Tactile 3D spring depression animation
        if (gameObject.activeInHierarchy)
        {
            if (pressCoroutine != null)
            {
                StopCoroutine(pressCoroutine);
            }
            pressCoroutine = StartCoroutine(AnimateButtonPress());
        }
    }

    private System.Collections.IEnumerator AnimateButtonPress()
    {
        EnsureInitialScale();
        float elapsed = 0f;
        float duration = 0.14f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float squash = 1f - 0.12f * Mathf.Sin(t * Mathf.PI);
            transform.localScale = initialButtonScale * squash;
            yield return null;
        }

        transform.localScale = initialButtonScale;
        pressCoroutine = null;
    }

    public void ApplyLanguage(int index)
    {
        currentLanguageIndex = index;
        TMP_FontAsset targetFont = null;
        string targetTitle = "";
        string targetDesc = "";
        string targetBtn = "";

        switch (currentLanguageIndex)
        {
            case 0: // Indonesian (Latin) -> Button shows "English"
                targetFont = language1Font != null ? language1Font : originalDescFont;
                targetTitle = language1Title;
                targetDesc = language1Text;
                targetBtn = buttonTextLang1;
                break;

            case 1: // English (Latin) -> Button shows "Aksara Jawa"
                targetFont = language3Font != null ? language3Font : (language1Font != null ? language1Font : originalDescFont);
                targetTitle = language3Title;
                targetDesc = language3Text;
                targetBtn = buttonTextLang2;
                break;

            case 2: // Aksara Jawa (Noto Sans Javanese) -> Button shows "Bahasa Indonesia"
                targetFont = language2Font != null ? language2Font : originalDescFont;
                targetTitle = language2Title;
                targetDesc = language2Text;
                targetBtn = buttonTextLang3;
                break;
        }

        if (descriptionTextMesh != null)
        {
            if (targetFont != null)
            {
                descriptionTextMesh.font = targetFont;
            }
            descriptionTextMesh.text = targetDesc;
        }

        if (titleTextMesh != null)
        {
            if (targetFont != null)
            {
                titleTextMesh.font = targetFont;
            }
            titleTextMesh.text = targetTitle;
        }

        if (buttonTextMesh != null)
        {
            // Keep button in legible Latin font
            if (originalButtonFont != null)
            {
                buttonTextMesh.font = originalButtonFont;
            }
            else if (language1Font != null)
            {
                buttonTextMesh.font = language1Font;
            }

            if (!string.IsNullOrEmpty(targetBtn))
            {
                buttonTextMesh.text = targetBtn;
            }
        }
    }
}
