using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CleanUpUI : MonoBehaviour
{
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject pauseGame;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private Button doubleButton;
    [SerializeField] private string nameScene = "HomeScene";
    [SerializeField] private TextMeshProUGUI textWin;
    [SerializeField] private TextMeshProUGUI textLose;
    [SerializeField] private TextMeshProUGUI titleWin;
    [SerializeField] private TextMeshProUGUI titleLose;
    [SerializeField] private string stringWin;
    [SerializeField] private string stringLose;
    [SerializeField] private string stringTitleWin;
    [SerializeField] private string stringTitleLose;
    
    private Color _normalTimerColor;
    private MiniThirdStateGame _state;

    private void Awake()
    {
        _normalTimerColor = timerText.color;
        HideAllPanels();
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
        if (_state != null) _state.OnPhaseChanged -= HandlePhaseChanged;
    }

    public void Bind(MiniThirdStateGame state)
    {
        if (_state != null) _state.OnPhaseChanged -= HandlePhaseChanged;
        _state = state;
        _state.OnPhaseChanged += HandlePhaseChanged;
    }

    public void ResetUI()
    {
        timerText.color = _normalTimerColor;
        HideAllPanels();
    }

    public void ShowSavedResult(ThirdResult result)
    {
        winPanel.SetActive(result == ThirdResult.Won);
        losePanel.SetActive(result == ThirdResult.Lost);
        pauseGame.SetActive(false);
        RefreshDoubleButton();
    }

    public void SetDoubleButtonInteractable(bool value)
    {
        if (doubleButton != null) doubleButton.interactable = value;
    }

    public void RefreshDoubleButton()
    {
        if (doubleButton != null)
            doubleButton.interactable = !SaveGame.IsCleanDoubleClaimed();
    }

    private void HandlePhaseChanged(MiniThirdPhase phase)
    {
        winPanel.SetActive(phase == MiniThirdPhase.Won);
        losePanel.SetActive(phase == MiniThirdPhase.Lost);
        pauseGame.SetActive(phase == MiniThirdPhase.Paused);
        UpdateResultText(phase);
        if (phase == MiniThirdPhase.Won) RefreshDoubleButton();
    }

    public void LoadHome()
    {
        SceneManager.LoadScene(nameScene);
    }

    private void HideAllPanels()
    {
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        pauseGame.SetActive(false);
    }
    private void UpdateResultText(MiniThirdPhase phase)
    {
        if (phase == MiniThirdPhase.Won)
        {
            if (textWin != null)
                textWin.text = stringWin;

            if (titleWin != null)
                titleWin.text = stringTitleWin;
        }
        else if (phase == MiniThirdPhase.Lost)
        {
            if (textLose != null)
                textLose.text = stringLose;

            if (titleLose != null)
                titleLose.text = stringTitleLose;
        }
    }

    private void UpdateText(float t) => timerText.text = TimeManager.Format(t);
    private void OnWarning() => timerText.color = warningColor;
}