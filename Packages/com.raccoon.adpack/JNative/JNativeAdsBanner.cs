using System;
using GoogleMobileAds.Api;
using JKit.Monetize.Ads;
using RCKit.Ads.AdMob;
using UnityEngine;
using LoadAdError = JKit.Monetize.Ads.LoadAdError;

namespace RCKit.Ads.JNative
{
    /// <summary>
    /// Banner sử dụng JNativeAds NativeOverlay với template banner và không có nút quit.
    /// </summary>
    public class JNativeAdsBanner : JNativeAdsUnit, IAdsBanner, IAdsRemovable
    {
        public event Action OnShowed = delegate { };
        public event Action OnHidden = delegate { };
        public event Action OnLoadFailed = delegate { };
        public event Action OnLoadSucceed = delegate { };

        private readonly Rect _rect;
        private readonly long _refresh;

        private NativeOverlay _overlay;
        private bool _isRemoved;
        private bool _isShowed;
        private bool _isLoading;
        private bool _registeredOnPaid;
        private bool _isCollapse;
        private bool _hasClose;
        private float _quitButtonScale;
        private float _quitButtonCoverScale;

        public JNativeAdsBanner(string adUnitId, Rect rect, long refresh, bool isShowed) : base(adUnitId)
        {
            _rect = rect;
            if (refresh <= 0)
                refresh = 15;
            _refresh = refresh;
            _isShowed = isShowed;
        }

        public JNativeAdsBanner SetCollapse(bool isCollapse, bool hasClose)
        {
            _isCollapse = isCollapse;
            _hasClose = hasClose;
            return this;
        }

        public JNativeAdsBanner SetQuitButtonScale(float scale)
        {
            _quitButtonScale = scale;
            return this;
        }

        public JNativeAdsBanner SetQuitButtonCoverScale(float scale)
        {
            _quitButtonCoverScale = scale;
            return this;
        }

        public bool IsShowed() => _isShowed;

        public void Load()
        {
            if (_isRemoved || _isLoading) return;
            if (_overlay != null) return;
            _isLoading = true;

#if UNITY_EDITOR
            Debug.Log("[JNativeAdsBanner] Editor: Load");
            _isLoading = false;
            OnLoadSucceed();
#else
            var quitType = _hasClose ? NativeOptions.NATIVE_QUIT : NativeOptions.NONE_QUIT;
            var template = _isCollapse ? NativeOptions.TEMPLATE_PORTRAIT : NativeOptions.TEMPLATE_BANNER;
            var options = new NativeOptions.Builder()
                         .Template(template)
                         .PositionAndSize(_rect)
                         .Quit(quitType)
                         .Refresh(_refresh)
                         .Build(adUnitId);

            NativeOverlay.Load(options, OnOverlayLoaded);
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
                Debug.LogError($"[JNativeAdsBanner] Load failed: {error}");
                OnLoadFailed();
                return;
            }

            _overlay = overlay;
            _overlay.SetScaleButtonQuit(_quitButtonScale);
            _overlay.SetScaleButtonQuitCover(_quitButtonCoverScale);
            if (!_registeredOnPaid)
                _overlay.OnAdPaid += OnAdsPaid;
            // Áp trạng thái show/hide hiện tại lên overlay vừa load
            if (_isShowed) _overlay.Show();
            else _overlay.Hide();

            OnLoadSucceed();
            if (AdsManager.IsPlatformIOS)
                _registeredOnPaid = true;
        }

        public void Show()
        {
            if (_isRemoved || _isShowed) return;
            _isShowed = true;
#if UNITY_EDITOR
            Debug.Log("[JNativeAdsBanner] Editor: Show");
#else
            _overlay?.Show();
#endif
            OnShowed();
        }

        public void Hide()
        {
            if (_isRemoved || !_isShowed) return;
            _isShowed = false;
#if UNITY_EDITOR
            Debug.Log("[JNativeAdsBanner] Editor: Hide");
#else
            _overlay?.Hide();
#endif
            OnHidden();
        }

        private void OnAdsPaid(AdValue adValue)
        {
            GoogleAdsMediation.RaisePaid(adValue, adUnitId, AdPaidInfo.FormatNative);
        }

        public void Destroy()
        {
            _registeredOnPaid = false;
            if (_overlay != null)
            {
                _overlay.Destroy();
                _overlay = null;
            }
        }

        public bool IsReady()
        {
            return _overlay != null;
        }

        public void SetRefreshTime(long refreshTime)
        {
            if (refreshTime <= 0)
                refreshTime = 15;
            _overlay?.SetRefreshTime(refreshTime);
        }

        /// use for banner only, default is dark theme if not set
        /// NativeOptions.BANNER_THEME_LIGHT / BANNER_THEME_DARK
        public void SetBannerTheme(string theme)
        {
            _overlay?.SetBannerTheme(theme);
        }

        void IAdsRemovable.OnRemoved()
        {
            _isRemoved = true;
            Destroy();
            if (_isShowed)
            {
                _isShowed = false;
                OnHidden();
            }
        }

        // private Rect RecalculatePolicySize(Rect rect)
        // {
        //     //recalcu size of native banner with min size = 120dp
        //     var density = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
        //     var minSizePx = MinPolicySizeDp * density;
        //
        //     var result = rect;
        //     if (result.width < minSizePx) result.width = minSizePx;
        //     if (result.height < minSizePx) result.height = minSizePx;
        //
        //     // Growing width/height above can push x+width or y+height past the screen edge
        //     // (GetBannerRect computes x/y from the pre-growth size for right/bottom anchors),
        //     // so re-clamp the origin to keep the whole rect on screen.
        //     result.width = Mathf.Min(result.width, Screen.width);
        //     result.height = Mathf.Min(result.height, Screen.height);
        //     result.x = Mathf.Clamp(result.x, 0, Screen.width - result.width);
        //     result.y = Mathf.Clamp(result.y, 0, Screen.height - result.height);
        //
        //     return result;
        // }
    }
}
