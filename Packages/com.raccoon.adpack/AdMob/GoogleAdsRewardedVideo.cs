using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace RCKit.Ads.AdMob
{
    public class GoogleAdsRewardedVideo : GoogleAdsUnit, IAdsRewarded
    {
        public event Action OnAdShow = delegate { };
        public event Action OnLoadFailed = delegate { };
        public event Action OnLoadSucceed = delegate { };
        public event Action OnClosed = delegate { };

        private RewardedAd _rewardedAd;
        private bool _isLoading;
        private bool _isShowing;

        public GoogleAdsRewardedVideo(string adUnitId) : base(adUnitId) { }

        public void Load()
        {
            if (_isLoading) return;
            if (_rewardedAd != null && _rewardedAd.CanShowAd()) return;
            _isLoading = true;
            Destroy();
            RewardedAd.Load(adUnitId, new AdRequest(), OnAdLoaded);
        }

        private async void OnAdLoaded(RewardedAd ad, LoadAdError error)
        {
            // AdMob callbacks có thể được raise từ background thread → switch về main thread.
            await Awaitable.MainThreadAsync();

            _isLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogError($"[GoogleAdsRewardedVideo] Load failed: {error?.GetMessage()}");
                OnLoadFailed();
                return;
            }

            _rewardedAd = ad;
            OnLoadSucceed();
        }

        public bool IsReady() => !_isShowing && _rewardedAd != null && _rewardedAd.CanShowAd();

        private async void OnAdsPaid(AdValue adValue)
        {
            // Callback impression/revenue từ AdMob → switch về main thread trước khi forward lên Mediation.
            await Awaitable.MainThreadAsync();
            GoogleAdsMediation.RaisePaid(adValue, adUnitId, AdPaidInfo.FormatRewarded, _rewardedAd?.GetResponseInfo());
        }

        public void Show(Action onRewarded, Action onFailed)
        {
            if (!IsReady())
            {
                onFailed?.Invoke();
                return;
            }

            OnAdShow();
            _isShowing = true;
            var earned = false;
            var invoked = false;

            _rewardedAd.OnAdPaid += OnAdsPaid;
            _rewardedAd.OnAdFullScreenContentClosed += async () =>
            {
                await Awaitable.MainThreadAsync();
                Finish();
            };

            _rewardedAd.OnAdFullScreenContentFailed += async error =>
            {
                await Awaitable.MainThreadAsync();
                Debug.LogError($"[GoogleAdsRewardedVideo] Show failed: {error?.GetMessage()}");
                Finish();
            };

            _rewardedAd.Show(_ =>
            {
                // Đánh dấu đã rewarded; callback chính thức được invoke ở OnAdFullScreenContentClosed
                // để đảm bảo user đã đóng ad trước khi nhận thưởng.
                earned = true;
            });
            return;

            void Finish()
            {
                if (invoked) return;
                invoked = true;
                if (earned) onRewarded?.Invoke();
                else onFailed?.Invoke();
                OnClosed();
                Destroy();
            }
        }

        public void Destroy()
        {
            _isShowing = false;
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
        }

        public void SetRefreshTime(long refreshTime)
        {
        }

        public void SetQuitButtonScale(float scale)
        {
        }
    }
}