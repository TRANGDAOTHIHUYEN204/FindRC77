using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHomeManager : MonoBehaviour
{
    [SerializeField] private LevelData[] levels;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Image infoImage;        
    [SerializeField] private Sprite[] sprites;       
    [SerializeField] private Sprite[] spritesInfo;   
    [SerializeField] private TextMeshProUGUI levelDifficulty;

    void Start()
    {
        int index = Mathf.Min(SaveGame.CurrentLevel, levels.Length - 1);
        var level = levels[index];
        int d = (int)level.Difficulty;

        buttonImage.sprite = sprites[d];

        infoImage.sprite = spritesInfo[d];
        var c = infoImage.color;
        c.a = 1f;
        infoImage.color = c;

        levelDifficulty.text = level.Difficulty.ToString();
    }
}