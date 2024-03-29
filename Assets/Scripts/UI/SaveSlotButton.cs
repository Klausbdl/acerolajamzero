using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SaveSlotButton : MonoBehaviour
{
    public int id;
    //TODO: fix this
    //public Button deleteButton;

    //private void Start()
    //{
    //    Navigation navigation = new Navigation();
    //    navigation.selectOnRight = deleteButton;
    //    Navigation none = new Navigation();
    //    none.mode = Navigation.Mode.None;
    //    navigation.selectOnLeft.navigation = none;
    //    GetComponent<Button>().navigation = navigation;
    //}

    public void LoadGame()
    {
        GameManager.Instance.OnPlayGameButton(id);
        AudioManager.Instance.PlayPlayPress();
    }

    public void OnSelect(BaseEventData eventData)
    {
        GameManager.Instance.uiManager.CenterToItem(GetComponent<RectTransform>());
    }

    public void DeleteSave()
    {
        GameManager.Instance.DeleteSave(id);
        AudioManager.Instance.PlayButtonBackPress();
    }
}
