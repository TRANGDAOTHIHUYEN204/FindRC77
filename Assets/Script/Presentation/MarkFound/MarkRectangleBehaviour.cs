using System.Collections;
using UnityEngine;

public class MarkRectangleBehaviour : IDrawShape
{
    private readonly LineRenderer _lineRenderer;

    private const float Padding = 1.15f;

    public MarkRectangleBehaviour(LineRenderer lineRenderer)
    {
        _lineRenderer = lineRenderer;
    }

    public IEnumerator Draw(Vector2 center, Vector2 size, float lineWidth, Color color, int segments, float duration)
    {
        if (_lineRenderer == null)
            yield break;
        
        _lineRenderer.transform.position = new Vector3(center.x, center.y, 0f);
        
        _lineRenderer.useWorldSpace = false;

        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;

        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;

        _lineRenderer.sortingOrder = Mathf.Max(_lineRenderer.sortingOrder, 1);
        
        float halfW = Mathf.Max(0.001f, size.x * 0.5f * Padding + lineWidth * 0.5f);

        float halfH = Mathf.Max(0.001f, size.y * 0.5f * Padding + lineWidth * 0.5f);

        Vector3[] points = { new Vector3(-halfW,  halfH, 0f), new Vector3( halfW,  halfH, 0f), new Vector3( halfW, -halfH, 0f), new Vector3(-halfW, -halfH, 0f) };

        yield return LineDrawUtil.AnimateDraw(_lineRenderer, points, true, duration);
    }
}