using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerCameraDirector : MonoBehaviour
{
    public PlayerController pc;

    public float xRot, xRotSpeed, xLerpSpeed;
    public float yRot, yRotSpeed, yLerpSpeed;
    public Vector3 camLocalOffset;
    public float maxZoom = 10;
    float targetX, targetY, targetZoom;
    float targetOffsetX, targetOffsetY, targetOffsetZ;
    bool targetOffset;
    public bool TargetOffset
    {
        get { return targetOffset; }
        set { targetOffset = value; }
    }
    public float TargetX
    {
        get { return targetX; }
        set { targetX = value; }
    }
    public float TargetY
    {
        get { return targetY; }
        set { targetY = value; }
    }
    public float XRotSpeed
    {
        get { return xRotSpeed; }
        set { xRotSpeed = value; }
    }
    public float YRotSpeed
    {
        get { return yRotSpeed; }
        set { yRotSpeed = value; }
    }
    public float XLerpSpeed
    {
        get { return xLerpSpeed; }
        set { xLerpSpeed = value; }
    }
    public float YLerpSpeed
    {
        get { return yLerpSpeed; }
        set { yLerpSpeed = value; }
    }
    public float TargetOffsetX
    {
        get { return targetOffsetX; }
        set { targetOffsetX = value; }
    }
    public float TargetOffsetY
    {
        get { return targetOffsetY; }
        set { targetOffsetY = value; }
    }
    public float TargetOffsetZ
    {
        get { return targetOffsetZ; }
        set { targetOffsetZ = value; }
    }
    public float TargetMaxZoom
    {
        get { return targetZoom; }
        set { targetZoom = value; }
    }

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
        if (xLerpSpeed > 0) xRot = Mathf.Lerp(pc.xRot, targetX, Time.unscaledDeltaTime * xLerpSpeed);
        pc.xRot = xRot;
        
        yRot += Time.unscaledDeltaTime * yRotSpeed;
        if (yRot < 0) yRot = 360;
        if (yRot > 360) yRot = 0;

        if (yLerpSpeed > 0) yRot = Mathf.Lerp(pc.yRot, targetY, Time.unscaledDeltaTime * yLerpSpeed);
        pc.yRot = yRot;

        if (targetOffset) camLocalOffset = new Vector3(targetOffsetX, targetOffsetY, targetOffsetZ);
        pc.camLocalOffset = Vector3.Lerp(pc.camLocalOffset, camLocalOffset, Time.unscaledDeltaTime * 3);
        
        if (targetZoom != 0) maxZoom = targetZoom;
        pc.maxZoom = Mathf.Lerp(pc.maxZoom, maxZoom, Time.unscaledDeltaTime * 3);
    }
}
