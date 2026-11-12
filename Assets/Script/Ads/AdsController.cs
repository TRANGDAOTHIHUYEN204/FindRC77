using System;
using System.Collections;
using UnityEngine;
using RCKit.Ads.Main;

public class AdsController : MonoBehaviour
{
    public static AdsController Instance { get; private set; }

    [SerializeField] private AD_TYPE secondAd;
    [SerializeField] private AD_TYPE interAd;
    [SerializeField] private AD_TYPE rewardAd;
    [SerializeField] private AD_TYPE resumeAd;
    [SerializeField] private float interCooldown = 60f;
    [SerializeField] private GameObject resumeLoadingPanel;
    [SerializeField] private float showTimeout = 60f;
    [SerializeField] private float rewardShowTimeout = 120f;

    public Func<bool> CanShowResumeAd;

    private float _interTimer;
    private bool _gameplayStarted;
    private bool _secondShown;
    private bool _startPressed;
    private bool _sdkInitRequested;

    private bool _interPending;
    private int _showingCount;

    private bool _wasBackgrounded;
    private bool _resumeLoading;
    private float _prevTimeScale = 1f;
    private Coroutine _resumeRoutine;

    public float InterTimer => _interTimer;
    public bool GameplayStarted => _gameplayStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[Ads] Duplicate AdsController destroyed.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[Ads] AdsController Awake.");
    }

    private void OnDestroy()
    {
        if (_resumeRoutine != null)
            StopCoroutine(_resumeRoutine);

        if (_resumeLoading)
            SetResumeLoading(false);

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!_gameplayStarted)
            return;

        if (_showingCount > 0 || _interPending)
            return;

        _interTimer += Time.unscaledDeltaTime;
    }
    
    public void OnFirstGameplayStarted()
    {
        if (_gameplayStarted)
            return;

        _gameplayStarted = true;
        _interTimer = 0f;

        Debug.Log("[Ads] First gameplay started. Inter timer = 0.");

        PreloadInter();
        PreloadReward();
    }

    public void InitializeSdk()
    {
#if ENABLE_RACCOON_ADMOB
        if (_sdkInitRequested)
        {
            Debug.Log("[Ads] SDK init already requested. Skip.");
            return;
        }

        var mgr = RaccoonAdsManager.Instance;

        if (mgr == null)
        {
            Debug.LogError("[Ads] RaccoonAdsManager.Instance is NULL!");
            return;
        }

        _sdkInitRequested = true;

        Debug.Log("[Ads] Initialize SDK.");

        mgr.Initialize(OnSdkInitialized);
#else
        Debug.LogError("[Ads] ENABLE_RACCOON_ADMOB is NOT defined!");
#endif
    }

    private void OnSdkInitialized()
    {
        Debug.Log("[Ads] SDK initialized.");

        PreloadSecond();
        PreloadInter();

        if (_gameplayStarted)
            PreloadReward();
    }

    public void OnLoadingFinished()
    {
        Debug.Log("[Ads] OnLoadingFinished CALLED.");

#if ENABLE_RACCOON_ADMOB
        var mgr = RaccoonAdsManager.Instance;

        if (mgr == null)
        {
            Debug.LogError("[Ads] OnLoadingFinished: Manager NULL.");
            return;
        }

        mgr.OnLoadingSceneLoaded();
#endif
    }

    public void PreloadInter()
    {
#if ENABLE_RACCOON_ADMOB
        var mgr = RaccoonAdsManager.Instance;

        if (mgr == null || !mgr.Initialized || mgr.IsAdsRemoved)
            return;

        if (_interPending)
            return;

        if (mgr.AdsIsReady(interAd))
            return;

        Debug.Log("[Ads] Preload Inter.");

        mgr.PreloadAdsWaitShow(interAd);
#endif
    }

    public void PreloadReward()
    {
#if ENABLE_RACCOON_ADMOB
        var mgr = RaccoonAdsManager.Instance;

        if (mgr == null || !mgr.Initialized || mgr.IsAdsRemoved)
            return;

        if (mgr.AdsIsReady(rewardAd))
            return;

        Debug.Log("[Ads] Preload Reward.");

        mgr.PreloadAdsWaitShow(rewardAd);
#endif
    }

    public void PreloadSecond()
    {
#if ENABLE_RACCOON_ADMOB
        if (_secondShown)
            return;

        var mgr = RaccoonAdsManager.Instance;

        if (mgr == null || !mgr.Initialized || mgr.IsAdsRemoved)
            return;

        if (mgr.AdsIsReady(secondAd))
            return;

        Debug.Log("[Ads] Preload Second.");

        mgr.PreloadAdsWaitShow(secondAd);
#endif
    }
    
    public bool ConsumeFirstStart()
    {
        if (_startPressed)
            return false;

        bool can = CanShowSecond();
        if (can) _startPressed = true;

        Debug.Log($"[Ads] First Start pressed. Can show second: {can}");
        return can;
    }

    public bool CanShowSecond()
    {
#if ENABLE_RACCOON_ADMOB
        if (_secondShown)
            return false;

        var mgr = RaccoonAdsManager.Instance;

        if (mgr == null || !mgr.Initialized || mgr.IsAdsRemoved)
            return false;

        return mgr.AdsIsReady(secondAd);
#else
        return false;
#endif
    }

    public void RequestSecond(Action onContinue)
    {
        Debug.Log("[Ads] RequestSecond CALLED.");

        if (_secondShown)
        {
            Debug.Log("[Ads] Second already shown.");
            onContinue?.Invoke();
            return;
        }

        TryShow(secondAd, true, shown =>
        {
            Debug.Log($"[Ads] Second Ad Result: {shown}");

            if (shown)
                _secondShown = true;

            onContinue?.Invoke();
        }, showTimeout);
    }

    public bool CanRequestInter()
    {
        if (!_gameplayStarted)
            return false;
#if  TEST_FAST_INTERVAL_INTER
          if (_interTimer < 10)
            return false;
#else
        if (_interTimer < interCooldown)
            return false;
#endif
        if (_interPending || _showingCount > 0)
            return false;

#if ENABLE_RACCOON_ADMOB
        var mgr = RaccoonAdsManager.Instance;

        if (mgr == null || !mgr.Initialized || mgr.IsAdsRemoved)
            return false;

        return mgr.AdsIsReady(interAd);
#else
        return false;
#endif
    }

    public void RequestInter(Action onContinue)
    {
        Debug.Log($"[Ads] RequestInter. Timer = {_interTimer:F1}s");

        if (_interPending)
        {
            Debug.LogWarning("[Ads] Inter already pending. Skip duplicate.");
            onContinue?.Invoke();
            return;
        }

        if (!CanRequestInter())
        {
            Debug.Log("[Ads] Inter conditions not met. Skip.");

            PreloadInter();
            onContinue?.Invoke();
            return;
        }

        _interPending = true;

        TryShow(interAd, true, shown =>
        {
            Debug.Log($"[Ads] Inter Ad Result: {shown}");

            if (shown)
            {
                _interTimer = 0f;
                Debug.Log("[Ads] Inter completed. Timer reset = 0.");
            }

            _interPending = false;

            PreloadInter();

            onContinue?.Invoke();
        }, showTimeout);
    }
    

    public void RequestReward(Action onRewarded, Action onFailed = null)
    {
        Debug.Log("[Ads] RequestReward CALLED.");

        TryShow(rewardAd, false, ok =>
        {
            Debug.Log($"[Ads] Reward Ad Result: {ok}");

            if (ok)
                onRewarded?.Invoke();
            else
                onFailed?.Invoke();

            PreloadReward();
        }, rewardShowTimeout);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            _wasBackgrounded = !(
                _showingCount > 0 ||
                _resumeLoading ||
                IsGateShowing()
            );
            return;
        }

        if (!_wasBackgrounded)
            return;

        _wasBackgrounded = false;

        if (_resumeRoutine != null)
            return;

        if (CanShowResumeAd != null && !CanShowResumeAd())
            return;

        _resumeRoutine = StartCoroutine(CoShowResumeAd());
    }

    private IEnumerator CoShowResumeAd()
    {
        yield return new WaitForSecondsRealtime(0.1f);

#if ENABLE_RACCOON_ADMOB
        var mgr = RaccoonAdsManager.Instance;

        if (mgr == null || !mgr.Initialized || mgr.IsAdsRemoved)
        {
            _resumeRoutine = null;
            yield break;
        }

        if (!mgr.AdsIsReady(resumeAd))
        {
            Debug.Log("[Ads] Resume ad not ready. Preload.");

            mgr.PreloadAdsWaitShow(resumeAd);
            _resumeRoutine = null;
            yield break;
        }
#endif

        SetResumeLoading(true);

        Debug.Log("[Ads] Show Resume Ad.");

        TryShow(resumeAd, true, shown =>
        {
            Debug.Log($"[Ads] Resume Ad Result: {shown}");

            SetResumeLoading(false);
            _resumeRoutine = null;
        }, showTimeout);
    }

    private void SetResumeLoading(bool show)
    {
        if (show == _resumeLoading)
            return;

        _resumeLoading = show;

        if (resumeLoadingPanel)
            resumeLoadingPanel.SetActive(show);

        if (show)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = _prevTimeScale;
        }
    }
    

    private void TryShow(
        AD_TYPE type,
        bool requireReady,
        Action<bool> onDone,
        float timeout = 60f)
    {
        Debug.Log($"[Ads] TryShow: {type}");

#if ENABLE_RACCOON_ADMOB
        var mgr = RaccoonAdsManager.Instance;

        if (mgr == null || !mgr.Initialized || mgr.IsAdsRemoved)
        {
            Debug.LogWarning("[Ads] SDK unavailable or ads removed.");
            onDone?.Invoke(false);
            return;
        }

        if (requireReady && !mgr.AdsIsReady(type))
        {
            Debug.LogWarning($"[Ads] Ad NOT READY: {type}");

            mgr.PreloadAdsWaitShow(type);
            onDone?.Invoke(false);
            return;
        }

        _showingCount++;

        bool finished = false;
        Coroutine watchdog = null;

        void Finish(bool ok)
        {
            if (finished)
                return;

            finished = true;

            if (watchdog != null)
            {
                StopCoroutine(watchdog);
                watchdog = null;
            }

            _showingCount = Mathf.Max(0, _showingCount - 1);

            Debug.Log($"[Ads] Show result: {type} = {ok}");

            onDone?.Invoke(ok);
        }
        
        if (timeout > 0f)
            watchdog = StartCoroutine(CoWatchdog(timeout, type, () => Finish(false)));

        try
        {
            mgr.ShowAd(type, Finish);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Finish(false);
        }
#else
        Debug.LogError("[Ads] ENABLE_RACCOON_ADMOB is NOT defined.");
        onDone?.Invoke(false);
#endif
    }

    private IEnumerator CoWatchdog(float seconds, AD_TYPE type, Action onTimeout)
    {
        yield return new WaitForSecondsRealtime(seconds);

        Debug.LogWarning($"[Ads] Watchdog timeout ({seconds}s): {type}. Force finish.");
        onTimeout?.Invoke();
    }

    private bool IsGateShowing()
    {
#if ENABLE_RACCOON_ADMOB
        return AdLoadingGate.Instance != null &&
               AdLoadingGate.Instance.IsAdShowing;
#else
        return false;
#endif
    }
}