using System.Collections;
using UnityEngine;

public class MarkFound : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;

    private IDrawShape _drawShape;
    private Coroutine _drawRoutine;

    
    public void Init(ShapeMark shapeMark, Vector2 center, Vector2 size, float lineWidth, Color lineColor, int segments, float duration = 0.5f)
    {
        _drawShape = CreateBehaviour(shapeMark);

        if (_drawRoutine != null) StopCoroutine(_drawRoutine);
        _drawRoutine = StartCoroutine(_drawShape.Draw(center, size, lineWidth, lineColor, segments, duration));
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
}