
using System;
using System.Collections;
using UnityEngine;

public class StarTrailEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float curveHeight = 80f;

    private RectTransform _rect;
    private RectTransform _movementRoot;
    private Coroutine _moveCoroutine;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    public void Play(
        RectTransform startPoint,
        RectTransform endPoint,
        Action onComplete)
    {
        if (startPoint == null || endPoint == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        _movementRoot = _rect.parent as RectTransform;

        if (_movementRoot == null)
        {
            onComplete?.Invoke();
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();

        Camera uiCamera = canvas != null &&
                          canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        Vector2 startScreen =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera, startPoint.position);

        Vector2 endScreen =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera, endPoint.position);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _movementRoot, startScreen, uiCamera, out Vector2 startLocal) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _movementRoot, endScreen, uiCamera, out Vector2 endLocal))
        {
            onComplete?.Invoke();
            return;
        }

        _rect.anchoredPosition = startLocal;

        _moveCoroutine = StartCoroutine(
            MoveRoutine(startLocal, endLocal, onComplete));
    }

    private IEnumerator MoveRoutine(
        Vector2 start,
        Vector2 end,
        Action onComplete)
    {
        float elapsed = 0f;

        Vector2 control = (start + end) * 0.5f
                        + Vector2.up * curveHeight;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = duration > 0f
                ? Mathf.Clamp01(elapsed / duration)
                : 1f;
            
            float easedT = 1f - (1f - t) * (1f - t);
            
            float inverseT = 1f - easedT;

            _rect.anchoredPosition =
                inverseT * inverseT * start
                + 2f * inverseT * easedT * control
                + easedT * easedT * end;

            yield return null;
        }

        _rect.anchoredPosition = end;
        _moveCoroutine = null;

        onComplete?.Invoke();
    }

    private void OnDisable()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }
    }
}
