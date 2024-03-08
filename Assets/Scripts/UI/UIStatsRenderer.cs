using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

#if UNITY_EDITOR
[ExecuteInEditMode]
#endif
[RequireComponent(typeof(CanvasRenderer))]
public class UIStatsRenderer : Graphic
{
    public MyStats stats;
    [Range(0, 1)] public List<float> statsValues = new List<float>();

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (stats == null) return;
        if (stats.statsCount < 3) return;
        int sides = stats.statsCount;

        if(statsValues.Count < sides)
        {
            for(int i = 0; i < sides-statsValues.Count; i++)
            {
                statsValues.Add(0);
            }
        }
        else if(statsValues.Count > sides)
        {
            for (int i = 0; i < statsValues.Count - sides; i++)
            {
                statsValues.Remove(statsValues[^1]);
            }
        }

        CreateStat(vh, stats, sides);

        for (int i = 1; i <= sides; i++)
        {
            if (i < sides)
                vh.AddTriangle(0, i, i + 1); //add to the next side
            else
                vh.AddTriangle(0, i, 1); //add to the first side
        }
    }

    void CreateStat(VertexHelper vh, MyStats s, int sides)
    {
        // Create vertex template
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = new Color32(0, 0, 0, 0);

        //first vertex
        vertex.position = Vector2.zero;
        vh.AddVert(vertex);
        vertex.color = color;
        for (int i = 1; i <= sides; i++)
        {
            vertex.position = Vector2.zero;
            float angle = Mathf.Deg2Rad * (i * (360f / sides));
            float radius = Mathf.Lerp(s.radiusMin, s.radiusMax, statsValues[i-1]);
            vertex.position.x += Mathf.Sin(angle) * radius;
            vertex.position.y += Mathf.Cos(angle) * radius;
            vh.AddVert(vertex);
        }
    }
}
[Serializable]
public class MyStats
{
    public int statsCount = 3;
    public float radiusMin = 0;
    public float radiusMax = 100;
}
