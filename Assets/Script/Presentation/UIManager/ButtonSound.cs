using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    [SerializeField] private AudioClip soundButton;

    private Button _button;

    private void Awake() => _button = GetComponent<Button>();

    private void OnEnable() => _button.onClick.AddListener(PlaySound);

    private void OnDisable() => _button.onClick.RemoveListener(PlaySound);

    private void PlaySound()
    {
        if (soundButton != null && SoundManager.Instance != null)
            SoundManager.Instance.PlaySfx(soundButton);
    }
}