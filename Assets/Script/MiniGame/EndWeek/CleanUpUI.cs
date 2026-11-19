using System;
using System.Collections;
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
    [SerializeField] private float winDelay = 1.5f;
    [SerializeField] private float loseDelay = 1.5f;
    [SerializeField] private float pauseDelay = 0f;

    private Color _normalTimerColor;
    private MiniThirdStateGame _state;
    private Coroutine _showRoutine;

    private void Awake()
    {
        _normalTimerColor = timerText.color;
        HideAllPanels(false);
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
        StopShowRoutine();
    }

    public void Bind(MiniThirdStateGame state)
    {
        if (_state != null) _state.OnPhaseChanged -= HandlePhaseChanged;
        _state = state;
        _state.OnPhaseChanged += HandlePhaseChanged;
    }

    public void ResetUI()
    {
        StopShowRoutine();
        timerText.color = _normalTimerColor;
        HideAllPanels(true); // fade out
    }

    public void ShowSavedResult(ThirdResult result)
    {
        StopShowRoutine();
        SetPanel(winPanel, result == ThirdResult.Won);
        SetPanel(losePanel, result == ThirdResult.Lost);
        SetPanel(pauseGame, false);
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
    public void ClosePause()
    {
        SetPanel(pauseGame, false);
    }

    private void HandlePhaseChanged(MiniThirdPhase phase)
    {
        StopShowRoutine();
        _showRoutine = StartCoroutine(ShowPanelRoutine(phase));
    }

    private IEnumerator ShowPanelRoutine(MiniThirdPhase phase)
    {
        if (phase != MiniThirdPhase.Won)    SetPanel(winPanel, false);
        if (phase != MiniThirdPhase.Lost)   SetPanel(losePanel, false);
        if (phase != MiniThirdPhase.Paused) SetPanel(pauseGame, false);

        float delay = phase switch
        {
            MiniThirdPhase.Won => winDelay,
            MiniThirdPhase.Lost => loseDelay,
            MiniThirdPhase.Paused => pauseDelay,
            _ => 0f
        };

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        UpdateResultText(phase);

        if (phase == MiniThirdPhase.Won)
        {
            RefreshDoubleButton();
            SetPanel(winPanel, true);
        }
        else if (phase == MiniThirdPhase.Lost)
        {
            SetPanel(losePanel, true);
        }
        else if (phase == MiniThirdPhase.Paused)
        {
            SetPanel(pauseGame, true);
        }

        _showRoutine = null;
    }

    private void StopShowRoutine()
    {
        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }
    }
    private void SetPanel(GameObject panel, bool show, Action onClosed = null)
    {
        if (!panel)
        {
            if (!show) onClosed?.Invoke();
            return;
        }

        PanelAnimation anim = panel.GetComponentInChildren<PanelAnimation>(true);

        if (anim == null)
        {
            panel.SetActive(show);
            if (!show) onClosed?.Invoke();
            return;
        }

        if (show)
        {
            panel.SetActive(true);
            anim.Show();
        }
        else
        {
            if (!panel.activeSelf)
            {
                onClosed?.Invoke();
                return;
            }

            anim.Close(() =>
            {
                panel.SetActive(false);
                onClosed?.Invoke();
            });
        }
    }

    private void HideAllPanels(bool animated)
    {
        if (animated)
        {
            SetPanel(winPanel, false);
            SetPanel(losePanel, false);
            SetPanel(pauseGame, false);
        }
        else
        {
            winPanel.SetActive(false);
            losePanel.SetActive(false);
            pauseGame.SetActive(false);
        }
    }

    public void LoadHome()
    {
        SceneManager.LoadScene(nameScene);
    }

    private void UpdateResultText(MiniThirdPhase phase)
    {
        if (phase == MiniThirdPhase.Won)
        {
            if (textWin != null) textWin.text = stringWin;
            if (titleWin != null) titleWin.text = stringTitleWin;
        }
        else if (phase == MiniThirdPhase.Lost)
        {
            if (textLose != null) textLose.text = stringLose;
            if (titleLose != null) titleLose.text = stringTitleLose;
        }
    }

    private void UpdateText(float t) => timerText.text = TimeManager.Format(t);
    private void OnWarning() => timerText.color = warningColor;
}