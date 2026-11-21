using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Tap3DButton : MonoBehaviour
{
    public GameObject cardText;

    private Camera cachedCamera;
    private Renderer cachedRenderer;
    private bool hasCachedRenderer = false;

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

    void Start()
    {
        if (cardText != null)
        {
            cardText.SetActive(false);
        }
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
        {
            cachedCamera = Camera.main;
        }
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

    private void HandleTap()
    {
        TriggerButtonFeedback();
        if (cardText != null)
        {
            cardText.SetActive(!cardText.activeSelf);
        }
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

    void Update()
    {
        if (!IsVisible()) return;

#if ENABLE_INPUT_SYSTEM
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            UpdateCamera();
            if (cachedCamera == null) return;

            Vector2 tapPosition = Pointer.current.position.ReadValue();
            Ray ray = cachedCamera.ScreenPointToRay(tapPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    HandleTap();
                }
            }
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0))
        {
            UpdateCamera();
            if (cachedCamera == null) return;

            Ray ray = cachedCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    HandleTap();
                }
            }
        }
#endif
    }
}
