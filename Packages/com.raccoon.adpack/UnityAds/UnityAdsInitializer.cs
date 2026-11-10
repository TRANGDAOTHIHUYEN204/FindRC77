#if ENABLE_UNITY_AD_SDK && ENABLE_RACCOON_ADMOB
using RCKit.Ads.AdMob;
using UnityEngine;
using UnityEngine.Advertisements;

namespace RCKit.Ads.UnityAds
{
    // Unity Ads chỉ dùng cho UA (advertiser-only): init SDK để gửi signal cho Unity Vector, không show ads Unity.
    // Phải gọi sau khi UMP (AdMob) gather consent xong để truyền consent sang Unity Ads trước khi init.
    public class UnityAdsInitializer : IUnityAdsInitializationListener
    {
        private static UnityAdsInitializer instance;
        private static bool initializing;

        public static async void Initialize()
        {
            if (initializing) return;
            initializing = true;

            await UmpTcfConsent.WaitForConsentAsync();
            InitializeAfterConsent();
        }

        private static void InitializeAfterConsent()
        {
            var settings = Resources.Load<UnityAdsSettings>(UnityAdsSettings.ResourcesName);
            if (settings == null)
            {
                Debug.LogWarning($"[UnityAds] Missing Resources/{UnityAdsSettings.ResourcesName} asset, skip init");
                return;
            }

            var gameId = settings.GameId;
            if (string.IsNullOrEmpty(gameId) || !Advertisement.isSupported || Advertisement.isInitialized) return;

            SetConsent(UmpTcfConsent.HasVendorConsent(UnityVendorId, UnityConsentPurposes, "[UnityAds]"));

            instance = new UnityAdsInitializer();
            Advertisement.Initialize(gameId, false, instance);
        }

        private static void SetConsent(bool consent)
        {
            var privacyMetaData = new MetaData("privacy");
            privacyMetaData.Set("consent", consent ? "true" : "false");
            Advertisement.SetMetaData(privacyMetaData);

            Debug.Log($"[UnityAds] Set consent: {consent}");
        }

        // Theo IAB Global Vendor List: "Unity Technologies SF" id 1549, các purpose cần consent: 1, 3, 4.
        private const int UnityVendorId = 1549;
        private static readonly int[] UnityConsentPurposes = { 1, 3, 4 };

        public void OnInitializationComplete()
        {
            Debug.Log("[UnityAds] Initialization complete");
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            Debug.LogWarning($"[UnityAds] Initialization failed: {error} - {message}");
        }
    }
}
#endif
