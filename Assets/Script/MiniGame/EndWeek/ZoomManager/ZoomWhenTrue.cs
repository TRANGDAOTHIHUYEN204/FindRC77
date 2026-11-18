
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(DropTarget))]
public class ZoomWhenTrue : MonoBehaviour
{
    [SerializeField] private Transform zoomTarget;
    [SerializeField] private bool useBounce = true;
    [SerializeField] private float amplitude = 0.3f;
    [SerializeField] private float bounces = 2f;
    [SerializeField] private float damping = 4f;
    [SerializeField] private float bounceDuration = 0.5f;
    [SerializeField, Range(0f, 1f)]
    private float volumeKeep = 0.5f;
    [SerializeField] private bool keepBottomFixed = true;

    private DropTarget _drop;
    private RectTransform _rect;
    private Vector3 _baseScale;
    private Vector2 _basePos;
    private Coroutine _bounceRoutine;

    private void Awake()
    {
        _drop = GetComponent<DropTarget>();

        if (zoomTarget == null)
            zoomTarget = transform;

        _rect = zoomTarget as RectTransform;
        _baseScale = zoomTarget.localScale;

        if (_rect != null)
            _basePos = _rect.anchoredPosition;
    }

    private void OnEnable()
    {
        DropTarget.AnyAccepted += OnAccepted;
    }

    private void OnDisable()
    {
        DropTarget.AnyAccepted -= OnAccepted;

        if (_bounceRoutine != null)
        {
            StopCoroutine(_bounceRoutine);
            _bounceRoutine = null;
        }

        ResetTarget();
    }

    private void OnAccepted(DropTarget t)
    {
        if (t != _drop) return;

        Play();
    }

    public void Play()
    {
        if (_bounceRoutine != null)
        {
            StopCoroutine(_bounceRoutine);
            _bounceRoutine = null;
        }

        ResetTarget();

        if (useBounce)
            _bounceRoutine = StartCoroutine(Bounce());
    }

    private IEnumerator Bounce()
    {
        float t = 0f;

        while (t < bounceDuration)
        {
            t += Time.unscaledDeltaTime;

            float n = Mathf.Clamp01(t / bounceDuration);

            float k = Mathf.Exp(-damping * n)
                      * Mathf.Sin(n * bounces * Mathf.PI * 2f);

            ApplyBounce(
                1f + amplitude * k,
                1f - amplitude * k * volumeKeep
            );

            yield return null;
        }

        ResetTarget();
        _bounceRoutine = null;
    }

    private void ApplyBounce(float yMul, float xMul)
    {
        float sy = _baseScale.y * yMul;

        zoomTarget.localScale = new Vector3(
            _baseScale.x * xMul,
            sy,
            _baseScale.z
        );

        if (keepBottomFixed && _rect != null)
        {
            float h = _rect.rect.height;

            _rect.anchoredPosition = new Vector2(
                _basePos.x,
                _basePos.y +
                _rect.pivot.y * h * (sy - _baseScale.y)
            );
        }
    }

    private void ResetTarget()
    {
        if (zoomTarget == null) return;

        zoomTarget.localScale = _baseScale;

        if (_rect != null)
            _rect.anchoredPosition = _basePos;
    }
}
