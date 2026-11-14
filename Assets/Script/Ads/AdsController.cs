using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using RCKit.Ads.Main;

public class AdsController : MonoBehaviour
{
    public static AdsController Instance { get; private set; }

    [SerializeField] private AD_TYPE secondAd;
    [SerializeField] private AD_TYPE interAd;
    [SerializeField] private AD_TYPE rewardAd;
    [SerializeField] private float interCooldown = 60f;
    [SerializeField] private float showTimeout = 60f;
    [SerializeField] private float rewardShowTimeout = 120f;
    [SerializeField] private float minGapBetweenAds = 5f;

    public Func<bool> IsBlocked;

    private float _interTimer;
    private bool _gameplayStarted;
    private bool _secondShown;
    private bool _startPressed;
    private bool _sdkInitRequested;

    private bool _interPending;
    private int _showingCount;

    private float _lastAdClosedTime = -999f;
    private const float MinHardTimeout = 300f;

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
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!_gameplayStarted)
            return;

        if (_showingCount > 0 || _interPending)
            return;
        if (IsBlocked != null && IsBlocked())
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

        _startPressed = true;

        Debug.Log("[Ads] First Start level pressed.");

        return true;
    }

    public bool CanShowSecond()
    {
#if ENABLE_RACCOON_ADMOB
        if (_secondShown)
            return false;
        if (_showingCount > 0)
            return false;

        if (Time.unscaledTime - _lastAdClosedTime < minGapBetweenAds)
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

        if (_showingCount > 0)
        {
            Debug.LogWarning("[Ads] Another ad is showing. Skip Second.");
            onContinue?.Invoke();
            return;
        }

        TryShow(
            secondAd,
            true,
            shown =>
            {
                Debug.Log($"[Ads] Second Ad Result: {shown}");
                onContinue?.Invoke();
            },
            showTimeout,
            onStart: () => _secondShown = true);
    }

    public bool CanRequestInter()
    {
        if (!_gameplayStarted)
            return false;
        
        if (IsBlocked != null && IsBlocked())
            return false;

#if TEST_FAST_INTERVAL_INTER
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
            Debug.LogWarning("[Ads] Inter already pending.");
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

        TryShow(
            interAd,
            true,
            shown =>
            {
                Debug.Log($"[Ads] Inter Ad Result: {shown}");

                _interPending = false;

                if (shown)
                {
                    _interTimer = 0f;
                    Debug.Log("[Ads] Inter closed. Timer reset = 0.");
                }

                PreloadInter();

                onContinue?.Invoke();
            },
            showTimeout
        );
    }

    public void RequestReward(Action onRewarded, Action onFailed = null)
    {
        Debug.Log("[Ads] RequestReward CALLED.");

        if (_showingCount > 0)
        {
            Debug.LogWarning("[Ads] Another ad is showing. Skip Reward.");
            onFailed?.Invoke();
            return;
        }

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

    private void TryShow(
        AD_TYPE type,
        bool requireReady,
        Action<bool> onDone,
        float timeout = 60f,
        Action onStart = null)
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
            _lastAdClosedTime = Time.unscaledTime;

            Debug.Log($"[Ads] Show result: {type} = {ok}");

            onDone?.Invoke(ok);
        }
        float hardTimeout = timeout > 0f ? Mathf.Max(timeout, MinHardTimeout) : 0f;

        if (hardTimeout > 0f)
            watchdog = StartCoroutine(CoWatchdog(hardTimeout, type, () => finished, () => Finish(false)));

        try
        {
            onStart?.Invoke();

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
    
    private IEnumerator CoWatchdog(float seconds, AD_TYPE type, Func<bool> isFinished, Action onTimeout)
    {
        yield return new WaitForSecondsRealtime(seconds);

        while (!isFinished() && IsGateShowing())
        {
            Debug.Log($"[Ads] Watchdog: {type} still showing, keep waiting.");
            yield return new WaitForSecondsRealtime(1f);
        }

        if (isFinished())
            yield break;
        yield return new WaitForSecondsRealtime(1f);

        if (isFinished())
            yield break;

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