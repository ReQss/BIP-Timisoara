using UnityEngine;
using UnityEngine.InputSystem;

public class CatMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool useArrowKeys;
    [SerializeField] private bool useGamepadAndIJKL;

    private Vector2 input;
    private Animator animator;
    private DogSpriteAnimator dogSpriteAnimator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        dogSpriteAnimator = GetComponent<DogSpriteAnimator>();
    }

    private void Update()
    {
        // Each character has its own keyboard scheme; the second player also accepts a gamepad.
        Keyboard keyboard = Keyboard.current;
        if (useGamepadAndIJKL)
        {
            input.x = ReadAxis(keyboard, Key.J, Key.L);
            input.y = ReadAxis(keyboard, Key.K, Key.I);

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                input += gamepad.leftStick.ReadValue();
                input += gamepad.dpad.ReadValue();
            }
        }
        else
        {
            input.x = useArrowKeys
                ? ReadAxis(keyboard, Key.LeftArrow, Key.RightArrow)
                : ReadAxis(keyboard, Key.A, Key.D);
            input.y = useArrowKeys
                ? ReadAxis(keyboard, Key.DownArrow, Key.UpArrow)
                : ReadAxis(keyboard, Key.S, Key.W);
        }

        // Zapobiega szybszemu chodzeniu po skosie
        input = input.normalized;

        // Ruch
        transform.position += (Vector3)input * moveSpeed * Time.deltaTime;

        // Animator
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetFloat("MoveX", input.x);
            animator.SetFloat("MoveY", input.y);
            animator.SetFloat("speed", input.sqrMagnitude);
        }

        if (dogSpriteAnimator != null)
        {
            dogSpriteAnimator.SetMovement(input);
        }
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
