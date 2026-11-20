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
            RaisePaid(adValue, adUnitId, adFormat, responseInfo, AdSource.FromResponseInfo(responseInfo));
        }

        // JNative không có ResponseInfo của GMA Unity, plugin trả ad source riêng.
        public static void RaisePaid(AdValue adValue, string adUnitId, string adFormat, AdSource source)
        {
            RaisePaid(adValue, adUnitId, adFormat, null, source);
        }

        private static void RaisePaid(AdValue adValue, string adUnitId, string adFormat,
            ResponseInfo responseInfo, AdSource source)
        {
            OnPaid?.Invoke(adValue);
            OnPaidInfo?.Invoke(new AdPaidInfo(adValue, adUnitId, adFormat, responseInfo, source));
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
        // null với JNative, dùng Source thay thế.
        public readonly ResponseInfo ResponseInfo;
        public readonly AdSource Source;

        public AdPaidInfo(AdValue adValue, string adUnitId, string adFormat, ResponseInfo responseInfo, AdSource source)
        {
            AdValue = adValue;
            AdUnitId = adUnitId ?? string.Empty;
            AdFormat = adFormat ?? string.Empty;
            ResponseInfo = responseInfo;
            Source = source;
        }
    }

    // Ad source (network) thực sự đã serve ad; field rỗng khi SDK không trả.
    public readonly struct AdSource
    {
        private readonly string _name;
        private readonly string _id;
        private readonly string _instanceName;
        private readonly string _instanceId;
        private readonly string _mediationGroupName;
        private readonly string _mediationAbTestName;
        private readonly string _mediationAbTestVariant;

        // Property thay vì field để default(AdSource) cũng không trả null.
        public string Name => _name ?? string.Empty;
        public string Id => _id ?? string.Empty;
        public string InstanceName => _instanceName ?? string.Empty;
        public string InstanceId => _instanceId ?? string.Empty;
        public string MediationGroupName => _mediationGroupName ?? string.Empty;
        public string MediationAbTestName => _mediationAbTestName ?? string.Empty;
        public string MediationAbTestVariant => _mediationAbTestVariant ?? string.Empty;

        public AdSource(string name, string id, string instanceName, string instanceId,
            string mediationGroupName, string mediationAbTestName, string mediationAbTestVariant)
        {
            _name = name;
            _id = id;
            _instanceName = instanceName;
            _instanceId = instanceId;
            _mediationGroupName = mediationGroupName;
            _mediationAbTestName = mediationAbTestName;
            _mediationAbTestVariant = mediationAbTestVariant;
        }

        public static AdSource FromResponseInfo(ResponseInfo responseInfo)
        {
            var adapter = responseInfo?.GetLoadedAdapterResponseInfo();
            var extras = responseInfo?.GetResponseExtras();
            return new AdSource(adapter?.AdSourceName, adapter?.AdSourceId, adapter?.AdSourceInstanceName,
                adapter?.AdSourceInstanceId, Get(extras, "mediation_group_name"),
                Get(extras, "mediation_ab_test_name"), Get(extras, "mediation_ab_test_variant"));
        }

        private static string Get(System.Collections.Generic.Dictionary<string, string> dict, string key)
        {
            return dict != null && dict.TryGetValue(key, out var value) ? value : null;
        }
    }
}
