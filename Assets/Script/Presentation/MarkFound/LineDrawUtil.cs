using System.Collections;
using UnityEngine;

public static class LineDrawUtil
{
    public static IEnumerator AnimateDraw(LineRenderer lr, Vector3[] points, bool closed, float duration)
        => AnimateDraw(new[] { lr }, points, closed, duration);

    public static IEnumerator AnimateDraw(LineRenderer[] lrs, Vector3[] points, bool closed, float duration)
    {
        if (points == null || points.Length < 2) yield break;

        int total = closed ? points.Length + 1 : points.Length;
        Vector3[] path = new Vector3[total];
        for (int i = 0; i < points.Length; i++) path[i] = points[i];
        if (closed) path[total - 1] = points[0];

        foreach (var lr in lrs)
        {
            lr.enabled = true;
            lr.loop = false;
            lr.numCornerVertices = 4;
            lr.numCapVertices = 4;
        }

        if (duration > 0f)
        {
            int segCount = total - 1;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / duration;
                float progress = Mathf.Clamp01(t) * segCount;
                int full = Mathf.Min(Mathf.FloorToInt(progress), segCount);
                float partial = progress - full;
                bool hasPartial = full < segCount;
                int count = full + 1 + (hasPartial ? 1 : 0);

                foreach (var lr in lrs)
                {
                    lr.positionCount = count;
                    for (int i = 0; i <= full; i++) lr.SetPosition(i, path[i]);
                    if (hasPartial)
                        lr.SetPosition(count - 1, Vector3.Lerp(path[full], path[full + 1], partial));
                }
                yield return null;
            }
        }

        foreach (var lr in lrs)
        {
            lr.positionCount = points.Length;
            lr.SetPositions(points);
            lr.loop = closed;
            lr.enabled = true;
        }
    }
}