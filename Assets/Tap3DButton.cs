using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Tap3DButton : MonoBehaviour
{
    public GameObject cardText;

    private Camera cachedCamera;

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

    void Update()
    {
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
                    if (cardText != null)
                    {
                        cardText.SetActive(!cardText.activeSelf);
                    }
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
                    if (cardText != null)
                    {
                        cardText.SetActive(!cardText.activeSelf);
                    }
                }
            }
        }
#endif
    }
}
