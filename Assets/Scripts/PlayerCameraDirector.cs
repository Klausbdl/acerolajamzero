using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCameraDirector : MonoBehaviour
{
    public PlayerController pc;

    public float xRot, xRotSpeed, xLerpSpeed;
    public float yRot, yRotSpeed, yLerpSpeed;
    public Vector3 camLocalOffset;
    public float maxZoom = 10;

    void Update()
    {
        if(pc == null)
        {
            if(GameManager.Instance.playerController != null)
                pc = GameManager.Instance.playerController;
            return;
        }
        if (pc.isPlaying) return;

        xRot += Time.unscaledDeltaTime * xRotSpeed;
        xRot = Mathf.Clamp(xRot, -89f, 89f);
        if (xLerpSpeed > 0) xRot = Mathf.Lerp(pc.xRot, xRot, Time.unscaledDeltaTime * xLerpSpeed);
        pc.xRot = xRot;
        
        yRot += Time.unscaledDeltaTime * yRotSpeed;
        if (yRot < 0) yRot = 360;
        if (yRot > 360) yRot = 0;

        if (yLerpSpeed > 0) yRot = Mathf.Lerp(pc.yRot, yRot, Time.unscaledDeltaTime * yLerpSpeed);
        pc.yRot = yRot;


        pc.camLocalOffset = Vector3.Lerp(pc.camLocalOffset, camLocalOffset, Time.unscaledDeltaTime * 3);
        pc.maxZoom = Mathf.Lerp(pc.maxZoom, maxZoom, Time.unscaledDeltaTime * 3);
    }

    public void Selected(int id)
    {
        EventSystem.current.SetSelectedGameObject(GameManager.Instance.uiManager.buttons[id].gameObject);
    }
}
