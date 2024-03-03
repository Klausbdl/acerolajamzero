using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [Header("Components")]
    public WeaponsData weaponData;
    public LegsData legsData;
    public PlayerController playerController;

    [Header("Player Attributes")]
    public int vitality; //max hp
    public int defense; //damage reduction
    public int agility; //movement speed
    public int strength; //attack damage
    public int dexterity; //attack speed
    public int jump; //jump height
    public int currency;

    public override void Awake()
    {
        base.Awake();
        playerController = FindAnyObjectByType<PlayerController>();
    }
}
