#if ENABLE_TIKTOK_SDK && ENABLE_RACCOON_ADMOB
using UnityEngine;

namespace RCKit.Ads.TikTok
{
    // Đặt trong folder Resources với tên ResourcesName để TikTokInitializer load được lúc runtime.
    // App ID (package name) lấy tự động từ Application.identifier, phải khớp App đã tạo trên Events Manager.
    [CreateAssetMenu(fileName = ResourcesName, menuName = "Raccoon/TikTok Settings")]
    public class TikTokSettings : ScriptableObject
    {
        public const string ResourcesName = "TikTokSettings";

        [Tooltip("TikTok App ID trong Events Manager. Nhiều Ad Account: nối bằng dấu phẩy, không có khoảng trắng.")]
        public string tiktok_app_id_android;

        [Tooltip("App Secret trong Events Manager (cùng trang Settings với TikTok App ID).")]
        public string app_secret_android;

        [Tooltip("Bật để test event trên Events Manager > Test Events. Tắt trước khi release.")]
        public bool debug_mode;
    }
}
#endif
