using System;
using UnityEngine;

namespace RCKit.Ads.Raccoon
{
    // RaccoonAdsBanner is a wrapper for IAdsBanner that adds automatic reloading functionality when the ad fails to load. It also implements IAdsRemovable and IAdsUnit interfaces for additional functionality.
    public class RaccoonAdsBanner : IAdsUnit, IAdsBanner, IAdsRemovable
    {
        public event Action OnShowed = delegate { };
        public event Action OnHidden = delegate { };
        public event Action OnLoadFailed = delegate { };
        public event Action OnLoadSucceed = delegate { };
        
        private IAdsBanner _banner;
        private float _reloadInterval;
        // Số lần load fail liên tiếp — giãn delay reload: base, x2, x4... tối đa MaxReloadDelay.
        private int _consecutiveFails;
        private const float MaxReloadDelay = 15f;

        private float NextReloadDelay()
        {
            var delay = Mathf.Min(_reloadInterval * (1 << Mathf.Min(_consecutiveFails, 10)), MaxReloadDelay);
            _consecutiveFails++;
            return delay;
        }
        private bool _hideByOtherAd = false;

        public RaccoonAdsBanner(IAdsBanner banner, float reloadInterval = 1f)
        {
            _banner = banner;
            _reloadInterval = reloadInterval;
            _banner.OnLoadFailed += OnAdsLoadFailed;
            _banner.OnLoadSucceed += OnAdsLoadedSucceed;
            _banner.OnShowed += OnAdsShowed;
            _banner.OnHidden += OnAdsHidden;
        }

        ~RaccoonAdsBanner()
        {
            _banner.OnLoadFailed -= OnAdsLoadFailed;
            _banner.OnLoadSucceed -= OnAdsLoadedSucceed;
            _banner.OnShowed -= OnAdsShowed;
            _banner.OnHidden -= OnAdsHidden;
        }

        private void OnAdsShowed()
        {
            OnShowed();
        }

        private void OnAdsHidden()
        {
            if (!_hideByOtherAd)
                OnHidden();
        } 
        private void OnAdsLoadedSucceed()
        {
            _consecutiveFails = 0;
            OnLoadSucceed();
        }

        private async void OnAdsLoadFailed()
        {
            OnLoadFailed();
            await Awaitable.WaitForSecondsAsync(NextReloadDelay());
            _banner.Load();
        }
        public bool IsShowed() => _banner.IsShowed();
        public void Show()
        {
            _hideByOtherAd = false;
            _banner.Show();
        }

        public void Hide() => _banner.Hide();
        public bool HideByOtherAd
        {
            get => _hideByOtherAd;
            set => _hideByOtherAd = value;
        }

        public void Load() => _banner.Load();
        public void Destroy()
        {
            _hideByOtherAd = false;
            _banner.Destroy();
        }

        public bool IsReady() => _banner.IsReady();
        
        public void SetRefreshTime(long refreshTime)
        {
            _banner?.SetRefreshTime(refreshTime);
        }
        
        /// use for banner only, default is dark theme if not set
        /// NativeOptions.BANNER_THEME_LIGHT / BANNER_THEME_DARK
        public void SetBannerTheme(string theme)
        {
            _banner?.SetBannerTheme(theme);
        }

        public void OnRemoved()
        {
            if (_banner is IAdsRemovable removable)
                removable.OnRemoved();
        }

        public void ClearOnHiddenEvent()
        {
            OnHidden = delegate { };
        }
    }
}