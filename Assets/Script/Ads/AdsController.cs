using System;
using System.Collections;
using UnityEngine;
using RCKit.Ads.Main;

public class AdsController : MonoBehaviour
{
    public static AdsController Instance { get; private set; }

    [SerializeField] private AD_TYPE firstAd;
    [SerializeField] private AD_TYPE secondAd;
    [SerializeField] private AD_TYPE interAd;
    [SerializeField] private AD_TYPE rewardAd;

    [SerializeField] private float interCooldown = 60f;
    [SerializeField] private float showTimeout = 30f;
    [SerializeField] private float firstReadyTimeout = 40f;
    [SerializeField] private float sdkInitTimeout = 60f;

    private float _interTimer;
    private bool _gameplayStarted;
    private bool _secondShown;
    private bool _isShowing;

    private bool _firstPrepared;
    private bool _firstRequested;
    private bool _firstFinished;
    private bool _firstPreloadRequested;

    private Coroutine _preloadCoroutine;

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
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (_gameplayStarted && !_isShowing)
            _interTimer += Time.unscaledDeltaTime;
    }

    public void OnFirstGameplayStarted()
    {
        if (_gameplayStarted) return;

        _gameplayStarted = true;
        _interTimer = 0f;
        Debug.Log("[Ads] First gameplay started. Inter timer = 0.");
    }

    #region FIRST AD

    public void PrepareFirst()
    {
        Debug.Log("[Ads] PrepareFirst CALLED.");

#if ENABLE_RACCOON_ADMOB
        if (_firstPrepared)
        {
            Debug.Log("[Ads] First already prepared. Skip duplicate.");
            return;
        }

        var mgr = RaccoonAdsManager.Instance;
        if (mgr == null)
        {
            Debug.LogError("[Ads] RaccoonAdsManager.Instance is NULL!");
            return;
        }

        _firstPrepared = true;
        Debug.Log($"[Ads] Initialize SDK. FirstAd = {firstAd}");

        mgr.Initialize(null);
        _preloadCoroutine = StartCoroutine(CoPreloadFirst());
#else
        Debug.LogError("[Ads] ENABLE_RACCOON_ADMOB is NOT defined!");
#endif
    }

    public void RequestFirst(Action onDone)
    {
        Debug.Log("[Ads] RequestFirst CALLED.");

#if ENABLE_RACCOON_ADMOB
        if (_firstFinished)
        {
            Debug.Log("[Ads] First already finished. Skip.");
            onDone?.Invoke();
            return;
        }

        if (_firstRequested)
        {
            Debug.LogWarning("[Ads] First request already running.");
            return;
        }

        var mgr = RaccoonAdsManager.Instance;
        if (mgr == null)
        {
            Debug.LogError("[Ads] RequestFirst: Manager NULL.");
            _firstFinished = true;
            onDone?.Invoke();
            return;
        }

        if (mgr.IsAdsRemoved)
        {
            Debug.Log("[Ads] Ads removed. Skip First.");
            _firstFinished = true;
            onDone?.Invoke();
            return;
        }

        if (!_firstPrepared) PrepareFirst();

        _firstRequested = true;
        StartCoroutine(CoShowFirst(mgr, onDone));
#else
        Debug.LogError("[Ads] ENABLE_RACCOON_ADMOB is NOT defined.");
        _firstFinished = true;
        onDone?.Invoke();
#endif
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

        if (_firstRequested && !_firstFinished)
        {
            Debug.LogWarning("[Ads] First still processing. Do not finish loading yet.");
            return;
        }

        mgr.OnLoadingSceneLoaded();
        mgr.StopLoadingAd(firstAd);
        Debug.Log($"[Ads] StopLoadingAd: {firstAd}");
#endif
    }

#if ENABLE_RACCOON_ADMOB

    private IEnumerator CoPreloadFirst()
    {
        Debug.Log("[Ads] CoPreloadFirst START.");

        float waited = 0f;

        while (waited < sdkInitTimeout)
        {
            var mgr = RaccoonAdsManager.Instance;
            if (mgr == null)
            {
                Debug.LogError("[Ads] Manager NULL during initialization.");
                _preloadCoroutine = null;
                yield break;
            }

            if (mgr.Initialized) break;

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        var manager = RaccoonAdsManager.Instance;
        if (manager == null || !manager.Initialized)
        {
            Debug.LogWarning($"[Ads] SDK initialization timeout after {waited:F1}s.");
            _preloadCoroutine = null;
            yield break;
        }

        Debug.Log($"[Ads] SDK Initialized: {manager.Initialized}");
        Debug.Log($"[Ads] SDK initialization took {waited:F2}s.");

        if (_firstFinished)
        {
            Debug.Log("[Ads] First already finished. Skip preload.");
            _preloadCoroutine = null;
            yield break;
        }

        bool ready = manager.AdsIsReady(firstAd);
        Debug.Log($"[Ads] First ready before preload: {ready}");

        if (!ready && !_firstPreloadRequested)
        {
            _firstPreloadRequested = true;

            bool autoPreload = manager.IsPreloadAd(firstAd);
            Debug.Log($"[Ads] First auto preload config: {autoPreload}");

            if (!autoPreload)
            {
                Debug.Log($"[Ads] Starting preload: {firstAd}");
                manager.PreloadAdsWaitShow(firstAd, success => Debug.Log($"[Ads] First preload result: {success}"));
            }
            else
            {
                Debug.Log("[Ads] First configured for SDK auto preload.");
            }
        }

        Debug.Log("[Ads] CoPreloadFirst FINISHED.");
        _preloadCoroutine = null;
    }

    private IEnumerator CoShowFirst(RaccoonAdsManager mgr, Action onDone)
    {
        Debug.Log("[Ads] CoShowFirst START.");

        float waited = 0f;
        float nextLogTime = 0f;

        while (waited < firstReadyTimeout)
        {
            if (mgr == null)
            {
                Debug.LogError("[Ads] Manager NULL while waiting First.");
                break;
            }

            if (mgr.IsAdsRemoved)
            {
                Debug.Log("[Ads] Ads removed while waiting.");
                break;
            }

            if (mgr.Initialized && mgr.AdsIsReady(firstAd))
            {
                Debug.Log($"[Ads] First READY after {waited:F2}s.");
                break;
            }

            if (waited >= nextLogTime)
            {
                Debug.Log($"[Ads] Waiting First... SDK={mgr.Initialized}, Ready={(mgr.Initialized && mgr.AdsIsReady(firstAd))}, Waited={waited:F1}s");
                nextLogTime += 2f;
            }

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        bool isReady = mgr != null && mgr.Initialized && !mgr.IsAdsRemoved && mgr.AdsIsReady(firstAd);
        Debug.Log($"[Ads] First final ready: {isReady}");
        Debug.Log($"[Ads] First waited: {waited:F2}s");

        if (!isReady)
        {
            Debug.LogWarning("[Ads] First NOT READY. Skip.");
            FinishFirst(onDone);
            yield break;
        }

        Debug.Log($"[Ads] SHOW FIRST AD: {firstAd}");

        _isShowing = true;

        bool done = false;
        bool showResult = false;

        try
        {
            mgr.ShowAdUntilLoaded(firstAd, false, success =>
            {
                Debug.Log($"[Ads] First callback: {success}");
                showResult = success;
                done = true;
            });
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            done = true;
        }

        float showWaited = 0f;

        while (!done && showWaited < showTimeout)
        {
            showWaited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!done) Debug.LogWarning("[Ads] First SHOW TIMEOUT!");
        else Debug.Log($"[Ads] First show result: {showResult}");

        _isShowing = false;
        FinishFirst(onDone);
    }

    private void FinishFirst(Action onDone)
    {
        if (_firstFinished) return;

        _firstFinished = true;
        _firstRequested = false;
        _isShowing = false;

        Debug.Log("[Ads] CoShowFirst FINISHED.");
        onDone?.Invoke();
    }

#endif

    #endregion

    #region SECOND / INTER / REWARD

    public void RequestSecond(Action onContinue)
    {
        Debug.Log("[Ads] RequestSecond CALLED.");

        if (_secondShown)
        {
            Debug.Log("[Ads] Second already shown. Skip.");
            onContinue?.Invoke();
            return;
        }

        TryShow(secondAd, shown =>
        {
            Debug.Log($"[Ads] Second Ad Result: {shown}");
            if (shown) _secondShown = true;
            onContinue?.Invoke();
        });
    }

    public void RequestInter(Action onContinue)
    {
        Debug.Log($"[Ads] RequestInter CALLED. Timer: {_interTimer:F1}s");

        if (_interTimer < interCooldown)
        {
            Debug.Log("[Ads] Inter cooldown not reached. Skip.");
            onContinue?.Invoke();
            return;
        }

        TryShow(interAd, shown =>
        {
            Debug.Log($"[Ads] Inter Ad Result: {shown}");
            if (shown) _interTimer = 0f;
            onContinue?.Invoke();
        });
    }

    public void RequestReward(Action onRewarded, Action onFailed = null)
    {
        Debug.Log("[Ads] RequestReward CALLED.");

        TryShow(rewardAd, ok =>
        {
            Debug.Log($"[Ads] Reward Ad Result: {ok}");
            if (ok) onRewarded?.Invoke();
            else onFailed?.Invoke();
        });
    }

    private void TryShow(AD_TYPE type, Action<bool> onDone)
    {
        Debug.Log($"[Ads] TryShow CALLED: {type}");

#if ENABLE_RACCOON_ADMOB
        var mgr = RaccoonAdsManager.Instance;

        if (mgr == null)
        {
            Debug.LogError("[Ads] TryShow: Manager NULL.");
            onDone?.Invoke(false);
            return;
        }

        if (!mgr.Initialized)
        {
            Debug.LogWarning("[Ads] SDK not initialized.");
            onDone?.Invoke(false);
            return;
        }

        if (_isShowing)
        {
            Debug.LogWarning("[Ads] Another ad is showing.");
            onDone?.Invoke(false);
            return;
        }

        if (IsGateShowing())
        {
            Debug.LogWarning("[Ads] AdLoadingGate is showing.");
            onDone?.Invoke(false);
            return;
        }

        if (mgr.IsAdsRemoved)
        {
            Debug.Log("[Ads] Ads removed. Skip.");
            onDone?.Invoke(false);
            return;
        }

        if (!mgr.AdsIsReady(type))
        {
            Debug.LogWarning($"[Ads] Ad NOT READY: {type}");
            mgr.PreloadAdsWaitShow(type);
            onDone?.Invoke(false);
            return;
        }

        Debug.Log($"[Ads] SHOW AD: {type}");

        _isShowing = true;

        bool finished = false;

        void Finish(bool ok)
        {
            if (finished) return;

            finished = true;
            _isShowing = false;

            Debug.Log($"[Ads] Show result: {type} = {ok}");
            onDone?.Invoke(ok);
        }

        try
        {
            mgr.ShowAd(type, Finish);
            StartCoroutine(CoWatchdog(() => finished, () => Finish(false)));
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

    private bool IsGateShowing()
    {
#if ENABLE_RACCOON_ADMOB
        bool isShowing = AdLoadingGate.Instance != null && AdLoadingGate.Instance.IsAdShowing;
        Debug.Log($"[Ads] Loading Gate IsShowing: {isShowing}");
        return isShowing;
#else
        return false;
#endif
    }

    private IEnumerator CoWatchdog(Func<bool> isDone, Action onTimeout)
    {
        float t = 0f;

        while (!isDone() && t < showTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!isDone())
        {
            Debug.LogWarning("[Ads] Watchdog timeout!");
            onTimeout?.Invoke();
        }
    }

    #endregion
}