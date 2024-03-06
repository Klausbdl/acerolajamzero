using UnityEngine;

public class PlayerCameraDirector : MonoBehaviour
{
    public PlayerController pc;

    public float xRot, xRotSpeed, yRot, yRotSpeed;
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
        xRot += Time.deltaTime * xRotSpeed;
        xRot = Mathf.Clamp(xRot, -89f, 89f);
        pc.xRot = xRot;
        
        yRot += Time.deltaTime * yRotSpeed;
        if (yRot < 0) yRot = 360;
        if (yRot > 360) yRot = 0;
        pc.yRot = yRot;


        pc.camLocalOffset = Vector3.Lerp(pc.camLocalOffset, camLocalOffset, Time.deltaTime * 3);
        pc.maxZoom = Mathf.Lerp(pc.maxZoom, maxZoom, Time.deltaTime * 3);
    }
}
