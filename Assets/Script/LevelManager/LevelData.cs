using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : ScriptableObject
{
    [SerializeField] private int numberLevel;
    [SerializeField] private int countDifferent;
    [SerializeField] private Sprite rawSprite;
    [SerializeField] private Sprite errorSprite;
    [SerializeField] private float timeLimit;
    [SerializeField] private MarkData[] _markData;
    [SerializeField] private List<DifferentPointInfo> differentPoints;

    public int NumberLevel => numberLevel;
    public int CountDifferent => countDifferent;
    public Sprite RawSprite => rawSprite;
    public Sprite ErrorSprite => errorSprite;
    public float TimeLimit => timeLimit;
    public MarkData[] MarkData => _markData;
    public   List<DifferentPointInfo> DifferentPoints => differentPoints;
#if UNITY_EDITOR
    public void SetDifferentPoints(List<DifferentPointInfo> points)
    {
        differentPoints = points;
        countDifferent = points.Count;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}