using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
    {
        private Vector2 movementInput;
        private Vector2 lookInput;

        private float multiplier = 5f;

        public InputActionReference move;
        public InputActionReference look;

    void Update()
    {
        if(move != null)
        {
            movementInput = move.action.ReadValue<Vector2>();
        }

        if(look != null)
        {
            lookInput = look.action.ReadValue<Vector2>();
        }

        movementInput *= multiplier;
        lookInput *= multiplier;

        transform.Translate(movementInput.x * Time.deltaTime, 0, movementInput.y * Time.deltaTime);
        transform.Rotate(0, lookInput.x * Time.deltaTime, 0);
    }
}
