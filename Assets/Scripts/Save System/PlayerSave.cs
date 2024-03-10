using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class PlayerSave
{
    public string message = "If you managed to deserialize this (which might be easy lol), consider not cheating for the sake of reaching the end. You dont need all 600 levels to get the true ending ;) To make it easier to edit, change the extension to .json. After editing, change it back to .save";
    public int saveId = -1;
    public string lastSaveDate = DateTime.Now.ToString();
    public Inventory playerInventory = new Inventory();
    public List<ArmModule> equippedLeftArmModules = new List<ArmModule>();
    public List<ArmModule> equippedRightArmModules = new List<ArmModule>();
    public LegModule equippedLegModule = new LegModule();
    public PlayerAttributes attributes = new PlayerAttributes();

    public PlayerSave save
    {
        get { 
            lastSaveDate = DateTime.Now.ToString();
            return this;
        }
    }
    public override string ToString()
    {
        return $"<align=left>Save Slot: {saveId}\r\n<align=right>{lastSaveDate}\r\n<align=left>level {attributes.Level}";
    }

    public int GetAvailableSlots(int side)
    {
        return Mathf.Clamp(playerInventory.slots[side] - (side == 0 ? equippedLeftArmModules.Count : equippedRightArmModules.Count) + (playerInventory.slots[side] > 1 ? 0 : 1), 0, 12);
    }

    public float GetArmDamage(int side)
    {
        float d = 0;
        switch (side)
        {
            case 0: equippedLeftArmModules.ForEach(x => d += x.damage); break;
            case 1: equippedRightArmModules.ForEach(x => d += x.damage); break;
        }
        return d;
    }
    public float GetArmKnockback(int side)
    {
        float k = 0;
        switch (side)
        {
            case 0: equippedLeftArmModules.ForEach(x => k += x.knockback); break;
            case 1: equippedRightArmModules.ForEach(x => k += x.knockback); break;
        }
        return k;
    }
    public float GetArmSpeed(int side)
    {
        float s = 0;
        switch (side)
        {
            case 0:
                equippedLeftArmModules.ForEach(x => s += x.speed);
                s /= equippedLeftArmModules.Count;
                break;
            case 1:
                equippedRightArmModules.ForEach(x => s += x.speed);
                s /= equippedRightArmModules.Count;
                break;
        }
        return s + 0.6f;
    }

    public float GetAnimatorValue(ArmModule.ArmModuleType type, int side)
    {
        float v = 0;
        int modulesCount;
        int total;
        switch (side)
        {
            default:
            case 0:
                modulesCount = equippedLeftArmModules.Where(m => m.moduleType == type).ToList().Count;
                total = equippedLeftArmModules.Count;
                break;
            case 1:
                modulesCount = equippedRightArmModules.Where(m => m.moduleType == type).ToList().Count;
                total = equippedRightArmModules.Count;
                break;
        }
        v = (float)modulesCount / total;
        v = Mathf.Clamp(v, 0.01f, 1);
        return v;
    }
}

[Serializable]
public class PlayerAttributes
{
    int level;
    public int Level
    {
        get {
            level = vitality + defense + agility + strength + dexterity + jump - 5;
            return level;
        }
    }
    public int vitality = 1; //max hp
    public int defense = 1; //damage reduction
    public int agility = 1; //movement speed
    public int strength = 1; //attack damage
    public int dexterity = 1; //attack speed
    public int jump = 1; //jump height
    public int currency = 0;

    public PlayerAttributes(int l = 1, int v = 1, int d = 1, int a = 1, int s = 1, int dx = 1, int j = 1, int c = 0)
    {
        level = l;
        vitality = v; defense = d; agility = a; strength = s; dexterity = dx; jump = j; currency = c;
    }

    public int GetMaxHp()
    {
        int hptoAdd = 10;
        for (int i = 0; i <= vitality; i++)
        {
            float a = Mathf.Sin(i * 0.149f) * 3.7f + 10;
            float b = Mathf.Sin(i * 0.02f) * 23 + 5;
            hptoAdd += (int)(a + b) - 10;
        }
        //80 is the starting max hp
        return hptoAdd + 80;
    }
}