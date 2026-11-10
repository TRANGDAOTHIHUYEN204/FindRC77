using System;
using UnityEngine;

namespace RCKit.Ads.Max
{
    public class MaxAdsRewardedVideo : MaxAdsUnit, IAdsRewarded
    {
        public event Action OnLoadFailed = delegate { };
        public event Action OnLoadSucceed = delegate { };

        private Action _onRewarded;
        private Action _onFailed;
        private bool _earned;
        private bool _isLoading;

        public MaxAdsRewardedVideo(string adUnitId) : base(adUnitId)
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnAdLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnAdLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnAdHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailed;
        }

        public void Load()
        {
            if (_isLoading) return;
            if (MaxSdk.IsRewardedAdReady(adUnitId)) return;
            _isLoading = true;
            MaxSdk.LoadRewardedAd(adUnitId);
        }

        public bool IsReady() => MaxSdk.IsRewardedAdReady(adUnitId);

        public void Show(Action onRewarded, Action onFailed)
        {
            if (!IsReady())
            {
                onFailed?.Invoke();
                return;
            }

            _onRewarded = onRewarded;
            _onFailed = onFailed;
            _earned = false;
            MaxSdk.ShowRewardedAd(adUnitId);
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
            Debug.LogError($"[MaxAdsRewardedVideo] Load failed: {errorInfo.Message}");
            OnLoadFailed();
        }

        private void OnAdReceivedReward(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != this.adUnitId) return;
            // Đánh dấu đã rewarded; callback chính thức được invoke ở OnAdHidden
            // để đảm bảo user đã đóng ad trước khi nhận thưởng.
            _earned = true;
        }

        private void OnAdHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != this.adUnitId) return;

            var rewardedCb = _onRewarded;
            var failedCb = _onFailed;
            _onRewarded = null;
            _onFailed = null;

            if (_earned)
            {
                rewardedCb?.Invoke();
            }
            else
            {
                failedCb?.Invoke();
            }
        }

        private void OnAdDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != this.adUnitId) return;
            Debug.LogError($"[MaxAdsRewardedVideo] Display failed: {errorInfo.Message}");

            var failedCb = _onFailed;
            _onRewarded = null;
            _onFailed = null;
            failedCb?.Invoke();
        }

        public void Destroy()
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnAdLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnAdLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnAdReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnAdHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnAdDisplayFailed;
        }
        
        public void SetRefreshTime(long refreshTime)
        {
        }

        public void SetQuitButtonScale(float scale)
        {
        }
    }
}