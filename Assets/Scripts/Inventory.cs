using System;
using System.Collections.Generic;

[Serializable]
public class Inventory
{
    public int[] slots = new int[2] { 1, 1 };
    public List<ArmModule> leftArmModules = new List<ArmModule>();
    public List<ArmModule> rightArmModules = new List<ArmModule>();
    public List<LegModule> legModules = new List<LegModule>();
}
[Serializable]
public class ItemModule
{
    public string name;

    public ItemModule() { }
}
[Serializable]
public class ArmModule : ItemModule
{
    public enum ArmModuleType
    {
        PUNCH = 0,
        SWORD,
        CANNON
    }
    public ArmModuleType moduleType;
    public float damage;
    public float knockback;
    public float speed;

    public ArmModule() { }
}
[Serializable]
public class LegModule : ItemModule
{
    public enum LegModuleType
    {
        NORMAL = 0, WHEEL, BALL, SPIKE, FOOT, HOVER, PISTON
    }
    public LegModuleType moduleType;
    public float speed;
    public float jump;
    public LegModule() { }
}