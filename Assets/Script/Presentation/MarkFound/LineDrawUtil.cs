using System.Collections;
using UnityEngine;

public static class LineDrawUtil
{
    public static LineRenderer SetupOutline(LineRenderer main, LineRenderer outline, float lineWidth, float thickness)
    {
        if (outline == null)
        {
            Transform t = main.transform.Find("Outline");
            GameObject go = t != null ? t.gameObject : new GameObject("Outline");
            go.transform.SetParent(main.transform, false);
            outline = go.GetComponent<LineRenderer>();
            if (outline == null) outline = go.AddComponent<LineRenderer>();
        }

        outline.useWorldSpace = false;
        outline.sharedMaterial = main.sharedMaterial;
        outline.sortingLayerID = main.sortingLayerID;
        outline.sortingOrder = main.sortingOrder - 1;
        outline.alignment = main.alignment;
        outline.startWidth = outline.endWidth = lineWidth + thickness * 2f;
        outline.startColor = outline.endColor = Color.white;
        return outline;
    }
    public static IEnumerator AnimateDraw(LineRenderer lr, Vector3[] points, bool closed, float duration,
        int cornerVertices = 0, int capVertices = 0)
        => AnimateDraw(lr, null, points, closed, duration, cornerVertices, capVertices);

    public static IEnumerator AnimateDraw(LineRenderer lr, LineRenderer outline, Vector3[] points, bool closed,
        float duration, int cornerVertices = 0, int capVertices = 0)
    {
        if (points == null || points.Length < 2) yield break;

        int total = closed ? points.Length + 1 : points.Length;
        Vector3[] path = new Vector3[total];
        for (int i = 0; i < points.Length; i++) path[i] = points[i];
        if (closed) path[total - 1] = points[0];

        Prepare(lr, cornerVertices, capVertices);
        if (outline != null) Prepare(outline, cornerVertices, capVertices);

        if (duration > 0f)
        {
            int segCount = total - 1;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                float normalizedTime = Mathf.Clamp01(t);
                float easedTime = Mathf.Sin(normalizedTime * Mathf.PI * 0.5f);
                float progress = easedTime * segCount;
                int full = Mathf.Min(Mathf.FloorToInt(progress), segCount);
                float partial = progress - full;
                bool hasPartial = full < segCount;
                int count = full + 1 + (hasPartial ? 1 : 0);

                ApplyPartial(lr, path, full, partial, hasPartial, count);
                if (outline != null)
                    ApplyPartial(outline, path, full, partial, hasPartial, count);
                yield return null;
            }
        }

        Finish(lr, points, closed);
        if (outline != null) Finish(outline, points, closed);
    }

    private static void Prepare(LineRenderer lr, int cornerVertices, int capVertices)
    {
        lr.enabled = true;
        lr.loop = false;
        lr.numCornerVertices = cornerVertices;
        lr.numCapVertices = capVertices;
    }

    private static void ApplyPartial(LineRenderer lr, Vector3[] path, int full, float partial, bool hasPartial, int count)
    {
        lr.positionCount = count;
        for (int i = 0; i <= full; i++) lr.SetPosition(i, path[i]);
        if (hasPartial)
            lr.SetPosition(count - 1, Vector3.Lerp(path[full], path[full + 1], partial));
    }

    private static void Finish(LineRenderer lr, Vector3[] points, bool closed)
    {
        lr.positionCount = points.Length;
        lr.SetPositions(points);
        lr.loop = closed;
        lr.enabled = true;
    }
}