using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("First screen")]
    public Button startButton;

    [Header("Main menu")]
    public Button newGameButton;
    public Button continueButton;
    public TextMeshProUGUI lastSlotText;
    public Button loadButton;
    public Button loadBackButton;
    [Space(8)]
    public GameObject saveSlotsContent;
    public GameObject saveSlotPrefab;
    public List<SaveSlotButton> saveSlotButtonList = new List<SaveSlotButton>();
}
