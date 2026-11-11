using System.Collections;
using UnityEngine;

public class PanelAnimation : MonoBehaviour
{

    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private Transform panel;
    [SerializeField] private float openDuration = 0.2f;
    [SerializeField] private Vector3 openStartScale = new Vector3(0.5f, 0.5f, 0.5f);
    [SerializeField] private float closeDuration = 0.12f;
    [SerializeField] private Vector3 closeEndScale = Vector3.zero;
    
    [SerializeField] private bool blockGameInput = true;
    [SerializeField] private float blockHoldAfterClose = 0.25f;

    private Vector3 _targetScale = Vector3.one;
    private float _targetAlpha = 1f;
    private bool _initialized;
    private bool _closing;
    private bool _blockRegistered;
    private Coroutine _routine;
    private System.Action _onClosed;

    private float Alpha
    {
        get => fadeGroup ? fadeGroup.alpha : 1f;
        set { if (fadeGroup) fadeGroup.alpha = value; }
    }

    private void SetRaycast(bool value)
    {
        if (fadeGroup) fadeGroup.blocksRaycasts = value;
    }

    private void SetScale(Vector3 s)
    {
        if (panel) panel.localScale = s;
    }

    private Vector3 GetScale() => panel ? panel.localScale : Vector3.one;

    private void RegisterBlock()
    {
        if (!blockGameInput || _blockRegistered) return;
        _blockRegistered = true;
        UIInputBlocker.RegisterOpen();
    }

    private void ReleaseBlock()
    {
        if (!_blockRegistered) return;
        _blockRegistered = false;
        UIInputBlocker.RegisterClose(blockHoldAfterClose);
    }
    

    private void Init()
    {
        if (_initialized) return;

        if (panel)
        {
            _targetScale = panel.localScale;
            if (_targetScale == Vector3.zero) _targetScale = Vector3.one;
        }

        if (fadeGroup)
        {
            _targetAlpha = fadeGroup.alpha > 0f ? fadeGroup.alpha : 1f;
        }

        _initialized = true;
    }

    private void Awake() => Init();

    private void OnEnable()
    {
        Init();
        _closing = false;
        RegisterBlock();
        PlayOpen(openStartScale, 0f);
    }

    private void OnDisable()
    {
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        _closing = false;
        _onClosed = null;
        ReleaseBlock();

        if (_initialized)
        {
            SetScale(_targetScale);
            Alpha = _targetAlpha;
            SetRaycast(true);
        }
    }
    

    public void Show()
    {
        Init();
        _onClosed = null;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true); 
            return;
        }

        if (!_closing && _routine == null) return;
        _closing = false;
        RegisterBlock();
        PlayOpen(GetScale(), Alpha);
    }

    private void PlayOpen(Vector3 fromScale, float fromAlpha)
    {
        if (_routine != null) StopCoroutine(_routine);
        SetRaycast(true);
        _routine = StartCoroutine(OpenRoutine(fromScale, fromAlpha));
    }

    private IEnumerator OpenRoutine(Vector3 fromScale, float fromAlpha)
    {
        float t = 0f;
        SetScale(fromScale);
        Alpha = fromAlpha;

        while (t < openDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / openDuration);
            SetScale(Vector3.LerpUnclamped(fromScale, _targetScale, EaseOutBack(p)));
            Alpha = Mathf.Lerp(fromAlpha, _targetAlpha, p);

            yield return null;
        }

        SetScale(_targetScale);
        Alpha = _targetAlpha;
        _routine = null;
    }
    public void Close() => Close(null);

    public void Close(System.Action callback)
    {
        Init();
        if (!gameObject.activeInHierarchy)
        {
            callback?.Invoke();
            return;
        }

        if (_closing)
        {
            if (callback != null) _onClosed += callback;
            return;
        }

        _closing = true;
        _onClosed = callback;
        if (_routine != null) StopCoroutine(_routine);

        ReleaseBlock();
        SetRaycast(false);
        _routine = StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        float t = 0f;
        Vector3 fromScale = GetScale();
        float fromAlpha = Alpha; 

        while (t < closeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / closeDuration);
            
            SetScale(Vector3.LerpUnclamped(fromScale, closeEndScale, EaseInCubic(p)));
            Alpha = Mathf.Lerp(fromAlpha, 0f, p);

            yield return null;
        }

        SetScale(closeEndScale);
        Alpha = 0f;
        _routine = null;
        _closing = false;

        var cb = _onClosed;
        _onClosed = null;
        cb?.Invoke();
        
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }
    

    private static float EaseInCubic(float x) => x * x * x;

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
    }
}