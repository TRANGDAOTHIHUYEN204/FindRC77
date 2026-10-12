using UnityEngine;

public class CircleDifferentBehaviour
{
    private readonly LineRenderer _lineRenderer;
    public CircleDifferentBehaviour(LineRenderer lineRenderer)
    {
        _lineRenderer = lineRenderer;
    }

    public void DrawCircle(Vector2 center, float radius, float lineWidth, Color color, int segments)
    {
        if (_lineRenderer == null) return;

        _lineRenderer.transform.position = new Vector3(center.x, center.y, 0f);

        segments = Mathf.Max(3, segments);
        _lineRenderer.useWorldSpace = false;
        _lineRenderer.loop = true;
        _lineRenderer.positionCount = segments;
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;
        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            _lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
    }
}