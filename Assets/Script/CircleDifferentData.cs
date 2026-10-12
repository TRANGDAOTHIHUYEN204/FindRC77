using UnityEngine;

[CreateAssetMenu(fileName = "CircleDifferentData", menuName = "Scriptable Objects/CircleDifferentData")]
public class CircleDifferentData : ScriptableObject
{
    [SerializeField] private float radius;
    [SerializeField] private float lineWidth;
    [SerializeField] private Color colorLine;
    [SerializeField] private int segments;
    
}
