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
    [SerializeField] private TextMeshProUGUI numberLevelText;

    private static readonly Color[] difficultyColors =
    {
        HexToColor("#085712"), 
        HexToColor("#4D1406"), 
        HexToColor("#500C07"), 
    };

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

        levelDifficulty.text = level.Difficulty.ToString().ToUpper();

        numberLevelText.color = difficultyColors[Mathf.Clamp(d, 0, difficultyColors.Length - 1)];
    }

    private static Color HexToColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}