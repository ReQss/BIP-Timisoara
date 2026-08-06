using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class CafeRuntimeSetup
{
    private const string SystemName = "Cafe Customer System";
    private const float CustomerPositionYOffset = -0.6f;
    private static readonly Vector3 EntrancePosition = new Vector3(1.74f, -1.75f + CustomerPositionYOffset, 0f);
    private static readonly Vector3[] ChairPositions =
    {
        new Vector3(-0.08f, 2.17f + CustomerPositionYOffset, 0f),
        new Vector3(3.43f, 0.36f + CustomerPositionYOffset, 0f),
        new Vector3(6.01f, 2.65f + CustomerPositionYOffset, 0f),
        new Vector3(7.41f, -1.61f + CustomerPositionYOffset, 0f),
        new Vector3(11.03f, 0.08f + CustomerPositionYOffset, 0f)
    };

    public static void Ensure(Texture2D characterSheet, BeverageDefinition[] beverages, Sprite fridgeSprite)
    {
        CafeCustomerDirector existingDirector = UnityEngine.Object.FindAnyObjectByType<CafeCustomerDirector>();
        if (existingDirector != null)
        {
            existingDirector.ApplyChairLayout(EntrancePosition, ChairPositions);
            return;
        }

        if (characterSheet == null || beverages == null || beverages.Length == 0)
        {
            return;
        }

        FrogCustomer template = CreateCustomerTemplate(characterSheet, beverages);
        GameObject system = new GameObject(SystemName);
        CafeCustomerDirector director = system.AddComponent<CafeCustomerDirector>();

        Transform entrance = CreateMarker(system.transform, "Entrance", EntrancePosition);
        Transform exit = CreateMarker(system.transform, "Exit", EntrancePosition + Vector3.left * 0.65f);

        CafeDestination[] seats = new CafeDestination[ChairPositions.Length];
        for (int i = 0; i < seats.Length; i++)
        {
            seats[i] = CreateDestination(system.transform, "Chair " + (i + 1), ChairPositions[i], CafeDestination.DestinationKind.Seat, Vector2.up);
        }

        Tilemap inside = FindTilemap("Tables");
        Vector3 toiletPosition = inside != null
            ? inside.transform.TransformPoint(inside.localBounds.max + new Vector3(-0.5f, -0.5f))
            : new Vector3(4f, 3f);
        CafeDestination toilet = CreateDestination(system.transform, "Toilet", toiletPosition, CafeDestination.DestinationKind.Toilet, Vector2.down);
        director.Configure(template, entrance, exit, seats, toilet);

        GameObject fridge = new GameObject("Beverage Fridge");
        SpriteRenderer fridgeRenderer = fridge.AddComponent<SpriteRenderer>();
        fridgeRenderer.sprite = fridgeSprite != null ? fridgeSprite : beverages[0].icon;
        fridgeRenderer.color = new Color(0.65f, 0.88f, 1f);
        fridgeRenderer.sortingOrder = 20;
        BeverageFridge fridgeComponent = fridge.AddComponent<BeverageFridge>();
        fridgeComponent.Configure(beverages);
        Tilemap kitchen = FindTilemap("Kitchen");
        fridge.transform.position = kitchen != null
            ? kitchen.transform.TransformPoint(kitchen.localBounds.center + Vector3.right * (kitchen.localBounds.extents.x - 0.75f))
            : new Vector3(4f, 2f);
    }

    public static BeverageDefinition[] CreateBeverageMenu(Texture2D sheet, int count)
    {
        int itemCount = Mathf.Clamp(count, 1, 20);
        BeverageDefinition[] menu = new BeverageDefinition[itemCount];
        string[] names =
        {
            "Coffee", "Tea", "Cocoa", "Latte", "Espresso",
            "Iced Coffee", "Milk Tea", "Green Tea", "Berry Tea", "Lemon Tea",
            "Cola", "Orange Soda", "Lemon Soda", "Grape Soda", "Sparkling Water",
            "Orange Juice", "Apple Juice", "Berry Juice", "Lemonade", "Fruit Punch"
        };

        for (int i = 0; i < itemCount; i++)
        {
            int column = i % 13;
            int row = i / 13;
            Sprite icon = Sprite.Create(
                sheet,
                new Rect(column * 16, sheet.height - (row + 1) * 16, 16, 16),
                new Vector2(0.5f, 0.5f),
                16f,
                0,
                SpriteMeshType.FullRect);
            icon.name = "Beverage " + (i + 1);
            menu[i] = new BeverageDefinition
            {
                type = (BeverageType)(i + 1),
                displayName = names[i],
                icon = icon
            };
        }

        return menu;
    }

    private static FrogCustomer CreateCustomerTemplate(Texture2D sheet, BeverageDefinition[] beverages)
    {
        GameObject root = new GameObject("Traveller Customer Template");
        root.hideFlags = HideFlags.HideInHierarchy;
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 1000;
        FrogCustomer customer = root.AddComponent<FrogCustomer>();

        Sprite[][] rows = new Sprite[8][];
        for (int row = 0; row < rows.Length; row++)
        {
            rows[row] = new Sprite[9];
            for (int column = 0; column < 9; column++)
            {
                rows[row][column] = Sprite.Create(
                    sheet,
                    new Rect(1 + column * 23, 289 - row * 36, 23, 36),
                    new Vector2(0.5f, 0f),
                    32f,
                    0,
                    SpriteMeshType.FullRect);
            }
        }

        Sprite[] Idle(int row) => new[] { rows[row][0] };
        Sprite[] Walk(int row) => rows[row].Skip(1).ToArray();
        renderer.sprite = rows[0][0];
        customer.ConfigureAnimationFrames(
            Idle(0), Idle(4), Idle(6), Idle(2), Walk(0), Walk(4), Walk(6), Walk(2),
            Idle(1), Idle(3), Idle(7), Idle(5), Walk(1), Walk(3), Walk(7), Walk(5));
        customer.ConfigureBeverages(beverages);
        root.SetActive(false);
        return customer;
    }

    private static Tilemap FindTilemap(string name)
    {
        return UnityEngine.Object.FindObjectsByType<Tilemap>()
            .FirstOrDefault(tilemap => string.Equals(tilemap.name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static Transform CreateMarker(Transform parent, string name, Vector3 position)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent);
        marker.transform.position = position;
        return marker.transform;
    }

    private static CafeDestination CreateDestination(Transform parent, string name, Vector3 position, CafeDestination.DestinationKind kind, Vector2 facing)
    {
        CafeDestination destination = CreateMarker(parent, name, position).gameObject.AddComponent<CafeDestination>();
        destination.Configure(kind, facing);
        return destination;
    }
}
