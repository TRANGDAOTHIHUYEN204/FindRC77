using System.Collections.Generic;
using System.Globalization;
#if ENABLE_APPSFLYER
using AppsFlyerSDK;
#endif
using UnityEngine;

namespace Raccoon
{
    #if ENABLE_APPSFLYER
    public class GameAppsFlyer : MonoBehaviour, 
        IAppsFlyerConversionData,
        IAppsFlyerPurchaseValidation,
        IAppsFlyerPurchaseRevenueDataSource,
        IAppsFlyerPurchaseRevenueDataSourceStoreKit2
    {
        [Header("SDK key")]
        public string devKey = "Qhno4yJY6KHmZp9uS9DRe4";

        [Header("Android is package_name, IOS is appId")]
        public string appID;
        
        [Header("Enable AppsFlyer Purchase")]
        public bool enableAppsFlyerPurchase = false;

        //public bool isDebug;

        private bool _initialized = false;
        public bool isSandbox = false;          // ⚠️ Set false before releasing!

        // Start is called before the first frame update
        void Start()
        {
            // These fields are set from the editor so do not modify!
            //******************************//
            //AppsFlyer.setIsDebug(isDebug);
            AppsFlyer.initSDK(devKey, appID, this);
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
            AppsFlyer.startSDK();
        }

        // Mark AppsFlyer CallBacks
        public void onConversionDataSuccess(string conversionData)
        {
            AppsFlyer.AFLog("didReceiveConversionData", conversionData);
            _initialized = true;
        }

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
    public class GameAppsFlyer : MonoBehaviour
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