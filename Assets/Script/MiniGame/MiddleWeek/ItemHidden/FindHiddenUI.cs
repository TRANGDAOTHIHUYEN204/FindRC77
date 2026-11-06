using Char.Haptic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FindHiddenUI : MonoBehaviour
{
    [SerializeField] private MiniGameManager game;
    [SerializeField] private FindHiddenLevelSpawner levelSpawner;

    [SerializeField] private Image background;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TextMeshProUGUI textLevel;
    [SerializeField] private string sceneName = "HomeScene";

    private void OnEnable()
    {
        game.OnWin += ShowWin;
        game.OnWrongClick += HandleWrongClick;
        levelSpawner.OnLevelLoaded += OnLevelLoaded;
        HidePanels();
    }

    private void OnDisable()
    {
        game.OnWin -= ShowWin;
        game.OnWrongClick -= HandleWrongClick;
        levelSpawner.OnLevelLoaded -= OnLevelLoaded;
    }

    private void OnLevelLoaded(int _)
    {
        background.sprite = levelSpawner.BackgroundImage;
        UpdateLevelText();
        HidePanels();
    }

    private void ShowWin()
    {
        HidePanels();
        winPanel.SetActive(true);
        Haptics.Light();
    }

    private void HandleWrongClick()
    {
        Haptics.Heavy();
    }

    public void ReturnHome()
    {
        game.SaveProgress();
        SceneManager.LoadScene(sceneName);
    }

    public void OnReplayClicked()
    {
        HidePanels();
        game.StartGame(false);
    }

    public void OnNextLevelClicked()
    {
        if (SaveMiniGame.AllCompleted)
        {
            ReturnHome();
            return;
        }

        levelSpawner.SetLevel(SaveMiniGame.CurrentLevel);
        game.StartGame(false);
    }

    public void ShowPause()
    {
        game.Pause();
        pausePanel.SetActive(true);
    }

    public void ClosePause()
    {
        pausePanel.SetActive(false);
        game.Resume();
    }

    private void UpdateLevelText()
    {
        if (textLevel != null)
            textLevel.text = "Level " +  (levelSpawner.CurrentLevel + 1).ToString();
    }

    private void HidePanels()
    {
        winPanel.SetActive(false);
        pausePanel.SetActive(false);
    }
}