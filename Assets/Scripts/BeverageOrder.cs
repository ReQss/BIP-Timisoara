using System;
using UnityEngine;

public enum BeverageType
{
    None,
    Coffee,
    Tea,
    Cocoa,
    Latte,
    Espresso,
    IcedCoffee,
    MilkTea,
    GreenTea,
    BerryTea,
    LemonTea,
    Cola,
    OrangeSoda,
    LemonSoda,
    GrapeSoda,
    SparklingWater,
    OrangeJuice,
    AppleJuice,
    BerryJuice,
    Lemonade,
    FruitPunch
}

[Serializable]
public struct BeverageDefinition
{
    public BeverageType type;
    public string displayName;
    public Sprite icon;
}

public interface IBeverageCarrier
{
    bool UsesCatControls { get; }
    Transform CarrierTransform { get; }
    void SetHeldBeverage(BeverageDefinition beverage);
    void SetFridgeMenuOpen(bool open);
}
