using UnityEngine;

public sealed class CafeDestination : MonoBehaviour
{
    public enum DestinationKind
    {
        Seat,
        Toilet
    }

    [SerializeField] private DestinationKind kind;

    public DestinationKind Kind => kind;
    public bool IsOccupied { get; private set; }

    public bool TryReserve()
    {
        if (IsOccupied)
        {
            return false;
        }

        IsOccupied = true;
        return true;
    }

    public void Release()
    {
        IsOccupied = false;
    }

    public void Configure(DestinationKind destinationKind)
    {
        kind = destinationKind;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = kind == DestinationKind.Toilet
            ? new Color(0.25f, 0.75f, 1f, 0.9f)
            : new Color(0.95f, 0.7f, 0.15f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.22f);
        Gizmos.DrawLine(transform.position + Vector3.left * 0.18f, transform.position + Vector3.right * 0.18f);
        Gizmos.DrawLine(transform.position + Vector3.down * 0.18f, transform.position + Vector3.up * 0.18f);
    }
}
