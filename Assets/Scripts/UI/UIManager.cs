using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

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
    [Header("HUD")]
    public UICircleRenderer hpCirclesRenderer;
    public TextMeshProUGUI currencyText;

    private void Start()
    {
        string monthName = DateTime.Now.ToString("MMM", CultureInfo.InvariantCulture);
        dateTimeText.text = $"{DateTime.Now.Hour}:{DateTime.Now.Minute}\n{DateTime.Now.Day.ToString("00")}/{monthName}/{DateTime.Now.Year}";
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
}
