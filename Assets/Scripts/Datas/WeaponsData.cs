using UnityEngine;

public class WeaponsData : MonoBehaviour
{
    [Header("Data")]
    //swords
    public Transform sword;
    public Transform greatsword;
    public Transform hammer;
    public Transform katana;
    //punch
    public Transform normal;
    public Transform stone;
    public Transform spike;
    public Transform hand;
    //gun
    public Transform cannon;
    public Transform rifle;
    public Transform laser;
    public Transform mouth;

    [Header("Debug")]
    public float valuesRange = 20;
    public float fontSize = 15;
    public float dataFontSize = 15;
    public Color[] colors = new Color[12];

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Vector3 pos = transform.position + Vector3.one * valuesRange / 2f;
        Gizmos.DrawWireCube(pos, Vector3.one * valuesRange);

        UtilsFunctions.DrawGizmoString("0,0,0", transform.position, Color.white, Vector2.one, fontSize);
        UtilsFunctions.DrawGizmoString("1,1,1", transform.position + Vector3.one * valuesRange, Color.white, Vector2.one, fontSize);
        
        UtilsFunctions.DrawGizmoString("damage", transform.position + Vector3.forward * valuesRange, Color.white, Vector2.one, fontSize);
        UtilsFunctions.DrawGizmoString("knockback", transform.position + Vector3.right * valuesRange, Color.white, Vector2.one, fontSize);
        UtilsFunctions.DrawGizmoString("speed", transform.position + Vector3.up * valuesRange, Color.white, Vector2.one, fontSize);

        Transform[] transforms = transform.GetComponentsInChildren<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            Gizmos.color = colors[i];
            if (t == transform) continue;
            Vector3 tPos = t.position;

            Gizmos.DrawLine(new Vector3(tPos.x, transform.position.y, transform.position.z), new Vector3(tPos.x, transform.position.y, tPos.z));
            Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y, tPos.z), new Vector3(tPos.x, transform.position.y, tPos.z));
            Gizmos.DrawLine(new Vector3(tPos.x, tPos.y, transform.position.z), tPos);
            Gizmos.DrawLine(new Vector3(transform.position.x, tPos.y, tPos.z), tPos);
            Gizmos.DrawLine(new Vector3(tPos.x, transform.position.y, tPos.z), tPos);
            Gizmos.DrawLine(new Vector3(tPos.x, transform.position.y, transform.position.z), new Vector3(tPos.x, tPos.y, transform.position.z));
            Gizmos.DrawLine(new Vector3(transform.position.x, transform.position.y, tPos.z), new Vector3(transform.position.x, tPos.y, tPos.z));

            string dmg = (t.localPosition.z / valuesRange).ToString("0.##");
            string kbk = (t.localPosition.x / valuesRange).ToString("0.##");
            string spd = (t.localPosition.y / valuesRange).ToString("0.##");
            string text = $"{t.name}\ndmg: {dmg}\nkbk: {kbk}\nspd: {spd}";
            UtilsFunctions.DrawGizmoString(text, tPos, Color.white, Vector2.one, dataFontSize);
        }
    }
}