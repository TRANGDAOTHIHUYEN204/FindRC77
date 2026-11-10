using UnityEngine;

namespace RCKit.Ads.Max
{
    public class MaxAdsAppOpen : MaxAdsUnit, IAppOpenAds, IAdsRemovable
    {
        private bool _isRemoved;
        private bool _isLoading;
        private bool _showOnLoad;
        private bool _initialized;
        private int _reloadInterval;

        public MaxAdsAppOpen(string adUnitId, int reloadInterval = 5) : base(adUnitId)
        {
            _reloadInterval = reloadInterval;
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAdLoaded;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAdLoadFailed;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAdHidden;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAdDisplayFailed;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAdRevenuePaid;
            Application.focusChanged += OnApplicationFocusChanged;
        }

        public void Initialize()
        {
            _initialized = true;
            if (_isRemoved) return;
            _showOnLoad = true;
            LoadAd();
        }

        void IAdsRemovable.OnRemoved()
        {
            _isRemoved = true;
            Unsubscribe();
        }

        private void OnApplicationFocusChanged(bool hasFocus)
        {
            if (_isRemoved) return;
            if (!_initialized) return;
            if (!hasFocus) return;
            if (IsAvailable())
                MaxSdk.ShowAppOpenAd(adUnitId);
            else LoadAd();
        }

        private bool IsAvailable()
        {
            return MaxSdk.IsAppOpenAdReady(adUnitId);
        }

        private void LoadAd()
        {
            if (_isRemoved) return;
            if (_isLoading) return;
            _isLoading = true;
            MaxSdk.LoadAppOpenAd(adUnitId);
        }

        private async void LoadAdAsync(float seconds)
        {
            await Awaitable.WaitForSecondsAsync(seconds);
            LoadAd();
        }

        private void OnAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != this.adUnitId) return;
            _isLoading = false;
            if (_isRemoved) return;

            Debug.Log("[MaxAdsAppOpen] Load succeeded.");

            if (_showOnLoad)
            {
                _showOnLoad = false;
                if (IsAvailable()) MaxSdk.ShowAppOpenAd(this.adUnitId);
            }
        }

        private void OnAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (adUnitId != this.adUnitId) return;
            _isLoading = false;
            Debug.LogError($"[MaxAdsAppOpen] Load failed: {errorInfo.Message}");
            LoadAdAsync(_reloadInterval);
        }

        private void OnAdHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != this.adUnitId) return;
            LoadAd();
        }

        private void OnAdDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != this.adUnitId) return;
            Debug.LogError($"[MaxAdsAppOpen] Display failed: {errorInfo.Message}");
            LoadAd();
        }

        private void OnAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != this.adUnitId) return;
        }

        private void Unsubscribe()
        {
            Application.focusChanged -= OnApplicationFocusChanged;
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent -= OnAdLoaded;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent -= OnAdLoadFailed;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent -= OnAdHidden;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent -= OnAdDisplayFailed;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent -= OnAdRevenuePaid;
        }

		public void SetRefreshTime(long refreshTime)
        {
        }

        public void SetQuitButtonScale(float scale)
        {
        }
    }
}