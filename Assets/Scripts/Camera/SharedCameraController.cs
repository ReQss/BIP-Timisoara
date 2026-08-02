using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SharedCameraController : MonoBehaviour
{
    [Header("Players")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    [Header("Follow")]
    [SerializeField] private float followSpeed = 5f;

    [Header("Zoom")]
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 9f;

    [SerializeField] private float zoomStartDistance = 2f;
    [SerializeField] private float zoomEndDistance = 8f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (player1 == null || player2 == null)
            return;

        FollowPlayers();
        Zoom();
    }

    private void FollowPlayers()
    {
        Vector3 center = (player1.position + player2.position) / 2f;

        Vector3 targetPosition = new Vector3(
            center.x,
            center.y,
            transform.position.z);

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime);
    }

    private void Zoom()
    {
        float distance = Vector2.Distance(
            player1.position,
            player2.position);

        float t = Mathf.InverseLerp(
            zoomStartDistance,
            zoomEndDistance,
            distance);

        float targetZoom = Mathf.Lerp(
            minZoom,
            maxZoom,
            t);

        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetZoom,
            followSpeed * Time.deltaTime);
    }
}