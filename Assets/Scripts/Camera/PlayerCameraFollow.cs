using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform otherPlayer;

    [Header("Movement Smoothness")]
    [SerializeField] private float followSpeed = 8f;

    [Header("Zoom Settings")]
    [SerializeField] private float minOrthographicSize = 5f;  // Zoom przy pełnym podziale / bliskości
    [SerializeField] private float maxOrthographicSize = 10f; // Zoom przy braku podziału, gdy gracze się oddalają
    [SerializeField] private float zoomSpeed = 5f;

    private Camera cam;
    private float splitAmount;

    public Transform Player => player;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void SetSplitAmount(float value)
    {
        splitAmount = Mathf.Clamp01(value);
    }

    private void LateUpdate()
    {
        if (player == null || otherPlayer == null)
            return;

        // 1. Wyznaczenie środka i pozycji docelowej
        Vector3 center = (player.position + otherPlayer.position) * 0.5f;

        // Gdy splitAmount = 0 -> kamera celuje w środek między graczami
        // Gdy splitAmount = 1 -> kamera celuje bezpośrednio w przydzielonego gracza
        Vector3 targetPos = Vector3.Lerp(center, player.position, splitAmount);
        targetPos.z = transform.position.z;

        // Płynny ruch kamery
        transform.position = Vector3.Lerp(
            transform.position, 
            targetPos, 
            followSpeed * Time.deltaTime);

        // 2. Dynamiczny Zoom
        float distance = Vector2.Distance(player.position, otherPlayer.position);
        
        // Gdy ekrany są połączone (splitAmount = 0), zoom dostosowuje się do odległości graczy.
        // Gdy ekrany się rozdzielą (splitAmount = 1), wracamy do standardowego zbliżenia na gracza.
        float targetSize = Mathf.Lerp(
            Mathf.Clamp(distance * 0.5f + 2f, minOrthographicSize, maxOrthographicSize),
            minOrthographicSize,
            splitAmount
        );

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, zoomSpeed * Time.deltaTime);
    }
}
