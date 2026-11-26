using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

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
    [SerializeField] private float winDelay = 1.5f;
    [SerializeField] private float loseDelay = 1.5f;

    private MiniSecondStateGame state;
    private Color _normalTimerColor;
    private MiniSecondPhase _lastPhase;
    private Coroutine _showRoutine;

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
        StopShowRoutine();
    }

    private void OnDestroy()
    {
        Unbind();
        StopShowRoutine();
    }

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
        StopShowRoutine();
        timerText.color = _normalTimerColor;
        _lastPhase = default;
        HideAllPanels(true); 
    }
    public void ClosePause()
    {
        SetPanel(pausePanel, false);
    }

    [Header("Punch số điểm trên thanh tiến độ")]
    [SerializeField] private float scorePunch = 0.35f;
    [SerializeField] private float scorePunchDuration = 0.3f;

    private void OnScoreChanged(int total, int delta)
    {
        textScore.text = total.ToString();
        if (delta <= 0) return;

        var t = textScore.rectTransform;
        t.DOKill(true);
        t.localScale = Vector3.one;
        t.DOPunchScale(Vector3.one * scorePunch, scorePunchDuration, 6, 0.6f);
    }

    private void OnPhaseChanged(MiniSecondPhase phase)
    {
        StopShowRoutine();
        _showRoutine = StartCoroutine(ShowPanelRoutine(phase));
    }

    private IEnumerator ShowPanelRoutine(MiniSecondPhase phase)
    {
        if (phase != MiniSecondPhase.Paused) SetPanel(pausePanel, false);
        if (phase != MiniSecondPhase.Won)    SetPanel(winPanel, false);
        if (phase != MiniSecondPhase.Lost)   SetPanel(losePanel, false);

        float delay = phase == MiniSecondPhase.Won ? winDelay
                    : phase == MiniSecondPhase.Lost ? loseDelay : 0f;

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        UpdateResultText(phase);

        if (phase == MiniSecondPhase.Paused)
        {
            SetPanel(pausePanel, true);
        }
        else if (phase == MiniSecondPhase.Won)
        {
            RefreshDoubleButton();
            SetPanel(winPanel, true);
        }
        else if (phase == MiniSecondPhase.Lost)
        {
            SetPanel(losePanel, true);
        }

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
            SetPanel(pausePanel, false);
            SetPanel(winPanel, false);
            SetPanel(losePanel, false);
        }
        else
        {
            pausePanel.SetActive(false);
            winPanel.SetActive(false);
            losePanel.SetActive(false);
        }
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
            if (textWin != null) textWin.text = stringWin;
            if (titleWin != null) titleWin.text = stringTitleWin;
        }
        else if (phase == MiniSecondPhase.Lost)
        {
            if (textLose != null) textLose.text = stringLose;
            if (titleLose != null) titleLose.text = stringTitleLose;
        }
    }

    private void OnWarning() => timerText.color = warningColor;
}