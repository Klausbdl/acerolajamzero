using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
[ExecuteInEditMode]
#endif
[RequireComponent(typeof(CanvasRenderer))]
public class UICircleRenderer : Graphic
{
#if UNITY_EDITOR
    public bool setAllColor;
#endif

    [Header("Global Settings")]
    public int sidesAdd = 0;
    public float offsetAdd;
    public float noiseSpeed = 1;
    [Header("Position")]
    public Vector2 centerAdd;
    public Vector2 moveSpeedMultiplier = Vector2.one;
    [Header("Rotation")]
    public float rotAdd;
    public float rotSpeedMultiplier = 1;
    [Header("Scale")]
    public float radiusSpeedMultiplier;
    public float radiusMagnitudeMultiplier;
    public float radiusAdd;

    public List<MyCircle> circles = new List<MyCircle>();

#if UNITY_EDITOR
    int circlesCount;
#endif

    protected override void Start()
    {
        circles.ForEach(c => {
            c.Start(centerAdd, rotAdd, radiusAdd); 
            SetAllDirty(); });
    }

    private void Update()
    {
        circles.ForEach(c => {
            c.Update(centerAdd, moveSpeedMultiplier, rotAdd, rotSpeedMultiplier, radiusSpeedMultiplier, radiusMagnitudeMultiplier, radiusAdd);
            SetAllDirty();
        });

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (circles.Count != circlesCount)
                circles.ForEach(c => { 
                    c.Start(centerAdd, rotAdd, radiusAdd); 
                    SetAllDirty(); });

            circlesCount = circles.Count;

            if (setAllColor)
            {
                setAllColor = false;
                circles.ForEach(c => {
                    c.color = color;
                    c.Start(centerAdd, rotAdd, radiusAdd);
                    SetAllDirty();
                });
            }
        }
#endif
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (circles.Count == 0) return;

        int lastCenter = 0;
        for (int c = 0; c < circles.Count; c++)
        {
            MyCircle circ = circles[c];
            int sides = circ.sides + sidesAdd;
            if (sides < 3) sides = 3;

            CreateCircle(vh, circ, sides);

            for (int i = 1; i <= sides; i++)
            {
                if(i < sides)
                    vh.AddTriangle(lastCenter, lastCenter + i, lastCenter + i + 1); //add to the next side
                else
                    vh.AddTriangle(lastCenter, lastCenter + i, lastCenter + 1); //add to the first side
            }

            lastCenter += sides + 1;
        }
    }

    void CreateCircle(VertexHelper vh, MyCircle c, int sides)
    {
        // Create vertex template
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = c.color;

        //first vertex
        Vector2 center = c.pos;
        float rotAngle = Mathf.Deg2Rad * (c.rotation + rotAdd + c.startRot);
        center.x += Mathf.Cos(rotAngle) * (c.centerOffset + offsetAdd);
        center.y += Mathf.Sin(rotAngle) * (c.centerOffset + offsetAdd);

        vertex.position = center;

        vh.AddVert(vertex);
        
        for (int i = 1; i <= sides; i++)
        {
            vertex.position = center;

            float angle = Mathf.Deg2Rad * (i * (360f / sides)) - rotAngle;
            vertex.position.x += Mathf.Sin(angle) * c.radius;
            vertex.position.y += Mathf.Cos(angle) * c.radius;
            vertex.position.x += (Mathf.PerlinNoise1D((c.timeOffset + Time.unscaledTime + i*100) * noiseSpeed * c.noiseSpeed) - .5f) * c.noise;
            vertex.position.y += (Mathf.PerlinNoise1D((c.timeOffset + Time.unscaledTime + i*200) * noiseSpeed * c.noiseSpeed) - .5f) * c.noise;
            vh.AddVert(vertex);
        }
    }
}

[Serializable]
public class MyCircle
{
    [Header("Main")]
    public Color color = new Color(1, 1, 1, 1);
    [Range(3, 128)] public int sides = 16;
    public float timeOffset = 0;
    public float centerOffset;
    public float noise = 0;
    public float noiseSpeed = 1;
    [Header("Position")]
    public Vector2 center;
    [HideInInspector] public Vector2 pos;
    public Vector2 moveSpeed;
    public Vector2 moveMagnitude;
    [Header("Rotation")]
    public float startRot;
    [ReadOnly] public float rotation;
    public float rotSpeed;
    [Header("Scale")]
    public float originalRadius = 100;
    [HideInInspector] public float radius;
    public float radiusSpeed;
    public float radiusMagnitude;
    public float radiusAdd;
    public void Start(Vector2 cAdd, float rotAdd, float radAdd)
    {
        pos = center + cAdd;
        rotation = startRot + rotAdd;
        radius = originalRadius + radAdd;
    }

    public void Update(Vector2 cAdd, Vector2 msMul, float rotAdd, float rotSMul, float radSMul, float radMMul, float radAdd)
    {
        //rotation += Time.unscaledDeltaTime * rotSpeed * rotSMul;
        //if (rotation > 360) rotation = 0;
        //if (rotation < 0) rotation = 360;
        rotation = ((Time.unscaledTime + timeOffset) * rotSpeed * rotSMul) % 360f + startRot;

        radius = originalRadius + (Mathf.Sin((Time.unscaledTime + timeOffset) * radiusSpeed * radSMul) * (radiusMagnitude * radMMul) + (radiusAdd + radAdd));

        float sin = Mathf.Sin((rotation + rotAdd) * Mathf.Deg2Rad);
        float cos = Mathf.Cos((rotation + rotAdd) * Mathf.Deg2Rad);

        Vector2 magX = Vector2.zero;
        magX.x = cos * moveMagnitude.x;
        magX.y = sin * moveMagnitude.x;
        magX *= Mathf.Sin((Time.unscaledTime + timeOffset) * moveSpeed.x * msMul.x);

        sin = Mathf.Sin((rotation + rotAdd + 90) * Mathf.Deg2Rad);
        cos = Mathf.Cos((rotation + rotAdd + 90) * Mathf.Deg2Rad);
        Vector2 magY = Vector2.zero;
        magY.x = cos * moveMagnitude.y;
        magY.y = sin * moveMagnitude.y;
        magY *= Mathf.Sin((Time.unscaledTime + timeOffset) * moveSpeed.y * msMul.y);

        pos = center + cAdd + magX + magY;
    }

    public override string ToString()
    {
        return $"\npos: {pos} rot: {rotation} radius: {radius}";
    }
}