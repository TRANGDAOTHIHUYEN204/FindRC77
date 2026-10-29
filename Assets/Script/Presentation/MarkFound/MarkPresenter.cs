using UnityEngine;

public class MarkPresenter : MonoBehaviour
{
    [SerializeField] private FactoryManager factoryManager;

    [SerializeField, Range(0.1f, 1f)] private float visualRatio = 0.5f;

    [SerializeField] private float smallThreshold = 0.05f;
    [SerializeField] private float smallLineMul = 0.7f;

    private int _session;
    public int Session =>  _session;
    
    private readonly System.Collections.Generic.List<MarkFound> _marks = new();
    public bool DrawPair(DifferentPair pair, MarkData data, int session)
    {
        if (session != _session) return false;   
        DrawPair(pair, data);
        return true;
    }

    public void DrawPair(DifferentPair pair, MarkData data)
    {
        if (data == null)
        {
            Debug.LogError("MarkData null", this);
            return;
        }

        DrawMark(pair, pair.RootA, data);
        DrawMark(pair, pair.RootB, data);
    }

    private void DrawMark(DifferentPair pair, RectTransform root, MarkData data)
    {
        MarkFound mark = factoryManager.Create();
        if (mark == null) return;
        _marks.Add(mark);

        pair.GetMarkWorldRect(root, out Vector3 center, out Vector2 size);

        float shapeSize = Mathf.Min(size.x, size.y) * visualRatio;

        float imageWidth = root.rect.width * root.lossyScale.x;
        bool isSmall = shapeSize / imageWidth <= smallThreshold;
        float lw = data.LineWidth * (isSmall ? smallLineMul : 1f);

        float path = Mathf.Max(shapeSize - lw, 0.01f);
        Vector2 pathSize = new Vector2(path, path);

        Transform t = mark.transform;
        t.SetParent(root, false);
        t.localRotation = Quaternion.identity;
        Vector3 s = root.lossyScale;
        t.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);

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