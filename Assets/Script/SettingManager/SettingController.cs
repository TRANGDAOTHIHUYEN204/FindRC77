using UnityEngine;
using UnityEngine.UI;
using Char.Haptic;

public class SettingController : MonoBehaviour
{
   
    private void Sync(Toggle t, bool isOn)
    {
        if (!t) return;
        t.SetIsOnWithoutNotify(isOn);

        var visual = t.GetComponent<ToggleVisual>();
        if (!visual) visual = t.GetComponentInParent<ToggleVisual>();
        if (!visual) visual = t.GetComponentInChildren<ToggleVisual>();
        if (visual) visual.Refresh();
    }

    public void ToggleMusic()
    {
        if (SoundManagerHome.Instance != null) SoundManagerHome.Instance.ToggleMusic();
        else if (SoundManager.Instance != null) SoundManager.Instance.ToggleMusic();
    }

    public void ToggleSound()
    {
        if (SoundManagerHome.Instance != null) SoundManagerHome.Instance.ToggleSfx();
        else if (SoundManager.Instance != null) SoundManager.Instance.ToggleSfx();
    }

    public void MuteHaptic()
    {
        Haptics.Enabled = !Haptics.Enabled;
    }
}