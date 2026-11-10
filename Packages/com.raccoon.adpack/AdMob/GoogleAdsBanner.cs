using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace RCKit.Ads.AdMob
{
    public class GoogleAdsBanner : GoogleAdsUnit, IAdsBanner, IAdsRemovable
    {
        public event Action OnShowed = delegate { };
        public event Action OnHidden = delegate { };
        public event Action OnLoadFailed = delegate { };
        public event Action OnLoadSucceed = delegate { };

        private bool _isRemoved;
        private bool _isShowed;
        private bool _isLoading;
        private bool _isLoaded;
        private BannerView _bannerView;


        public GoogleAdsBanner(string adUnitId, AdSize adSize, AdPosition adPosition, bool isShowed) : base(adUnitId)
        {
            _isShowed = isShowed;
            _bannerView = new BannerView(adUnitId, adSize, adPosition);
            _bannerView.OnBannerAdLoaded += OnBannerAdLoaded;
            _bannerView.OnBannerAdLoadFailed += OnBannerAdLoadFailed;
            _bannerView.OnAdPaid += OnAdsPaid;
            if(!IsShowed()) _bannerView.Hide();
        }


        private async void OnBannerAdLoaded()
        {
            // AdMob callbacks có thể được raise từ background thread → switch về main thread.
            await Awaitable.MainThreadAsync();
            _isLoading = false;
            _isLoaded = true;
            OnLoadSucceed();
            if (IsShowed()) 
                _bannerView.Show();
        }

        private async void OnBannerAdLoadFailed(LoadAdError error)
        {
            await Awaitable.MainThreadAsync();
            _isLoading = false;
            Debug.LogError(error.GetMessage());
            OnLoadFailed();
        }

        private async void OnAdsPaid(AdValue adValue)
        {
            // Callback impression/revenue từ AdMob → switch về main thread trước khi forward lên Mediation.
            await Awaitable.MainThreadAsync();
            GoogleAdsMediation.RaisePaid(adValue, adUnitId, AdPaidInfo.FormatBanner, _bannerView?.GetResponseInfo());
        }

        public bool IsShowed() => _isShowed && !_isRemoved;

        public void Show()
        {
            if (_isRemoved || _isShowed) return;
            _isShowed = true;
            OnShowed();
            if(_bannerView == null) return;
            _bannerView.Show();
        }

        public void Hide()
        {
            if (_isRemoved || !_isShowed) return;
            _isShowed = false;
            OnHidden();
            if(_bannerView == null) return;
            _bannerView.Hide();
        }

        public void Load()
        {
            if (_isRemoved || _isLoading) return;
            if (_bannerView == null) return;
            _isLoading = true;
            _bannerView.LoadAd(new AdRequest());
        }

        public void Destroy()
        {
            if (_bannerView != null)
            {
                _isLoaded = false;
                _bannerView.OnBannerAdLoaded -= OnBannerAdLoaded;
                _bannerView.OnBannerAdLoadFailed -= OnBannerAdLoadFailed;
                _bannerView.OnAdPaid -= OnAdsPaid;
                _bannerView.Destroy();
                _bannerView = null;
            }
        }

        public bool IsReady()
        {
            return _isLoaded;
        }

        public void SetRefreshTime(long refreshTime)
        {
        }
        
        public void SetBannerTheme(string theme)
        {
        }

        public void SetBannerMinSizeMedia()
        {
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
    }
}