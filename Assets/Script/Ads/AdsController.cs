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

    [SerializeField] private float interCooldown = 60f;
    [SerializeField] private float showTimeout = 30f;

    [Header("Resume Ad (ẩn app rồi vào lại)")]
    [SerializeField] private AD_TYPE resumeAd;
    [Tooltip("Loading panel hiện trước ad, tắt sau khi ad đóng (nên là con của AdsController hoặc Canvas DontDestroyOnLoad, sorting cao nhất)")]
    [SerializeField] private GameObject resumeLoadingPanel;

    /// <summary>StateGame set vào để chặn resume ad (vd: đang tutorial).</summary>
    public Func<bool> CanShowResumeAd;

    private float _interTimer;
    private bool _gameplayStarted;
    private bool _secondShown;
    private bool _isShowing;
    private bool _sdkInitRequested;

    private bool _wasBackgrounded;
    private bool _resumeLoading;
    private float _prevTimeScale = 1f;
    private Coroutine _resumeRoutine;

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
        if (_resumeRoutine != null) StopCoroutine(_resumeRoutine);
        if (_resumeLoading) SetResumeLoading(false);
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

    #region SDK INIT

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
        mgr.Initialize(null);
#else
        Debug.LogError("[Ads] ENABLE_RACCOON_ADMOB is NOT defined!");
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

        mgr.OnLoadingSceneLoaded();
#endif
    }

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

    #endregion

    #region RESUME AD

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            // Pause do chính ad overlay / loading gây ra -> không tính là player thoát app
            _wasBackgrounded = !(_isShowing || _resumeLoading || IsGateShowing());
            return;
        }

        // Player quay lại app
        if (!_wasBackgrounded) return;
        _wasBackgrounded = false;

        if (!_gameplayStarted) return;
        if (_resumeRoutine != null) return;
        if (CanShowResumeAd != null && !CanShowResumeAd()) return;

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

        // Ad chưa sẵn sàng -> không hiện loading, preload cho lần sau
        if (!mgr.AdsIsReady(resumeAd))
        {
            Debug.Log("[Ads] Resume ad not ready. Preload.");
            mgr.PreloadAdsWaitShow(resumeAd);
            _resumeRoutine = null;
            yield break;
        }
#endif

        SetResumeLoading(true);

        Debug.Log("[Ads] Show resume ad.");
        TryShow(resumeAd, shown =>
        {
            Debug.Log($"[Ads] Resume Ad Result: {shown}");
            if (shown) _interTimer = 0f; // tránh inter hiện ngay sau resume ad
            SetResumeLoading(false);
            _resumeRoutine = null;
        });
    }

    private void SetResumeLoading(bool show)
    {
        if (show == _resumeLoading) return;
        _resumeLoading = show;

        if (resumeLoadingPanel) resumeLoadingPanel.SetActive(show);

        // Đứng game trong lúc loading + ad để không bị mất thời gian / thua
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

    #endregion

    #region CORE

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