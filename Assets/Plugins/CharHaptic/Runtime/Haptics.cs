using MoreMountains.NiceVibrations;
using UnityEngine;

namespace Char.Haptic
{
    public enum VibrateStyle
    {
        Light, Medium, Heavy
    }

    /// <summary>
    /// Cửa duy nhất để rung device. Tự đọc/ghi cờ bật-tắt vào PlayerPrefs,
    /// không phụ thuộc service locator hay scene nào.
    /// </summary>
    public static class Haptics
    {
        // Giữ đúng key cũ của RC62/RC67 để người chơi cũ không mất setting khi đổi sang package này
        public const string PrefKey = "GameVibrate";

        private static bool? _enabled;

        public static bool Enabled
        {
            get
            {
                _enabled ??= PlayerPrefs.GetInt(PrefKey, 1) == 1;
                return _enabled.Value;
            }
            set
            {
                _enabled = value;
                PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            }
        }

        // Domain reload có thể bị tắt trong editor, reset cache để không giữ giá trị của phiên trước
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _enabled = null;

        public static void Vibrate(VibrateStyle style = VibrateStyle.Light)
        {
            switch (style)
            {
                case VibrateStyle.Heavy:  Play(HapticTypes.HeavyImpact);  break;
                case VibrateStyle.Medium: Play(HapticTypes.MediumImpact); break;
                default:                  Play(HapticTypes.LightImpact);  break;
            }
        }

        public static void Light()  => Play(HapticTypes.LightImpact);
        public static void Medium() => Play(HapticTypes.MediumImpact);
        public static void Heavy()  => Play(HapticTypes.HeavyImpact);

        /// <summary>Các kiểu khác của NiceVibrations: Selection, Success, Warning, Failure.</summary>
        public static void Play(HapticTypes type)
        {
            if (!Enabled) return;
            MMVibrationManager.Haptic(type);
        }
    }
}
