using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkFound : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;

    private IDrawShape _drawShape;
    private Coroutine _drawRoutine;
    [SerializeField] private BounceEffect bounceEffect;
    private Coroutine _bounceRoutine;

    private Action _onDrawDone;

    [Header("Ngôi sao chạy ở đầu nét vẽ")]
    [SerializeField] private Sprite headStarSprite;
    [Tooltip("Kích thước sao so với độ dày nét.")]
    [SerializeField] private float headStarSizeMul = 6.75f;
    [Tooltip("Tốc độ xoay (độ/giây).")]
    [SerializeField] private float headStarSpin = 720f;
    [Tooltip("Tại thời điểm này (0-1) sao to nhất, sau đó thu nhỏ về 0 lúc vẽ xong.")]
    [SerializeField, Range(0.1f, 0.95f)] private float headStarPeakAt = 0.7f;
    [SerializeField] private float headStarPeakScale = 1.1f;

    [Header("Vệt đuôi phía sau ngôi sao")]
    [SerializeField] private bool headStarTrails = true;
    [Tooltip("Độ dài vệt so với cỡ ngôi sao (vệt bám theo đúng đoạn nét vừa vẽ phía sau sao).")]
    [SerializeField] private float trailLengthMul = 2.2f;
    [Tooltip("Độ dày đầu vệt so với độ dày nét.")]
    [SerializeField] private float trailWidthMul = 1.6f;
    [SerializeField] private Color trailColor = new Color(1f, 0.82f, 0.25f, 1f);

    private SpriteRenderer _headStar;
    private Coroutine _starRoutine;
    private LineRenderer _trail;
    private readonly List<Vector3> _trailPts = new List<Vector3>(64);

    public void SetOnDrawDone(Action cb) => _onDrawDone = cb;

    public void Init(ShapeMark shapeMark, Vector2 center, Vector2 size, float lineWidth, Color lineColor, int segments, float duration = 0.7f)
    {
        _drawShape = CreateBehaviour(shapeMark);

        if (_drawRoutine != null) StopCoroutine(_drawRoutine);
        _drawRoutine = StartCoroutine(DrawRoutine(center, size, lineWidth, lineColor, segments, duration));

        if (_starRoutine != null) { StopCoroutine(_starRoutine); _starRoutine = null; }
        if (headStarSprite != null)
        {
            EnsureHeadStar();
            _starRoutine = StartCoroutine(HeadStarRoutine(lineWidth, duration));
        }
    }

    private void EnsureHeadStar()
    {
        if (_headStar == null)
        {
            var go = new GameObject("HeadStar");
            go.transform.SetParent(transform, false);
            _headStar = go.AddComponent<SpriteRenderer>();
        }
        _headStar.sprite = headStarSprite;
        _headStar.gameObject.SetActive(false);

        if (headStarTrails)
        {
            if (_trail == null) _trail = CreateTrail("HeadTrail");
            SetupTrail(_trail);
        }
    }

    private LineRenderer CreateTrail(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var tr = go.AddComponent<LineRenderer>();
        tr.enabled = false;
        return tr;
    }

    private void SetupTrail(LineRenderer tr)
    {
        tr.sharedMaterial = lineRenderer.sharedMaterial;
        tr.useWorldSpace = true;
        tr.loop = false;
        tr.numCapVertices = 4;
        tr.numCornerVertices = 2;
        tr.alignment = lineRenderer.alignment;
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        tr.receiveShadows = false;
        // điểm 0 = sát ngôi sao, điểm cuối = đuôi: nhỏ dần + mờ dần về 0
        tr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(trailColor, 0f), new GradientColorKey(trailColor, 1f) },
            new[] { new GradientAlphaKey(trailColor.a, 0f), new GradientAlphaKey(0f, 1f) });
        tr.colorGradient = g;
        tr.positionCount = 0;
        tr.enabled = false;
    }

    /// Dựng vệt đuôi chạy dọc giữa đoạn nét vừa vẽ phía sau ngôi sao (luôn cong theo nét, không phụ thuộc FPS).
    private void UpdateTrails(int n, float z, float s, float starWorldSize, float lineWidth)
    {
        _trailPts.Clear();
        float maxLen = starWorldSize * trailLengthMul * s;
        if (n >= 2 && maxLen > 0.0001f)
        {
            Vector3 cur = LinePointWorld(n - 1);
            _trailPts.Add(cur);
            float acc = 0f;
            for (int i = n - 2; i >= 0; i--)
            {
                Vector3 q = LinePointWorld(i);
                float d = Vector2.Distance(cur, q);
                if (d < 1e-6f) continue;
                if (acc + d >= maxLen)
                {
                    _trailPts.Add(Vector3.Lerp(cur, q, (maxLen - acc) / d));
                    break;
                }
                acc += d;
                _trailPts.Add(q);
                cur = q;
            }
        }

        if (_trailPts.Count < 2)
        {
            _trail.enabled = false;
            return;
        }

        int count = _trailPts.Count;
        _trail.positionCount = count;
        for (int i = 0; i < count; i++)
            _trail.SetPosition(i, new Vector3(_trailPts[i].x, _trailPts[i].y, z - 0.005f));

        _trail.widthMultiplier = lineWidth * trailWidthMul * Mathf.Max(s, 0.4f);
        _trail.enabled = true;
    }

    private Vector3 LinePointWorld(int i)
    {
        Vector3 p = lineRenderer.GetPosition(i);
        return lineRenderer.useWorldSpace ? p : lineRenderer.transform.TransformPoint(p);
    }

    private void HideTrails()
    {
        if (_trail != null) { _trail.positionCount = 0; _trail.enabled = false; }
    }

    private IEnumerator HeadStarRoutine(float lineWidth, float duration)
    {
        Vector2 spriteSize = headStarSprite.bounds.size;
        float baseScale = lineWidth * headStarSizeMul / Mathf.Max(0.0001f, Mathf.Max(spriteSize.x, spriteSize.y));
        Transform st = _headStar.transform;
        float angle = 0f;
        float t = 0f;

        float starWorldSize = lineWidth * headStarSizeMul;
        bool trails = headStarTrails && _trail != null;

        _headStar.gameObject.SetActive(true);
        while (t < 1f)
        {
            float k = Mathf.Clamp01(t);
            float s = k < headStarPeakAt
                ? Mathf.Lerp(1f, headStarPeakScale, k / headStarPeakAt)
                : Mathf.Lerp(headStarPeakScale, 0f, (k - headStarPeakAt) / (1f - headStarPeakAt));

            int n = lineRenderer.positionCount;
            if (n > 0)
            {
                Vector3 p = lineRenderer.GetPosition(n - 1);
                if (!lineRenderer.useWorldSpace) p = lineRenderer.transform.TransformPoint(p);
                float z = lineRenderer.transform.position.z;
                // gần camera hơn nét một chút để vẽ đè lên nét (cùng sorting order)
                st.position = new Vector3(p.x, p.y, z - 0.01f);

                // hai vệt bám theo đoạn nét phía sau sao, nằm giữa nét và ngôi sao
                if (trails) UpdateTrails(n, z, s, starWorldSize, lineWidth);
            }

            angle -= headStarSpin * Time.unscaledDeltaTime;
            st.localRotation = Quaternion.Euler(0f, 0f, angle);
            st.localScale = Vector3.one * (baseScale * s);

            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, duration);
            yield return null;
        }

        _headStar.gameObject.SetActive(false);
        HideTrails();
        _starRoutine = null;
    }

    private IEnumerator DrawRoutine(Vector2 center, Vector2 size, float lineWidth, Color lineColor, int segments, float duration)
    {
        yield return _drawShape.Draw(center, size, lineWidth, lineColor, segments, duration);

        _drawRoutine = null;

        Action cb = _onDrawDone;
        _onDrawDone = null;
        cb?.Invoke();
    }

    private IDrawShape CreateBehaviour(ShapeMark shapeMark)
    {
        switch (shapeMark)
        {
            case ShapeMark.Rectangle:
                return new MarkRectangleBehaviour(lineRenderer);
            default:
                return new MarkCircleBehaviour(lineRenderer);
        }
    }

    public void PlayWinBounce()
    {
        if (!gameObject.activeInHierarchy) return;
        if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
        _bounceRoutine = StartCoroutine(WidthBounce());
    }

    private IEnumerator WidthBounce()
    {
        const float duration = 0.35f;
        const float amount = 0.8f;
        float baseMul = 1f;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(e / duration) * Mathf.PI);
            lineRenderer.widthMultiplier = baseMul * (1f + k * amount);
            yield return null;
        }
        lineRenderer.widthMultiplier = baseMul;
        _bounceRoutine = null;
    }

    private void OnDisable()
    {
        _onDrawDone = null;
        _drawRoutine = null;
        _bounceRoutine = null;
        _starRoutine = null;
        if (_headStar != null) _headStar.gameObject.SetActive(false);
        HideTrails();
    }
}