using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using Raccoon;
using RCKit.Ads.Raccoon;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_RACCOON_MAX
using RCKit.Ads.Max;
#endif

#if ENABLE_RACCOON_JNATIVE
using RCKit.Ads.JNative;
#endif

#if ENABLE_RACCOON_ADMOB
using RCKit.Ads.AdMob;
#endif

namespace RCKit.Ads
{
    public class AdsGameExample : MonoBehaviour
    {
        [SerializeField] private RectTransform _jNativeTransform;
        [SerializeField] private Text _text;

        public static IAdsRewarded Rewarded;
        public static IAdsInterstitial Interstitial;
        public static IAdsBanner Banner;
        public static IAppOpenAds AppOpenAds;

        public AdsManager manager;

        private void Awake()
        {
            // BuildJNative();
        }
#if ENABLE_RACCOON_ADMOB
        public void BuildAdmob()
        {
            var config = new GoogleAdsConfig();
            var consent = new GoogleUMPConsent
            {
                debugGeography = DebugGeography.EEA,
            };
            var mediation = new GoogleAdsMediation();

            var rewarded = new GoogleAdsRewardedVideo("ca-app-pub-3940256099942544/5224354917");
            var interstitial = new GoogleAdsInterstitial("ca-app-pub-3940256099942544/1033173712");
            var banner = new GoogleAdsBanner(
                "ca-app-pub-3940256099942544/9214589741",
                AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth),
                AdPosition.Bottom,
                false
            );
            var appOpen = new GoogleAdsAppOpen("ca-app-pub-3940256099942544/9257395921");

            Rewarded = new RaccoonAdsRewarded(rewarded);
            Interstitial = new RaccoonAdsInterstitial(interstitial, reloadInterval: 5);
            Banner = new RaccoonAdsBanner(banner, reloadInterval: 5);
            AppOpenAds = appOpen;

            mediation.OnMediationInitializeCompleted += Rewarded.Load;
            mediation.OnMediationInitializeCompleted += Interstitial.Load;
            mediation.OnMediationInitializeCompleted += Banner.Load;
            mediation.OnMediationInitializeCompleted += AppOpenAds.Initialize;
            mediation.OnMediationInitializeCompleted += Banner.Load;

            manager = new AdsManager(consent)
            {
                configs = new IConfigProvider[] { config },
                mediations = new IMediationProvider[] { mediation },
                removables = new IAdsRemovable[] { banner, interstitial, appOpen }
            };
            GoogleAdsMediation.OnPaid += OnAdPaid;
        }

        
#endif

#if ENABLE_RACCOON_MAX
        public void BuildMax()
        {
            var consent = new GoogleUMPConsent();
            var mediation = new MaxMediation();

            var rewarded = new MaxAdsRewardedVideo("d8c22137af5629e1");
            var interstitial = new MaxAdsInterstitial("d364647867b17bcd");
            var banner = new MaxAdsBanner("ddea66362bd61db0", false);
            var appOpen = new MaxAdsAppOpen("b19a597e9cf2615e");

            Rewarded = new RaccoonAdsRewarded(rewarded);
            Interstitial = new RaccoonAdsInterstitial(interstitial, reloadInterval: 5);
            Banner = new RaccoonAdsBanner(banner, reloadInterval: 5);
            AppOpenAds = appOpen;

            mediation.OnMediationInitializeCompleted += Rewarded.Load;
            mediation.OnMediationInitializeCompleted += Interstitial.Load;
            mediation.OnMediationInitializeCompleted += Banner.Load;
            mediation.OnMediationInitializeCompleted += AppOpenAds.Initialize;

            manager = new AdsManager(consent)
            {
                configs = new IConfigProvider[] { },
                mediations = new IMediationProvider[] { mediation },
                removables = new IAdsRemovable[] { banner, interstitial, appOpen }
            };
        }
#endif

#if ENABLE_RACCOON_JNATIVE
        public void BuildJNative()
        {
            // JNativeAds backed by GoogleMobileAds → tái sử dụng config + consent của AdMob.
            var config = new GoogleAdsConfig();
            var consent = new GoogleUMPConsent
            {
                debugGeography = DebugGeography.EEA,
                testDeviceHashedIds = new List<string>()
            };
            var mediation = new GoogleAdsMediation();

            // Banner cần Rect định vị: lấy từ RectTransform được assign trong Inspector.
            var bannerRect = GetRectFromTransform(_jNativeTransform);

            var rewarded = new JNativeAdsRewarded("ca-app-pub-3940256099942544/2247696110");
            var interstitial = new JNativeAdsInterstitial("ca-app-pub-3940256099942544/2247696110");
            // var banner = new JNativeAdsBanner("ca-app-pub-3940256099942544/2247696110", bannerRect, 30, false, false, false);
            var appOpen = new JNativeAdsAppOpen("ca-app-pub-3940256099942544/2247696110");

            Rewarded = new RaccoonAdsRewarded(rewarded);
            Interstitial = new RaccoonAdsInterstitial(interstitial, reloadInterval: 5);
            // Banner = new RaccoonAdsBanner(banner, reloadInterval: 5);
            AppOpenAds = appOpen;

            mediation.OnMediationInitializeCompleted += Rewarded.Load;
            mediation.OnMediationInitializeCompleted += Interstitial.Load;
            mediation.OnMediationInitializeCompleted += Banner.Load;
            mediation.OnMediationInitializeCompleted += AppOpenAds.Initialize;
            manager = new AdsManager(consent)
            {
                configs = new IConfigProvider[] { config }, 
                mediations = new IMediationProvider[] { mediation },
                removables = new IAdsRemovable[] { interstitial, appOpen }
            };
        }
#endif


        private static Rect GetRectFromTransform(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return new Rect(0, Screen.height - 100, Screen.width, 100);

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            var x = corners[0].x;
            var y = corners[0].y;
            var width = corners[2].x  - corners[0].x;
            var height = corners[2].y - corners[0].y;
            return new Rect(x, y, width, height);
        }

        private void Start()
        {
            // manager.Initialize();
        }

        public void RemoveAds()
        {
            // manager.AdsRemoved = true;
        }

        public void ShowRewarded()
        {
            Rewarded.Show(
                () => _text.text = "Rewarded!",
                () => _text.text = "Failed to show rewarded video."
            );
        }

        public void ShowInterstitial()
        {
            Interstitial.Show(isSuccess => _text.text = isSuccess ? "Interstitial closed." : "Interstitial failed to show.");
        }
        
        public void ShowBanner()
        {
            Banner.Show();
        }

        public void HideBanner()
        {
            Banner.Hide();
        }
        
        private void OnAdPaid(AdValue adValue)
        {
            GameAppsFlyer.SendAdRevenue(adValue.Value, adValue.CurrencyCode);
        }
    }
}