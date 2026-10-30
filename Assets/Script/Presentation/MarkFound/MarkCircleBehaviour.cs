using System.Collections;
using UnityEngine;

public class MarkCircleBehaviour : IDrawShape
{
    private readonly LineRenderer _lineRenderer;
    private LineRenderer _outline;

    private const float Padding = 1f;       
    private const float OutlineThickness = 0.01f; 

    public MarkCircleBehaviour(LineRenderer lineRenderer) => _lineRenderer = lineRenderer;

    public IEnumerator Draw(Vector2 center, Vector2 size, float lineWidth, Color color, int segments, float duration)
    {
        if (_lineRenderer == null) yield break;

        _lineRenderer.transform.position = new Vector3(center.x, center.y, 0f);
        _lineRenderer.useWorldSpace = false;
        _lineRenderer.startWidth = _lineRenderer.endWidth = lineWidth;
        _lineRenderer.startColor = _lineRenderer.endColor = color;
        _lineRenderer.sortingOrder = Mathf.Max(_lineRenderer.sortingOrder, 1);

        SetupOutline(lineWidth);
        
        segments = Mathf.Max(24, segments);
        float radius = size.x * 0.5f + lineWidth * 0.5f;

        Vector3[] pts = new Vector3[segments];
        for (int i = 0; i < segments; i++)
        {
            float a = Mathf.PI * 0.5f - i * Mathf.PI * 2f / segments;
            pts[i] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
        }

        yield return LineDrawUtil.AnimateDraw(_lineRenderer, _outline, pts, true, duration, 8, 8);
    }

    private void SetupOutline(float lineWidth)
    {
        if (_outline == null)
        {
            Transform t = _lineRenderer.transform.Find("Outline");
            GameObject go = t != null ? t.gameObject : new GameObject("Outline");
            go.transform.SetParent(_lineRenderer.transform, false);
            _outline = go.GetComponent<LineRenderer>();
            if (_outline == null) _outline = go.AddComponent<LineRenderer>();
        }

        _outline.useWorldSpace = false;
        _outline.sharedMaterial = _lineRenderer.sharedMaterial;
        _outline.sortingLayerID = _lineRenderer.sortingLayerID;
        _outline.sortingOrder = _lineRenderer.sortingOrder - 1;
        _outline.alignment = _lineRenderer.alignment;
        _outline.startWidth = _outline.endWidth = lineWidth + OutlineThickness * 2f;
        _outline.startColor = _outline.endColor = Color.white;
    }
}