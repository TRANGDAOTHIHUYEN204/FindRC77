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
    private MiniSecondStateGame state;
    private Color _normalTimerColor;

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
    }

    private void OnScoreChanged(int total, int delta) => textScore.text = total.ToString();

    private void OnPhaseChanged(MiniSecondPhase phase)
    {
        pausePanel.SetActive(phase == MiniSecondPhase.Paused);
        winPanel.SetActive(phase == MiniSecondPhase.Won);
        losePanel.SetActive(phase == MiniSecondPhase.Lost);
        if (phase == MiniSecondPhase.Won)
            RefreshDoubleButton();
    }

    public void LoadSceneHome()
    {
        SceneManager.LoadScene(sceneName);
    }
    private void UpdateText(float t) => timerText.text = TimeManager.Format(t);

    private void OnWarning() => timerText.color = warningColor;
}