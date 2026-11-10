using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCKit.Ads.Main
{
    // [CreateAssetMenu(fileName = "RCKit", menuName = "Ads/AdUnitData")]
    public class AdsScriptableObj : ScriptableObject
    {
        public bool use_test_ad;
        [SerializeField] private List<AdUnitConfig>  AdUnitConfigs = new List<AdUnitConfig>();

        private List<AdUnitConfig> AdUnitConfigsTest;
        
        public List<AdUnitConfig> GetAdUnitConfigs
        {
            get
            {
                if (!use_test_ad) return AdUnitConfigs;
                AdUnitConfigsTest ??= BuildTestAdUnitConfigs();
                return AdUnitConfigsTest;
            }
        }

        // Clone AdUnitConfigs sang AdUnitConfigsTest, thay ad_id bằng test ad unit id của AdMob
        // (giữ nguyên ad_type/removable), để bật use_test_ad là chạy được ngay mà không cần
        // config tay danh sách test riêng.
        private List<AdUnitConfig> BuildTestAdUnitConfigs()
        {
            var result = new List<AdUnitConfig>(AdUnitConfigs.Count);
            foreach (var config in AdUnitConfigs)
            {
                var testAdId = GetTestAdUnitId(config.ad_type);
                result.Add(new AdUnitConfig
                {
                    ad_type = config.ad_type,
                    ad_id_android = testAdId,
                    ad_id_ios = testAdId,
                    is_multi_ad = config.is_multi_ad,
                    ad_id_android_2 = config.is_multi_ad ? testAdId : config.ad_id_android_2,
                    ad_id_ios_2 = config.is_multi_ad ? testAdId : config.ad_id_ios_2,
                    removable = config.removable,
                    preload_ad = config.preload_ad,
                    reload_on_fail = config.reload_on_fail,
                    time_close_native = config.time_close_native,
                    layout_quit_native = config.layout_quit_native,
                    layout_template_native = config.layout_template_native,
                    layout_support_media = config.layout_support_media,
                    has_close = config.has_close,
                    use_only_portrait = config.use_only_portrait,
                });
            }
            return result;
        }

        // Test ad unit id chính thức của AdMob (https://developers.google.com/admob/unity/test-ads).
        // Ad "native" (JNative/NativeOverlay) dùng chung 1 id Native Advanced cho mọi format,
        // đúng convention đã dùng trong AdsGameExample.cs (BuildJNative).
        private static string GetTestAdUnitId(AD_TYPE type)
        {
            var isNative = (int)type / 10 % 2 == 1;
            var isBanner = (int)type is >= 80 and < 100;
            var isReward = (int)type is >= 40 and < 60;
            var isAppOpen = (int)type is >= 60 and < 80;

            if (isNative) return "ca-app-pub-3940256099942544/2247696110";
            if (isBanner) return "ca-app-pub-3940256099942544/9214589741";
            if (isReward) return "ca-app-pub-3940256099942544/5224354917";
            if (isAppOpen) return "ca-app-pub-3940256099942544/9257395921";
            return "ca-app-pub-3940256099942544/1033173712"; // interstitial
        }
    }

    [Serializable]
    public class AdUnitConfig
    {
        public AD_TYPE  ad_type;
        [FormerlySerializedAs("ad_id")] public string ad_id_android;
        public string ad_id_ios;
        public bool is_multi_ad; // bật thêm 1 ad id thứ 2 cho cùng ad_type (ad_id_android_2/ad_id_ios_2)
        public string ad_id_android_2;
        public string ad_id_ios_2;
        public bool removable;
        public bool preload_ad;
        public bool reload_on_fail = true;
        public int time_close_native = 3;
        [FormerlySerializedAs("layoutQuitInterNative")] public NATIVE_LAYOUT_QUIT_INTER  layout_quit_native;
        [FormerlySerializedAs("layoutTemplateNative")] public NATIVE_TEMPLATE_TYPE  layout_template_native;
        [FormerlySerializedAs("layoutSupportMedia")] public bool layout_support_media = true;
        public bool has_close = true; // chỉ áp dụng cho BANNER_COLLAPSE/BANNER_COLLAPSE_NATIVE: có hiện nút đóng banner hay không
        public bool use_only_portrait = false; // chỉ áp dụng cho native full

        public string ad_id
        {
            get
            {
#if UNITY_IOS
                return ad_id_ios;
#else
                return ad_id_android;
#endif
            }
        }

        public string ad_id_2
        {
            get
            {
#if UNITY_IOS
                return ad_id_ios_2;
#else
                return ad_id_android_2;
#endif
            }
        }
    }

    public enum NATIVE_TEMPLATE_TYPE : byte
    {
        DEFAULT = 0, //= origin 2
        FULLSCREEN_MEDIA_2,
        FULLSCREEN_FAN,
        FULLSCREEN_BANNER,
        FULLSCREEN_ORIGIN,
        FULLSCREEN_ORIGIN_2,
    }

    public enum NATIVE_LAYOUT_QUIT_INTER : byte
    {
        DEFAULT = 0, // = inter 3
        INTER_2,
        INTER_3,
        INTER_1,
    }

    /// <summary>
    /// LUÔN THÊM TYPE THEO KIỂU x = TYPE ADMOB và x+10 = TYPE NATIVE
    /// </summary>
    public enum AD_TYPE : int
    {
        INTER_FORCE = 0,
        INTER_FORCE_2,
        INTER_FORCE_3,

        INTER_FORCE_NATIVE = 10,
        INTER_FORCE_NATIVE_2,
        INTER_FORCE_NATIVE_3,

        INTER_OPEN = 20,
        INTER_OPEN_2,
        INTER_OPEN_3,

        INTER_OPEN_NATIVE = 30,
        INTER_OPEN_NATIVE_2,
        INTER_OPEN_NATIVE_3,

        REWARD_ITEM = 40,
        REWARD_ITEM_2,
        REWARD_ITEM_3,

        REWARD_ITEM_NATIVE = 50,
        REWARD_ITEM_NATIVE_2,
        REWARD_ITEM_NATIVE_3,


        OPEN_AD = 60,
        OPEN_AD_2,

        OPEN_AD_NATIVE = 70,
        OPEN_AD_NATIVE_2,

        BANNER = 80,
        BANNER_2,
        
        BANNER_NATIVE = 90,
        BANNER_NATIVE_2,
        
        OPEN_AD_FIRST = 100,
        
        OPEN_AD_FIRST_NATIVE = 110,
        OPEN_AD_FIRST_NATIVE_2,
        
        BANNER_COLLAPSE = 120,
        
        BANNER_COLLAPSE_NATIVE = 130,
        BANNER_COLLAPSE_NATIVE_2,
        BANNER_COLLAPSE_NATIVE_3,
    }

    public enum BANNER_TYPE
    {
        ADAPTIVE = 0,
        BANNER,
        MEDIUM_RECTANGLE,
        IABBANNER,
        LEADERBOARD,
    }
}
