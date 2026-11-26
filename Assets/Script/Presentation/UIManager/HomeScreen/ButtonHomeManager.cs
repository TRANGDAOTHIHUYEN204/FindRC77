using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHomeManager : MonoBehaviour
{
    // Dùng chung catalog với GamePlay để index SaveGame.CurrentLevel trỏ đúng level,
    // và chỉ load level hiện tại thay vì giữ ảnh của mọi level trong RAM.
    [SerializeField] private LevelCatalog catalog;
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
        if (catalog == null || catalog.Count == 0) return;
        int index = Mathf.Clamp(SaveGame.CurrentLevel, 0, catalog.Count - 1);
        LevelData level = catalog[index].Load();
        if (level == null) return;
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