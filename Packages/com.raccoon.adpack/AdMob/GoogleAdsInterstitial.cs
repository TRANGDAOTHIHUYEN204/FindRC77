using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace RCKit.Ads.AdMob
{
    public class GoogleAdsInterstitial : GoogleAdsUnit, IAdsInterstitial, IAdsRemovable
    {
        public event Action OnAdShow = delegate { };
        public event Action OnLoadFailed = delegate { };
        public event Action OnLoadSucceed = delegate { };
        public event Action OnClosed = delegate { };

        private InterstitialAd _interstitialAd;
        private bool _isRemoved;
        private bool _isLoading;
        private bool _isShowing;

        public GoogleAdsInterstitial(string adUnitId) : base(adUnitId) { }

        public void Load()
        {
            if (_isLoading) return;
            if (_interstitialAd != null && _interstitialAd.CanShowAd()) return;
            Destroy();
            _isLoading = true;
            InterstitialAd.Load(adUnitId, new AdRequest(), OnAdLoaded);
        }

        private async void OnAdLoaded(InterstitialAd ad, LoadAdError error)
        {
            // AdMob callbacks có thể được raise từ background thread → switch về main thread.
            await Awaitable.MainThreadAsync();

            _isLoading = false;
            if (error != null || ad == null)
            {
                Debug.LogError($"[GoogleAdsInterstitial] Load failed: {error?.GetMessage()}");
                OnLoadFailed();
                return;
            }

            _interstitialAd = ad;
            OnLoadSucceed();
        }

        public bool IsReady()
        {
            if (_isRemoved || _isShowing) return false;
            return _interstitialAd != null && _interstitialAd.CanShowAd();
        }

        private async void OnAdsPaid(AdValue adValue)
        {
            // Callback impression/revenue từ AdMob → switch về main thread trước khi forward lên Mediation.
            await Awaitable.MainThreadAsync();
            GoogleAdsMediation.RaisePaid(adValue, adUnitId, AdPaidInfo.FormatInterstitial, _interstitialAd?.GetResponseInfo());
        }

        public void Show(Action<bool> onCallback)
        {
            if (!IsReady())
            {
                onCallback?.Invoke(false);
                return;
            }

            OnAdShow();
            _isShowing = true;
            var invoked = false;

            _interstitialAd.OnAdFullScreenContentClosed += async () => {
                await Awaitable.MainThreadAsync();
                Finish(true);
            };

            _interstitialAd.OnAdFullScreenContentFailed += async error => {
                await Awaitable.MainThreadAsync();
                Debug.LogError($"[GoogleAdsInterstitial] Show failed: {error?.GetMessage()}");
                Finish(false);
            };

            _interstitialAd.OnAdPaid += OnAdsPaid;
            _interstitialAd.Show();

            return;

            void Finish(bool isSuccess)
            {
                if (invoked) return;
                invoked = true;
                onCallback?.Invoke(isSuccess);
                OnClosed();
                Destroy();
            }
        }

        public void Destroy()
        {
            _isShowing = false;
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }
        }

        public void SetRefreshTime(long refreshTime)
        {
        }

        public void SetQuitButtonScale(float scale)
        {
        }

        public void SetQuitButtonCoverScale(float scale)
        {
            
        }

        void IAdsRemovable.OnRemoved()
        {
            _isRemoved = true;
            Destroy();
        }
    }
}