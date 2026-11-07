using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PointSpawnItem", menuName = "Scriptable Objects/PointSpawnItem")]
public class PointSpawnItem : ScriptableObject
{
    [SerializeField] private Vector3[] points;
    public IReadOnlyList<Vector3> Points => points;
}
