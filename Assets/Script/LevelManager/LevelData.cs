using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : ScriptableObject
{
    [SerializeField] private int numberLevel;
    [SerializeField] private int countDifferent;
    [SerializeField] private Sprite rawSprite;
    [SerializeField] private Sprite errorSprite;
    [SerializeField] private float timeLimit;
    [SerializeField] private MarkData[] _markData;


    public int NumberLevel => numberLevel;
    public int CountDifferent => countDifferent;
    public Sprite RawSprite => rawSprite;
    public Sprite ErrorSprite => errorSprite;
    public float TimeLimit => timeLimit;
    public MarkData[] MarkData => _markData;

}