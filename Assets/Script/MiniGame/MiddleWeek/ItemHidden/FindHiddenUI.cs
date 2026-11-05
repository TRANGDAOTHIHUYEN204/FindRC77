using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FindHiddenUI : MonoBehaviour
{
    [SerializeField] private MiniGameManager game;
    [SerializeField] private FindHiddenLevelSpawner levelSpawner;

    [SerializeField] private Image background;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private GameObject winPanel;

    private void OnEnable()
    {
        game.OnLivesChanged += UpdateLives;
        game.OnProgress += UpdateProgress;
        game.OnWin += ShowWin;
        levelSpawner.OnLevelLoaded += OnLevelLoaded;
        HidePanels();
    }

    private void OnDisable()
    {
        game.OnLivesChanged -= UpdateLives;
        game.OnProgress -= UpdateProgress;
        game.OnWin -= ShowWin;
        levelSpawner.OnLevelLoaded -= OnLevelLoaded;
    }

    private void OnLevelLoaded(int _)
    {
        background.sprite = levelSpawner.BackgroundImage;
        HidePanels();
    }

    private void UpdateLives(int lives) => livesText.text = lives.ToString();
    private void UpdateProgress(int c, int t) => progressText.text = $"{c}/{t}";
    private void ShowWin() => winPanel.SetActive(true);
    

    private void HidePanels()
    {
        winPanel.SetActive(false);
    }
    
    public void OnReplayClicked() => game.StartGame();

    public void OnNextLevelClicked()
    {
        levelSpawner.SetLevel(levelSpawner.CurrentLevel + 1);
        game.StartGame();
    }
}