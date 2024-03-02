using System.Net;
using UnityEngine;

public class LegsData : MonoBehaviour
{
    [Header("Data")]
    public Transform normal;
    public Transform wheel;
    public Transform ball;
    public Transform spike;
    public Transform foot;
    public Transform hover;
    public Transform piston;

    [Header("Debug")]
    public float valuesRange = 20;
    public float fontSize = 15;
    public float dataFontSize = 15;
    public Color[] colors = new Color[12];

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Vector3 pos = transform.position + new Vector3(valuesRange / 2f, 0, valuesRange / 2f);
        Gizmos.DrawWireCube(pos, new Vector3(valuesRange, 0, valuesRange));

        UtilsFunctions.DrawGizmoString("0,0", transform.position, Color.white, Vector2.one, fontSize);
        UtilsFunctions.DrawGizmoString("1,1", transform.position + new Vector3(valuesRange, 0, valuesRange), Color.white, Vector2.one, fontSize);

        UtilsFunctions.DrawGizmoString("speed", transform.position + Vector3.forward * valuesRange, Color.white, Vector2.one, fontSize);
        UtilsFunctions.DrawGizmoString("jump", transform.position + Vector3.right * valuesRange, Color.white, Vector2.one, fontSize);
        
        Transform[] transforms = transform.GetComponentsInChildren<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            Gizmos.color = colors[i];
            if (t == transform) continue;
            Vector3 tPos = t.position;

            Gizmos.DrawLine(new Vector3(tPos.x, transform.position.y, transform.position.z), new Vector3(tPos.x, transform.position.y, tPos.z));
            Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y, tPos.z), new Vector3(tPos.x, transform.position.y, tPos.z));

            string spd = (t.localPosition.z / valuesRange).ToString("0.##");
            string jump = (t.localPosition.x / valuesRange).ToString("0.##");
            string text = $"{t.name}\njump: {jump}\nspd: {spd}";
            UtilsFunctions.DrawGizmoString(text, tPos, Color.white, Vector2.one, dataFontSize);
        }
    }
}
