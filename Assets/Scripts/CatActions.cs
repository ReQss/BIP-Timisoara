using UnityEngine;
using System.Collections.Generic;

public class CatActions : MonoBehaviour, IBeverageCarrier
{
    // Przechowuje tag aktualnego obszaru, w którym znajduje się kot (np. "Garden", "Bathroom")
    public string currentAreaTag = "";
    [SerializeField, Min(0.25f)] private float customerInteractionDistance = 1.25f;

    private BeverageDefinition heldBeverage;
    private SpriteRenderer heldBeverageRenderer;
    private bool fridgeMenuOpen;

    public bool UsesCatControls => true;
    public Transform CarrierTransform => transform;
    public bool IsFridgeMenuOpen => fridgeMenuOpen;

    private void Awake()
    {
        CreateHeldBeverageDisplay();
    }

    void Update()
    {
        if (GameManager.IsGameplayInputBlocked || fridgeMenuOpen)
        {
            return;
        }

        // Sprawdzamy wciśnięcie klawisza E w każdej klatce
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!InteractWithCafe())
            {
                PerformActionForCurrentArea();
            }
        }
    }

    private bool InteractWithCafe()
    {
        BeverageFridge fridge = FindAnyObjectByType<BeverageFridge>();
        if (fridge != null && fridge.IsInRange(transform.position))
        {
            fridge.ShowDrinkSelection(this);
            return true;
        }

        if (heldBeverage.type == BeverageType.None)
        {
            return false;
        }

        FrogCustomer[] customers = FindObjectsByType<FrogCustomer>();
        FrogCustomer closest = null;
        float closestDistance = customerInteractionDistance;
        foreach (FrogCustomer customer in customers)
        {
            if (!customer.IsWaitingForOrder || customer.RequestedBeverage != heldBeverage.type)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, customer.transform.position);
            if (distance <= closestDistance)
            {
                closest = customer;
                closestDistance = distance;
            }
        }

        if (closest == null ||
            !closest.TryServe(heldBeverage.type, transform.position, BeverageServer.Cat))
        {
            return false;
        }

        heldBeverage = default;
        UpdateHeldBeverageDisplay();
        return true;
    }

    public void SetHeldBeverage(BeverageDefinition beverage)
    {
        heldBeverage = beverage;
        UpdateHeldBeverageDisplay();
    }

    public void SetFridgeMenuOpen(bool open)
    {
        fridgeMenuOpen = open;
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (open && body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }

    private void CreateHeldBeverageDisplay()
    {
        GameObject display = new GameObject("Held Beverage");
        display.transform.SetParent(transform, false);
        display.transform.localPosition = new Vector3(0.42f, 0.3f, 0f);
        display.transform.localScale = Vector3.one * 0.9f;
        heldBeverageRenderer = display.AddComponent<SpriteRenderer>();
        heldBeverageRenderer.sortingOrder = 2100;
        heldBeverageRenderer.enabled = false;
    }

    private void UpdateHeldBeverageDisplay()
    {
        heldBeverageRenderer.sprite = heldBeverage.icon;
        heldBeverageRenderer.enabled = heldBeverage.type != BeverageType.None && heldBeverage.icon != null;
    }

    // Uniwersalna metoda decydująca o akcji na podstawie tagu obszaru
    private void PerformActionForCurrentArea()
    {
        // Jeśli kot nie jest w żadnym obszarze
        if (string.IsNullOrEmpty(currentAreaTag))
        {
            Debug.Log("Kot nie znajduje się w żadnym wyznaczonym obszarze.");
            return;
        }

        // Dopasowanie akcji do tagu obszaru - TUTAJ DODAJESZ NOWE OBSZARY
        switch (currentAreaTag)
        {
            case "Garden":
                TakeAPiss();
                break;

            case "Bathroom":
                CleanTheBathroom();
                break;

            // Przykład łatwego dodania nowego obszaru:
            // case "Kitchen":
            //     StealFood();
            //     break;

            default:
                Debug.Log($"Brak zdefiniowanej akcji dla obszaru o tagu: {currentAreaTag}");
                break;
        }
    }

    public void TakeAPiss()
    {
        Debug.Log("Kot siknie w ogródku!");
    }

    public void CleanTheBathroom()
    {
        Debug.Log("Kot sprząta w toalecie!");
    }

    // Automatyczne przypisanie obszaru przy wejściu w trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Przypisujemy tag obiektu, z którym kot koliduje
        currentAreaTag = other.tag;
        Debug.Log($"Kot wszedł do obszaru: {currentAreaTag}");
    }

    // Czyszczenie obszaru przy wyjściu z triggera
    private void OnTriggerExit2D(Collider2D other)
    {
        // Jeśli obiekt, z którego wychodzimy, to nasz aktualny obszar, resetujemy go
        if (other.tag == currentAreaTag)
        {
            Debug.Log($"Kot wyszedł z obszaru: {currentAreaTag}");
            currentAreaTag = "";
        }
    }
}
