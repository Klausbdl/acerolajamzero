using System;
using System.Collections.Generic;
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
    public PlayerAttributes attributes;

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
        return playerInventory.slots[side] - (side == 0 ? equippedLeftArmModules.Count : equippedRightArmModules.Count) + (playerInventory.slots[side] > 1 ? 0 : 1);
    }
}

[Serializable]
public struct PlayerAttributes
{
    int level;
    public int Level
    {
        get {
            level = UpdateLevel();
            return level;
        }
    }
    public int vitality; //max hp
    public int defense; //damage reduction
    public int agility; //movement speed
    public int strength; //attack damage
    public int dexterity; //attack speed
    public int jump; //jump height
    public int currency;

    public int UpdateLevel()
    {
        return vitality + defense + agility + strength + dexterity + jump;
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