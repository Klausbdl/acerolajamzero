using System.Collections;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [Header("Prefabs")]
    public GameObject[] playerPrefabs;
    public Transform playerPosSpawn;

    [Header("Materials")]
    public Material daySkyMaterial;
    public Material nightSkyMaterial;

    [Header("Components")]
    public WeaponsData weaponData;
    public LegsData legsData;
    public PlayerController playerController;
    public GameObject roomObject;
    
    Animator canvasAnimator;

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
        
        canvasAnimator = GetComponent<Animator>();
    }

    public void StartGame()
    {
        canvasAnimator.SetTrigger("Open Game");
        StartCoroutine(StartGameRoutine());
    }

    IEnumerator StartGameRoutine()
    {
        Debug.Log("Begin Start Game Coroutine");
        yield return new WaitForSeconds(6f);

        Debug.Log("Spawning player");
        foreach (var item in playerPrefabs)
        {
            var i = Instantiate(item, playerPosSpawn.position, Quaternion.identity);
            i.name = item.name;
        }

        playerController = FindAnyObjectByType<PlayerController>();

        yield return new WaitForEndOfFrame();
        Debug.Log("Changing skybox");
        roomObject.SetActive(false);
        RenderSettings.skybox = daySkyMaterial;
        DynamicGI.UpdateEnvironment();

        yield return null;
    }
}
