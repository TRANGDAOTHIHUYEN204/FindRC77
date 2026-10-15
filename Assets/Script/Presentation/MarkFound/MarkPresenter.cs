using UnityEngine;

public class MarkPresenter : MonoBehaviour
{
    [SerializeField] private FactoryManager factoryManager;

    public void DrawPair(DifferentPair pair, MarkData data)
    {
        if (data == null) { Debug.LogError("MarkData null", this); return; }

        DrawMark(pair, pair.RootA, data);
        DrawMark(pair, pair.RootB, data);
    }

    private void DrawMark(DifferentPair pair, RectTransform root, MarkData data)
    {
        MarkFound mark = factoryManager.Create();
        if (mark == null) return;

        pair.GetWorldRect(root, out Vector2 center, out Vector2 size);
        mark.Init(data.ShapeMark, center, size, data.LineWidth, data.ColorLine, data.Segments);
    }
}