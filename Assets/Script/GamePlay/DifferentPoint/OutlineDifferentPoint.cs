using UnityEngine;

public class OutlineDifferentPoint : MonoBehaviour
{
    [Header("Line Settings")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color lineColor = Color.red;
    [SerializeField] private float lineWidth = 4f;
    [SerializeField] private int sortingOrder = 10;

    private LineRenderer _line;

    private void EnsureLine()
    {
        if (_line != null) return;

        var go = new GameObject("Outline");
        go.transform.SetParent(transform, false);

        _line = go.AddComponent<LineRenderer>();
        _line.useWorldSpace = false;
        _line.loop = true;
        _line.widthMultiplier = lineWidth;
        _line.numCornerVertices = 4;
        _line.startColor = _line.endColor = lineColor;
        _line.sortingOrder = sortingOrder;
        _line.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        _line.enabled = false;
    }

    public void Draw(Vector3[] points, Vector2 size)
    {
        EnsureLine();

        if (points == null || points.Length < 2)
        {
            Vector2 h = size * 0.5f;
            points = new[]
            {
                new Vector3(-h.x,  h.y, 0f),
                new Vector3( h.x,  h.y, 0f),
                new Vector3( h.x, -h.y, 0f),
                new Vector3(-h.x, -h.y, 0f),
            };
        }

        _line.positionCount = points.Length;
        _line.SetPositions(points);
        _line.enabled = true;
    }

    public void Clear()
    {
        if (_line != null) _line.enabled = false;
    }
}