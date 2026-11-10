using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace RCKit.Ads.AdMob
{
    public class GoogleAdsRewardedInterstitial : GoogleAdsUnit, IAdsRewarded
    {
        public event Action OnAdShow = delegate { };
        public event Action OnLoadFailed = delegate { };
        public event Action OnLoadSucceed = delegate { };
        public event Action OnClosed = delegate { };

        private RewardedInterstitialAd _rewardedInterstitialAd;
        private bool _isLoading;
        private bool _isShowing;

        public GoogleAdsRewardedInterstitial(string adUnitId) : base(adUnitId) { }

        public void Load()
        {
            if (_isLoading) return;
            if (_rewardedInterstitialAd != null && _rewardedInterstitialAd.CanShowAd()) return;

            Destroy();
            _isLoading = true;
            RewardedInterstitialAd.Load(adUnitId, new AdRequest(), OnAdLoaded);
        }

        private async void OnAdLoaded(RewardedInterstitialAd ad, LoadAdError error)
        {
            // AdMob callbacks có thể được raise từ background thread → switch về main thread.
            await Awaitable.MainThreadAsync();

            _isLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogError($"[GoogleAdsRewardedInterstitial] Load failed: {error?.GetMessage()}");
                OnLoadFailed();
                return;
            }

            _rewardedInterstitialAd = ad;
            OnLoadSucceed();
        }

        public bool IsReady() => !_isShowing && _rewardedInterstitialAd != null && _rewardedInterstitialAd.CanShowAd();

        private async void OnAdsPaid(AdValue adValue)
        {
            // Callback impression/revenue từ AdMob → switch về main thread trước khi forward lên Mediation.
            await Awaitable.MainThreadAsync();
            GoogleAdsMediation.RaisePaid(adValue, adUnitId, AdPaidInfo.FormatRewardedInterstitial, _rewardedInterstitialAd?.GetResponseInfo());
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

            _rewardedInterstitialAd.OnAdPaid += OnAdsPaid;
            _rewardedInterstitialAd.OnAdFullScreenContentClosed += async () =>
            {
                await Awaitable.MainThreadAsync();
                Finish();
            };

            _rewardedInterstitialAd.OnAdFullScreenContentFailed += async error =>
            {
                await Awaitable.MainThreadAsync();
                Debug.LogError($"[GoogleAdsRewardedInterstitial] Show failed: {error?.GetMessage()}");
                Finish();
            };

            _rewardedInterstitialAd.Show(_ =>
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
            if (_rewardedInterstitialAd != null)
            {
                _rewardedInterstitialAd.Destroy();
                _rewardedInterstitialAd = null;
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