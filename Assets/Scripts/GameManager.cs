using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using TMPro;


public class GameManager : Singleton<GameManager>
{
    [Header("Prefabs")]
    public GameObject[] playerPrefabs;
    public Transform playerPosSpawn;

    [Header("Materials")]
    public Material daySkyMaterial;
    public Material nightSkyMaterial;

    [Header("Components")]
    public Animator gameCanvasAnimator;
    public WeaponsData weaponData;
    public LegsData legsData;
    public PlayerController playerController;
    public GameObject roomObject;
    public UIManager uiManager;
    public Light sunLight;
    Animator canvasAnimator;
    
    [Header("Player Attributes")]
    public PlayerAttributes playerAttributes;
    public List<PlayerSave> saves = new List<PlayerSave>();
    public PlayerSave currentSave;

    [Header("Game")]
    public bool pause = false;
    public GameObject[] levels;
    public Transform[] startPoints;

    public override void Awake()
    {
        base.Awake();

        canvasAnimator = GetComponent<Animator>();

        sunLight.transform.rotation = Quaternion.Euler(170.477f, 0, 0);
        sunLight.intensity = 3.1f;
        RenderSettings.skybox = nightSkyMaterial;
        DynamicGI.UpdateEnvironment();

        EventSystem.current.SetSelectedGameObject(uiManager.startButton.gameObject);

        uiManager.LoadGraphicsAndAudio();
    }

    public void Update()
    {
        if (Input.GetButtonDown("Pause"))
        {
            pause = !pause;
            if (pause)
                PauseRun();
            else
                ResumeRun();
        }
    }

    public void StartGame()
    {
        StartCoroutine(StartGameRoutine());
    }

    public void CloseGame()
    {
        StartCoroutine(CloseGameRoutine());
    }

    IEnumerator StartGameRoutine()
    {
        Debug.Log("Begin Start Game Coroutine");
        yield return new WaitForSeconds(6f);

        #region environment stuff
        Debug.Log("Changing skybox to day");
        roomObject.SetActive(false);
        sunLight.transform.rotation = Quaternion.Euler(95, 0, 0);
        sunLight.intensity = 1;
        RenderSettings.skybox = daySkyMaterial;
        DynamicGI.UpdateEnvironment();
        #endregion

        yield return new WaitForEndOfFrame();

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

    IEnumerator CloseGameRoutine()
    {
        Debug.Log("Begin Close Game Coroutine");

        #region environment stuff
        Debug.Log("Changing skybox to night");
        roomObject.SetActive(false);
        sunLight.transform.rotation = Quaternion.Euler(170.477f, 0, 0);
        sunLight.intensity = 3.1f;
        RenderSettings.skybox = nightSkyMaterial;
        DynamicGI.UpdateEnvironment();
        #endregion

        Debug.Log("End Close Game Coroutine");
        yield return null;
    }
    //--------------------------------------------------------------------------
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
            saves.Add(currentSave);
            SaveToJson(currentSave, newSlot);
        }
        else
        {
            Debug.Log($"Loading from slot: {slot}");
            
            PlayerPrefs.SetInt("Last Slot", slot);
            currentSave = saves[slot];
        }

        gameCanvasAnimator.SetTrigger("Start Game");

        #region spawn player
        if(playerController == null)
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
        uiManager.currencyText.text = currentSave.attributes.currency.ToString();
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
    //--------------------------------------------------------------------------
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
        uiManager.UpdateHpCircles(currentSave.attributes.GetMaxHp());
        
        yield return new WaitForEndOfFrame();
        
        playerController.transform.position = startPoints[0].position;
        playerController.camAnchor.position = playerController.transform.position;
        playerController.maxZoom = 10;
        playerController.followOffset = new Vector3(0, 3, 0);
        playerController.camLocalOffset = new Vector3(0, 0, 0);
        
        yield return new WaitForEndOfFrame();
        
        playerController.controller.enabled = true;
        playerController.isPlaying = true;

        Debug.Log("End Start Run Coroutine");
    }

    public void PauseRun()
    {
        Debug.Log("pause");
        gameCanvasAnimator.SetTrigger("Pause Run");
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        playerController.isPlaying = false;
        Time.timeScale = 0;
    }

    public void ResumeRun()
    {
        Debug.Log("resume");
        gameCanvasAnimator.SetTrigger("Pause Run");
        pause = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        playerController.isPlaying = true;
        Time.timeScale = 1;
    }
}
