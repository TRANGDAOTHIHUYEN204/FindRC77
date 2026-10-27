using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class OutlineDifferentPoint : MaskableGraphic
{

    [SerializeField] private float lineWidth = 2f;

    [SerializeField] private float dashLength = 10f;
    [SerializeField] private float gapLength = 6f;

    private readonly List<Vector2> _points = new List<Vector2>();
    private bool _loop = true;

    public override Texture mainTexture => s_WhiteTexture;
    
    public void Draw(IList<Vector3> points, RectTransform pointsSpace, bool loop = true)
    {
        if (points == null) { Clear(); return; }

        _points.Clear();
        for (int i = 0; i < points.Count; i++)
            _points.Add(ToLocal(points[i], pointsSpace));

        Finish(loop);
    }

    public void Draw(IList<Vector2> points, RectTransform pointsSpace, bool loop = true)
    {
        if (points == null) { Clear(); return; }

        _points.Clear();
        for (int i = 0; i < points.Count; i++)
            _points.Add(ToLocal(points[i], pointsSpace));

        Finish(loop);
    }

    public void Clear()
    {
        _points.Clear();
        SetVerticesDirty();
    }

    private Vector2 ToLocal(Vector3 p, RectTransform space)
    {
        if (space == null) return p;
        Vector3 world = space.TransformPoint(new Vector3(p.x, p.y, 0f));
        return rectTransform.InverseTransformPoint(world);
    }

    private void Finish(bool loop)
    {
        _loop = loop;
        raycastTarget = false;
        if (_points.Count < 2) _points.Clear();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        int count = _points.Count;
        if (count < 2) return;

        int segCount = _loop ? count : count - 1;
        bool useDash = dashLength > 0.001f && gapLength > 0.001f;
        float period = dashLength + gapLength;
        float carry = 0f; 

        for (int i = 0; i < segCount; i++)
        {
            Vector2 a = _points[i];
            Vector2 b = _points[(i + 1) % count];

            if (!useDash)
            {
                AddQuad(vh, a, b);
                continue;
            }

            float segLen = Vector2.Distance(a, b);
            if (segLen < 0.0001f) continue;

            Vector2 dir = (b - a) / segLen;
            float t = 0f;

            while (t < segLen)
            {
                if (carry < dashLength)
                {
                    float len = Mathf.Min(dashLength - carry, segLen - t);
                    AddQuad(vh, a + dir * t, a + dir * (t + len));
                    t += len;
                    carry += len;
                }
                else
                {
                    float len = Mathf.Min(period - carry, segLen - t);
                    t += len;
                    carry += len;
                }

                if (carry >= period) carry -= period;
            }
        }
        Debug.Log($"[Outline] Mesh verts={vh.currentVertCount}");
    }

    private void AddQuad(VertexHelper vh, Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        if (d.sqrMagnitude < 0.0001f) return;

        Vector2 n = new Vector2(-d.y, d.x).normalized * (lineWidth * 0.5f);

        int idx = vh.currentVertCount;
        vh.AddVert(a - n, color, Vector2.zero);
        vh.AddVert(a + n, color, Vector2.zero);
        vh.AddVert(b + n, color, Vector2.zero);
        vh.AddVert(b - n, color, Vector2.zero);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx + 2, idx + 3, idx);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}