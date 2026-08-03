using UnityEngine;
using System.Collections.Generic;

public class CatActions : MonoBehaviour
{
    // Przechowuje tag aktualnego obszaru, w którym znajduje się kot (np. "Garden", "Bathroom")
    public string currentAreaTag = "";

    void Start()
    {
        
    }

    void Update()
    {
        // Sprawdzamy wciśnięcie klawisza E w każdej klatce
        if (Input.GetKeyDown(KeyCode.E))
        {
            PerformActionForCurrentArea();
        }
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