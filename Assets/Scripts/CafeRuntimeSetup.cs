using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class CafeRuntimeSetup
{
    private const string SystemName = "Cafe Customer System";

    public static void Ensure(Texture2D characterSheet, BeverageDefinition[] beverages, Sprite fridgeSprite)
    {
        if (UnityEngine.Object.FindAnyObjectByType<CafeCustomerDirector>() != null ||
            characterSheet == null || beverages == null || beverages.Length == 0)
        {
            return;
        }

        FrogCustomer template = CreateCustomerTemplate(characterSheet, beverages);
        GameObject system = new GameObject(SystemName);
        CafeCustomerDirector director = system.AddComponent<CafeCustomerDirector>();

        Tilemap inside = FindTilemap("Tables");
        Tilemap outside = FindTilemap("Outside tables");
        List<Vector3> insideTables = FindFurnitureCenters(inside, 5);
        List<Vector3> outsideTables = FindFurnitureCenters(outside, 3);
        List<Vector3> tables = insideTables.Concat(outsideTables).ToList();

        Vector3 entrancePosition = outsideTables.Count > 0
            ? outsideTables.OrderBy(point => point.x).First() + new Vector3(-2f, -1.5f)
            : new Vector3(-6f, -3f);
        Transform entrance = CreateMarker(system.transform, "Entrance", entrancePosition);
        Transform exit = CreateMarker(system.transform, "Exit", entrancePosition + Vector3.left * 0.8f);

        CafeDestination[] seats = new CafeDestination[8];
        for (int i = 0; i < seats.Length; i++)
        {
            Vector3 table = i < tables.Count ? tables[i] : new Vector3(-3f + (i % 4) * 2f, -1f + (i / 4) * 3f);
            seats[i] = CreateDestination(system.transform, "Table Seat " + (i + 1), table + Vector3.down * 0.65f, CafeDestination.DestinationKind.Seat, Vector2.up);
        }

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

    private static List<Vector3> FindFurnitureCenters(Tilemap tilemap, int count)
    {
        if (tilemap == null)
        {
            return new List<Vector3>();
        }

        HashSet<Vector3Int> cells = new HashSet<Vector3Int>();
        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(cell)) cells.Add(cell);
        }

        List<List<Vector3Int>> clusters = new List<List<Vector3Int>>();
        while (cells.Count > 0)
        {
            Vector3Int start = cells.First();
            cells.Remove(start);
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            queue.Enqueue(start);
            List<Vector3Int> cluster = new List<Vector3Int>();
            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                cluster.Add(current);
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    Vector3Int neighbour = current + new Vector3Int(x, y);
                    if (cells.Remove(neighbour)) queue.Enqueue(neighbour);
                }
            }
            clusters.Add(cluster);
        }

        return clusters.OrderByDescending(cluster => cluster.Count).Take(count)
            .Select(cluster => cluster.Select(tilemap.GetCellCenterWorld).Aggregate(Vector3.zero, (sum, point) => sum + point) / cluster.Count)
            .OrderBy(point => point.y).ThenBy(point => point.x).ToList();
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
