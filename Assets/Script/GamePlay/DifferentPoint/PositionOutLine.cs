using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PositionOutLine", menuName = "Scriptable Objects/PositionOutLine")]
public class PositionOutLine : ScriptableObject
{
    public List<Vector3> points = new List<Vector3>();
}