using System;
using System.Collections.Generic;
using UnityEngine;

public class MarkPresenter : MonoBehaviour
{
    [SerializeField] private FactoryManager factoryManager;

    [SerializeField] private float smallRel = 0.04f;
    [SerializeField] private float bigRel   = 0.15f;
    [SerializeField] private float smallRatio  = 1.5f;
    [SerializeField] private float normalRatio = 1.0f;
    [SerializeField] private float bigRatio    = 0.6f;
    [SerializeField] private float minInnerPercent = 0.08f;
    [SerializeField] private float maxInnerPercent = 0.22f;

    [Header("Nét")]
    [SerializeField, Range(0.7f, 1f)] private float smallLineMul = 0.75f;

    private int _session;
    public int Session => _session;

    private readonly List<MarkFound> _marks = new();

    public bool DrawPair(DifferentPair pair, MarkData data, int session,
                         Action<RectTransform> onFinished = null)
    {
        if (session != _session) return false;
        DrawPair(pair, data, onFinished);
        return true;
    }

    public void DrawPair(DifferentPair pair, MarkData data, Action<RectTransform> onFinished = null)
    {
        if (data == null)
        {
            Debug.LogError("MarkData null", this);
            return;
        }

        int session = _session;
        bool fired = false;

        void OnMarkDone(RectTransform from)
        {
            if (fired || session != _session) return;
            fired = true;
            onFinished?.Invoke(from);
        }

        DrawMark(pair, pair.RootA, data, OnMarkDone);
        DrawMark(pair, pair.RootB, data, OnMarkDone);
    }

    private float GetRatio(float rel, out float t)
    {
        if (rel <= smallRel)
        {
            t = 0f;
            return smallRatio;
        }

        if (rel >= bigRel)
        {
            t = 1f;
            return bigRatio;
        }

        float mid = (smallRel + bigRel) * 0.5f;
        if (rel < mid)
        {
            float k = Mathf.InverseLerp(smallRel, mid, rel);
            t = k * 0.5f;
            return Mathf.Lerp(smallRatio, normalRatio, k);
        }
        else
        {
            float k = Mathf.InverseLerp(mid, bigRel, rel);
            t = 0.5f + k * 0.5f;
            return Mathf.Lerp(normalRatio, bigRatio, k);
        }
    }

    private void DrawMark(DifferentPair pair, RectTransform root, MarkData data, Action<RectTransform> onDone)
    {
        MarkFound mark = factoryManager.Create();
        if (mark == null) return;
        _marks.Add(mark);

        pair.GetMarkWorldRect(root, out Vector3 center, out Vector2 size);

        float imageWidth = root.rect.width * root.lossyScale.x;
        if (imageWidth <= 0.0001f) imageWidth = 1f;

        float path;   
        float lw;
        float t;

        if (pair.TryGetMarkWorldRadius(root, out float radiusWorld))
        {
            path = radiusWorld * 2f;
            GetRatio(path / imageWidth, out t);
            lw = Mathf.Lerp(data.LineWidth * smallLineMul, data.LineWidth, t);
        }
        else
        {
            float rawSize = Mathf.Min(size.x, size.y);
            float rel     = rawSize / imageWidth;

            float ratio = GetRatio(rel, out t);

            float inner = Mathf.Clamp(rawSize * ratio, imageWidth * minInnerPercent, imageWidth * maxInnerPercent);

            lw = Mathf.Lerp(data.LineWidth * smallLineMul, data.LineWidth, t);
            path = inner + lw;
        }

        Vector2 pathSize = new Vector2(path, path);

        Transform tr = mark.transform;
        tr.SetParent(root, false);
        tr.localRotation = Quaternion.identity;
        Vector3 s = root.lossyScale;
        tr.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);

        mark.SetOnDrawDone(() => onDone?.Invoke(root));

        mark.Init(data.ShapeMark, (Vector2)center, pathSize, lw, data.ColorLine, data.Segments);

        RectTransform frame = (RectTransform)root.parent;
        int order = (root == pair.RootA) ? 20 : 30;
        FrameMaskUtil.ApplyMask(frame, mark, order);
    }

    public void ClearMarks()
    {
        _session++;

        for (int i = _marks.Count - 1; i >= 0; i--)
        {
            MarkFound m = _marks[i];
            if (m == null) continue;

            m.StopAllCoroutines();
            factoryManager.Release(m.gameObject);
        }
        _marks.Clear();
    }

    public void PlayWinBounce()
    {
        for (int i = 0; i < _marks.Count; i++)
        {
            if (_marks[i] == null || !_marks[i].gameObject.activeInHierarchy) continue;
            _marks[i].PlayWinBounce();
        }
    }
}