using UnityEngine;

public class MarkPresenter : MonoBehaviour
{
    [SerializeField] private FactoryManager factoryManager;
    private readonly System.Collections.Generic.List<MarkFound> _marks = new();
    private static readonly Vector3[] _corners = new Vector3[4];

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

        pair.GetShapeWorldRect(root, out Vector3 center, out Vector2 size);

        float baseWidth = data.LineWidth;
        
        float shapeSize = Mathf.Min(size.x, size.y);
        float referenceSize = 1f;
        
        float sizeFactor = shapeSize / referenceSize;
        float targetWidth = baseWidth * sizeFactor;
        float minWidth = baseWidth * 0.85f;
        float maxWidth = baseWidth * 1.105f;

        float lw = Mathf.Clamp(targetWidth, minWidth, maxWidth);
        
        Vector2 pathSize = new Vector2(Mathf.Max(size.x - lw, 0.01f), Mathf.Max(size.y - lw, 0.01f));

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