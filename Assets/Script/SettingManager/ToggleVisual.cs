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
    [SerializeField] private bool autoCalculatePos = true;
    [SerializeField] private float padding = 4f;
    [SerializeField] private Vector2 offPos;
    [SerializeField] private Vector2 onPos;
    [SerializeField] private bool logPositions;
    [SerializeField] private float slideDuration = 0.2f;
    [SerializeField] private float bgDuration = 0.15f;

    private Vector2 _offPosOn, _onPosOn;
    private Vector2 _offPosMute, _onPosMute;

    private Coroutine _co;

    private void Awake()
    {
        toggle.onValueChanged.AddListener(OnToggle);
    }

    private void OnEnable()
    {
        CalculatePositions();
        toggle.SetIsOnWithoutNotify(ReadSavedState());
        Refresh();
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggle);
    }

    public void RecalculateAndRefresh()
    {
        CalculatePositions();
        Refresh();
    }

    private void CalculatePositions()
    {
        if (!autoCalculatePos)
        {
            _offPosOn = _offPosMute = offPos;
            _onPosOn = _onPosMute = onPos;
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(toggleOn.rectTransform);

        RectTransform track = toggleOn.rectTransform;

        _offPosOn   = CalcPos(circleOn.rectTransform,   track, false);
        _onPosOn    = CalcPos(circleOn.rectTransform,   track, true);
        _offPosMute = CalcPos(circleMute.rectTransform, track, false);
        _onPosMute  = CalcPos(circleMute.rectTransform, track, true);

        if (logPositions)
        {
            Debug.Log($"[{name}] trackRect={track.rect} circleOn={circleOn.rectTransform.rect.size} " +
                      $"offOn={_offPosOn} onOn={_onPosOn} offMute={_offPosMute} onMute={_onPosMute}", this);
        }
    }
    private Vector2 CalcPos(RectTransform circle, RectTransform track, bool right)
    {
        RectTransform parent = circle.parent as RectTransform;
        
        Vector2 tMin, tMax;
        if (track == parent)
        {
            tMin = track.rect.min;
            tMax = track.rect.max;
        }
        else if (track.parent == parent)
        {
            Vector2 p = track.localPosition;
            Vector2 s = track.localScale;
            tMin = p + Vector2.Scale(track.rect.min, s);
            tMax = p + Vector2.Scale(track.rect.max, s);
        }
        else
        {
            Vector3[] c = new Vector3[4];
            track.GetWorldCorners(c);
            tMin = parent.InverseTransformPoint(c[0]);
            tMax = parent.InverseTransformPoint(c[2]);
        }
        Vector2 size = Vector2.Scale(circle.rect.size, circle.localScale);
        float cx = right ? tMax.x - padding - size.x * 0.5f
                         : tMin.x + padding + size.x * 0.5f;
        float cy = (tMin.y + tMax.y) * 0.5f;
        Vector2 desiredCenter = new Vector2(cx, cy);
        
        Vector2 pivotLocal = desiredCenter - Vector2.Scale(circle.rect.center, circle.localScale);
        
        Rect pr = parent.rect;
        Vector2 aMin = new Vector2(Mathf.Lerp(pr.xMin, pr.xMax, circle.anchorMin.x),
                                   Mathf.Lerp(pr.yMin, pr.yMax, circle.anchorMin.y));
        Vector2 aMax = new Vector2(Mathf.Lerp(pr.xMin, pr.xMax, circle.anchorMax.x),
                                   Mathf.Lerp(pr.yMin, pr.yMax, circle.anchorMax.y));
        Vector2 anchorRef = aMin + Vector2.Scale(circle.pivot, aMax - aMin);

        return pivotLocal - anchorRef;
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

        circleOn.rectTransform.anchoredPosition   = isOn ? _onPosOn   : _offPosOn;
        circleMute.rectTransform.anchoredPosition = isOn ? _onPosMute : _offPosMute;
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

        Vector2 inFrom  = isOn ? _offPosOn   : _onPosMute;
        Vector2 inTo    = isOn ? _onPosOn    : _offPosMute;
        Vector2 outFrom = isOn ? _offPosMute : _onPosOn;
        Vector2 outTo   = isOn ? _onPosMute  : _offPosOn;

        float inStartA    = circleIn.color.a;
        float outStartA   = circleOut.color.a;
        float bgInStartA  = bgIn.color.a;
        float bgOutStartA = bgOut.color.a;

        circleIn.rectTransform.anchoredPosition  = inFrom;
        circleOut.rectTransform.anchoredPosition = outFrom;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);

            circleIn.rectTransform.anchoredPosition  = Vector2.Lerp(inFrom,  inTo,  t);
            circleOut.rectTransform.anchoredPosition = Vector2.Lerp(outFrom, outTo, t);

            SetAlpha(circleIn,  Mathf.Lerp(inStartA,  1f, t));
            SetAlpha(circleOut, Mathf.Lerp(outStartA, 0f, t));

            yield return null;
        }

        circleIn.rectTransform.anchoredPosition  = inTo;
        circleOut.rectTransform.anchoredPosition = outTo;
        SetAlpha(circleIn, 1f);
        SetAlpha(circleOut, 0f);

        elapsed = 0f;
        while (elapsed < bgDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / bgDuration);

            SetAlpha(bgIn,  Mathf.Lerp(bgInStartA,  1f, t));
            SetAlpha(bgOut, Mathf.Lerp(bgOutStartA, 0f, t));

            yield return null;
        }

        SetAlpha(bgIn, 1f);
        SetAlpha(bgOut, 0f);
        _co = null;
    }
}