using UnityEngine;
using TMPro;
using System;
using System.Collections;

public class ProcessingBar : MonoBehaviour
{
    [SerializeField] private RectTransform mask;
    [SerializeField] private RectTransform fill;
    [SerializeField] private TMP_Text label;
    [SerializeField] private float duration = 3f;
    [SerializeField] private bool playOnStart = true;

    [SerializeField] private string baseText = "Loading";
    [SerializeField] private float dotInterval = 0.4f;   
    [SerializeField] private int maxDots = 3;
    [SerializeField] private bool keepTextWidth = true;  
    [SerializeField] private bool animateAfterComplete = false; 
    [SerializeField] private float startOffset = 750f;

    [SerializeField, Range(0f, 1f)] private float adsAt = 0.8f;

    [SerializeField] private AnimationCurve curve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.3f, 0.55f),
        new Keyframe(0.75f, 0.75f),
        new Keyframe(1f, 1f));

    private float _elapsed;
    private bool _playing;
    private bool _paused;
    private bool _adsTriggered;
    private Action _onComplete;

    private float _dotTimer;
    private int _dotCount = -1;
    private string[] _dotStrings; 

    private int SafeMaxDots => Mathf.Max(1, maxDots);
    private float SafeDotInterval => Mathf.Max(0.01f, dotInterval);

    public bool IsPlaying => _playing;
    public bool IsPaused => _paused;

    private void Awake()
    {
        BuildDotStrings();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxDots = Mathf.Max(1, maxDots);
        dotInterval = Mathf.Max(0.01f, dotInterval);
        duration = Mathf.Max(0f, duration);

        BuildDotStrings();
        int current = _dotCount;
        _dotCount = -1;
        if (Application.isPlaying && current >= 0) SetDots(current);
    }
#endif

    private IEnumerator Start()
    {
        if (!Validate()) yield break;

        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.pivot = new Vector2(0.5f, 0.5f);
        fill.sizeDelta = Vector2.zero;

        yield return null;
        Canvas.ForceUpdateCanvases();

        SetProgress(0f);
        if (playOnStart) Play();
    }

    private bool Validate()
    {
        return mask != null && fill != null;
    }

    public void Play(Action onComplete = null)
    {
        _onComplete = onComplete;
        _elapsed = 0f;
        _playing = true;
        _paused = false;
        _adsTriggered = false;

        _dotTimer = 0f;
        SetDots(1);

        SetProgress(0f);
    }
    
    public void Pause()
    {
        _paused = true;
    }
    
    public void Resume()
    {
        _paused = false;
    }
    
    public void SetBaseText(string text)
    {
        baseText = text ?? string.Empty;
        BuildDotStrings();

        int current = _dotCount;
        _dotCount = -1;
        if (current >= 0) SetDots(current);
    }

    private void Update()
    {
        UpdateDots(); 

        if (!_playing || _paused) return;

        _elapsed += Time.unscaledDeltaTime;
        float t = duration > 0f ? Mathf.Clamp01(_elapsed / duration) : 1f;
        float p = curve != null ? curve.Evaluate(t) : t;
        SetProgress(p);
        
        if (!_adsTriggered && (p >= adsAt || t >= 1f))
        {
            _adsTriggered = true;
            OnReachAds();
            if (_paused) return; 
        }

        if (t < 1f) return;
        _playing = false;

        if (!animateAfterComplete) SetDots(SafeMaxDots); 

        var cb = _onComplete;
        _onComplete = null;
        cb?.Invoke();
    }

    private void OnReachAds()
    {
        // ads
    }

    private void UpdateDots()
    {
        if (label == null) return;
        if (!_playing && !animateAfterComplete) return;

        float interval = SafeDotInterval;
        _dotTimer += Time.unscaledDeltaTime;
        if (_dotTimer < interval) return;
        _dotTimer -= interval;
        int current = _dotCount < 1 ? 0 : _dotCount;
        int next = current % SafeMaxDots + 1;
        SetDots(next);
    }

    private void BuildDotStrings()
    {
        int max = SafeMaxDots;
        string prefix = baseText ?? string.Empty;

        _dotStrings = new string[max + 1];
        for (int i = 0; i <= max; i++)
        {
            string visible = new string('.', i);
            string hidden = new string('.', max - i);

            _dotStrings[i] = keepTextWidth && hidden.Length > 0
                ? prefix + visible + "<alpha=#00>" + hidden
                : prefix + visible;
        }
    }

    private void SetDots(int count)
    {
        if (label == null) return;

        int max = SafeMaxDots;
        count = Mathf.Clamp(count, 0, max);
        if (count == _dotCount) return;

        if (_dotStrings == null || _dotStrings.Length != max + 1) BuildDotStrings();

        _dotCount = count;
        label.text = _dotStrings[count]; 
    }

    public void SetProgress(float p)
    {
        if (mask == null || fill == null) return;

        p = Mathf.Clamp01(p);
        float x = Mathf.Lerp(-startOffset, 0f, p);
        fill.anchoredPosition = new Vector2(x, 0f);
    }
}