using UnityEngine;

public class MarkPresenter : MonoBehaviour
{
    [SerializeField] private FactoryManager factoryManager;

    public void DrawMark(DifferentPoint point, MarkData data)
    {
        MarkFound mark = factoryManager.Create();

        mark.Init(data.ShapeMark, point.Center, point.Size, data.LineWidth, data.ColorLine, data.Segments);
    }

    public void DrawPair(DifferentPair pair, MarkData data)
    {
        DrawMark(pair.PointA, data);
        DrawMark(pair.PointB, data);
    }
}
