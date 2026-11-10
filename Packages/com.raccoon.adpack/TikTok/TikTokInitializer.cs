#if ENABLE_TIKTOK_SDK && ENABLE_RACCOON_ADMOB
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RCKit.Ads.AdMob;
using UnityEngine;

namespace RCKit.Ads.TikTok
{
    // TikTok App Events SDK (module TikTok trong Pangle/TikTok Combined SDK, pag-sdk >= 7.7) chỉ dùng cho UA:
    // gửi install/launch + ad revenue (IAA D7 VBO), không show ads Pangle. Hiện mới hỗ trợ Android.
    // Init với disableAutoStart để event chỉ queue local, chờ UMP xong mới startTrack nếu có consent.
    public static class TikTokInitializer
    {
        private const string LogTag = "[TikTok]";

        // Theo IAB Global Vendor List: "TikTok Ad Network" id 986, các purpose cần consent: 1, 2, 3, 4, 10.
        private const int TikTokVendorId = 986;
        private static readonly int[] TikTokConsentPurposes = { 1, 2, 3, 4, 10 };

        private static bool initializing;

        public static void Initialize()
        {
            if (initializing) return;
            initializing = true;

#if UNITY_ANDROID && !UNITY_EDITOR
            InitializeAndroid();
#else
            Debug.Log($"{LogTag} Only supported on Android device, skip init");
#endif
        }

        #region Ad revenue

        // Schema "admob_sdk" theo doc TikTok In-app ad revenue reporting; không field nào được null.
        private static string BuildAdMobRevenueJson(AdPaidInfo info)
        {
            var adSourceName = string.Empty;
            var adSourceId = string.Empty;
            var adSourceInstanceName = string.Empty;
            var adSourceInstanceId = string.Empty;
            var mediationGroupName = string.Empty;
            var mediationAbTestName = string.Empty;
            var mediationAbTestVariant = string.Empty;

            var adapterInfo = info.ResponseInfo?.GetLoadedAdapterResponseInfo();
            if (adapterInfo != null)
            {
                adSourceName = adapterInfo.AdSourceName ?? string.Empty;
                adSourceId = adapterInfo.AdSourceId ?? string.Empty;
                adSourceInstanceName = adapterInfo.AdSourceInstanceName ?? string.Empty;
                adSourceInstanceId = adapterInfo.AdSourceInstanceId ?? string.Empty;
            }

            var extras = info.ResponseInfo?.GetResponseExtras();
            if (extras != null)
            {
                mediationGroupName = GetOrEmpty(extras, "mediation_group_name");
                mediationAbTestName = GetOrEmpty(extras, "mediation_ab_test_name");
                mediationAbTestVariant = GetOrEmpty(extras, "mediation_ab_test_variant");
            }

            var sb = new StringBuilder("{");
            AppendString(sb, "device_ad_mediation_platform", "admob_sdk");
            AppendRaw(sb, "value", info.AdValue.Value.ToString(CultureInfo.InvariantCulture));
            AppendString(sb, "currency_code", info.AdValue.CurrencyCode ?? string.Empty);
            AppendRaw(sb, "precision", ((int)info.AdValue.Precision).ToString(CultureInfo.InvariantCulture));
            AppendString(sb, "ad_unit_id", info.AdUnitId);
            AppendString(sb, "ad_source_name", adSourceName);
            AppendString(sb, "ad_source_id", adSourceId);
            AppendString(sb, "ad_source_instance_name", adSourceInstanceName);
            AppendString(sb, "ad_source_instance_id", adSourceInstanceId);
            AppendString(sb, "mediation_group_name", mediationGroupName);
            AppendString(sb, "mediation_ab_test_name", mediationAbTestName);
            AppendString(sb, "mediation_ab_test_variant", mediationAbTestVariant);
            AppendString(sb, "ad_format", info.AdFormat);
            sb.Length--; // bỏ dấu phẩy cuối
            sb.Append('}');
            return sb.ToString();
        }

        private static string GetOrEmpty(Dictionary<string, string> dict, string key)
        {
            return dict.TryGetValue(key, out var value) && value != null ? value : string.Empty;
        }

        private static void AppendRaw(StringBuilder sb, string key, string rawValue)
        {
            sb.Append('"').Append(key).Append("\":").Append(rawValue).Append(',');
        }

        private static void AppendString(StringBuilder sb, string key, string value)
        {
            sb.Append('"').Append(key).Append("\":\"");
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append("\",");
        }

        #endregion

#if UNITY_ANDROID && !UNITY_EDITOR
        private const string SdkClass = "com.tiktok.TikTokBusinessSdk";

        private static bool debugMode;

        private static async void InitializeAndroid()
        {
            var settings = Resources.Load<TikTokSettings>(TikTokSettings.ResourcesName);
            if (settings == null)
            {
                Debug.LogWarning($"{LogTag} Missing Resources/{TikTokSettings.ResourcesName} asset, skip init");
                return;
            }

            if (string.IsNullOrEmpty(settings.tiktok_app_id_android) || string.IsNullOrEmpty(settings.app_secret_android))
            {
                Debug.LogWarning($"{LogTag} TikTok App ID / App Secret is empty, skip init");
                return;
            }

            debugMode = settings.debug_mode;
            InitializeSdk(settings.app_secret_android.Trim(), Application.identifier,
                settings.tiktok_app_id_android.Replace(" ", ""), debugMode);
            GoogleAdsMediation.OnPaidInfo += OnAdPaid;

            await UmpTcfConsent.WaitForConsentAsync();
            var consent = UmpTcfConsent.HasVendorConsent(TikTokVendorId, TikTokConsentPurposes, LogTag);
            Debug.Log($"{LogTag} Consent: {consent}");
            if (!consent) return;

            RunOnUiThread(() =>
            {
                using var sdk = new AndroidJavaClass(SdkClass);
                sdk.CallStatic("startTrack");
            });
        }

        private static void OnAdPaid(AdPaidInfo info)
        {
            var json = BuildAdMobRevenueJson(info);
            // SDK không log payload, in ra để đối chiếu với Events Manager khi test.
            if (debugMode) Debug.Log($"{LogTag} ad_revenue: {json}");
            RunOnUiThread(() => TrackAdRevenue(json));
        }

        // SDK chỉ hỗ trợ init/gọi trên main thread của Android (UI thread), không phải Unity main thread.
        // Mọi call đều post qua runOnUiThread nên giữ đúng thứ tự: init -> startTrack/trackTTEvent.
        private static void RunOnUiThread(System.Action action)
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    action();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"{LogTag} Native call failed: {e.Message}");
                }
            }));
        }

        private static void InitializeSdk(string appSecret, string appId, string ttAppId, bool debugMode)
        {
            RunOnUiThread(() =>
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var config = new AndroidJavaObject("com.tiktok.TikTokBusinessSdk$TTConfig", activity, appSecret);
                // Các setter trả về chính TTConfig (builder), dispose ref trả về ngay.
                config.Call<AndroidJavaObject>("setAppId", appId)?.Dispose();
                config.Call<AndroidJavaObject>("setTTAppId", ttAppId)?.Dispose();
                config.Call<AndroidJavaObject>("disableAutoStart")?.Dispose();
                // Game IAA không kèm Google Play Billing => tắt auto track Purchase.
                config.Call<AndroidJavaObject>("disableAutoIapTrack")?.Dispose();
                if (debugMode)
                {
                    config.Call<AndroidJavaObject>("openDebugMode")?.Dispose();
                    using var logLevelClass = new AndroidJavaClass("com.tiktok.TikTokBusinessSdk$LogLevel");
                    using var debugLevel = logLevelClass.GetStatic<AndroidJavaObject>("DEBUG");
                    config.Call<AndroidJavaObject>("setLogLevel", debugLevel)?.Dispose();
                }

                using var sdk = new AndroidJavaClass(SdkClass);
                sdk.CallStatic("initializeSdk", config, new InitCallback());
            });
        }

        private static void TrackAdRevenue(string adRevenueJson)
        {
            using var json = new AndroidJavaObject("org.json.JSONObject", adRevenueJson);
            using var eventClass = new AndroidJavaClass("com.tiktok.appevents.base.TTAdRevenueEvent");
            using var builder = eventClass.CallStatic<AndroidJavaObject>("newBuilder", json);
            using var adRevenueEvent = builder.Call<AndroidJavaObject>("build");
            using var sdk = new AndroidJavaClass(SdkClass);
            sdk.CallStatic("trackTTEvent", adRevenueEvent);
        }

        private class InitCallback : AndroidJavaProxy
        {
            public InitCallback() : base("com.tiktok.TikTokBusinessSdk$TTInitCallback") { }

            public void success()
            {
                Debug.Log($"{LogTag} Initialization complete");
            }

            public void fail(int code, string msg)
            {
                Debug.LogWarning($"{LogTag} Initialization failed: {code} - {msg}");
            }
        }
#endif
    }
}
#endif
