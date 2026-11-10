using System;
using UnityEngine;

namespace RCKit.Ads.Max
{
    public class MaxAdsInterstitial : MaxAdsUnit, IAdsInterstitial, IAdsRemovable
    {
        public event Action OnLoadFailed = delegate { };
        public event Action OnLoadSucceed = delegate { };

        private bool _isRemoved;
        private bool _isLoading;
        private Action<bool> _showCallback;

        public MaxAdsInterstitial(string adUnitId) : base(adUnitId)
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHidden;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailed;
        }

        public void Load()
        {
            if (_isRemoved || _isLoading) return;
            if (MaxSdk.IsInterstitialReady(adUnitId)) return;
            _isLoading = true;
            MaxSdk.LoadInterstitial(adUnitId);
        }

        public bool IsReady()
        {
            if(_isRemoved) return false;
            return MaxSdk.IsInterstitialReady(adUnitId);
        }

        public void Show(Action<bool> onCallback)
        {
            if (!IsReady())
            {
                onCallback?.Invoke(false);
                return;
            }

            _showCallback = onCallback;
            MaxSdk.ShowInterstitial(adUnitId);
        }

        private void OnAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != this.adUnitId) return;
            _isLoading = false;
            OnLoadSucceed();
        }

        private void OnAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (adUnitId != this.adUnitId) return;
            _isLoading = false;
            Debug.LogError($"[MaxAdsInterstitial] Load failed: {errorInfo.Message}");
            OnLoadFailed();
        }

        private void OnAdHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != this.adUnitId) return;
            var cb = _showCallback;
            _showCallback = null;
            cb?.Invoke(true);
        }

        private void OnAdDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != this.adUnitId) return;
            Debug.LogError($"[MaxAdsInterstitial] Display failed: {errorInfo.Message}");
            var cb = _showCallback;
            _showCallback = null;
            cb?.Invoke(false);
        }

        public void Destroy()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnAdLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnAdLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnAdHidden;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnAdDisplayFailed;
        }

        void IAdsRemovable.OnRemoved()
        {
            _isRemoved = true;
            Destroy();
        }
        
        public void SetRefreshTime(long refreshTime)
        {
        }

        public void SetQuitButtonScale(float scale)
        {
        }
    }
}