using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Char.Haptic;

public class ToggleVisual : MonoBehaviour
{
    public enum ToggleType { Music, Sound, Haptic }

    [SerializeField] private Toggle toggle;
    [SerializeField] private ToggleType type;
    
    [SerializeField] private Image toggleOn;
    [SerializeField] private Image toggleOff;
    
    [SerializeField] private Image circleOn;
    [SerializeField] private Image circleMute;
    
    [SerializeField] private Vector2 offPos;
    [SerializeField] private Vector2 onPos;

    [SerializeField] private float slideDuration = 0.2f;
    [SerializeField] private float bgDuration = 0.15f;

    private Coroutine _co;

    private void Awake()
    {
        toggle.onValueChanged.AddListener(OnToggle);
    }
    
    private void OnEnable()
    {
        toggle.SetIsOnWithoutNotify(ReadSavedState());
        Refresh();
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggle);
    }

    private bool ReadSavedState()
    {
        switch (type)
        {
            case ToggleType.Music: return SaveGame.MusicOn;
            case ToggleType.Sound: return SaveGame.SfxOn;
            default: return Haptics.Enabled;
        }
    }

    public void Refresh()
    {
        if (_co != null) { StopCoroutine(_co); _co = null; }
        SetInstant(toggle.isOn);
    }

    private void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    private void SetInstant(bool isOn)
    {
        SetAlpha(toggleOn, isOn ? 1f : 0f);
        SetAlpha(toggleOff, isOn ? 0f : 1f);
        SetAlpha(circleOn, isOn ? 1f : 0f);
        SetAlpha(circleMute, isOn ? 0f : 1f);

        circleOn.rectTransform.anchoredPosition = isOn ? onPos : offPos;
        circleMute.rectTransform.anchoredPosition = isOn ? onPos : offPos;
    }

    private void OnToggle(bool isOn)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(Animate(isOn));
    }

    private IEnumerator Animate(bool isOn)
    {
        Image circleIn  = isOn ? circleOn   : circleMute;
        Image circleOut = isOn ? circleMute : circleOn;

        Image bgIn  = isOn ? toggleOn  : toggleOff;
        Image bgOut = isOn ? toggleOff : toggleOn;

        Vector2 fromPos = isOn ? offPos : onPos;
        Vector2 toPos   = isOn ? onPos  : offPos;

        float inStartA    = circleIn.color.a;
        float outStartA   = circleOut.color.a;
        float bgInStartA  = bgIn.color.a;
        float bgOutStartA = bgOut.color.a;

        circleIn.rectTransform.anchoredPosition  = fromPos;
        circleOut.rectTransform.anchoredPosition = fromPos;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);

            Vector2 pos = Vector2.Lerp(fromPos, toPos, t);
            circleIn.rectTransform.anchoredPosition = pos;
            circleOut.rectTransform.anchoredPosition = pos;

            SetAlpha(circleIn,  Mathf.Lerp(inStartA, 1f, t));
            SetAlpha(circleOut, Mathf.Lerp(outStartA, 0f, t));

            yield return null;
        }

        circleIn.rectTransform.anchoredPosition  = toPos;
        circleOut.rectTransform.anchoredPosition = toPos;
        SetAlpha(circleIn, 1f);
        SetAlpha(circleOut, 0f);

        elapsed = 0f;
        while (elapsed < bgDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / bgDuration);

            SetAlpha(bgIn,  Mathf.Lerp(bgInStartA, 1f, t));
            SetAlpha(bgOut, Mathf.Lerp(bgOutStartA, 0f, t));

            yield return null;
        }

        SetAlpha(bgIn, 1f);
        SetAlpha(bgOut, 0f);
        _co = null;
    }
}