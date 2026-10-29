using TMPro;
using UnityEngine;
using System.Collections;

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
    [SerializeField] private AudioClip clickWarningSound;
    [SerializeField] private TextMeshProUGUI loseText;
    private int _lastBeepSecond = -1;
    [SerializeField] private TextMeshProUGUI loseLevelText;
    [SerializeField] private TextMeshProUGUI winLevelText;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private TextMeshProUGUI levelText;
    private string _levelLabel;
    [SerializeField] private GameObject winTutorial;
    [SerializeField] private int warningStartSecond = 10;
    [SerializeField, Range(0f, 1f)] private float warningMinVolume = 0.3f;
    [SerializeField, Range(0f, 1f)] private float warningMaxVolume = 1f;
    [SerializeField] private AudioClip loseGameSound;
    [SerializeField] private string loseTextGame;
    [SerializeField] private HeartLiveManager heartManager;
    [SerializeField] private MarkPresenter markPresenter;
    [SerializeField] private DifferentPairManagerHolder pairHolder;
    [SerializeField] private TextMeshProUGUI winTutorialText;
    [SerializeField] private TextMeshProUGUI winTutorialLevelText;  
    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        SetText();
    }

    public void Init(float timeLimit)
    {
        TimeLimit = timeLimit;
        TimeLeft = timeLimit;
        State = GameState.Playing;
        Time.timeScale = 1f;
        SetPanels(false, false, false, false);
        if (outline) outline.SetFill(1f);
        _lastShownSecond = -1;
        _lastBeepSecond = -1;

        _levelLabel = GetLevelLabel();
        SetText();
        UpdateTimeText();
    }
    public void WinTutorial(string textWin)
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        SetPanels(false, false, false, true);
        if (winTutorialText) winTutorialText.text = textWin;
        Time.timeScale = 0f;
    }
    
    public void WinGame(string textWin)
    {
        if (State != GameState.Playing) return;
        State = GameState.Won;
        SetPanels(false, true, false, false);
        winText.text = textWin;
        Time.timeScale = 0f;
    }

    public void LoseGame()
    {
        if (State != GameState.Playing) return;
        State = GameState.Lost;
        SoundManager.Instance.PlaySfx(loseGameSound);
        SetPanels(false, false, true, false);
        loseText.text = loseTextGame;
        Time.timeScale = 0f;
    }

    public void SetText()
    {
        if (levelText) levelText.text = _levelLabel;
        if (winLevelText) winLevelText.text = _levelLabel;
        if (loseLevelText) loseLevelText.text = _levelLabel;
        if (winTutorialLevelText) winTutorialLevelText.text = _levelLabel;
    }

    private void Update()
    {
        if (State != GameState.Playing) return;
        if (levelManager.IsTutorial) return;
        TimeLeft -= Time.deltaTime;

        int sec = Mathf.CeilToInt(TimeLeft);
        if (sec <= warningStartSecond && sec > 0 && sec != _lastBeepSecond)
        {
            _lastBeepSecond = sec;
            float t = (warningStartSecond - sec) / (float)(warningStartSecond - 1);
            float volume = Mathf.Lerp(warningMinVolume, warningMaxVolume, t);

            SoundManager.Instance.PlaySfx(clickWarningSound, volume);
        }

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
        SetPanels(true, false, false, false);
        Time.timeScale = 0f;
    }
    [SerializeField] private SpawnChallengeNode challengeNodes;

    public void ResetGame()
    {
        Time.timeScale = 1f;

        markPresenter.ClearMarks();
        pairHolder.Rebuild();
        heartManager.ResetLives();
        challengeNodes.ResetNodes();

        Init(levelManager.TimeLimit);
    }

    public void ResumeGame()
    {
        Debug.Log($"ResumeGame called, State = {State}");
        if (State != GameState.Paused) return;
        Time.timeScale = 1f;
        SetPanels(false, false, false, false);
        State = GameState.Playing;
    }

    private string GetLevelLabel()
    {
        if (levelManager.IsTutorial) return "WELCOME";
        return "LEVEL " + (SaveGame.CurrentLevel + 1);
    }

    private void SetPanels(bool pause, bool win, bool lose, bool winTutorialLevel)
    {
        SetPanel(pausePanel, pause);
        SetPanel(winPanel, win);
        SetPanel(losePanel, lose);
        SetPanel(winTutorial,  winTutorialLevel);
    }

    private void SetPanel(GameObject panel, bool show)
    {
        if (!panel) return;

        var anim = panel.GetComponentInChildren<PanelAnimation>(true);
        if (anim == null)
        {
            panel.SetActive(show);
            return;
        }

        if (show)
        {
            panel.SetActive(true);
            anim.Show();
        }
        else
        {
            if (!panel.activeSelf) return;
            anim.Close(() => panel.SetActive(false));
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && !levelManager.IsTutorial) PauseGame();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}