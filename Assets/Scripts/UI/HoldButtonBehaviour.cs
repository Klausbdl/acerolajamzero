using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldButtonBehaviour : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,IPointerExitHandler
{
    public float delay = .1f;
    bool active;
    Button button;

    void Start()
    {
        button = GetComponent<Button>();
    }

    IEnumerator HoldButton()
    {
        if (!button.interactable) yield break;
        
        yield return new WaitForSeconds(.4f);
        
        while (active)
        {
            if (!button.interactable)
            {
                active = false;
                yield break;
            }
            button.onClick.Invoke();
            yield return new WaitForSeconds(delay);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        active = true;
        StartCoroutine(HoldButton());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        active = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        active = false;
    }
}
