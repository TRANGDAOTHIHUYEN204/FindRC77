using System;
using GoogleMobileAds.Api;
using JKit.Monetize.Ads;
using RCKit.Ads.AdMob;
using UnityEngine;
using LoadAdError = JKit.Monetize.Ads.LoadAdError;

namespace RCKit.Ads.JNative
{
    /// <summary>
    /// Rewarded sử dụng JNativeAds NativeOverlay full-screen + INTER_QUIT (countdown).
    /// User chỉ được rewarded khi ad đã impression và sau đó user đóng.
    /// </summary>
    public class JNativeAdsRewarded : JNativeAdsUnit, IAdsRewarded
    {
        public event Action OnAdShow = delegate { };
        public event Action OnLoadFailed = delegate { };
        public event Action OnLoadSucceed = delegate { };
        public event Action OnClosed = delegate { };

        private NativeOverlay _overlay;
        private bool _isLoading;
        private bool _isShowing;
        private long _refresh;
        private bool _registeredOnPaid;

        public JNativeAdsRewarded(string adUnitId, long refresh = 15, bool supportMedia = true) : base(adUnitId, supportMedia)
        {
            this._refresh = refresh;
        }

        public void Load()
        {
            if (_isLoading) return;
            if (_overlay != null) return;
            _isLoading = true;

#if UNITY_EDITOR
            Debug.Log("[JNativeAdsRewarded] Editor: Load");
            _isLoading = false;
            OnLoadSucceed();
#else
            NativeOverlay.Load(adUnitId, _refresh, OnOverlayLoaded);
#endif
        }

        private void OnOverlayLoaded(NativeOverlay overlay, LoadAdError error)
        {
            _isLoading = false;

            if (overlay == null)
            {
                Debug.LogError($"[JNativeAdsRewarded] Load failed: {error}");
                OnLoadFailed();
                return;
            }

            _overlay = overlay;
            if (!_registeredOnPaid)
                _overlay.OnAdPaid += OnAdsPaid;
            _overlay.Hide();
            OnLoadSucceed();
            if (AdsManager.IsPlatformIOS)
                _registeredOnPaid = true;
        }

        private void OnAdsPaid(AdValue adValue)
        {
            GoogleAdsMediation.RaisePaid(adValue, adUnitId, AdPaidInfo.FormatNative);
        }

        public bool IsReady()
        {
#if UNITY_EDITOR
            return !_isShowing;
#else
            return _overlay != null && !_isShowing;
#endif
        }

        public void Show(Action onRewarded, Action onFailed)
        {
            if (!IsReady())
            {
                onFailed?.Invoke();
                return;
            }

            OnAdShow();
            _isShowing = true;

#if UNITY_EDITOR
            Debug.Log("[JNativeAdsRewarded] Editor: Show (auto rewarded)");
            _isShowing = false;
            onRewarded?.Invoke();
            OnClosed();
#else

            void Finish()
            {
                onRewarded?.Invoke();
                OnClosed();
                Destroy();
            }

            _overlay.OnClosed += Finish;
            _overlay.Show();
#endif
        }

        public void Destroy()
        {
            _registeredOnPaid = false;
            _isShowing = false;
            if (_overlay != null)
            {
                _overlay.Destroy();
                _overlay = null;
            }
        }

        public void SetRefreshTime(long refreshTime)
        {
            _overlay?.SetRefreshTime(refreshTime);
        }

        public void SetQuitButtonScale(float scale)
        {
            _overlay?.SetScaleButtonQuit(scale);
        }
    }
}
