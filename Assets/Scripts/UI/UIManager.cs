using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.LookDev;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("First screen")]
    public Button startButton;
    public TextMeshProUGUI dateTimeText;

    [Header("Main menu")]
    public Button newGameButton;
    public Button continueButton;
    public TextMeshProUGUI lastSlotText;
    public Button loadButton;
    public Button loadBackButton;
    [Space(8)]
    public GameObject saveSlotsContent;
    public ScrollRect savesScrollRect;
    public GameObject saveSlotPrefab;
    public List<SaveSlotButton> saveSlotButtonList = new List<SaveSlotButton>();
    [Header("Buttons")]
    public Button[] buttons;
    [Header("Settings Menu")]
    #region settings components
    public Slider sensiXSlider;
    public Slider sensiYSlider;
    public Toggle invertYToggle;
    [Space(8)]
    public UIOptionSelector windowModeSelector;
    FullScreenMode windowMode;
    public UIOptionSelector resolutionSelector;
    List<Resolution> resolutions;
    public Toggle vsyncToggle;
    [Space(8)]
    public AudioMixer masterMixer;
    public Slider masterAudioSlider;
    public Slider musicAudioSlider;
    public Slider fxAudioSlider;
    #endregion
    [Header("Inventory Menu")]
    //stats
    public UIStatsRenderer statsRenderer;
    public TextMeshProUGUI vitText;
    public TextMeshProUGUI defText;
    public TextMeshProUGUI agiText;
    public TextMeshProUGUI strText;
    public TextMeshProUGUI dexText;
    public TextMeshProUGUI jmpText;
    //inventory
    public GameObject moduleToggle;
    public GameObject leftArmContent;
    public TextMeshProUGUI leftArmSlotCounterText;
    public GameObject rightArmContent;
    public TextMeshProUGUI rightArmSlotCounterText;
    public GameObject legsContent;
    public List<Toggle> leftArmToggles = new List<Toggle>();
    public List<Toggle> rightArmToggles = new List<Toggle>();
    public List<Toggle> legsToggles = new List<Toggle>();
    [Header("HUD")]
    public UICircleRenderer hpCirclesRenderer;
    public TextMeshProUGUI currencyText;
    public TextMeshProUGUI timerText;

    private void Start()
    {
        string monthName = DateTime.Now.ToString("MMM", CultureInfo.InvariantCulture);
        dateTimeText.text = $"{DateTime.Now.Hour.ToString("00")}:{DateTime.Now.Minute.ToString("00")}\n{DateTime.Now.Day.ToString("00")}/{monthName}/{DateTime.Now.Year}";
    }

    public void Selected(int id)
    {
        EventSystem.current.SetSelectedGameObject(buttons[id].gameObject);
    }

    public void LoadGraphicsAndAudio()
    {
        Debug.Log("Loading Graphics and Audio settings");

        #region graphics
        //window mode
        if (PlayerPrefs.HasKey("Config.WindowMode"))
            windowModeSelector.UpdateIndex(PlayerPrefs.GetInt("Config.WindowMode"));
        else
        {
            PlayerPrefs.SetInt("Config.WindowMode", 2);
            windowModeSelector.UpdateIndex(2);
        }
        switch (PlayerPrefs.GetInt("Config.WindowMode"))
        {
            default:
            case 0: windowMode = FullScreenMode.Windowed; break;
            case 1: windowMode = FullScreenMode.ExclusiveFullScreen; break;
            case 2: windowMode = FullScreenMode.FullScreenWindow; break;
        }

        //resolution
        //initial load
        resolutions = Screen.resolutions.ToList();
        resolutions = resolutions.GroupBy(resolution => new { resolution.width, resolution.height }).Select(group => group.First()).ToList();
        float userRatio = (float)resolutions[^1].width / resolutions[^1].height;
        resolutions = resolutions.Where(resolution => Mathf.Approximately((float)resolution.width/resolution.height, userRatio)).Distinct().ToList();
        if (resolutionSelector.Options.Count == 0)
        {
            List<string> list = new List<string>();
            foreach (var res in resolutions)
            {
                string resOption = res.width.ToString() + "x" + res.height.ToString();
                list.Add(resOption);
            }
            resolutionSelector.ClearAll();
            resolutionSelector.AddOptions(list);
        }

        if (PlayerPrefs.HasKey("Config.Resolution"))
            resolutionSelector.UpdateIndex(PlayerPrefs.GetInt("Config.Resolution"));
        else
        {
            PlayerPrefs.SetInt("Config.Resolution", resolutions.Count - 1);
            resolutionSelector.UpdateIndex(resolutions.Count - 1);
        }

        Resolution currentRes;
        if (resolutionSelector.currentIndex <= resolutions.Count - 1)
            currentRes = resolutions[resolutionSelector.currentIndex];
        else
            currentRes = resolutions[resolutions.Count - 1];

        //apply resolution
        Screen.SetResolution(currentRes.width, currentRes.height, windowMode);
        FixCamera();

        //v sync
        if (PlayerPrefs.HasKey("Config.VSync"))
            vsyncToggle.isOn = PlayerPrefs.GetInt("Config.VSync") == 1;
        else
        {
            PlayerPrefs.SetInt("Config.InvertY", 0);
            vsyncToggle.isOn = false;
        }
        QualitySettings.vSyncCount = PlayerPrefs.GetInt("Config.VSync");
        #endregion

        #region audio
        //audio master
        if (PlayerPrefs.HasKey("Config.Master Volume"))
            masterAudioSlider.value = PlayerPrefs.GetFloat("Config.Master Volume");
        else
        {
            PlayerPrefs.SetFloat("Config.Master Volume", 1);
            masterAudioSlider.value = 1;
        }
        masterMixer.SetFloat("Master Volume", UtilsFunctions.LinearToDecibel(masterAudioSlider.value));

        //audio music
        if (PlayerPrefs.HasKey("Config.Music Volume"))
            musicAudioSlider.value = PlayerPrefs.GetFloat("Config.Music Volume");
        else
        {
            PlayerPrefs.SetFloat("Config.Music Volume", .8f);
            musicAudioSlider.value = .8f;
        }
        masterMixer.SetFloat("Music Volume", UtilsFunctions.LinearToDecibel(masterAudioSlider.value));

        //audio fxs
        if (PlayerPrefs.HasKey("Config.FX Volume"))
            fxAudioSlider.value = PlayerPrefs.GetFloat("Config.FX Volume");
        else
        {
            PlayerPrefs.SetFloat("Config.FX Volume", .8f);
            fxAudioSlider.value = .8f;
        }
        masterMixer.SetFloat("FX Volume", UtilsFunctions.LinearToDecibel(masterAudioSlider.value));
        #endregion

        PlayerPrefs.Save();
        GameManager.Instance.debugString += $"\nLoaded graphics and Audio";
    }

    public void LoadPlayerSettings()
    {
        Debug.Log("Loading Player settings");

        #region gameplay
        //sensitivity x
        if (PlayerPrefs.HasKey("Config.SensiX"))
            sensiXSlider.value = PlayerPrefs.GetFloat("Config.SensiX");
        else
        {
            PlayerPrefs.SetFloat("Config.SensiX", .5f);
            sensiXSlider.value = .5f;
        }
        GameManager.Instance.playerController.sensitivityX = sensiXSlider.value * 10;

        //sensitivity Y
        if (PlayerPrefs.HasKey("Config.SensiY"))
            sensiYSlider.value = PlayerPrefs.GetFloat("Config.SensiY");
        else
        {
            PlayerPrefs.SetFloat("Config.SensiY", .5f);
            sensiYSlider.value = .5f;
        }
        GameManager.Instance.playerController.sensitivityY = sensiYSlider.value * 10;

        //invert Y
        if (PlayerPrefs.HasKey("Config.InvertY"))
            invertYToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("Config.InvertY") == 1);
        else
        {
            PlayerPrefs.SetInt("Config.InvertY", 0);
            invertYToggle.SetIsOnWithoutNotify(false);
        }
        GameManager.Instance.playerController.sensitivityY = (sensiYSlider.value * 10) * (invertYToggle.isOn ? -1 : 1);
        #endregion

        PlayerPrefs.Save();
    }

    #region gameplay
    public void SetSensitivityX(float value)
    {
        PlayerPrefs.SetFloat("Config.SensiX", value);
        GameManager.Instance.playerController.sensitivityX = sensiXSlider.value * 10;
    }
    public void SetSensitivityY(float value)
    {
        PlayerPrefs.SetFloat("Config.SensiY", value);
        GameManager.Instance.playerController.sensitivityY = sensiYSlider.value * 10;
    }
    public void SetInvertY(bool toggle)
    {
        PlayerPrefs.SetInt("Config.InvertY", toggle ? 1 : 0);
        GameManager.Instance.playerController.sensitivityY = (sensiYSlider.value * 10) * (toggle ? -1 : 1);
    }
    #endregion

    #region graphics
    public void SetWindowMode(int index)
    {
        PlayerPrefs.SetInt("Config.WindowMode", index);
        switch (index)
        {
            default:
            case 0: windowMode = FullScreenMode.Windowed; break;
            case 1: windowMode = FullScreenMode.ExclusiveFullScreen; break;
            case 2: windowMode = FullScreenMode.FullScreenWindow; break;
        }

        #region apply graphics
        Screen.SetResolution(resolutions[resolutionSelector.currentIndex].width,
            resolutions[resolutionSelector.currentIndex].height,
            windowMode);
        #endregion
        FixCamera();
    }
    public void SetResolution(int index)
    {
        PlayerPrefs.SetInt("Config.Resolution", index);
        Screen.SetResolution(resolutions[index].width, resolutions[index].height, windowMode);

        FixCamera();
    }
    public void SetVSync(bool toggle)
    {
        PlayerPrefs.SetInt("Config.VSync", toggle ? 1 : 0);
        QualitySettings.vSyncCount = toggle ? 1 : 0;
    }
    void FixCamera()
    {
        Vector2 resTarget = new Vector2(1920, 1080);
        Vector2 resViewport = new Vector2(Screen.width, Screen.height);
        Vector2 resNormalized = resTarget / resViewport;
        Vector2 size = resNormalized / Mathf.Max(resNormalized.x, resNormalized.y);
        Camera.main.rect = new Rect(default, size) { center = new Vector2(.5f, .5f) };
    }
    #endregion

    #region audio settings
    public void SetMasterVolume(float volume)
    {
        PlayerPrefs.SetFloat("Config.Master Volume", volume);
        masterMixer.SetFloat("Master Volume", UtilsFunctions.LinearToDecibel(volume));
    }
    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("Config.Music Volume", volume);
        masterMixer.SetFloat("Music Volume", UtilsFunctions.LinearToDecibel(volume));
    }
    public void SetFXVolume(float volume)
    {
        PlayerPrefs.SetFloat("Config.FX Volume", volume);
        masterMixer.SetFloat("FX Volume", UtilsFunctions.LinearToDecibel(volume));
    }
    #endregion

    public void CenterToItem(RectTransform target)
    {
        ////Debug.Log($"{target.name} {savesScrollRect.content.rect.height} {target.anchoredPosition.y}");
        //float normalizedPosition = (Mathf.Abs(target.anchoredPosition.y) - target.rect.height / 2f) / savesScrollRect.content.rect.height;
        //normalizedPosition = Mathf.Clamp01(1 - normalizedPosition);
        //savesScrollRect.verticalNormalizedPosition = normalizedPosition;
        //Debug.Log($"{(Mathf.Abs(target.anchoredPosition.y) - target.rect.height / 2f)}\n{normalizedPosition}");

        float scrollViewHeight = savesScrollRect.viewport.sizeDelta.y;
        float scrollViewMid = -scrollViewHeight / 2f;

        if (target.anchoredPosition.y > scrollViewMid) return;
        
        float contentEnd = -savesScrollRect.content.sizeDelta.y;

        if(target.anchoredPosition.y < contentEnd - scrollViewMid)
        {
            savesScrollRect.verticalNormalizedPosition = 0;
            return;
        }

        float normPosTopBottom = Mathf.InverseLerp(scrollViewMid, contentEnd - scrollViewHeight, target.anchoredPosition.y);
        float normPosBottomTop = 1 - normPosTopBottom;

        savesScrollRect.verticalNormalizedPosition = normPosBottomTop;
    }

    public void UpdateStats(PlayerAttributes attributes)
    {
        vitText.text = "Vit " + attributes.vitality;
        defText.text = "Def " + attributes.defense;
        agiText.text = "Agi " + attributes.agility;
        strText.text = "Str " + attributes.strength;
        dexText.text = "Dex " + attributes.dexterity;
        jmpText.text = "Jmp " + attributes.jump;
        
        statsRenderer.statsValues[5] = attributes.vitality / 100f;
        statsRenderer.statsValues[0] = attributes.defense / 100f;
        statsRenderer.statsValues[1] = attributes.agility / 100f;
        statsRenderer.statsValues[2] = attributes.strength / 100f;
        statsRenderer.statsValues[3] = attributes.dexterity / 100f;
        statsRenderer.statsValues[4] = attributes.jump / 100f;
        statsRenderer.SetAllDirty();

        currencyText.text = attributes.currency.ToString();
    }
    public void UpdateInventory(PlayerSave save)
    {
        Inventory inv = save.playerInventory;

        legsToggles.ForEach(t => { Destroy(t.gameObject); });
        legsToggles.Clear();

        leftArmToggles.ForEach(t => { Destroy(t.gameObject); });
        leftArmToggles.Clear();
        
        rightArmToggles.ForEach(t => { Destroy(t.gameObject); });
        rightArmToggles.Clear();

        foreach (var leg in inv.legModules)
        {
            Toggle legToggle = Instantiate(moduleToggle, legsContent.transform).GetComponent<Toggle>();
            legToggle.group = legsContent.GetComponent<ToggleGroup>();
            legToggle.GetComponentInChildren<TextMeshProUGUI>().text = leg.name;
            legToggle.onValueChanged.AddListener(on =>
            {
                if (on) EquipLegModule(leg);
            });
            if (leg.name == save.equippedLegModule.name)
                legToggle.isOn = true;
        }

        foreach (var arm in inv.leftArmModules)
        {
            Toggle armToggle = Instantiate(moduleToggle, leftArmContent.transform).GetComponent<Toggle>();
            armToggle.GetComponentInChildren<TextMeshProUGUI>().text = arm.name;
            armToggle.onValueChanged.AddListener(on =>
            {
                EquipArmModule(arm, 0, on, armToggle);
            });
            if (save.equippedLeftArmModules.Any(mod => mod.name == arm.name))
                armToggle.isOn = true;
            
            leftArmToggles.Add(armToggle);
        }

        foreach (var arm in inv.rightArmModules)
        {
            Toggle armToggle = Instantiate(moduleToggle, rightArmContent.transform).GetComponent<Toggle>();
            armToggle.GetComponentInChildren<TextMeshProUGUI>().text = arm.name;
            armToggle.onValueChanged.AddListener(on =>
            {
                EquipArmModule(arm, 1, on, armToggle);
            });
            if (save.equippedRightArmModules.Any(mod => mod.name == arm.name))
                armToggle.isOn = true;

            rightArmToggles.Add(armToggle);
        }

        leftArmSlotCounterText.text = $"Available: {GameManager.Instance.currentSave.GetAvailableSlots(0)}";
        rightArmSlotCounterText.text = $"Available: {GameManager.Instance.currentSave.GetAvailableSlots(1)}";
        
        //TODO: desabilitar toggles se tiver com todos os slots cheios
    }
    public void UpdateHpCircles(int currHp)
    {
        //each circle = 100 hp
        //each side+3 = 10hp
        //max: 2200 --> 22 circles
        hpCirclesRenderer.circles.Clear();
        int howManyCircles = Mathf.CeilToInt(currHp / 100);
        int remainder = currHp - (howManyCircles * 100);
        for (int i = 0; i < howManyCircles+1; i++)
        {
            MyCircle c = new MyCircle();
            c.timeOffset = -i * .1f;
            c.sides = 13;
            c.noise = 4.5f;
            c.noiseSpeed = 1;
            c.center.x = i * 40;
            c.originalRadius = 15;
            c.radiusSpeed = 7;
            c.radiusMagnitude = .5f;
            c.radiusAdd = 1;

            if (i == howManyCircles)
            {
                c.sides = remainder / 10;
                c.originalRadius = 20;
                c.noise = -.125f * remainder + 17;
                c.noiseSpeed = 1 + (1 - remainder/100f);
            }

            hpCirclesRenderer.circles.Add(c);
        }
        
        hpCirclesRenderer.SetAllDirty();
    }

    public void EquipLegModule(LegModule module)
    {
        //update save
        GameManager.Instance.currentSave.equippedLegModule = module;

        List<SkinnedMeshRenderer> renderers = new List<SkinnedMeshRenderer>
        {
            GameManager.Instance.playerController.bodyParts[2],
            GameManager.Instance.playerController.bodyParts[3]
        };
        int blendShapeIndex = (int)module.moduleType - 1;
        StartCoroutine(DeformLeg(renderers, blendShapeIndex));      
    }
    IEnumerator DeformLeg(List<SkinnedMeshRenderer> renderers, int blendShapeIndex)
    {
        float duration = .15f;
        float timer = 0;

        while(timer <= duration)
        {
            timer += Time.unscaledDeltaTime;

            foreach (var r in renderers)
            {
                if(blendShapeIndex == -1)
                    for(int i = 0 ; i < 5; i++)
                    {
                        if (r.GetBlendShapeWeight(i) > 0)
                            r.SetBlendShapeWeight(i, Mathf.Lerp(100, 0, timer / duration));
                    }                        
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if(i == blendShapeIndex)
                            r.SetBlendShapeWeight(i, Mathf.Lerp(0, 100, timer / duration));
                        else
                        {
                            if (r.GetBlendShapeWeight(i) > 0)
                                r.SetBlendShapeWeight(i, Mathf.Lerp(100, 0, timer / duration));
                        }
                    }
                }
            }
            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }

    public void EquipArmModule(ArmModule module, int side, bool on, Toggle toggle)
    {
        if (on)
        {
            if(side == 0)
            {
                if (GameManager.Instance.currentSave.equippedLeftArmModules.Any(m => m.name != module.name))
                    GameManager.Instance.currentSave.equippedLeftArmModules.Add(module);

                leftArmSlotCounterText.text = $"Available: {GameManager.Instance.currentSave.GetAvailableSlots(side)}";
            }
            else
            {
                if (GameManager.Instance.currentSave.equippedRightArmModules.Any(m => m.name != module.name))
                    GameManager.Instance.currentSave.equippedRightArmModules.Add(module);

                rightArmSlotCounterText.text = $"Available: {GameManager.Instance.currentSave.GetAvailableSlots(side)}";
            }
            //if there are no more slots
            if (GameManager.Instance.currentSave.GetAvailableSlots(side) <= 0)
            {
                if (side == 0)
                {
                    if (GameManager.Instance.currentSave.playerInventory.slots[side] > 1)
                        leftArmToggles.ForEach(t => {
                            if (!t.isOn) t.interactable = false;
                        });
                    else
                    {
                        leftArmToggles.ForEach(t => {
                            if (t != toggle)
                                t.isOn = false;
                        });
                    }                        
                }
                else
                {
                    if (GameManager.Instance.currentSave.playerInventory.slots[side] > 1)
                        rightArmToggles.ForEach(t => {
                            if (!t.isOn) t.interactable = false;
                        });
                    else
                        rightArmToggles.ForEach(t => {
                            if (t != toggle)
                                t.isOn = false;
                        });
                }
            }
        }
        else
        {
            if(side == 0) //left side
            {
                //checar se é o unico
                if (GameManager.Instance.currentSave.equippedLeftArmModules.Count == 1)
                {
                    toggle.SetIsOnWithoutNotify(true);
                }
                else
                {
                    ArmModule mToRemove = GameManager.Instance.currentSave.equippedLeftArmModules.FirstOrDefault(m => m.name == module.name);
                    GameManager.Instance.currentSave.equippedLeftArmModules.Remove(mToRemove);
                    
                    leftArmToggles.ForEach(t => {
                        if (!t.isOn) t.interactable = true;
                    });
                }
                leftArmSlotCounterText.text = $"Available: {GameManager.Instance.currentSave.GetAvailableSlots(side)}";
            }
            else //right side
            {
                //checar se é o unico
                if (GameManager.Instance.currentSave.equippedRightArmModules.Count == 1)
                {
                    toggle.SetIsOnWithoutNotify(true);
                }
                else
                {
                    ArmModule mToRemove = GameManager.Instance.currentSave.equippedRightArmModules.FirstOrDefault(m => m.name == module.name);
                    GameManager.Instance.currentSave.equippedRightArmModules.Remove(mToRemove);

                    rightArmToggles.ForEach(t => {
                        if (!t.isOn) t.interactable = true;
                    });
                }
                rightArmSlotCounterText.text = $"Available: {GameManager.Instance.currentSave.GetAvailableSlots(side)}";
            }
        }
        
    }
}
