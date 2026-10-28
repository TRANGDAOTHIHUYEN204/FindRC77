using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class OutlineDifferentPoint : MaskableGraphic
{
    [SerializeField] private float lineWidth = 2f;
    [SerializeField] private float dashLength = 10f;
    [SerializeField] private float gapLength = 6f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float drawDuration = 0.5f; 

    private readonly List<Vector2> _points = new List<Vector2>();
    private bool _loop = true;
    private Coroutine _fadeRoutine;
    private Coroutine _drawRoutine;
    private float _baseAlpha = -1f;
    private float _progress = 1f; 

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
        StopFade();
        StopDraw();
        _progress = 1f;
        _points.Clear();
        SetVerticesDirty();
    }

    public void Hide(float duration = -1f)
    {
        if (duration < 0f) duration = fadeDuration;

        if (!gameObject.activeInHierarchy || duration <= 0f || _points.Count == 0)
        {
            Clear();
            return;
        }

        StopDraw();
        StopFade();
        _fadeRoutine = StartCoroutine(FadeOut(duration));
    }

    private IEnumerator DrawIn(float duration)
    {
        _progress = 0f;
        SetVerticesDirty();

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _progress = Mathf.Clamp01(t / duration);
            SetVerticesDirty();
            yield return null;
        }

        _progress = 1f;
        _drawRoutine = null;
        SetVerticesDirty();
    }

    private void StopDraw()
    {
        if (_drawRoutine == null) return;
        StopCoroutine(_drawRoutine);
        _drawRoutine = null;
    }

    private IEnumerator FadeOut(float duration)
    {
        Color c = color;
        float startAlpha = c.a;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(startAlpha, 0f, t / duration);
            color = c;
            yield return null;
        }

        _fadeRoutine = null;
        _points.Clear();
        SetVerticesDirty();

        c.a = startAlpha;
        color = c;
    }

    private void StopFade()
    {
        if (_fadeRoutine == null) return;
        StopCoroutine(_fadeRoutine);
        _fadeRoutine = null;

        Color c = color;
        c.a = _baseAlpha >= 0f ? _baseAlpha : 1f;
        color = c;
    }

    private Vector2 ToLocal(Vector3 p, RectTransform space)
    {
        if (space == null) return p;
        Vector3 world = space.TransformPoint(new Vector3(p.x, p.y, 0f));
        return rectTransform.InverseTransformPoint(world);
    }

    private void EnsureClockwise()
    {
        int n = _points.Count;
        if (n < 3) return;

        float area = 0f;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = _points[i];
            Vector2 b = _points[(i + 1) % n];
            area += a.x * b.y - b.x * a.y;
        }

        if (area > 0f) _points.Reverse(1, n - 1);
    }

    private void Finish(bool loop)
    {
        StopFade();
        StopDraw();
        _baseAlpha = color.a;

        _loop = loop;
        raycastTarget = false;
        if (_points.Count < 2) _points.Clear();

        if (_loop) EnsureClockwise();

        if (_points.Count >= 2 && drawDuration > 0f && gameObject.activeInHierarchy)
        {
            _drawRoutine = StartCoroutine(DrawIn(drawDuration));
        }
        else
        {
            _progress = 1f;
        }

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
        
        float total = 0f;
        for (int i = 0; i < segCount; i++)
            total += Vector2.Distance(_points[i], _points[(i + 1) % count]);

        float limit = total * _progress;
        float traveled = 0f;

        for (int i = 0; i < segCount; i++)
        {
            Vector2 a = _points[i];
            Vector2 b = _points[(i + 1) % count];

            float segLen = Vector2.Distance(a, b);
            if (segLen < 0.0001f) continue;

            float segDraw = Mathf.Min(segLen, limit - traveled);
            if (segDraw <= 0f) break;

            Vector2 dir = (b - a) / segLen;

            if (!useDash)
            {
                AddQuad(vh, a, a + dir * segDraw);
            }
            else
            {
                float t = 0f;
                while (t < segDraw)
                {
                    if (carry < dashLength)
                    {
                        float len = Mathf.Min(dashLength - carry, segDraw - t);
                        AddQuad(vh, a + dir * t, a + dir * (t + len));
                        t += len;
                        carry += len;
                    }
                    else
                    {
                        float len = Mathf.Min(period - carry, segDraw - t);
                        t += len;
                        carry += len;
                    }

                    if (carry >= period) carry -= period;
                }
            }

            traveled += segLen;
        }
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