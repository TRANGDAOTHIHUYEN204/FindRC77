using System;
using UnityEngine;

namespace RCKit.Ads.Max
{
    public class MaxAdsBanner : MaxAdsUnit, IAdsBanner, IAdsRemovable
    {
        public event Action OnShowed = delegate { };
        public event Action OnHidden = delegate { };
        public event Action OnLoadFailed = delegate { };
        public event Action OnLoadSucceed = delegate { };

        private readonly MaxSdkBase.AdViewPosition _position;
        private bool _isRemoved;
        private bool _isShowed;
        private bool _created;
        private bool _isLoading;

        public MaxAdsBanner(string adUnitId, bool isShowed, MaxSdkBase.AdViewPosition position = MaxSdkBase.AdViewPosition.BottomCenter) : base(adUnitId)
        {
            _isShowed = isShowed;
            _position = position;

            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailed;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaid;
            
        }

        private void OnAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo arg2)
        {
            if(adUnitId != this.adUnitId) return;
            
        }

        private void OnBannerAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != this.adUnitId) return;
            _isLoading = false;
            OnLoadSucceed();

            if (_isShowed)
            {
                MaxSdk.ShowBanner(adUnitId);
                Debug.Log("Max Banner Showed");
            }
            else
            {
                MaxSdk.HideBanner(adUnitId);
                Debug.Log("Max Banner Hidden");
            }
        }

        private void OnBannerAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (adUnitId != this.adUnitId) return;
            _isLoading = false;
            Debug.LogError($"[MaxAdsBanner] Load failed: {errorInfo.Message}");
            OnLoadFailed();
        }

        public bool IsShowed() => _isShowed;

        public void Show()
        {
            if (_isRemoved || _isShowed) return;
            _isShowed = true;
            MaxSdk.ShowBanner(adUnitId);
            Debug.Log("Max Banner Showed");
            OnShowed();
        }

        public void Hide()
        {
            if (_isRemoved || !_isShowed) return;
            _isShowed = false;
            MaxSdk.HideBanner(adUnitId);
            Debug.Log("Max Banner Hidden");
            OnHidden();
        }

        public void Load()
        {
            if (_isRemoved || _isLoading) return;
            if (!_created)
            {
                MaxSdk.CreateBanner(adUnitId, new MaxSdkBase.AdViewConfiguration(_position));
                _created = true;
            }
            _isLoading = true;
            MaxSdk.LoadBanner(adUnitId);
        }

        public void Destroy()
        {
            if (!_created) return;
            MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnBannerAdLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnBannerAdLoadFailed;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
            MaxSdk.DestroyBanner(adUnitId);
            _created = false;
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
        
        public void SetRefreshTime(long refreshTime)
        {
        }
        
        public void SetBannerTheme(string theme)
        {
        }
    }
}