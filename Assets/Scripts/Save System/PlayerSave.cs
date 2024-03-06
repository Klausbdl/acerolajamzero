using System;
using UnityEngine;

public class PlayerSave
{
    public string message = "If you managed to deserialize this (which might be easy lol), consider not cheating for the sake of reaching the end. You dont need all 600 levels to get the true ending ;)";
    public int saveId;
    public string lastSaveDate = DateTime.Now.ToString();

    public PlayerAttributes attributes;

    public override string ToString()
    {
        return $"<align=left>Save Slot: {saveId}\r\n<align=right>{lastSaveDate}\r\n<align=left>level {attributes.level}";
    }
}

[Serializable]
public struct PlayerAttributes
{
    public int level;
    public int vitality; //max hp
    public int defense; //damage reduction
    public int agility; //movement speed
    public int strength; //attack damage
    public int dexterity; //attack speed
    public int jump; //jump height
    public int currency;

    public float GetMaxHp()
    {
        float maxHp = 10;
        for (int i = 0; i <= vitality; i++)
        {
            float a = Mathf.Sin(i * 0.149f) * 3.7f + 10;
            float b = Mathf.Sin(i * 0.02f) * 23 + 5;
            maxHp += a + b - 10;
        }

        return maxHp;
    }
}