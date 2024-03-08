using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;


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
    
    [Header("Player Attributes")]
    public PlayerAttributes playerAttributes;
    public List<PlayerSave> saves = new List<PlayerSave>();
    public PlayerSave currentSave;

    [Header("Game")]
    public bool pause = false;
    public GameObject[] levels;
    public Transform[] startPoints;
    public bool inRun;
    private float runTime = 0f;
    public Inventory shopInventory;

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

        EventSystem.current.SetSelectedGameObject(uiManager.startButton.gameObject);

        uiManager.LoadGraphicsAndAudio();

        foreach(var level in levels)
        {
            level.SetActive(false);
        }
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

        Debug.Log("End Start Game Coroutine");
    }

    public void CloseGame()
    {
        StartCoroutine(CloseGameRoutine());
    }
    IEnumerator CloseGameRoutine()
    {
        Debug.Log("Begin Close Game Coroutine");

        Debug.Log("End Close Game Coroutine");
        yield return null;
    }
    //-------------------------------------------------------------------------- inside game
    public void OnPlayGameButton(int slot)
    {
        if(slot == -1)
        {
            int newSlot = saves.Count;
            Debug.Log($"Starting New game {newSlot}");
            
            PlayerPrefs.SetInt("Last Slot", newSlot);
            //create new save
            PlayerSave newsave = new PlayerSave();
            newsave.saveId = newSlot;
            currentSave = newsave;

            //add first items to first save
            //arms
            currentSave.playerInventory.leftArmModules.Add(shopInventory.leftArmModules[4]);
            currentSave.playerInventory.rightArmModules.Add(shopInventory.rightArmModules[4]);
            currentSave.equippedLeftArmModules.Add(shopInventory.leftArmModules[4]);
            currentSave.equippedRightArmModules.Add(shopInventory.rightArmModules[4]);
            //legs
            currentSave.playerInventory.legModules.Add(shopInventory.legModules[0]);
            currentSave.equippedLegModule = currentSave.playerInventory.legModules[0];

            saves.Add(currentSave);
            SaveToJson(currentSave, newSlot);
        }
        else
        {
            Debug.Log($"Loading from slot: {slot}");
            
            PlayerPrefs.SetInt("Last Slot", slot);
            currentSave = saves[slot];
        }

        //DEBUG
        currentSave.playerInventory.leftArmModules.Add(shopInventory.leftArmModules[3]);
        currentSave.playerInventory.rightArmModules.Add(shopInventory.rightArmModules[5]);
        currentSave.playerInventory.leftArmModules.Add(shopInventory.leftArmModules[1]);
        currentSave.playerInventory.rightArmModules.Add(shopInventory.rightArmModules[11]);
        currentSave.playerInventory.rightArmModules.Add(shopInventory.rightArmModules[1]);
        currentSave.playerInventory.rightArmModules.Add(shopInventory.rightArmModules[2]);
        currentSave.playerInventory.legModules.Add(shopInventory.legModules[1]);
        currentSave.playerInventory.legModules.Add(shopInventory.legModules[2]);
        currentSave.playerInventory.legModules.Add(shopInventory.legModules[3]);

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
        #endregion

        uiManager.LoadPlayerSettings();

        Debug.Log("Loading Player Attributes");
        uiManager.UpdateStats(currentSave.attributes);
        uiManager.UpdateInventory(currentSave);

        canvasAnimator.SetTrigger("Start Game");
    }

    public void OnLoadButton()
    {
        //when clicking the load button, make the first slot selected
        EventSystem.current.SetSelectedGameObject(uiManager.saveSlotsContent.GetComponentsInChildren<Button>()[0].gameObject);
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
                    ssb.id = i;
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

            if(PlayerPrefs.GetInt("Last Slot") > saves.Count - 1)
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
    #endregion

    public void QuitApplication()
    {
        Application.Quit();
    }
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

        Debug.Log("Loading level 0");
        levels[0].SetActive(true);
        yield return new WaitForEndOfFrame();
        levels[0].GetComponentInChildren<ReflectionProbe>().RenderProbe();

        yield return new WaitForSeconds(3);
        Debug.Log("Setting up player");
        
        playerController.controller.enabled = false;
        
        yield return new WaitForEndOfFrame();
        
        playerController.transform.position = startPoints[0].position;
        playerController.camAnchor.position = playerController.transform.position;
        playerController.maxZoom = 10;
        playerController.followOffset = new Vector3(0, 3, 0);
        playerController.camLocalOffset = new Vector3(0, 0, 0);
        
        yield return new WaitForEndOfFrame();
        
        playerController.controller.enabled = true;
        playerController.isPlaying = true;

        inRun = true;
        runTime = 0;

        yield return new WaitForSeconds(1);

        int hp = 0;
        int maxHp = currentSave.attributes.GetMaxHp();
        int ammountToAdd = (int)(0.004f * maxHp + 1.2f);
        while (hp <= maxHp)
        {
            hp += ammountToAdd;
            hp = Mathf.Clamp(hp, 0, maxHp);
            uiManager.UpdateHpCircles(hp);
            yield return new WaitForEndOfFrame();
        }

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
