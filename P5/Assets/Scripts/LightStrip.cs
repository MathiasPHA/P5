using System.Collections.Generic;
using UnityEngine;

// Spawns point lights evenly along a LineRenderer (the visible strip)
[RequireComponent(typeof(LineRenderer))]
public class LightStrip : MonoBehaviour
{
    [SerializeField] private Color color = new Color(1f, 0.79f, 0.54f);
    [SerializeField] private float intensity = 0.8f;
    [SerializeField] private float range = 1.5f;
    [Tooltip("Distance between lights along the strip (metres)")]
    [SerializeField] private float spacing = 1.5f;
    [Tooltip("How far the lights sit from the strip, e.g. slightly down/out")]
    [SerializeField] private Vector3 lightOffset = new Vector3(0f, -0.05f, 0f);
    [SerializeField] private bool on = true;

    private readonly List<Light> lights = new List<Light>();
    private LineRenderer line;
    private Material stripMaterial;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        stripMaterial = line.material;
        SpawnLights();
        Apply();
    }

    public void SetOn(bool value) { on = value; Apply(); }
    public void SetColor(Color value) { color = value; Apply(); }

    private void Apply()
    {
        foreach (var l in lights)
        {
            l.enabled = on;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
        }

        if (stripMaterial != null && stripMaterial.HasProperty("_EmissionColor"))
        {
            stripMaterial.EnableKeyword("_EMISSION");
            stripMaterial.SetColor("_EmissionColor", on ? color * 3f : Color.black);
        }
    }

    private void SpawnLights()
    {
        int count = line.positionCount;
        if (count < 2) return;

        float carry = 0f;
        for (int i = 0; i < count - 1; i++)
        {
            Vector3 a = Point(i), b = Point(i + 1);
            float length = Vector3.Distance(a, b);

            for (float d = carry; d <= length; d += spacing)
                CreateLight(Vector3.Lerp(a, b, d / length) + lightOffset);

            carry = spacing - (length - carry) % spacing;
            if (carry >= spacing) carry = 0f;
        }
    }

    private Vector3 Point(int i)
    {
        Vector3 p = line.GetPosition(i);
        return line.useWorldSpace ? p : transform.TransformPoint(p);
    }

    private void CreateLight(Vector3 position)
    {
        var go = new GameObject("StripLight");
        go.transform.SetParent(transform, false);
        go.transform.position = position;

        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.shadows = LightShadows.None;
        lights.Add(l);
    }
}