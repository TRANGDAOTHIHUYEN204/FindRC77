using System;
using GoogleMobileAds.Api;
using JKit.Monetize.Ads;
using RCKit.Ads.AdMob;
using UnityEngine;
using LoadAdError = JKit.Monetize.Ads.LoadAdError;

namespace RCKit.Ads.JNative
{
    /// <summary>
    /// Interstitial sử dụng JNativeAds NativeOverlay full-screen với INTER_QUIT (countdown).
    /// </summary>
    public class JNativeAdsInterstitial : JNativeAdsUnit, IAdsInterstitial, IAdsRemovable
    {
        public event Action OnAdShow = delegate { };
        public event Action OnLoadFailed = delegate { };
        public event Action OnLoadSucceed = delegate { };
        public event Action OnClosed = delegate { };
        

        private NativeOverlay _overlay;
        private bool _isRemoved;
        private bool _isLoading;
        private bool _isShowing;
        private bool _registeredOnPaid;
        private string _layoutQuit;
        private string _template;
        private bool _reloadOnClick;
        private bool _closeAdOnClick = false;
        private bool _forceAdPortrait = false;
        private bool _isMultiAd;
        private string _adUnitId2;

        public JNativeAdsInterstitial(string adUnitId, bool supportMedia = true, string quitLayout = "inter", string template = "", bool reloadOnClick = true) : base(adUnitId, supportMedia)
        {
            _layoutQuit =  quitLayout;
            _template = template;
            _reloadOnClick =  reloadOnClick;
            Debug.Log("[JNativeAdsInterstitial] Init Ads " + _template + " --  " + _layoutQuit);
        }

        public void Load()
        {
            if (_isRemoved || _isLoading) return;
            if (_overlay != null) return;
            _isLoading = true;
            
            Debug.Log("[JNativeAdsInterstitial] Loading Ads");
#if UNITY_EDITOR
            Debug.Log("[JNativeAdsInterstitial] Editor: Load");
            _isLoading = false;
            OnLoadSucceed();
#else
            // NativeOverlay.Load(adUnitId, -1, supportMedia, _layoutQuit, _template, OnOverlayLoaded, timeClose, _reloadOnClick);
            var padding = NativeOverlay.GetPadding();
            var buildAdUnitId = _isMultiAd ? $"{adUnitId}_{_adUnitId2}" : adUnitId;
            var builder = new NativeOptions.Builder()
                .SetForceAdPortrait(_forceAdPortrait)
                .SetOptionFullScreen(_template)
                .Quit(_layoutQuit, padding.y, padding.x)
                .Refresh(-1)
                .SetPauseOnShow(true)
                .SetTimeClose(3)
                .SetReloadOnClick(_reloadOnClick)
                .Build(buildAdUnitId);
            NativeOverlay.Load(builder, OnOverlayLoaded);
#endif
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
                Debug.LogError($"[JNativeAdsInterstitial] Load failed: {error}");
                OnLoadFailed();
                return;
            }
            Debug.Log("[JNativeAdsInterstitial] Loaded Ads");

            _overlay = overlay;
            if (!_registeredOnPaid)
                _overlay.OnAdPaid += OnAdsPaid;
            // _overlay.Hide();
            OnLoadSucceed();
            if (AdsManager.IsPlatformIOS)
                _registeredOnPaid = true;

            OnFinishClickAd();
        }

        private void OnAdsPaid(AdValue adValue)
        {
            GoogleAdsMediation.RaisePaid(adValue, adUnitId, AdPaidInfo.FormatNative);
        }

        public bool IsReady()
        {
#if UNITY_EDITOR
            return !_isRemoved && !_isShowing;
#else
            return !_isRemoved && _overlay != null && !_isShowing;
#endif
        }

        public void Show(Action<bool> onCallback)
        {
            if (!IsReady())
            {
                onCallback?.Invoke(false);
                return;
            }

            _isShowing = true;
            OnAdShow();
            Debug.Log("[JNativeAdsInterstitial] : Show");

#if UNITY_EDITOR
            Debug.Log("[JNativeAdsInterstitial] Editor: Show");
            _isShowing = false;
            onCallback?.Invoke(true);
            OnClosed();
#else
            var invoked = false;
            var showStartTime = Time.realtimeSinceStartup;

            void Finish()
            {
                Debug.Log($"[JNativeAdsInterstitial] : Close (visible for {Time.realtimeSinceStartup - showStartTime:F2}s)");
                if (invoked) return;
                invoked = true;
                // Destroy() phải chạy trước onCallback: reload_after_show (RaccoonAdsInterstitial)
                // gọi Load() bên trong onCallback, mà Load() return sớm nếu _overlay còn khác null.
                Destroy();
                onCallback?.Invoke(true);
                OnClosed();
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

        public void SetQuitButtonCoverScale(float scale)
        {
            _overlay?.SetScaleButtonQuitCover(scale);
        }

        public void SetUseOnlyPortrait(bool useOnlyPortrait)
        {
            _forceAdPortrait = useOnlyPortrait;
        }

        public void SetCloseAdAfterClickAd()
        {
            _closeAdOnClick = true;
        }

        public void SetMultiAd(string adUnitId2)
        {
            if (string.IsNullOrEmpty(adUnitId2)) return;
            _isMultiAd = true;
            _adUnitId2 = adUnitId2;
        }

        private void OnFinishClickAd()
        {
            if(_overlay == null || !_closeAdOnClick) return;
            _overlay.OnClicked += () =>
            {
                _overlay?.Hide();
            };
        }

        void IAdsRemovable.OnRemoved()
        {
            _isRemoved = true;
            Destroy();
        }
    }
}
