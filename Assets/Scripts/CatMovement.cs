using UnityEngine;

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
        // Pobranie inputu
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        // Zapobiega szybszemu chodzeniu po skosie
        input = input.normalized;

        // Ruch
        transform.position += (Vector3)input * moveSpeed * Time.deltaTime;

        // Animator
        animator.SetFloat("MoveX", input.x);
        animator.SetFloat("MoveY", input.y);
        animator.SetFloat("speed", input.sqrMagnitude);
    }
}