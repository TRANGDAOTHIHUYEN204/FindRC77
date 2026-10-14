using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeOutline : MaskableGraphic
{
    [SerializeField] float thickness = 8f;
    [SerializeField] float cornerRadius = 30f;
    [SerializeField] int cornerSegments = 8;
    [Range(0f, 1f)] [SerializeField] float fill = 1f;

    readonly List<Vector2> path = new List<Vector2>();
    readonly List<float> cum = new List<float>();
    readonly List<Vector2> pts = new List<Vector2>();
    
    public void SetFill(float f)
    {
        f = Mathf.Clamp01(f);
        if (Mathf.Approximately(f, fill)) return;
        fill = f;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (fill <= 0f) return;

        BuildPath();
        float total = cum[cum.Count - 1];
        float gap = (1f - fill) * total;
        float a = gap;
        float b = total;

        pts.Clear();
        pts.Add(Sample(a));
        for (int i = 0; i < path.Count; i++)
            if (cum[i] > a && cum[i] < b) pts.Add(path[i]);
        pts.Add(Sample(b));
        if (pts.Count < 2) return;

        float h = thickness * 0.5f;
        int n = pts.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 n1 = i > 0 ? Normal(pts[i - 1], pts[i]) : Normal(pts[i], pts[i + 1]);
            Vector2 n2 = i < n - 1 ? Normal(pts[i], pts[i + 1]) : n1;
            Vector2 m = (n1 + n2).normalized;
            float scale = Mathf.Min(1f / Mathf.Max(Vector2.Dot(m, n1), 0.5f), 2f);
            Vector2 off = m * h * scale;

            vh.AddVert(pts[i] + off, color, Vector2.zero);
            vh.AddVert(pts[i] - off, color, Vector2.zero);
        }
        for (int i = 0; i < n - 1; i++)
        {
            int k = i * 2;
            vh.AddTriangle(k, k + 1, k + 2);
            vh.AddTriangle(k + 1, k + 3, k + 2);
        }
    }

    static Vector2 Normal(Vector2 p0, Vector2 p1)
    {
        Vector2 t = (p1 - p0).normalized;
        return new Vector2(t.y, -t.x);
    }

    Vector2 Sample(float d)
    {
        for (int i = 1; i < path.Count; i++)
            if (cum[i] >= d)
                return Vector2.Lerp(path[i - 1], path[i], Mathf.InverseLerp(cum[i - 1], cum[i], d));
        return path[path.Count - 1];
    }

    void BuildPath()
    {
        path.Clear();
        cum.Clear();

        Rect r = rectTransform.rect;
        float inset = thickness * 0.5f;
        float hw = r.width * 0.5f - inset;
        float hh = r.height * 0.5f - inset;
        float rad = Mathf.Max(0.01f, Mathf.Min(cornerRadius, hw, hh));
        Vector2 c0 = r.center;

        path.Add(c0 + new Vector2(0, hh));
        AddArc(c0 + new Vector2(hw - rad, hh - rad), rad, 90f, 0f);
        AddArc(c0 + new Vector2(hw - rad, -hh + rad), rad, 0f, -90f);
        AddArc(c0 + new Vector2(-hw + rad, -hh + rad), rad, -90f, -180f);
        AddArc(c0 + new Vector2(-hw + rad, hh - rad), rad, 180f, 90f);
        path.Add(c0 + new Vector2(0, hh));

        cum.Add(0f);
        for (int i = 1; i < path.Count; i++)
            cum.Add(cum[i - 1] + Vector2.Distance(path[i - 1], path[i]));
    }

    void AddArc(Vector2 center, float rad, float from, float to)
    {
        for (int i = 0; i <= cornerSegments; i++)
        {
            float ang = Mathf.Lerp(from, to, i / (float)cornerSegments) * Mathf.Deg2Rad;
            path.Add(center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}