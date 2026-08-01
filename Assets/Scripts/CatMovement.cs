using UnityEngine;

public class CatMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Vector2 movement;

    void Update()
    {
        // Pobranie inputu
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Zapobiega szybszemu chodzeniu po skosie
        movement = movement.normalized;

        // Ruch
        transform.position += (Vector3)(movement * moveSpeed * Time.deltaTime);
    }
}