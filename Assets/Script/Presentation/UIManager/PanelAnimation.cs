using System.Collections;
using UnityEngine;

public class PanelAnimation : MonoBehaviour
{
    [SerializeField] private float openDuration = 0.4f;
    [SerializeField] private float closeDuration = 0.12f;
    [SerializeField] private Vector3 startScale = Vector3.zero;

    private Vector3 _targetScale;
    private bool _initialized;
    private Coroutine _routine;
    private System.Action _onClosed;

    private void Init()
    {
        if (_initialized) return;
        _targetScale = transform.localScale;
        if (_targetScale == Vector3.zero) _targetScale = Vector3.one;
        _initialized = true;
    }

    private void Awake() => Init();

    private void OnEnable()
    {
        Init();
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ScaleRoutine(startScale, _targetScale, openDuration, EaseOutBack, false));
    }

    private void OnDisable()
    {
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        if (_initialized) transform.localScale = _targetScale;
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
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ScaleRoutine(transform.localScale, _targetScale, openDuration, EaseOutBack, false));
    }
    
    public void Close() => Close(null);

    public void Close(System.Action callback)
    {
        if (!gameObject.activeInHierarchy)
        {
            callback?.Invoke();
            return;
        }
        _onClosed = callback;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ScaleRoutine(transform.localScale, startScale, closeDuration, EaseInCubic, true));
    }

    private IEnumerator ScaleRoutine(Vector3 from, Vector3 to, float duration,
                                     System.Func<float, float> ease, bool deactivateAtEnd)
    {
        float t = 0f;
        transform.localScale = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = ease(Mathf.Clamp01(t / duration));
            transform.localScale = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }
        transform.localScale = to;
        _routine = null;

        if (deactivateAtEnd)
        {
            var cb = _onClosed;
            _onClosed = null;
            cb?.Invoke();                  
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
    }
    private static float EaseInCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }
    
    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
    }
    
}