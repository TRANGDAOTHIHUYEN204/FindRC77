using UnityEngine;

public enum GameState { Playing, Paused, Won, Lost }

public class StateGame : MonoBehaviour
{
    public static StateGame Instance { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    public GameState State { get; private set; } = GameState.Paused; 
    public float TimeLeft { get; private set; }
    public float TimeLimit { get; private set; }

    public bool IsPlaying => State == GameState.Playing;

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
    }

    public void PauseGame()
    {
        if (State != GameState.Playing) return;
        State = GameState.Paused;
        SetPanels(true, false, false);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (State != GameState.Paused) return;
        Time.timeScale = 1f;
        SetPanels(false, false, false);
        State = GameState.Playing;
    }

    public void WinGame()
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        SetPanels(false, true, false);
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