using UnityEngine;

[CreateAssetMenu(fileName = "CircleDifferentData", menuName = "Scriptable Objects/CircleDifferentData")]
public class CircleDifferentData : ScriptableObject
{
    [SerializeField] private float lineWidth;
    [SerializeField] private Color colorLine;
    [SerializeField, Min(3f)] private int segments;
    
    public float LineWidth =>  lineWidth;
    public Color ColorLine => colorLine;
    public int Segments => segments;

}
