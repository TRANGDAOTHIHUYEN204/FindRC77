using TMPro;
using UnityEngine;

public enum GameState { Playing, Paused, Won, Lost }

public class StateGame : MonoBehaviour
{
    public static StateGame Instance { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private TextMeshProUGUI currentTimeText;
    public GameState State { get; private set; } = GameState.Paused; 
    public float TimeLeft { get; private set; }
    public float TimeLimit { get; private set; }
    [SerializeField] private TextMeshProUGUI winText;
    public bool IsPlaying => State == GameState.Playing;
    [SerializeField] private TimeOutline outline;
    private int _lastShownSecond = -1;
    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }
    
    public void Init(float timeLimit)
    {
        TimeLimit = timeLimit;
        TimeLeft = timeLimit;
        State = GameState.Playing;
        Time.timeScale = 1f;
        SetPanels(false, false, false);
        if (outline) outline.SetFill(1f); 
        _lastShownSecond = -1;
        UpdateTimeText();
    }

    private void Update()
    {
        if (State != GameState.Playing) return;

        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            LoseGame();
        }
        if (outline) outline.SetFill(TimeLeft / TimeLimit);
        UpdateTimeText();
    }
    private void UpdateTimeText()
    {
        if (!currentTimeText) return;

        int sec = Mathf.CeilToInt(TimeLeft);
        if (sec == _lastShownSecond) return; 
        _lastShownSecond = sec;
        currentTimeText.text = $"{sec / 60}:{sec % 60:00}";
    }
    public void PauseGame()
    {
        if (State != GameState.Playing) return;
        Debug.Log("PauseGame called!\n" + System.Environment.StackTrace);
        State = GameState.Paused;
        SetPanels(true, false, false);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Debug.Log($"ResumeGame called, State = {State}");
        if (State != GameState.Paused) return;
        Time.timeScale = 1f;
        SetPanels(false, false, false);
        State = GameState.Playing;
    }

    public void WinGame(string textWin)
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        SetPanels(false, true, false);
        winText.text = textWin;
        Time.timeScale = 0f;
    }

    public void LoseGame()
    {
        if (State != GameState.Playing) return;
        State = GameState.Lost;
        SetPanels(false, false, true);
        Time.timeScale = 0f;
    }

    private void SetPanels(bool pause, bool win, bool lose)
    {
        if (pausePanel) pausePanel.SetActive(pause);
        if (winPanel) winPanel.SetActive(win);
        if (losePanel) losePanel.SetActive(lose);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) PauseGame();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}