using UnityEngine;

public class DifferentPair
{
    public DifferentPoint PointA { get; }
    public DifferentPoint PointB { get; }
    public bool IsFound { get; private set; }

    public DifferentPair(DifferentPoint a, DifferentPoint b)
    {
        PointA = a;
        PointB = b;
    }

    public bool TryHit(Vector2 worldPoint)
    {
        if (IsFound) return false;
        if (!PointA.IsHit(worldPoint) && !PointB.IsHit(worldPoint)) return false;

        IsFound = true;
        PointA.MarkFound();
        PointB.MarkFound();
        return true;
    }
}