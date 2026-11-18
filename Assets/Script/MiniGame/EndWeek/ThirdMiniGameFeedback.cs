using Char.Haptic;
using UnityEngine;

public class ThirdMiniGameFeedback : MonoBehaviour
{
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;

    private void OnEnable()
    {
        DropTarget.AnyAccepted += OnAccepted;
        DropTarget.AnyRejected += OnRejected;
    }

    private void OnDisable()
    {
        DropTarget.AnyAccepted -= OnAccepted;
        DropTarget.AnyRejected -= OnRejected;
    }

    private void OnAccepted(DropTarget _) => Play(correctClip);

    private void OnRejected(DropTarget _)
    {
        Play(wrongClip);
        Haptics.Vibrate(VibrateStyle.Medium);
    }

    private void Play(AudioClip clip)
    {
        if (clip != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(clip, volume);
    }
}