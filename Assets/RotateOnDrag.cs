using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Makes a 3D model interactable by rotating it when dragged horizontally from side to side,
/// with support for smooth automatic idle rotation (e.g. 3 degrees every 6 seconds).
/// Works with both touch on mobile and mouse in the Editor.
/// </summary>
public class RotateOnDrag : MonoBehaviour
{
    [Header("Auto Rotation")]
    [Tooltip("Automatically rotates the model slowly on its own.")]
    public bool autoRotate = true;

    [Tooltip("Auto-rotation speed in degrees per second (default 20.0 deg/sec).")]
    public float autoRotateSpeed = 20.0f;

    [Tooltip("Pause auto-rotation while user is actively dragging.")]
    public bool pauseAutoRotateOnDrag = true;

    [Tooltip("Seconds to wait after user drag ends before resuming auto-rotation.")]
    public float resumeAutoRotateDelay = 1.0f;

    [Header("Rotation Settings")]
    [Tooltip("Degrees of rotation per pixel of horizontal screen drag.")]
    public float rotationSpeed = 0.4f;

    [Tooltip("Invert horizontal rotation direction. When false, dragging right turns the front of the model to the right.")]
    public bool invertRotation = false;

    [Tooltip("Enable smooth momentum/inertia after releasing drag.")]
    public bool enableInertia = true;

    [Tooltip("How quickly momentum slows down after release.")]
    public float inertiaDamping = 5.0f;

    [Header("Interaction Settings")]
    [Tooltip("If true, dragging must start by touching/clicking on the model. If false, dragging anywhere on screen rotates.")]
    public bool requireTouchOnModel = true;

    [Tooltip("Axis of rotation in local space.")]
    public Vector3 rotationAxis = Vector3.up;

    private bool isDragging = false;
    private Vector2 lastPointerPos;
    private float currentAngularVelocity = 0f;
    private float timeSinceLastDrag = 999f;
    private Camera targetCamera;

    /// <summary>
    /// Current dragging state (useful for external scripts or unit tests).
    /// </summary>
    public bool IsDragging => isDragging;

    void Awake()
    {
        EnsureCollider();
    }

    void Start()
    {
        UpdateCamera();
    }

    void OnDisable()
    {
        isDragging = false;
        currentAngularVelocity = 0f;
    }

    /// <summary>
    /// Ensures this GameObject or its children have a Collider so touch raycasts can detect it.
    /// </summary>
    public void EnsureCollider()
    {
        if (GetComponent<Collider>() == null && GetComponentInChildren<Collider>() == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    b.Encapsulate(renderers[i].bounds);
                }
                
                box.center = transform.InverseTransformPoint(b.center);
                Vector3 worldSize = b.size;
                Vector3 lossy = transform.lossyScale;
                box.size = new Vector3(
                    lossy.x != 0 ? Mathf.Abs(worldSize.x / lossy.x) : 1f,
                    lossy.y != 0 ? Mathf.Abs(worldSize.y / lossy.y) : 1f,
                    lossy.z != 0 ? Mathf.Abs(worldSize.z / lossy.z) : 1f
                );
                
                // Add comfortable padding for mobile finger touch targets
                box.size = new Vector3(
                    Mathf.Max(box.size.x, 1.1f),
                    Mathf.Max(box.size.y, 2.0f),
                    Mathf.Max(box.size.z, 0.6f)
                );
            }
            else
            {
                box.center = Vector3.zero;
                box.size = new Vector3(1.1f, 2.0f, 0.6f);
            }
        }
    }

    void Update()
    {
        UpdateCamera();

        bool isPressed = false;
        bool wasPressedThisFrame = false;
        bool wasReleasedThisFrame = false;
        Vector2 pointerPos = Vector2.zero;

        GetPointerState(out isPressed, out wasPressedThisFrame, out wasReleasedThisFrame, out pointerPos);

        // 1. Touch/Click Began
        if (wasPressedThisFrame)
        {
            if (requireTouchOnModel)
            {
                if (CheckTouchHitsModel(pointerPos))
                {
                    StartDrag(pointerPos);
                }
            }
            else
            {
                StartDrag(pointerPos);
            }
        }

        // 2. Dragging in progress
        if (isDragging)
        {
            timeSinceLastDrag = 0f;

            if (isPressed)
            {
                float deltaX = pointerPos.x - lastPointerPos.x;
                if (Mathf.Abs(deltaX) > 0.0001f)
                {
                    RotateModel(deltaX);

                    if (Time.deltaTime > 0f)
                    {
                        float direction = invertRotation ? 1f : -1f;
                        float angle = direction * deltaX * rotationSpeed;
                        currentAngularVelocity = angle / Time.deltaTime;
                    }
                }
                else
                {
                    // Slowly diminish angular velocity if finger stops moving
                    currentAngularVelocity = Mathf.Lerp(currentAngularVelocity, 0f, 10f * Time.deltaTime);
                }

                lastPointerPos = pointerPos;
            }
            else
            {
                EndDrag();
            }
        }
        else if (wasReleasedThisFrame)
        {
            EndDrag();
        }

        // 3. Inertia & Smooth Auto-Rotation
        if (!isDragging)
        {
            timeSinceLastDrag += Time.deltaTime;

            // Handle inertia decay
            if (enableInertia && Mathf.Abs(currentAngularVelocity) > 0.01f)
            {
                float step = currentAngularVelocity * Time.deltaTime;
                transform.Rotate(rotationAxis, step, Space.Self);
                currentAngularVelocity = Mathf.Lerp(currentAngularVelocity, 0f, inertiaDamping * Time.deltaTime);
            }
            else
            {
                currentAngularVelocity = 0f;

                // Handle slow smooth automatic rotation
                if (autoRotate && (!pauseAutoRotateOnDrag || timeSinceLastDrag >= resumeAutoRotateDelay))
                {
                    float autoStep = autoRotateSpeed * Time.deltaTime;
                    transform.Rotate(rotationAxis, autoStep, Space.Self);
                }
            }
        }
    }

    private void StartDrag(Vector2 pointerPos)
    {
        isDragging = true;
        lastPointerPos = pointerPos;
        currentAngularVelocity = 0f;
        timeSinceLastDrag = 0f;
    }

    private void EndDrag()
    {
        isDragging = false;
        timeSinceLastDrag = 0f;
    }

    /// <summary>
    /// Rotates the model by a horizontal screen delta in pixels.
    /// Can be called directly by external scripts or test runners.
    /// </summary>
    /// <param name="deltaX">Horizontal screen pixel delta (positive = right, negative = left).</param>
    public void RotateModel(float deltaX)
    {
        float direction = invertRotation ? 1f : -1f;
        float angle = direction * deltaX * rotationSpeed;
        transform.Rotate(rotationAxis, angle, Space.Self);
    }

    private void UpdateCamera()
    {
        if (targetCamera == null || !targetCamera.isActiveAndEnabled)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindAnyObjectByType<Camera>();
            }
        }
    }

    private bool CheckTouchHitsModel(Vector2 screenPos)
    {
        if (targetCamera == null) return false;

        Ray ray = targetCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                return true;
            }
        }
        return false;
    }

    private void GetPointerState(out bool isPressed, out bool wasPressedThisFrame, out bool wasReleasedThisFrame, out Vector2 position)
    {
        isPressed = false;
        wasPressedThisFrame = false;
        wasReleasedThisFrame = false;
        position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Pointer.current != null)
        {
            isPressed = Pointer.current.press.isPressed;
            wasPressedThisFrame = Pointer.current.press.wasPressedThisFrame;
            wasReleasedThisFrame = Pointer.current.press.wasReleasedThisFrame;
            position = Pointer.current.position.ReadValue();
            return;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            position = touch.position;
            isPressed = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
            wasPressedThisFrame = touch.phase == TouchPhase.Began;
            wasReleasedThisFrame = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            return;
        }

        isPressed = Input.GetMouseButton(0);
        wasPressedThisFrame = Input.GetMouseButtonDown(0);
        wasReleasedThisFrame = Input.GetMouseButtonUp(0);
        position = Input.mousePosition;
#endif
    }
}
