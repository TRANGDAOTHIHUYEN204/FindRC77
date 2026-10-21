using UnityEngine;
using UnityEngine.UI;

namespace Char.Haptic
{
    /// <summary>Gắn vào một UI Toggle trong màn Settings để bật/tắt rung.</summary>
    [RequireComponent(typeof(Toggle))]
    public class HapticToggle : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private bool vibrateWhenTurnedOn = true;

        private void Reset() => toggle = GetComponent<Toggle>();

        private void Awake()
        {
            if (toggle == null) toggle = GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            toggle.SetIsOnWithoutNotify(Haptics.Enabled);
            toggle.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDisable() => toggle.onValueChanged.RemoveListener(OnValueChanged);

        private void OnValueChanged(bool value)
        {
            Haptics.Enabled = value;
            if (value && vibrateWhenTurnedOn) Haptics.Medium();
        }
    }
}
