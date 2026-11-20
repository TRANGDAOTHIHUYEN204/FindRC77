using System;
using System.Collections.Generic;
using System.Globalization;
#if ENABLE_APPSFLYER
using AppsFlyerSDK;
#endif
using UnityEngine;

namespace Raccoon
{
    // Phần dùng chung: luôn compile (có hay không AppsFlyer, mọi platform) để code game gọi được ở mọi nơi.
    public partial class GameAppsFlyer
    {
        private const string OrganicPrefKey = "rc_af_install_organic";
        private static Action<bool> _onOrganicResolved;
        private static bool? _isOrganic;
        private static bool _organicLoaded;

        /// <summary>
        /// Nguồn cài theo AppsFlyer (af_status): true = Organic, false = Non-organic, null = chưa biết.
        /// Lưu lại nên các lần mở app sau có ngay từ đầu.
        /// Chỉ có trên Android. Trên iOS luôn null: kit không đọc nguồn cài trên iOS, để game không chạy khác
        /// nhau giữa người review (luôn organic) và người dùng thật (App Store Guideline 2.3.1 / 2.3.12).
        /// </summary>
        public static bool? IsOrganic
        {
            get { EnsureOrganicLoaded(); return _isOrganic; }
        }

        /// <summary>
        /// Gọi khi biết nguồn cài (tham số: true = Organic). Đăng ký lúc đã có kết quả thì được gọi ngay.
        /// Gọi lại nếu kết quả đổi. Không bao giờ được gọi trên iOS.
        /// Ví dụ: <c>GameAppsFlyer.OnOrganicResolved += RaccoonNoti.SetOrganic;</c>
        /// </summary>
        public static event Action<bool> OnOrganicResolved
        {
            add
            {
                _onOrganicResolved += value;
                var known = IsOrganic;
                if (known.HasValue) InvokeOrganicHandler(value, known.Value);
            }
            remove { _onOrganicResolved -= value; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOrganicState()
        {
            // Cho trường hợp tắt Domain Reload trong Editor.
            _onOrganicResolved = null;
            _isOrganic = null;
            _organicLoaded = false;
        }

        private static void EnsureOrganicLoaded()
        {
            if (_organicLoaded) return;
            _organicLoaded = true;
#if !UNITY_IOS
            if (PlayerPrefs.HasKey(OrganicPrefKey)) _isOrganic = PlayerPrefs.GetInt(OrganicPrefKey) == 1;
#endif
        }

#if !UNITY_IOS
        private static void SetOrganic(bool organic)
        {
            EnsureOrganicLoaded();
            if (_isOrganic == organic) return;
            _isOrganic = organic;
            PlayerPrefs.SetInt(OrganicPrefKey, organic ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[AppsFlyer] Install type: {(organic ? "Organic" : "Non-organic")}");

            var handlers = _onOrganicResolved;
            if (handlers == null) return;
            foreach (var d in handlers.GetInvocationList()) InvokeOrganicHandler((Action<bool>)d, organic);
        }
#endif

        private static void InvokeOrganicHandler(Action<bool> handler, bool organic)
        {
            if (handler == null) return;
            try { handler(organic); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}

namespace Raccoon
{
    #if ENABLE_APPSFLYER
    public partial class GameAppsFlyer : MonoBehaviour, 
        IAppsFlyerConversionData,
        IAppsFlyerPurchaseValidation,
        IAppsFlyerPurchaseRevenueDataSource,
        IAppsFlyerPurchaseRevenueDataSourceStoreKit2
    {
        [Header("SDK key")]
        public string devKey = "Qhno4yJY6KHmZp9uS9DRe4";

        [Header("App ID")]
        [Tooltip("Android: package name. Để trống = dùng Application.identifier")]
        public string appIdAndroid;

        [Tooltip("iOS: App Store ID dạng số, ví dụ 6759906257 (bắt buộc khi build iOS)")]
        public string appIdIOS;

        [Header("Legacy - chỉ dùng khi ô platform tương ứng ở trên để trống")]
        [Tooltip("Field cũ dùng chung 2 platform. Nên chuyển giá trị sang appIdAndroid/appIdIOS rồi xoá.")]
        public string appID;

        /// <summary>App ID dùng cho platform. iOS: appIdIOS → appID. Android: appIdAndroid → appID → Application.identifier.</summary>
        public string GetAppId(bool ios)
        {
            var id = ios ? appIdIOS : appIdAndroid;
            if (string.IsNullOrWhiteSpace(id)) id = appID;
            if (!ios && string.IsNullOrWhiteSpace(id)) id = Application.identifier;
            return id?.Trim();
        }

        /// <summary>App Store ID hợp lệ: chỉ gồm chữ số (không có prefix "id").</summary>
        public static bool IsValidIOSAppId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            foreach (var c in id.Trim())
                if (c < '0' || c > '9') return false;
            return true;
        }

        [Header("Enable AppsFlyer Purchase")]
        public bool enableAppsFlyerPurchase = false;

        [Header("iOS: số giây chờ user trả lời ATT trước khi gửi install (0 = không chờ)")]
        public int attWaitTimeoutSeconds = 60;

        //public bool isDebug;

        private bool _initialized = false;
        public bool isSandbox = false;          // ⚠️ Set false before releasing!

        // Start is called before the first frame update
        void Start()
        {
            // These fields are set from the editor so do not modify!
            //******************************//
            //AppsFlyer.setIsDebug(isDebug);
#if UNITY_IOS
            var appId = GetAppId(true);
            if (!IsValidIOSAppId(appId))
                Debug.LogError($"[AppsFlyer] iOS App ID không hợp lệ: '{appId}'. Cần App Store ID dạng số (appIdIOS), nếu không AppsFlyer sẽ không ghi nhận install.");
#else
            var appId = GetAppId(false);
#endif
            AppsFlyer.initSDK(devKey, appId, this);
            //******************************/
            if (enableAppsFlyerPurchase)
            {
                AppsFlyerPurchaseConnector.init(this, Store.GOOGLE);

                // 3. Configure
                AppsFlyerPurchaseConnector.setIsSandbox(isSandbox);
                AppsFlyerPurchaseConnector.setAutoLogPurchaseRevenue(
                    AppsFlyerAutoLogPurchaseRevenueOptions
                        .AppsFlyerAutoLogPurchaseRevenueOptionsAutoRenewableSubscriptions,
                    AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsInAppPurchases
                );

                // 4. Build & start — purchases are now auto-detected!
                AppsFlyerPurchaseConnector.build();
                AppsFlyerPurchaseConnector.startObservingTransactions();
            }
#if UNITY_IOS && !UNITY_EDITOR
            // ATT popup được show sau khi UMP consent xong (RaccoonAdsManager.RequestShowATT),
            // nên phải bảo SDK chờ user trả lời ATT rồi mới gửi first_open/install, nếu không install sẽ thiếu IDFA.
            if (attWaitTimeoutSeconds > 0)
                AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval(attWaitTimeoutSeconds);
#endif
            AppsFlyer.startSDK();
        }

        // Mark AppsFlyer CallBacks
        public void onConversionDataSuccess(string conversionData)
        {
            AppsFlyer.AFLog("didReceiveConversionData", conversionData);
#if !UNITY_IOS
            ReadInstallType(conversionData);
#endif
        }

#if !UNITY_IOS
        // Chỉ Android: lấy af_status ("Organic" / "Non-organic") rồi báo ra OnOrganicResolved.
        private static void ReadInstallType(string conversionData)
        {
            try
            {
                var data = AppsFlyer.CallbackStringToDictionary(conversionData);
                if (data == null || !data.TryGetValue("af_status", out var raw) || raw == null) return;
                var status = raw.ToString();
                if (string.Equals(status, "Organic", StringComparison.OrdinalIgnoreCase)) SetOrganic(true);
                else if (string.Equals(status, "Non-organic", StringComparison.OrdinalIgnoreCase)) SetOrganic(false);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AppsFlyer] Không đọc được af_status: " + e.Message);
            }
        }
#endif

        public void onConversionDataFail(string error)
        {
            AppsFlyer.AFLog("didReceiveConversionDataWithError", error);
        }

        public void onAppOpenAttribution(string attributionData)
        {
            AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
        }

        public void onAppOpenAttributionFailure(string error)
        {
            AppsFlyer.AFLog("onAppOpenAttributionFailure", error);
        }

        private static readonly float microvalue = 1000000;
        public static void SendAdRevenue(string ad_unit, string ad_type, double revenue, string currency)
        {
            RegionInfo currentRegion = RegionInfo.CurrentRegion;
            // Get the country name
            string countryName = currentRegion.TwoLetterISORegionName;
            var realRevenue = (double)revenue / microvalue;
            Dictionary<string, string> additionalParams = new Dictionary<string, string>();

            additionalParams.Add(AdRevenueScheme.COUNTRY, countryName);
            additionalParams.Add(AdRevenueScheme.AD_UNIT, ad_unit);
            additionalParams.Add(AdRevenueScheme.AD_TYPE, ad_type);
            additionalParams.Add(AdRevenueScheme.PLACEMENT, "place");

            // Build ad revenue object
            AFAdRevenueData logRevenue = new AFAdRevenueData(
                monetization: "admob",                 // required
                mediation: MediationNetwork.GoogleAdMob, // enum (optional if known)
                currency: currency,                           // ISO 4217
                revenue: realRevenue                          // double
            );

            AppsFlyer.logAdRevenue(logRevenue, additionalParams);
            Debug.Log($"[AppsFlyer] Logged Ad Revenue: {realRevenue} {currency}, type: {ad_type}, unit: {ad_unit}");
        }
        
        public static void SendAdRevenue(double revenue, string currency)
        {
            RegionInfo currentRegion = RegionInfo.CurrentRegion;
            // Get the country name
            string countryName = currentRegion.TwoLetterISORegionName;
            var realRevenue = (double)revenue / microvalue;
            Dictionary<string, string> additionalParams = new Dictionary<string, string>();

            additionalParams.Add(AdRevenueScheme.COUNTRY, countryName);
            additionalParams.Add(AdRevenueScheme.PLACEMENT, "place");

            // Build ad revenue object
            AFAdRevenueData logRevenue = new AFAdRevenueData(
                monetization: "admob",                 // required
                mediation: MediationNetwork.GoogleAdMob, // enum (optional if known)
                currency: currency,                           // ISO 4217
                revenue: realRevenue                          // double
            );

            AppsFlyer.logAdRevenue(logRevenue, additionalParams);
            Debug.Log($"[AppsFlyer] Logged Ad Revenue: {realRevenue} {currency}");
        }

        public void didReceivePurchaseRevenueValidationInfo(string validationInfo)
        {
            AppsFlyer.AFLog("IAP Validated", validationInfo);
        }

        public void didReceivePurchaseRevenueError(string error)
        {
            AppsFlyer.AFLog("IAP Validation Error", error);
        }

        public Dictionary<string, object> PurchaseRevenueAdditionalParametersForProducts(HashSet<object> products, HashSet<object> transactions)
        {
            return new Dictionary<string, object> {
            };
        }

        public Dictionary<string, object> PurchaseRevenueAdditionalParametersStoreKit2ForProducts(HashSet<object> products, HashSet<object> transactions)
        {
            return new Dictionary<string, object> {
            };
        }
    }
#else
    public partial class GameAppsFlyer : MonoBehaviour
    {
        private static readonly float microvalue = 1000000;
        public static void SendAdRevenue(string ad_unit, string ad_type, double revenue, string currency)
        {
            // Get the country name
            var realRevenue = (double)revenue / microvalue;
            Debug.Log($"[AppsFlyer] Logged Ad Revenue: {realRevenue} {currency}, type: {ad_type}, unit: {ad_unit}");
        }
        
        public static void SendAdRevenue(double revenue, string currency)
        {
            // Get the country name
            var realRevenue = (double)revenue / microvalue;
            Debug.Log($"[AppsFlyer] Logged Ad Revenue: {realRevenue} {currency}");
        }
    }
#endif
}