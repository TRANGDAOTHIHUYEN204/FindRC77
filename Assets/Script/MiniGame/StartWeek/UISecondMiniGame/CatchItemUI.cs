using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CatchItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textScore;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private Button doubleRewardButton;
    [SerializeField] private string sceneName = "HomeScene";

    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip loseClip;
    [Range(0f, 1f)] [SerializeField] private float winVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float loseVolume = 1f;
    [SerializeField] private TextMeshProUGUI textWin;
    [SerializeField] private TextMeshProUGUI textLose;
    [SerializeField] private TextMeshProUGUI titleWin;
    [SerializeField] private TextMeshProUGUI titleLose;
    [SerializeField] private string stringWin;
    [SerializeField] private string stringLose;
    [SerializeField] private string stringTitleWin;
    [SerializeField] private string stringTitleLose;
    private MiniSecondStateGame state;
    private Color _normalTimerColor;
    private MiniSecondPhase _lastPhase;

    private void Awake() => _normalTimerColor = timerText.color;

    public void Bind(MiniSecondStateGame s)
    {
        Unbind();
        state = s;
        state.OnScoreChanged += OnScoreChanged;
        state.OnPhaseChanged += OnPhaseChanged;
        textScore.text = state.TotalScore.ToString();
    }

    private void Unbind()
    {
        if (state == null) return;
        state.OnScoreChanged -= OnScoreChanged;
        state.OnPhaseChanged -= OnPhaseChanged;
        state = null;
    }

    private void OnEnable()
    {
        timeManager.OnTimeChanged += UpdateText;
        timeManager.OnWarningStart += OnWarning;
    }

    private void OnDisable()
    {
        timeManager.OnTimeChanged -= UpdateText;
        timeManager.OnWarningStart -= OnWarning;
    }

    private void OnDestroy() => Unbind();

    public void SetDoubleButtonInteractable(bool value)
    {
        if (doubleRewardButton != null)
            doubleRewardButton.interactable = value;
    }

    public void RefreshDoubleButton()
    {
        if (doubleRewardButton == null) return;
        doubleRewardButton.gameObject.SetActive(true);
        doubleRewardButton.interactable = !SaveGame.IsCatchDoubleClaimed();
    }

    public void ResetUI()
    {
        timerText.color = _normalTimerColor;
        _lastPhase = default;
    }

    private void OnScoreChanged(int total, int delta) => textScore.text = total.ToString();
    private void OnPhaseChanged(MiniSecondPhase phase)
    {
        Debug.Log($"[CatchUI] Phase={phase}, last={_lastPhase}, completedBefore={SaveSecondMiniGame.Completed}");

        pausePanel.SetActive(phase == MiniSecondPhase.Paused);
        winPanel.SetActive(phase == MiniSecondPhase.Won);
        losePanel.SetActive(phase == MiniSecondPhase.Lost);

        UpdateResultText(phase);

        if (phase == MiniSecondPhase.Won)
            RefreshDoubleButton();

        if (phase != _lastPhase)
        {
            if (phase == MiniSecondPhase.Won)
            {
                PlaySfx(winClip, winVolume);
                SaveSecondMiniGame.Completed = true;
            }
            else if (phase == MiniSecondPhase.Lost)
            {
                PlaySfx(loseClip, loseVolume);
                SaveSecondMiniGame.Completed = true;
            }
        }

        _lastPhase = phase;
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null || SoundManager.Instance == null) return;
        SoundManager.Instance.PlaySfx(clip, volume);
    }

    public void LoadSceneHome()
    {
        SceneManager.LoadScene(sceneName);
    }

    private void UpdateText(float t) => timerText.text = TimeManager.Format(t);
    private void UpdateResultText(MiniSecondPhase phase)
    {
        if (phase == MiniSecondPhase.Won)
        {
            if (textWin != null)
                textWin.text = stringWin;

            if (titleWin != null)
                titleWin.text = stringTitleWin;
        }
        else if (phase == MiniSecondPhase.Lost)
        {
            if (textLose != null)
                textLose.text = stringLose;

            if (titleLose != null)
                titleLose.text = stringTitleLose;
        }
    }
    private void OnWarning() => timerText.color = warningColor;
}