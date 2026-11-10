using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using JKit.Monetize.Ads;
using RCKit.Ads.AdMob;
using UnityEngine;
using LoadAdError = JKit.Monetize.Ads.LoadAdError;

namespace RCKit.Ads.JNative
{
    /// <summary>
    /// App Open Ad sử dụng JNativeAds NativeOverlay full-screen với NATIVE_QUIT (đóng được ngay).
    /// </summary>
    public class JNativeAdsAppOpen : JNativeAdsUnit, IAppOpenAds, IAdsRemovable
    {
        private NativeOverlay _overlay;
        private bool _isRemoved;
        private bool _isLoading;
        private bool _isShowing;
        private bool _showOnLoad;
        private int _reloadInterval;
        // Số lần load fail liên tiếp — giãn delay reload: base, x2, x4... tối đa MaxReloadDelay.
        private int _consecutiveFails;
        private const float MaxReloadDelay = 15f;
        private bool _registeredOnPaid;

        public JNativeAdsAppOpen(string adUnitId, int reloadInterval = 5) : base(adUnitId)
        {
            _reloadInterval = reloadInterval;
            // AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
        }

        public void Initialize()
        {
            if (_isRemoved) return;
            // _showOnLoad = true;
            Load();
        }

        public event Action OnAdShow;
        public event Action OnLoadFailed;
        public event Action OnLoadSucceed;
        public event Action OnClosed;

        public bool IsReady()
        {
#if UNITY_EDITOR
            return !_isRemoved && !_isShowing;
#else
            return !_isRemoved && _overlay != null && !_isShowing;
#endif
        }

        public void SetRefreshTime(long refreshTime)
        {
            _overlay?.SetRefreshTime(refreshTime);
        }

        public void SetQuitButtonScale(float scale)
        {
            _overlay?.SetScaleButtonQuit(scale);
        }

        void IAdsRemovable.OnRemoved()
        {
            _isRemoved = true;
            // AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;
            Destroy();
        }

        // private void OnAppStateChanged(AppState state)
        // {
        //     if (_isRemoved) return;
        //     if (state == AppState.Foreground)
        //     {
        //         // if (IsAvailable()) ShowAd();
        //         // LoadAd();
        //     }
        // }

        private bool IsAvailable()
        {
            return _overlay != null;
        }

        public void Load()
        {
            if (_isRemoved) return;
            if (_isLoading) return;
            _isLoading = true;
            if (_overlay != null) Destroy();

#if UNITY_EDITOR
            Debug.Log("[JNativeAdsAppOpen] Editor: LoadAd");
            _isLoading = false;
            if (_showOnLoad)
            {
                _showOnLoad = false;
                Debug.Log("[JNativeAdsAppOpen] Editor: ShowAd");
            }
#else
            NativeOverlay.Load(adUnitId, 30, OnOverlayLoaded);
#endif
        }

        private async void LoadAdAsync(float seconds)
        {
            await Awaitable.WaitForSecondsAsync(seconds);
            Load();
        }

        private void OnOverlayLoaded(NativeOverlay overlay, LoadAdError error)
        {
            _isLoading = false;

            if (_isRemoved)
            {
                overlay?.Destroy();
                return;
            }

            if (overlay == null)
            {
                Debug.LogError($"[JNativeAdsAppOpen] Load failed: {error}");
                var delay = Mathf.Min(_reloadInterval * (1 << Mathf.Min(_consecutiveFails, 10)), MaxReloadDelay);
                _consecutiveFails++;
                LoadAdAsync(delay);
                return;
            }

            Debug.Log("[JNativeAdsAppOpen] Load succeeded.");
            _consecutiveFails = 0;
            _overlay = overlay;
            _overlay.OnClosed += OnAdClosed;
            if (!_registeredOnPaid)
                _overlay.OnAdPaid += OnAdsPaid;

            if (_showOnLoad)
            {
                _showOnLoad = false;
                Show(null);
            }
        }

        public void Show(Action onCallback)
        {
#if UNITY_EDITOR
            Debug.Log("[JNativeAdsAppOpen] Editor: ShowAd");
#else
            if (IsAvailable()) 
            {
                OnAdShow?.Invoke();
                _overlay.Show();
            }
#endif
        }

        private void OnAdsPaid(AdValue adValue)
        {
            GoogleAdsMediation.RaisePaid(adValue, adUnitId, AdPaidInfo.FormatNative);
        }

        private void OnAdClosed()
        {
            OnClosed?.Invoke();
            Destroy();
            if (_showOnLoad)
            {
                Load();
            }
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
    }
}