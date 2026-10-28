using UnityEngine;

[System.Serializable]
public class DifferentPointInfo
{
    public Vector3 Position;
    public Vector2 Size;
    public Vector3[] Outline;  
    public bool HasMarkCenter;
    public Vector3 MarkCenter;  
    
    public Vector3[] GetScaledOutline(Vector2 scale)
    {
        if (Outline == null || Outline.Length < 2) return null;

        var pts = new Vector3[Outline.Length];
        for (int i = 0; i < Outline.Length; i++)
            pts[i] = new Vector3(Outline[i].x * scale.x, Outline[i].y * scale.y, 0f);
        return pts;
    }
}