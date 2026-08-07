using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public sealed class CafeCustomerDirector : MonoBehaviour
{
    [SerializeField] private FrogCustomer customerPrefab;
    [SerializeField] private Transform entrance;
    [SerializeField] private Transform exit;
    [SerializeField] private CafeDestination[] seats;
    [SerializeField] private CafeDestination toilet;
    [SerializeField, Min(0.5f)] private float firstArrivalDelay = 1.5f;
    [SerializeField, Min(1f)] private float arrivalInterval = 8f;
    [SerializeField, Min(1)] private int maximumCustomers = 3;

    private readonly List<FrogCustomer> customers = new List<FrogCustomer>();
    private float nextArrivalTime;

    public int ServedCount { get; private set; }
    public int UnhappyCount { get; private set; }

    public void ResetRoundStatistics()
    {
        ServedCount = 0;
        UnhappyCount = 0;
    }

    private void Awake()
    {
        EnsureUiEventSystem();
        nextArrivalTime = Time.time + firstArrivalDelay;
    }

    private void Update()
    {
        customers.RemoveAll(customer => customer == null);
        if (Time.time < nextArrivalTime || customers.Count >= maximumCustomers)
        {
            return;
        }

        nextArrivalTime = Time.time + arrivalInterval;
        TrySpawnCustomer();
    }

    private void TrySpawnCustomer()
    {
        if (customerPrefab == null || entrance == null || exit == null)
        {
            return;
        }

        CafeDestination seat = FindAvailableSeat();
        if (seat == null)
        {
            return;
        }

        FrogCustomer customer = Instantiate(customerPrefab, entrance.position, Quaternion.identity);
        customer.name = "Traveller Customer";
        customer.gameObject.SetActive(true);
        customer.Initialize(this, seat, exit);
        customers.Add(customer);
    }

    private CafeDestination FindAvailableSeat()
    {
        if (seats == null || seats.Length == 0)
        {
            return null;
        }

        int start = Random.Range(0, seats.Length);
        for (int i = 0; i < seats.Length; i++)
        {
            CafeDestination seat = seats[(start + i) % seats.Length];
            if (seat != null && seat.Kind == CafeDestination.DestinationKind.Seat && seat.TryReserve())
            {
                return seat;
            }
        }

        return null;
    }

    public CafeDestination TryReserveToilet()
    {
        return toilet != null && toilet.Kind == CafeDestination.DestinationKind.Toilet && toilet.TryReserve()
            ? toilet
            : null;
    }

    public void CustomerFinished(FrogCustomer customer, bool wasServed)
    {
        customers.Remove(customer);
        if (wasServed)
        {
            ServedCount++;
        }
        else
        {
            UnhappyCount++;
        }
    }

    public void Configure(
        FrogCustomer prefab,
        Transform entrancePoint,
        Transform exitPoint,
        CafeDestination[] seatPoints,
        CafeDestination toiletPoint)
    {
        customerPrefab = prefab;
        entrance = entrancePoint;
        exit = exitPoint;
        seats = seatPoints;
        toilet = toiletPoint;
        maximumCustomers = Mathf.Min(5, seats != null ? seats.Length : 5);
    }

    public void ApplyChairLayout(Vector3 entrancePosition, IReadOnlyList<Vector3> chairPositions)
    {
        if (entrance != null)
        {
            entrance.position = entrancePosition;
        }
        if (exit != null)
        {
            exit.position = entrancePosition + Vector3.left * 0.65f;
        }

        CafeDestination[] configuredSeats = new CafeDestination[chairPositions.Count];
        for (int i = 0; i < chairPositions.Count; i++)
        {
            CafeDestination seat = seats != null && i < seats.Length ? seats[i] : null;
            if (seat == null)
            {
                GameObject marker = new GameObject("Chair " + (i + 1));
                marker.transform.SetParent(transform);
                seat = marker.AddComponent<CafeDestination>();
            }

            seat.transform.position = chairPositions[i];
            seat.Configure(CafeDestination.DestinationKind.Seat, Vector2.up);
            configuredSeats[i] = seat;
        }

        seats = configuredSeats;
        maximumCustomers = chairPositions.Count;
    }

    private static void EnsureUiEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<InputSystemUIInputModule>();
    }
}
