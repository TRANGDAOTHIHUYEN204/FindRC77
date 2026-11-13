using System;
using System.Collections.Generic;
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
    [SerializeField] private ScrollRect[] scrollRects;
    [SerializeField] private TextMeshProUGUI numberFiddenHide;
    [SerializeField] private TextMeshProUGUI textWinEvent;
    [SerializeField] private Button doubleRewardButton;

    private bool _transitioning;
    private bool _adPending;
    private bool _doubleClaimed;

    private readonly Dictionary<FindHiddenData, (int found, int total)> _progress = new();
    private int _foundTotal;
    private int _needTotal;
    private int _currentLevel;
    private void OnEnable()
    {
        game.OnWin += ShowWin;
        game.OnWrongClick += HandleWrongClick;
        levelSpawner.OnLevelLoaded += OnLevelLoaded;
        levelSpawner.OnTypeProgress += OnTypeProgress;

        HidePanels();
        SetScrollEnabled(true);
    }
    private void OnLevelLoaded(int level)
    {
        _currentLevel = level;
        background.sprite = levelSpawner.BackgroundImage;
        UpdateLevelText();

        RecalculateProgress();
        UpdateProgressText();

        RefreshDoubleButton();
        HidePanels();
        SetScrollEnabled(true);
    }

    private void ShowWin()
    {
        HidePanels();
        SetScrollEnabled(false);

        Debug.Log($"[Event] Before reward: {SaveGame.HintBalance}");

        bool claimed = SaveGame.TryClaimFindHiddenWin(_currentLevel);

        Debug.Log(
            $"[Event] Claimed: {claimed}, After reward: {SaveGame.HintBalance}"
        );

        RefreshDoubleButton();
        SetPanel(winPanel, true);

        Haptics.Light();
    }

    public void OnDoubleRewardClicked()
    {
        if (_adPending || SaveGame.IsFindHiddenDoubleClaimed(_currentLevel)) return;

        var ads = AdsController.Instance;
        if (ads == null)
        {
            Debug.LogWarning("[FindHidden] AdsController null.");
            return;
        }

        int level = _currentLevel;
        _adPending = true;
        SetDoubleButtonInteractable(false);

        ads.RequestReward(
            onRewarded: () =>
            {
                if (this == null) return;
                _adPending = false;

                if (SaveGame.TryClaimFindHiddenDouble(level))
                    Debug.Log($"[FindHidden] Double reward level {level} | +1 hint");

                RefreshDoubleButton();
            },
            onFailed: () =>
            {
                if (this == null) return;
                _adPending = false;
                SetDoubleButtonInteractable(true);
                Debug.Log("[FindHidden] Reward ad not ready / cancelled.");
            });
    }
    
    private void RefreshDoubleButton()
    {
        if (doubleRewardButton == null) return;

        bool claimed = SaveGame.IsFindHiddenDoubleClaimed(_currentLevel);

        doubleRewardButton.gameObject.SetActive(true);
        doubleRewardButton.interactable = !claimed && !_adPending;
    }
    
    private void OnDisable()
    {
        game.OnWin -= ShowWin;
        game.OnWrongClick -= HandleWrongClick;
        levelSpawner.OnLevelLoaded -= OnLevelLoaded;
        levelSpawner.OnTypeProgress -= OnTypeProgress;

        SetScrollEnabled(true);
    }
    
    private void OnTypeProgress(FindHiddenData data, int found, int total)
    {
        if (data == null) return;

        _progress[data] = (found, total);

        RecalculateProgress();
        UpdateProgressText();
    }

    private void RecalculateProgress()
    {
        _foundTotal = 0;
        _needTotal = 0;
        var currentTypes = new HashSet<FindHiddenData>();
        var level = levelSpawner.CurrentLevelData;

        if (level != null && level.Entries != null)
        {
            foreach (var entry in level.Entries)
            {
                if (entry.data != null)
                    currentTypes.Add(entry.data);
            }
        }

        foreach (var kv in _progress)
        {
            if (!currentTypes.Contains(kv.Key)) continue;

            _foundTotal += kv.Value.found;
            _needTotal += kv.Value.total;
        }
    }

    private void UpdateProgressText()
    {
        if (numberFiddenHide != null)
            numberFiddenHide.text = $"{_foundTotal}/{_needTotal}";
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
        if (_transitioning) return;

        _transitioning = true;

        SetPanel(winPanel, false, () =>
        {
            _transitioning = false;
            SetScrollEnabled(true);
            game.StartGame(false);
        });
    }

    public void OnNextLevelClicked()
    {
        if (_transitioning) return;

        if (SaveMiniGame.AllCompleted)
        {
            ReturnHome();
            return;
        }

        _transitioning = true;

        SetPanel(winPanel, false, () =>
        {
            _transitioning = false;
            SetScrollEnabled(true);

            levelSpawner.SetLevel(SaveMiniGame.CurrentLevel);
            game.StartGame(false);
        });
    }
    
    

    private void SetDoubleButtonInteractable(bool value)
    {
        if (doubleRewardButton != null)
            doubleRewardButton.interactable = value;
    }

    private void ResetDoubleReward()
    {
        _adPending = false;
        _doubleClaimed = false;

        if (doubleRewardButton != null)
        {
            doubleRewardButton.gameObject.SetActive(true);
            doubleRewardButton.interactable = true;
        }
    }

    public void ShowPause()
    {
        if (_transitioning || pausePanel.activeSelf) return;

        SetScrollEnabled(false);
        SetPanel(pausePanel, true);

        game.Pause();
    }

    public void ClosePause()
    {
        if (!pausePanel.activeSelf) return;

        game.Resume();

        SetPanel(pausePanel, false, () =>
        {
            SetScrollEnabled(true);
        });
    }

    private void SetPanel(
        GameObject panel,
        bool show,
        Action onClosed = null)
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

    private void HidePanels()
    {
        _transitioning = false;

        winPanel.SetActive(false);
        pausePanel.SetActive(false);
    }

    private void SetScrollEnabled(bool value)
    {
        if (scrollRects == null) return;

        foreach (ScrollRect sr in scrollRects)
        {
            if (sr == null) continue;

            if (!value)
            {
                sr.StopMovement();
                sr.velocity = Vector2.zero;
            }

            sr.enabled = value;
        }
    }

    private void UpdateLevelText()
    {
        if (textLevel != null)
            textLevel.text = "Zoo tour completed!";
        if (textWinEvent != null)
            textWinEvent.text = "You have completed your zoo tour. Thanks for visiting our friendly animals!";
    }
}