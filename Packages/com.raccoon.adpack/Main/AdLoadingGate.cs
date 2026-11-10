using System;
using System.Collections;
using UnityEngine;

namespace RCKit.Ads.Main
{
    /// <summary>Gate show ads không phụ thuộc SDK nào, chỉ nhận callback.
    /// Project tự cung cấp canPossiblyLoad / isReady / requestLoad / showAd theo SDK của mình.</summary>
    public class AdLoadingGate : MonoBehaviour
    {
        public static AdLoadingGate Instance;

        // Thời gian chờ load tối đa trước khi bỏ qua ads, quay lại game.
        // TODO: lấy từ Remote Config.
        public static float MaxWaitSeconds = 10f;

        // Loading tối thiểu trước khi showAd(): một số SDK (vd NativeOverlay) tự Hide() khi load xong,
        // show quá sớm sẽ race với Hide đó -> ad nháy rồi tự đóng.
        public static float MinShowDelaySeconds { get; private set; } = 1f;
        [SerializeField] private float _minShowDelaySeconds = 1f; // chỉnh trên scene, sync vào static ở Awake

        // Watchdog: force-resume sau ngần này giây kể từ PauseGame(), phòng khi SDK không bắn
        // callback đóng ad (vd "webview is destroyed") khiến game đứng ở timeScale = 0.
        public static float MaxPausedSeconds = 240f;

        [Header("Panel loading hiển thị khi đang chờ ads (gán theo từng project)")]
        public GameObject loadingPanel;

        // Chỉ cho 1 request show ads chạy tại 1 thời điểm, tránh loading panel chồng nhau.
        private bool _isRequesting = false;

        // Game được pause ngay khi request (không đợi ad hiện) để gameplay không chạy sau loading panel.
        [SerializeField] private bool _useWatchdog = false;
        private bool _pausedByGate = false;
        private float _cachedTimeScale = 1f;
        private Coroutine _watchdog;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            MinShowDelaySeconds = _minShowDelaySeconds;
        }

#if UNITY_EDITOR
        // Sync luôn khi sửa Inspector lúc đang Play.
        private void OnValidate()
        {
            if (Instance == this) MinShowDelaySeconds = _minShowDelaySeconds;
        }
#endif

        /// <summary>Pause timeScale + audio. An toàn vì load ads chạy ở native SDK
        /// và vòng chờ trong CoRequestShowAd dùng unscaled time.</summary>
        private void PauseGame()
        {
            if (_pausedByGate) return;
            _pausedByGate = true;
            _cachedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            if (_useWatchdog)
                _watchdog = StartCoroutine(CoWatchdogResume());
        }

        /// <summary>Tự resume nếu sau MaxPausedSeconds vẫn chưa ai gọi ResumeGame().</summary>
        private IEnumerator CoWatchdogResume()
        {
            yield return new WaitForSecondsRealtime(MaxPausedSeconds);
            if (_pausedByGate)
            {
                Debug.LogWarning("[AdLoadingGate] Ad không đóng sau " + MaxPausedSeconds +
                                  "s, force resume game để tránh đứng game.");
                loadingPanel?.SetActive(false);
                ResumeGame();
            }
        }

        /// <summary>Gọi khi request fail/timeout hoặc ad đã đóng. Luôn restore timeScale đã cache
        /// vì NativeOverlay (isPauseOnShow) có thể tự restore trước lệnh này.</summary>
        public void ResumeGame(bool force = false)
        {
            if (!_pausedByGate && !force) return;
            _pausedByGate = false;
            if (_cachedTimeScale <= 0) _cachedTimeScale = 1f;
            Time.timeScale = _cachedTimeScale;
            AudioListener.pause = false;
            loadingPanel?.SetActive(false);
            if (_watchdog != null)
            {
                StopCoroutine(_watchdog);
                _watchdog = null;
            }
        }

        /// <summary>Yêu cầu show 1 ad: pause game, hiện loading, chờ ad ready rồi show.</summary>
        /// <param name="canPossiblyLoad">Check nhanh (init / internet / no-ads). False = fail ngay, không hiện loading.</param>
        /// <param name="isReady">True khi ad đã cache sẵn sàng show.</param>
        /// <param name="requestLoad">Yêu cầu load, phải idempotent (đang load / đã có ad thì bỏ qua).</param>
        /// <param name="showAd">Gọi Show() thật lên SDK.</param>
        /// <param name="onResult">True = đã show; false = timeout / không load được.</param>
        /// <param name="minShowDelayOverride">Ghi đè MinShowDelaySeconds cho request này (vd ShowMultiAd). Null = mặc định.</param>
        public void RequestShowAd(
            Func<bool> canPossiblyLoad,
            Func<bool> isReady,
            Action requestLoad,
            Action showAd,
            Action<bool> onResult,
            float? minShowDelayOverride = null)
        {
            if (_isRequesting)
            {
                Debug.LogWarning("[AdLoadingGate] Đang có 1 request show ads khác, bỏ qua request mới.");
                onResult?.Invoke(false);
                return;
            }

            StartCoroutine(CoRequestShowAd(canPossiblyLoad, isReady, requestLoad, showAd, onResult, minShowDelayOverride ?? MinShowDelaySeconds));
        }

        private IEnumerator CoRequestShowAd(
            Func<bool> canPossiblyLoad,
            Func<bool> isReady,
            Action requestLoad,
            Action showAd,
            Action<bool> onResult,
            float minShowDelaySeconds)
        {
            _isRequesting = true;

            // 1) Chắc chắn không có ads -> fail ngay, không pause / không hiện loading.
            if (canPossiblyLoad != null && !canPossiblyLoad.Invoke())
            {
                _isRequesting = false;
                onResult?.Invoke(false);
                yield break;
            }

            PauseGame();

            loadingPanel?.SetActive(true);
            // 2) Ad đã cache sẵn -> chỉ chờ minShowDelaySeconds rồi show.
            if (isReady != null && isReady.Invoke())
            {
                yield return new WaitForSecondsRealtime(minShowDelaySeconds);
                if (AbortIfCannotLoad(canPossiblyLoad, onResult)) yield break;
                _isRequesting = false;
                showAd?.Invoke();
                onResult?.Invoke(true);
                HideAfterShow();
                yield break;
            }

            // 3) Chưa có ad -> request load, loading tối thiểu minShowDelaySeconds, tối đa MaxWaitSeconds.
            requestLoad?.Invoke();
            yield return new WaitForSecondsRealtime(minShowDelaySeconds);
            float waited = minShowDelaySeconds;
            while (waited < MaxWaitSeconds)
            {
                if (isReady != null && isReady.Invoke())
                {
                    if (AbortIfCannotLoad(canPossiblyLoad, onResult)) yield break;
                    _isRequesting = false;
                    showAd?.Invoke();
                    onResult?.Invoke(true);
                    HideAfterShow();
                    yield break;
                }

                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            loadingPanel?.SetActive(false);
            _isRequesting = false;
            ResumeGame(true);
            onResult?.Invoke(false);
        }

        /// <summary>Recheck canPossiblyLoad trước khi show (mạng có thể rớt lúc chờ).
        /// True = đã hủy, cleanup và báo fail qua onResult.</summary>
        private bool AbortIfCannotLoad(Func<bool> canPossiblyLoad, Action<bool> onResult)
        {
            if (canPossiblyLoad != null && !canPossiblyLoad.Invoke())
            {
                loadingPanel?.SetActive(false);
                _isRequesting = false;
                ResumeGame(true);
                onResult?.Invoke(false);
                return true;
            }
            return false;
        }

        public bool IsAdShowing => loadingPanel?.activeSelf ?? false;

        private void HideAfterShow()
        {
            StopCoroutine(DelayHideAfterShow());
            StartCoroutine(DelayHideAfterShow());
        }
        
        private IEnumerator DelayHideAfterShow()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            loadingPanel?.SetActive(false);
        }
    }
}
