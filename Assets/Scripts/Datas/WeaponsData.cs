using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WeaponsData : MonoBehaviour
{
    public GameManager manager;
    public bool update;
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
    public int[] prices;
    [Header("Debug")]
    public float valuesRange = 20;
    public float fontSize = 15;
    public float dataFontSize = 15;
    public Color[] colors = new Color[12];
#if UNITY_EDITOR
    private void Update()
    {
        if (update)
        {
            update = false;
            manager.shopInventory.leftArmModules.Clear();
            manager.shopInventory.rightArmModules.Clear();

            Transform[] transforms = transform.GetComponentsInChildren<Transform>();
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t == transform) continue;

                float dmg = t.localPosition.z / valuesRange;
                float kbk = t.localPosition.x / valuesRange;
                float spd = t.localPosition.y / valuesRange;

                ArmModule module = new ArmModule();
                module.name = t.name[0].ToString().ToUpper() + t.name.Substring(1);

                switch (i)
                {
                    case 1:
                        module.blendShapeIndex = 3;
                        module.moduleType = ArmModule.ArmModuleType.SWORD; break;
                    case 2:
                        module.blendShapeIndex = 4;
                        module.moduleType = ArmModule.ArmModuleType.SWORD; break;
                    case 3:
                        module.blendShapeIndex = 5;
                        module.moduleType = ArmModule.ArmModuleType.SWORD; break;
                    case 4:
                        module.blendShapeIndex = 6;
                        module.moduleType = ArmModule.ArmModuleType.SWORD; break;
                    case 5:
                        module.blendShapeIndex = -1;
                        module.moduleType = ArmModule.ArmModuleType.PUNCH; break;
                    case 6:
                        module.blendShapeIndex = 0;
                        module.moduleType = ArmModule.ArmModuleType.PUNCH; break;
                    case 7:
                        module.blendShapeIndex = 1;
                        module.moduleType = ArmModule.ArmModuleType.PUNCH; break;
                    case 8:
                        module.blendShapeIndex = 2;
                        module.moduleType = ArmModule.ArmModuleType.PUNCH; break;
                    case 9:
                        module.blendShapeIndex = 7;
                        module.moduleType = ArmModule.ArmModuleType.GUN; break;
                    case 10:
                        module.blendShapeIndex = 8;
                        module.moduleType = ArmModule.ArmModuleType.GUN; break;
                    case 11:
                        module.blendShapeIndex = 9;
                        module.moduleType = ArmModule.ArmModuleType.GUN; break;
                    case 12:
                        module.blendShapeIndex = 10;
                        module.moduleType = ArmModule.ArmModuleType.GUN; break;
                }
                module.damage = dmg;
                module.knockback = kbk;
                module.speed = spd;
                module.cost = prices[i-1];

                manager.shopInventory.leftArmModules.Add(module);
                manager.shopInventory.rightArmModules.Add(module);
            }
        }
    }
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
#endif
}
