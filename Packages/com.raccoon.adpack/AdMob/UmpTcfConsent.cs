using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace RCKit.Ads.AdMob
{
    // Đọc consent của từng vendor (Unity Ads, TikTok...) từ dữ liệu IAB TCF v2 mà UMP ghi ra,
    // dùng chung cho các SDK UA cần tự truyền consent trước khi init / gửi data.
    public static class UmpTcfConsent
    {
        // GoogleUMPConsent có watchdog 8s tự bắn OnDismissed khi form vẫn đang mở,
        // nên phải tự chờ user chọn xong trên form UMP rồi mới đọc consent.
        private const float ConsentWaitTimeoutSeconds = 60f;
        private const float ConsentPollIntervalSeconds = 0.5f;

        public static async Awaitable WaitForConsentAsync()
        {
            var waited = 0f;
            while (ConsentInformation.ConsentStatus == ConsentStatus.Required && waited < ConsentWaitTimeoutSeconds)
            {
                await Awaitable.WaitForSecondsAsync(ConsentPollIntervalSeconds);
                waited += ConsentPollIntervalSeconds;
            }
        }

        // ConsentInformation chỉ expose ConsentStatus/CanRequestAds; lựa chọn chi tiết của user được UMP
        // ghi theo chuẩn IAB TCF v2 vào default SharedPreferences (Android) / NSUserDefaults (iOS).
        private const string KeyGdprApplies = "IABTCF_gdprApplies";
        private const string KeyPurposeConsents = "IABTCF_PurposeConsents";
        private const string KeyVendorConsents = "IABTCF_VendorConsents";

        private static bool GdprApplies => ReadTcfInt(KeyGdprApplies) == 1;

        // Chuỗi TCF dạng "0101...", ký tự thứ (id - 1) là consent của purpose/vendor có id đó.
        private static bool HasTcfBit(string key, int id)
        {
            var bits = ReadTcfString(key);
            return bits.Length >= id && bits[id - 1] == '1';
        }

        // Ngoài vùng GDPR => true. Trong vùng GDPR => user phải consent cho vendorId (theo IAB Global
        // Vendor List) và đủ các purpose vendor đó khai báo. Không dùng CanRequestAds() vì nó vẫn true
        // khi user từ chối.
        public static bool HasVendorConsent(int vendorId, int[] purposes, string logTag)
        {
            if (ConsentInformation.ConsentStatus == ConsentStatus.NotRequired) return true;
            try
            {
                if (!GdprApplies) return ConsentInformation.CanRequestAds();
                if (!HasTcfBit(KeyVendorConsents, vendorId)) return false;
                foreach (var purpose in purposes)
                {
                    if (!HasTcfBit(KeyPurposeConsents, purpose)) return false;
                }
                return true;
            }
            catch (System.Exception e)
            {
                // Key TCF sai kiểu (ClassCastException...) => coi như chưa có consent.
                Debug.LogWarning($"{logTag} Read TCF failed: {e.Message}");
                return false;
            }
        }

        private static string ReadTcfString(string key)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var prefs = GetDefaultSharedPreferences();
            return prefs.Call<string>("getString", key, string.Empty) ?? string.Empty;
#else
            return PlayerPrefs.GetString(key, string.Empty);
#endif
        }

        private static int ReadTcfInt(string key)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var prefs = GetDefaultSharedPreferences();
            return prefs.Call<int>("getInt", key, 0);
#else
            return PlayerPrefs.GetInt(key, 0);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // PlayerPrefs của Unity ghi file riêng (<package>.v2.playerprefs) nên không đọc được TCF,
        // phải mở default SharedPreferences (<package>_preferences) mà UMP ghi vào.
        private static AndroidJavaObject GetDefaultSharedPreferences()
        {
            // AndroidApplication.currentContext được Unity cache, không được Dispose.
            var context = UnityEngine.Android.AndroidApplication.currentContext;
            var name = context.Call<string>("getPackageName") + "_preferences";
            return context.Call<AndroidJavaObject>("getSharedPreferences", name, 0);
        }
#endif
    }
}
