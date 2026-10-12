using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/LevelData")]
public class LevelData : ScriptableObject
{
    [SerializeField]  private int id;
    [SerializeField] private int numberLevel;
    [SerializeField] private int countDifferent;
    [SerializeField] private Sprite mainSprite;
    [SerializeField] private Sprite errorSprite;
    
}
