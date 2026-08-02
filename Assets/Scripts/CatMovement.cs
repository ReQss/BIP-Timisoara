using UnityEngine;
using UnityEngine.InputSystem;

public class CatMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Vector2 input;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Keep the cat keyboard-only so a connected controller exclusively moves the barman.
        Keyboard keyboard = Keyboard.current;
        input.x = ReadAxis(keyboard, Key.A, Key.D);
        input.y = ReadAxis(keyboard, Key.S, Key.W);

        // Zapobiega szybszemu chodzeniu po skosie
        input = input.normalized;

        // Ruch
        transform.position += (Vector3)input * moveSpeed * Time.deltaTime;

        // Animator
        animator.SetFloat("MoveX", input.x);
        animator.SetFloat("MoveY", input.y);
        animator.SetFloat("speed", input.sqrMagnitude);
    }

    private static float ReadAxis(Keyboard keyboard, Key negative, Key positive)
    {
        if (keyboard == null)
        {
            return 0f;
        }

        float value = 0f;
        if (keyboard[negative].isPressed)
        {
            value -= 1f;
        }

        if (keyboard[positive].isPressed)
        {
            value += 1f;
        }

        return value;
    }
}
