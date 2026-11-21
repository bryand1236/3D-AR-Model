using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    [Header("Floating Settings")]
    public float floatAmplitude = 0.008f;
    public float floatFrequency = 2.0f;

    [Header("Camera Facing")]
    public bool faceCamera = true;

    protected Vector3 initialLocalPos;
    protected bool hasInitialPos = false;

    // Cache the Camera transform to avoid repeated Camera.main overhead across all instances
    private static Camera cachedMainCamera;
    private static Transform cachedMainCameraTransform;

    public static Transform MainCameraTransform
    {
        get
        {
            if (cachedMainCameraTransform == null || cachedMainCamera == null || !cachedMainCamera.isActiveAndEnabled)
            {
                cachedMainCamera = Camera.main;
                if (cachedMainCamera != null)
                {
                    cachedMainCameraTransform = cachedMainCamera.transform;
                }
            }
            return cachedMainCameraTransform;
        }
    }

    protected virtual void Start()
    {
        if (!hasInitialPos)
        {
            initialLocalPos = transform.localPosition;
            hasInitialPos = true;
        }
    }

    protected virtual void OnEnable()
    {
        if (!hasInitialPos)
        {
            initialLocalPos = transform.localPosition;
            hasInitialPos = true;
        }
    }

    protected virtual void LateUpdate()
    {
        // Smoothly float up and down
        if (floatAmplitude > 0f)
        {
            float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.localPosition = initialLocalPos + new Vector3(0, yOffset, 0);
        }

        // Always face Main Camera (using cached camera transform)
        if (faceCamera)
        {
            Transform camTransform = MainCameraTransform;
            if (camTransform != null)
            {
                transform.rotation = camTransform.rotation;
            }
        }
    }
}
