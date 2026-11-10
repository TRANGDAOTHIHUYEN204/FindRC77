using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

namespace RCKit.Ads.AdMob
{
    public class GoogleAdsAppOpen : GoogleAdsUnit, IAppOpenAds, IAdsRemovable
    {
        // App open ads can be preloaded for up to 4 hours.
        private static readonly TimeSpan Timeout = TimeSpan.FromHours(4);

        private AppOpenAd _appOpenAd;
        private DateTime _expireTime;
        private bool _isRemoved;
        private bool _isLoading;
        private bool _showOnLoad;
        private int _reloadInterval;

        public GoogleAdsAppOpen(string adUnitId, int reloadInterval = 5) : base(adUnitId)
        {
            _reloadInterval = reloadInterval;
            // AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
        }

        public void Initialize()
        {
            if (_isRemoved) return;
            // First time: load and show immediately.
            // _showOnLoad = true;
            // Load();
        }

        public event Action OnAdShow;
        public event Action OnLoadFailed;
        public event Action OnLoadSucceed;
        public event Action OnClosed;

        public void SetRefreshTime(long refreshTime)
        {
        }

        public void SetQuitButtonScale(float scale)
        {
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
        //         if (IsAvailable())
        //         {
        //             OnAdShow?.Invoke();
        //             Show();
        //         }
        //         else
        //             Load();
        //     }
        // }
        
        private bool IsAvailable()
        {
            return _appOpenAd != null && _appOpenAd.CanShowAd() && DateTime.Now < _expireTime;
        }


        public void Load()
        {
            if (_isRemoved) return;
            if (_isLoading) return;
            _isLoading = true;
            if (_appOpenAd != null) Destroy();
            AppOpenAd.Load(adUnitId, new AdRequest(), OnAdLoaded);
        }

        public bool IsReady()
        {
            return _appOpenAd != null;
        }

        private async void LoadAdAsync(float seconds)
        {
            await Awaitable.WaitForSecondsAsync(seconds);
            Load();
        }

        private async void OnAdLoaded(AppOpenAd ad, LoadAdError error)
        {
            await Awaitable.MainThreadAsync();

            _isLoading = false;

            if (_isRemoved)
            {
                ad?.Destroy();
                return;
            }

            if (error != null || ad == null)
            {
                Debug.LogError($"[GoogleAdsAppOpen] Load failed: {error?.GetMessage()}");
                LoadAdAsync(_reloadInterval);
                return;
            }

            Debug.Log("[GoogleAdsAppOpen] Load succeeded.");
            _appOpenAd = ad;
            _expireTime = DateTime.Now + Timeout;

            RegisterEventHandlers(ad);

            if (_showOnLoad)
            {
                _showOnLoad = false;
                Show(null);
            }
        }

        public void Show(Action onCallback = null)
        {
            if (IsAvailable())
            {
                OnAdShow?.Invoke();
                _appOpenAd.Show();
            }
        }

        public void Destroy()
        {
            if (_appOpenAd != null)
            {
                _appOpenAd.Destroy();
                _appOpenAd = null;
            }
        }

        private void RegisterEventHandlers(AppOpenAd ad)
        {
            ad.OnAdPaid += OnAdPaid;
            ad.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
            ad.OnAdFullScreenContentFailed += OnAdFullScreenContentFailed;
        }

        private async void OnAdPaid(AdValue adValue)
        {
            await Awaitable.MainThreadAsync();
            GoogleAdsMediation.RaisePaid(adValue, adUnitId, AdPaidInfo.FormatAppOpen, _appOpenAd?.GetResponseInfo());
        }

        private async void OnAdFullScreenContentClosed()
        {
            await Awaitable.MainThreadAsync();
            OnClosed?.Invoke();
            Destroy();
            Load();
        }

        private async void OnAdFullScreenContentFailed(AdError error)
        {
            await Awaitable.MainThreadAsync();
            Debug.LogError($"[GoogleAdsAppOpen] Show failed: {error}");
            OnClosed?.Invoke();
            Destroy();
            Load();
        }
    }
}
