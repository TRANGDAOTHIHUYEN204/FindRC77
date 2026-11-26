using System;
using System.Collections;
using UnityEngine;
using Char.Haptic;
using MoreMountains.NiceVibrations;

public class GamePlayImage : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private LevelImageView levelImageView;
    [SerializeField] private DifferentClickHandler clickHandler;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private FactoryGameplay factoryManager;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private StateGame stateGame;
    [SerializeField] private SpawnChallengeNode spawnChallegeNode;
    [SerializeField] private HeartLiveManager heartManager;
    [SerializeField] private HintPresenter hintPresenter;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private MarkPresenter markPresenter;
    [SerializeField] private float beforeBounceDelay = 0.2f;
    [SerializeField] private float bounceDuration = 0.8f;
    [SerializeField] private RewardIntervalWin rewardIntervalWin;
    [SerializeField] private StreakManager streakManager;
    private bool _isPlaying;
    private bool _winStarted;

    private void Start()
    {
        Screen.orientation = ScreenOrientation.Portrait;
    }

    private void OnEnable()
    {
        levelManager.OnLevelLoaded += Build;
        spawnChallegeNode.OnAllChanged += OnAllNodesChanged;
    }

    private void OnDisable()
    {
        levelManager.OnLevelLoaded -= Build;
        spawnChallegeNode.OnAllChanged -= OnAllNodesChanged;
    }

    private void Build(LevelData level)
    {
        StopAllCoroutines();
        _winStarted = false;
        _isPlaying = false;
        rewardIntervalWin.Setup(levelManager.CountLevel);
        clickHandler.OnResetBegin();
        markPresenter.ClearMarks();
        clickHandler.OnResetEnd();
        clickHandler.ResetLoseState();
        hintPresenter.ResetForLevel();
        factoryManager.ReleaseAll();
        heartManager.ReleaseHearts();
        heartManager.ResetLives();
        levelImageView.ShowLevel(level);
        pairManagerHolder.Build(level, levelImageView.RootA, levelImageView.RootB);
        stateGame.Init(level.TimeLimit);
        spawnChallegeNode.Init(level.CountDifferent);
        _isPlaying = true;
    }

    public void ResetPlay()
    {
        StopAllCoroutines();
        _winStarted = false;
        _isPlaying = true;
    }

    private void Update()
    {
        if (!_isPlaying) return;
        if (!stateGame.IsPlaying || UIInputBlocker.IsBlocked) return;
        if (!inputManager.ClickedThisFrame) return;

        if (clickHandler.HandleClick(inputManager.ClickScreenPosition))
        {
            _isPlaying = false;
            stateGame.BeginWin();
        }
    }

    private void OnAllNodesChanged()
    {
        if (_winStarted) return;
        _winStarted = true;
        StartCoroutine(Win());
    }

    private IEnumerator Win()
    {
        _isPlaying = false;

        bool isTutorial = levelManager.IsTutorial;
        string textWin = levelManager.TextWin;
        int levelNumber = levelManager.CurrentIndex + 1;
        
        levelManager.MarkWin();
        if (!isTutorial && streakManager != null) streakManager.OnWin(levelManager.CurrentIndex);

        yield return new WaitForSeconds(beforeBounceDelay);

        Haptics.Play(HapticTypes.Success);
        SoundManager.Instance.PlaySfx(winSound);
        markPresenter.PlayWinBounce();
        spawnChallegeNode.PlayWinBounce();
        yield return new WaitForSeconds(bounceDuration);

        if (isTutorial)
        {
            Debug.Log("[Review] Win TUTORIAL in GamePlay -> mark pending");

            if (GameReviewManager.Instance != null)
                GameReviewManager.Instance.MarkTutorialCompleted();
            else
                Debug.LogWarning("[Review] Manager null at tutorial win");

            stateGame.WinTutorial(textWin);
            yield break;
        }

        bool isGift = rewardIntervalWin.IsGiftLevel(levelNumber);

        stateGame.WinGame(isGift ? string.Empty : textWin);
        rewardIntervalWin.Play(levelNumber);
    }
}