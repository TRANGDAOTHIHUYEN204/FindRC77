#if ENABLE_UNITY_AD_SDK && ENABLE_RACCOON_ADMOB
using UnityEngine;

namespace RCKit.Ads.UnityAds
{
    // Đặt trong folder Resources với tên ResourcesName để UnityAdsInitializer load được lúc runtime.
    [CreateAssetMenu(fileName = ResourcesName, menuName = "Raccoon/Unity Ads Settings")]
    public class UnityAdsSettings : ScriptableObject
    {
        public const string ResourcesName = "UnityAdsSettings";

        public string game_id_android;
        public string game_id_ios;

        public string GameId
        {
            get
            {
#if UNITY_IOS
                return game_id_ios;
#elif UNITY_ANDROID
                return game_id_android;
#else
                return string.Empty;
#endif
            }
        }
    }
}
#endif
