using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class ModuleToggle : MonoBehaviour
{
    Toggle mytoggle;
    
    private void Awake()
    {
        mytoggle = GetComponent<Toggle>();
    }

    public void OnClickBehaviour(BaseEventData eventData)
    {
        if (mytoggle.isOn)
            AudioManager.Instance.PlayEquipModule();
        else
            AudioManager.Instance.PlayUnequipModule();
    }
}
