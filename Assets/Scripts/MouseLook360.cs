using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook360 : MonoBehaviour
{
    public float mouseSensitivity = 0.1f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            rotationY += mouseDelta.x * mouseSensitivity;
            rotationX -= mouseDelta.y * mouseSensitivity;

            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
        }
    }
}