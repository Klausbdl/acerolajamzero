using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using System.Linq;
using UnityEditor.Rendering;


public class GameManager : Singleton<GameManager>
{
    [Header("Prefabs")]
    public GameObject[] playerPrefabs;
    public Transform playerPosSpawn;

    [Header("Materials")]
    public Material daySkyMaterial;
    public Material nightSkyMaterial;

    [Header("Components")]
    Animator canvasAnimator;
    public WeaponsData weaponData;
    public LegsData legsData;
    public PlayerController playerController;
    public GameObject roomObject;
    public UIManager uiManager;
    public Light sunLight;
    public AudioManager audioManager;
    public AudioLibrary audioLibrary;
    
    [Header("Player Attributes")]
    public List<PlayerSave> saves = new List<PlayerSave>();
    public PlayerSave currentSave;
    public PlayerAttributes PlayerAttributes
    {
        get { return currentSave.attributes; }
    }

    [Header("Game")]
    public bool pause = false;
    public GameObject[] levels;
    public Transform[] startPoints;
    public bool inRun;
    private float runTime = 0f;
    public Inventory shopInventory;
    public int currentLevel = 0;
    public List<Enemy> enemiesAlive = new List<Enemy>();
    public List<Enemy> enemiesDead = new List<Enemy>();
    public int oolCollected;

    //debug
    public string debugString = "";
    string debugUpdateString = "";

    private void Start()
    {
        Time.timeScale = 1;
        canvasAnimator = GetComponent<Animator>();
        canvasAnimator.SetTrigger("Start App");
        debugString += canvasAnimator.name;

        ChangeSky("night");

        uiManager.LoadGraphicsAndAudio();
        EventSystem.current.SetSelectedGameObject(uiManager.startButton.gameObject);

        foreach(var level in levels)
        {
            level.SetActive(false);
        }

        AudioManager.Instance.PlayAudio(audioLibrary.computerHumming, AudioManager.AudioType.MUSIC, 2);

        //StartCoroutine(LateStart());
    }

    IEnumerator LateStart()
    {
        yield return new WaitForEndOfFrame();
        
    }

    public void Update()
    {
        debugUpdateString = "";
        debugUpdateString += $"time:{Time.time}\nunscaled time:{Time.unscaledTime}";
        debugUpdateString += $"\npause:{pause} | in run:{inRun}";
        if(canvasAnimator.GetCurrentAnimatorClipInfo(0).Length > 0)
            debugUpdateString += $"\nanimator: {canvasAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name}";
        debugUpdateString += $"\nSun - rot:{sunLight.transform.rotation.eulerAngles} intensity:{sunLight.intensity}";


        if (Input.GetButtonDown("Pause"))
        {
            pause = !pause;
            if (pause)
                PauseRun();
            else
                ResumeRun();
        }

        if (inRun && !pause)
        {
            runTime += Time.deltaTime;

            float minutes = Mathf.FloorToInt(runTime / 60);
            float seconds = Mathf.FloorToInt(runTime % 60);
            float milliseconds = (runTime * 1000) % 1000;

            uiManager.timerText.text = string.Format("{0:00}'{1:00}''<size=60%>{2:000}", minutes, seconds, milliseconds);
        }
    }
    //-------------------------------------------------------------------------- before opening game
    public void OpenGame()
    {
        StartCoroutine(OpenGameRoutine());
    }
    IEnumerator OpenGameRoutine()
    {
        Debug.Log("Begin Start Game Coroutine");

        yield return new WaitForSeconds(4);

        #region ui stuff
        Debug.Log("Loading Saves");
        EventSystem.current.SetSelectedGameObject(uiManager.newGameButton.gameObject);
        LoadSaves();
        
        if (PlayerPrefs.HasKey("Last Slot"))
        {
            uiManager.lastSlotText.text = $"Save Slot {PlayerPrefs.GetInt("Last Slot")}";
            uiManager.continueButton.onClick.AddListener(() => {
                OnPlayGameButton(PlayerPrefs.GetInt("Last Slot"));
            });
        }
        else
        {
            uiManager.lastSlotText.text = "";
        }
        #endregion

        AudioManager.Instance.StopAudio(audioLibrary.computerHumming, 2);
        
        //menu music
        AudioManager.Instance.PlayAudio(audioLibrary.mainMenu[0], AudioManager.AudioType.MUSIC, 2, true, 0, 0);
        AudioManager.Instance.PlayAudio(audioLibrary.mainMenu[1], AudioManager.AudioType.MUSIC, 2);
        AudioManager.Instance.PlayAudio(audioLibrary.mainMenu[2], AudioManager.AudioType.MUSIC, 2, true, 0, 0);

        Debug.Log("End Start Game Coroutine");
    }

    public void CloseGame()
    {
        StartCoroutine(CloseGameRoutine());
    }
    IEnumerator CloseGameRoutine()
    {
        Debug.Log("Begin Close Game Coroutine");
        yield return new WaitForSeconds(5);
        AudioManager.Instance.PlayAudio(audioLibrary.computerHumming, AudioManager.AudioType.MUSIC, 1);
        Debug.Log("End Close Game Coroutine");
        yield return null;
    }
    public void QuitApplication()
    {
        Application.Quit();
    }
    //-------------------------------------------------------------------------- inside game
    public void OnLoadButton()
    {
        //when clicking the load button, make the first slot selected
        EventSystem.current.SetSelectedGameObject(uiManager.saveSlotsContent.GetComponentsInChildren<Button>()[0].gameObject);
    }
    public void OnPlayGameButton(int slot)
    {
        if(slot == -1)
        {
            int newSlot = 0;
            saves.Sort((a, b) => a.saveId.CompareTo(b.saveId));
            foreach (var save in saves)
            {
                Debug.Log(save.saveId + " " + newSlot);
                if (save.saveId == newSlot)
                    newSlot++;
                else
                    break;
            }

            Debug.Log($"Starting New game {newSlot}");
            
            PlayerPrefs.SetInt("Last Slot", newSlot);
            
            #region create first save
            PlayerSave newsave = new PlayerSave();
            newsave.saveId = newSlot;
            currentSave = newsave;
            saves.Add(currentSave);
            saves.Sort((a, b) => a.saveId.CompareTo(b.saveId));
            #endregion
            #region recreate saves buttons
            uiManager.saveSlotButtonList.ForEach(sb => Destroy(sb.gameObject));
            uiManager.saveSlotButtonList.Clear();
            foreach (var s in saves)
            {
                SaveSlotButton ssb = Instantiate(uiManager.saveSlotPrefab, uiManager.saveSlotsContent.transform).GetComponent<SaveSlotButton>();
                uiManager.saveSlotButtonList.Add(ssb);
                ssb.id = s.saveId;
                ssb.GetComponentInChildren<TextMeshProUGUI>().text = s.ToString();

                EventTrigger eventTrigger = ssb.GetComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.Cancel;
                entry.callback.AddListener((eventData) =>
                {
                    EventSystem.current.SetSelectedGameObject(uiManager.loadBackButton.gameObject);
                });
                eventTrigger.triggers.Add(entry);
            }
            
            #endregion
            
            uiManager.continueButton.interactable = true;
            uiManager.loadButton.interactable = true;
            uiManager.lastSlotText.text = $"Save Slot {newSlot}";

            //add first items to first save
            //arms
            currentSave.playerInventory.leftArmModules.Add(shopInventory.leftArmModules[4]);
            currentSave.playerInventory.rightArmModules.Add(shopInventory.rightArmModules[4]);
            currentSave.equippedLeftArmModules.Add(shopInventory.leftArmModules[4]);
            currentSave.equippedRightArmModules.Add(shopInventory.rightArmModules[4]);
            //legs
            currentSave.playerInventory.legModules.Add(shopInventory.legModules[0]);
            currentSave.equippedLegModule = currentSave.playerInventory.legModules[0];

            SaveToJson(currentSave, newSlot);
        }
        else
        {
            Debug.Log($"Loading from slot: {slot}");
            
            PlayerPrefs.SetInt("Last Slot", slot);
            uiManager.lastSlotText.text = $"Save Slot {PlayerPrefs.GetInt("Last Slot")}";
            currentSave = saves.FirstOrDefault(s => s.saveId == slot);
        }

        //TODO: DEBUG
        #region debug
        //currentSave.playerInventory.leftArmModules.Add(shopInventory.leftArmModules[3]);
        //currentSave.playerInventory.rightArmModules.Add(shopInventory.rightArmModules[5]);
        //currentSave.playerInventory.leftArmModules.Add(shopInventory.leftArmModules[1]);
        //currentSave.playerInventory.rightArmModules.Add(shopInventory.rightArmModules[11]);
        //currentSave.playerInventory.rightArmModules.Add(shopInventory.rightArmModules[1]);
        //currentSave.playerInventory.rightArmModules.Add(shopInventory.rightArmModules[2]);
        //currentSave.playerInventory.legModules.Add(shopInventory.legModules[1]);
        //currentSave.playerInventory.legModules.Add(shopInventory.legModules[2]);
        //currentSave.playerInventory.legModules.Add(shopInventory.legModules[3]);
        currentSave.attributes.currency = 999999;
        #endregion

        #region spawn player
        if (playerController == null)
        {
            Debug.Log("Spawning player");
            foreach (var item in playerPrefabs)
            {
                var i = Instantiate(item, playerPosSpawn.position, Quaternion.identity);
                i.name = item.name;
            }

            playerController = FindAnyObjectByType<PlayerController>();
        }
        else
            Debug.Log("Player already spawned");

        Debug.Log("Updating player variables");
        
        //blend shapes
        foreach (var skin in playerController.bodyParts)
            for (var i = 0; i < skin.sharedMesh.blendShapeCount; i++)
                if (skin.GetBlendShapeWeight(i) != -1)
                    skin.SetBlendShapeWeight(i, 0);
        
        uiManager.UpdateBodyDeformation();

        //variables
        UpdatePlayerVariables();
        #endregion

        uiManager.LoadPlayerSettings();

        Debug.Log("Loading Player Attributes");
        uiManager.UpdateStats(currentSave.attributes);
        uiManager.UpdateInventory(currentSave);
        uiManager.UpdateArmShop("slots");
        uiManager.UpdateLegShop("wheel");
        uiManager.UpdateLevelUp();

        canvasAnimator.SetTrigger("Start Game");

        //audios
        audioManager.oneShotMusic.PlayOneShot(audioLibrary.transition);

        AudioManager.Instance.PlayAudio(audioLibrary.mainMenu[0], AudioManager.AudioType.MUSIC, 1, true, 3, 1);
        AudioManager.Instance.PlayAudio(audioLibrary.mainMenu[2], AudioManager.AudioType.MUSIC, 1, true, 3, 1);
    }

    /// <summary>
    /// 0: max hp | 1: def | 2: speed | 3: left dmg 
    /// 4: right dmg | 5: left speed | 6: right speed
    /// 7: jump | 8: left kbk | 9: right kbk
    /// </summary>
    /// <returns></returns>
    public string UpdatePlayerVariables()
    {
        string returnString = "";
        //vitality
        playerController.maxHp = PlayerAttributes.GetMaxHp();
        playerController.curHp = playerController.maxHp;
        returnString += playerController.maxHp.ToString() + "_"; //0
        //defense
        playerController.defense = UtilsFunctions.Map(0, 100, 0, .5f, PlayerAttributes.defense);
        returnString += ((int)(playerController.defense * 10)).ToString() + "_"; //1
        //agility
        playerController.speed = UtilsFunctions.Map(0, 100, 10, 40, PlayerAttributes.agility) * currentSave.equippedLegModule.speed;
        playerController.speedMultiplier = UtilsFunctions.Map(0, 100, 1, 2, PlayerAttributes.agility);
        returnString += playerController.speed.ToString("0.00") + "_"; //2
        //strength
        float damageMultiplier = UtilsFunctions.Map(0, 100, 1, 10, PlayerAttributes.strength);
        playerController.leftDamage = currentSave.GetArmDamage(0) * damageMultiplier;
        returnString += playerController.leftDamage.ToString("0.00") + "_"; //3
        playerController.rightDamage = currentSave.GetArmDamage(1) * damageMultiplier;
        returnString += playerController.rightDamage.ToString("0.00") + "_"; //4
        //dex
        float dexMultiplier = UtilsFunctions.Map(0, 100, 1, 2, PlayerAttributes.dexterity);
        playerController.leftAttackSpeed = currentSave.GetArmSpeed(0) * dexMultiplier;
        returnString += playerController.leftAttackSpeed.ToString("0.00") + "_"; //5
        playerController.rightAttackSpeed = currentSave.GetArmSpeed(1) * dexMultiplier;
        returnString += playerController.rightAttackSpeed.ToString("0.00") + "_"; //6
        //jump
        playerController.jumpHeight = UtilsFunctions.Map(0, 100, 4f, 10f, PlayerAttributes.jump) * currentSave.equippedLegModule.jump;
        returnString += playerController.jumpHeight.ToString("0.00") + "_"; //7
        //knockback
        playerController.leftKnockback = currentSave.GetArmKnockback(0);
        returnString += playerController.leftKnockback.ToString("0.00") + "_"; //8
        playerController.rightKnockback = currentSave.GetArmKnockback(1);
        returnString += playerController.rightKnockback.ToString("0.00") + "_"; //9

        //animator variables
        playerController.leftPunchValue = currentSave.GetAnimatorValue(ArmModule.ArmModuleType.PUNCH, 0);
        playerController.rightPunchValue = currentSave.GetAnimatorValue(ArmModule.ArmModuleType.PUNCH, 1);
        playerController.leftSwordValue = currentSave.GetAnimatorValue(ArmModule.ArmModuleType.SWORD, 0);
        playerController.rightSwordValue = currentSave.GetAnimatorValue(ArmModule.ArmModuleType.SWORD, 1);
        playerController.leftShootValue = currentSave.GetAnimatorValue(ArmModule.ArmModuleType.GUN, 0);
        playerController.rightShootValue = currentSave.GetAnimatorValue(ArmModule.ArmModuleType.GUN, 1);

        playerController.UpdateModulePercentages();
        return returnString;
    }

    #region save system
    private void SaveToJson(PlayerSave s, int id)
    {
        string saveData = JsonUtility.ToJson(s);
        string filePath = Application.dataPath + $"/Saves/save {id}.save";
        string dataPath = Application.dataPath + "/Saves";
        if (!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        File.WriteAllText(filePath, saveData);
    }
    public void LoadSaves()
    {
        //clear saves
        uiManager.saveSlotButtonList.ForEach(t => { Destroy(t.gameObject); });
        uiManager.saveSlotButtonList.Clear();
        saves.Clear();

        //load saves from files
        string dataPath = Application.dataPath + "/Saves";

        if (!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        string[] jsonFiles = Directory.GetFiles(dataPath, "*.save");

        if (jsonFiles.Length > 0)
        {
            Debug.Log("Saves found");
            uiManager.continueButton.interactable = true;
            uiManager.loadButton.interactable = true;

            for (int i = 0; i < jsonFiles.Length; i++)
            {
                string jsonContents = File.ReadAllText(jsonFiles[i]);

                PlayerSave save = JsonUtility.FromJson<PlayerSave>(jsonContents);
                if (save != null)
                {
                    saves.Add(save);
                    SaveSlotButton ssb = Instantiate(uiManager.saveSlotPrefab, uiManager.saveSlotsContent.transform).GetComponent<SaveSlotButton>();
                    uiManager.saveSlotButtonList.Add(ssb);
                    ssb.id = save.saveId;
                    ssb.GetComponentInChildren<TextMeshProUGUI>().text = save.ToString();

                    EventTrigger eventTrigger = ssb.GetComponent<EventTrigger>();
                    EventTrigger.Entry entry = new EventTrigger.Entry();
                    entry.eventID = EventTriggerType.Cancel;
                    entry.callback.AddListener((eventData) =>
                    {
                        EventSystem.current.SetSelectedGameObject(uiManager.loadBackButton.gameObject);
                    });
                    eventTrigger.triggers.Add(entry);
                }
            }

            if(!saves.Any(s => s.saveId == PlayerPrefs.GetInt("Last Slot")))
            {
                uiManager.continueButton.interactable = false;
                PlayerPrefs.DeleteKey("Last Slot");
            }

        }
        else
        {
            Debug.Log("No saves found");
            uiManager.continueButton.interactable = false;
            uiManager.loadButton.interactable = false;
            PlayerPrefs.DeleteKey("Last Slot");
        }
    }
    public void SaveGame()
    {
        SaveToJson(currentSave.save, currentSave.saveId);
    }
    public void DeleteSave(int id)
    {
        string originalPath = Application.dataPath + $"/Saves/save {id}.save";

        if (File.Exists(originalPath))
        {
            File.Delete(originalPath);
        }

        SaveSlotButton saveSlotToDelete = uiManager.saveSlotButtonList.FirstOrDefault(x => x.id == id);
        int indexToDelete = saveSlotToDelete.id;
        Destroy(saveSlotToDelete.gameObject);
        uiManager.saveSlotButtonList.Remove(saveSlotToDelete);
        PlayerSave saveToDelete = saves.FirstOrDefault(x => x.saveId == id);
        saves.Remove(saveToDelete);

        if(PlayerPrefs.GetInt("Last Slot") == saveToDelete.saveId)
        {
            uiManager.continueButton.interactable = false;
            uiManager.lastSlotText.text = "";
            PlayerPrefs.DeleteKey("Last Slot");
        }

        if(saves.Count == 0)
            uiManager.loadButton.interactable = false;
    }
    #endregion
    //-------------------------------------------------------------------------- inside run
    public void StartRun()
    {
        StartCoroutine(StartRunRoutine());
    }
    IEnumerator StartRunRoutine()
    {
        Debug.Log("Begin Start Run Coroutine");
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        //music
        foreach (var m in audioLibrary.mainMenu)
            audioManager.StopAudio(m, 2, false, 0);

        foreach (var m in audioLibrary.gameplay)
            audioManager.PlayAudio(m, AudioManager.AudioType.MUSIC, 2, true, 3);

        Debug.Log($"Loading level {currentLevel}");
        #region level
        levels[currentLevel].SetActive(true);
        yield return new WaitForEndOfFrame();
        levels[currentLevel].GetComponentInChildren<ReflectionProbe>().RenderProbe();

        enemiesAlive = levels[currentLevel].GetComponentsInChildren<Enemy>().ToList();
        enemiesDead.Clear();
        uiManager.enemyCounterText.text = enemiesAlive.Count.ToString();
        #endregion
        
        yield return new WaitForSeconds(3);
        Debug.Log("Setting up player");
        #region player setup
        playerController.controller.enabled = false;
        
        yield return new WaitForEndOfFrame();
        
        playerController.transform.position = startPoints[currentLevel].position;
        playerController.camAnchor.position = playerController.transform.position;
        playerController.maxZoom = 10;
        playerController.followOffset = new Vector3(0, 3, 0);
        playerController.camLocalOffset = new Vector3(0, 0, 0);
        
        yield return new WaitForEndOfFrame();
        
        playerController.controller.enabled = true;
        playerController.isPlaying = true;
        UpdatePlayerVariables();
        #endregion

        inRun = true;
        runTime = 0;

        yield return new WaitForSeconds(1.5f);

        int hp = 0;
        int maxHp = currentSave.attributes.GetMaxHp();
        //int ammountToAdd = (int)(0.004f * maxHp + 1.2f);
        int ammountToAdd = (int)(0.001f * maxHp);
        while (hp < maxHp)
        {
            if (hp + ammountToAdd > maxHp)
                break;

            hp += ammountToAdd;
            uiManager.UpdateHpCircles(hp);

            if (hp % 100 == 0)
            {
                AudioManager.Instance.oneShotFx.PlayOneShot(AudioManager.Instance.library.healthUp);
            }

            yield return new WaitForEndOfFrame();
        }
        
        hp = maxHp;
        uiManager.UpdateHpCircles(hp);
        AudioManager.Instance.oneShotFx.PlayOneShot(AudioManager.Instance.library.healthUp);
        
        Debug.Log("End Start Run Coroutine");
    }

    public void PauseRun()
    {
        Debug.Log("pause");
        canvasAnimator.SetTrigger("Pause Run");
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        playerController.isPlaying = false;
        Time.timeScale = 0;
    }

    public void ResumeRun()
    {
        Debug.Log("resume");
        canvasAnimator.SetTrigger("Pause Run");
        pause = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        playerController.isPlaying = true;
        Time.timeScale = 1;
    }

    public void KillEnemy(Enemy enemy)
    {
        enemiesAlive.Remove(enemy);
        enemiesDead.Add(enemy);
        uiManager.enemyCounterText.text = enemiesAlive.Count.ToString();
    }
    //-------------------------------------------------------------------------- animation triggers
    public void ChangeSky(string sky)
    {
        debugString += $"\n{sky}";
        switch (sky)
        {
            case "day":
                Debug.Log("Changing skybox to DAY");
                roomObject.SetActive(false);
                sunLight.transform.rotation = Quaternion.Euler(95, 0, 0);
                sunLight.intensity = 1;
                RenderSettings.skybox = daySkyMaterial;
                break;
            case "night":
                Debug.Log("Changing skybox to NIGHT");
                roomObject.SetActive(true);
                sunLight.transform.rotation = Quaternion.Euler(170.477f, 0, 0);
                sunLight.intensity = 3.1f;
                RenderSettings.skybox = nightSkyMaterial;
                break;
        }

        DynamicGI.UpdateEnvironment();
        //ReflectionProbe[] probes = FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        //foreach (var p in probes)
        //    p.RenderProbe();
    }
#if UNITY_EDITOR
    private void OnGUI()
    {
        Rect labelRect = new Rect(50, 50, 600, 1000);
        GUI.color = Color.red;
        GUI.skin.label.fontSize = 18;
        string debugtext = "";
        debugtext += $"\nDebug string:\n{debugString}";
        debugtext += $"\n\nDebug Update string:{debugUpdateString}";

        //GUI.Label(labelRect, debugtext);
    }
#endif
}
