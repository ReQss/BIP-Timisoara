using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform otherPlayer;

    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float mergeDistance = 8f;

    private Vector3 targetPosition;

    public void SetMergeDistance(float distance)
    {
        mergeDistance = distance;
    }

    private void LateUpdate()
    {
        float dist = Vector2.Distance(player.position, otherPlayer.position);

        if (dist < mergeDistance)
        {
            Vector3 center = (player.position + otherPlayer.position) / 2f;

            targetPosition = new Vector3(
                center.x,
                center.y,
                transform.position.z);
        }
        else
        {
            targetPosition = new Vector3(
                player.position.x,
                player.position.y,
                transform.position.z);
        }

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime);
    }
}