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
    [SerializeField] private MarkData[] markData;
    [SerializeField] private List<DifferentPointInfo> differentPoints;
    [TextArea(3, 10)] [SerializeField] private string textWin;
    public int NumberLevel => numberLevel;
    public int CountDifferent => countDifferent;
    public Sprite RawSprite => rawSprite;
    public Sprite ErrorSprite => errorSprite;
    public float TimeLimit => timeLimit;
    public MarkData[] MarkData => markData;
    public List<DifferentPointInfo> DifferentPoints => differentPoints;
    public string TextWin => textWin;
#if UNITY_EDITOR
    public void SetDifferentPoints(List<DifferentPointInfo> points)
    {
        differentPoints = points;
        countDifferent = points.Count;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}