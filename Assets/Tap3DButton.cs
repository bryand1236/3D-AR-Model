using UnityEngine;
using UnityEngine.InputSystem;
public class Tap3DButton : MonoBehaviour
{
    public GameObject cardText;

    void Start()
    {
        cardText.SetActive(false);
    }

    void Update()
    {
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 tapPosition = Pointer.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(tapPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform)
                {
                    cardText.SetActive(!cardText.activeSelf);
                }
            }
        }
    }
}
