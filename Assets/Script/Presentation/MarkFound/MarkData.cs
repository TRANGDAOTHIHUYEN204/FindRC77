using UnityEngine;

public enum ShapeMark
{
    Circle,
    Rectangle
}

[CreateAssetMenu(fileName = "MarkData", menuName = "Scriptable Objects/ MarkData" )]
public class MarkData : ScriptableObject
{
    [SerializeField] private float lineWidth;
    [SerializeField] private Color colorLine;
    [SerializeField, Min(3f)] private int segments;
    [SerializeField] private ShapeMark shapeMark;
    public float LineWidth =>  lineWidth;
    public Color ColorLine => colorLine;
    public int Segments => segments;
    public ShapeMark ShapeMark => shapeMark;

}
