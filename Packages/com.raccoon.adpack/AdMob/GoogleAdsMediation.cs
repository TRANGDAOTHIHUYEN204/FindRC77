using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace RCKit.Ads.AdMob
{
    public class GoogleAdsMediation : IMediationProvider
    {
        public event Action OnMediationInitializeCompleted;
        public static event Action<AdValue> OnPaid;
        // Giống OnPaid nhưng kèm ad unit / format / ResponseInfo cho SDK cần schema chi tiết (TikTok).
        public static event Action<AdPaidInfo> OnPaidInfo;


        public void Initialize()
        {
#if UNITY_IOS
            MobileAds.SetiOSAppPauseOnBackground(true);
#endif
            MobileAds.Initialize(OnInitCompleted);
        }

        private async void OnInitCompleted(InitializationStatus status)
        {
            // Callback của MobileAds.Initialize có thể fire từ background thread → switch về main thread.
            await Awaitable.MainThreadAsync();
            OnMediationInitializeCompleted?.Invoke();
        }

        public static void RaisePaid(AdValue adValue, string adUnitId = null, string adFormat = null,
            ResponseInfo responseInfo = null)
        {
            OnPaid?.Invoke(adValue);
            OnPaidInfo?.Invoke(new AdPaidInfo(adValue, adUnitId, adFormat, responseInfo));
        }

        
    }

    public readonly struct AdPaidInfo
    {
        // Giá trị theo comment "banner/interstitial/rewarded/rewarded interstitial/native/splash" trong
        // doc TikTok In-app ad revenue reporting (AdMob).
        public const string FormatBanner = "banner";
        public const string FormatInterstitial = "interstitial";
        public const string FormatRewarded = "rewarded";
        public const string FormatRewardedInterstitial = "rewarded interstitial";
        public const string FormatNative = "native";
        public const string FormatAppOpen = "splash";

        public readonly AdValue AdValue;
        public readonly string AdUnitId;
        public readonly string AdFormat;
        // null với JNative (overlay chỉ trả AdValue).
        public readonly ResponseInfo ResponseInfo;

        public AdPaidInfo(AdValue adValue, string adUnitId, string adFormat, ResponseInfo responseInfo)
        {
            AdValue = adValue;
            AdUnitId = adUnitId ?? string.Empty;
            AdFormat = adFormat ?? string.Empty;
            ResponseInfo = responseInfo;
        }
    }
}
