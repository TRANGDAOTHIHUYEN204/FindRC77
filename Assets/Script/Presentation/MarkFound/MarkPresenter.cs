using UnityEngine;

public class MarkPresenter : MonoBehaviour
{
    [SerializeField] private FactoryManager factoryManager;

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

        pair.GetWorldRect(root, out Vector2 center, out Vector2 size);


        root.GetWorldCorners(_corners);
        Vector2 min = _corners[0];
        Vector2 max = _corners[2];

        const float padding = 1.15f;
        float lw = data.LineWidth;
        float radius = Mathf.Max(size.x, size.y) * 0.5f * padding + lw * 0.5f;
        radius = Mathf.Min(radius, Mathf.Min(max.x - min.x, max.y - min.y) * 0.5f);
        size = Vector2.one * ((radius - lw * 0.5f) / padding * 2f);

        center.x = Mathf.Clamp(center.x, min.x + radius, max.x - radius);
        center.y = Mathf.Clamp(center.y, min.y + radius, max.y - radius);

        Transform t = mark.transform;
        t.SetParent(root, false);
        t.localRotation = Quaternion.identity;
        Vector3 s = root.lossyScale;
        t.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);

        mark.Init(data.ShapeMark, center, size, lw, data.ColorLine, data.Segments);
        RectTransform frame = (RectTransform)root.parent;  
        int order = (root == pair.RootA) ? 20 : 30;          
        FrameMaskUtil.ApplyMask(frame, mark, order);
    }
}