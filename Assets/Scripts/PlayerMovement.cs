using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public InputActionReference move;
    public InputActionReference look;

    [SerializeField] private Transform cameraTransform;
    [SerializeField, Min(0f)] private float movementSpeed = 5f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;
    [SerializeField, Range(0f, 90f)] private float maximumPitch = 89f;

    private float pitch;

    private void Start()
    {
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>().transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Vector2 movementInput = move.action.ReadValue<Vector2>();
        Vector2 lookInput = look.action.ReadValue<Vector2>();

        transform.Translate(
            movementInput.x * movementSpeed * Time.deltaTime,
            0f,
            movementInput.y * movementSpeed * Time.deltaTime,
            Space.Self);

        transform.Rotate(0f, lookInput.x * mouseSensitivity, 0f, Space.Self);
        pitch = Mathf.Clamp(
            pitch - lookInput.y * mouseSensitivity,
            -maximumPitch,
            maximumPitch);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
