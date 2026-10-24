
using System;
using System.Collections;
using UnityEngine;

public class StarTrailEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float curveHeight = 80f;
    [SerializeField] private float visualScale = 0.65f;

    private RectTransform _rect;
    private RectTransform _movementRoot;
    private ParticleSystem _rootParticle;
    private ParticleSystem _visualParticle;
    private Coroutine _moveCoroutine;
    private Action _onComplete;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        CacheParticles();
        DisableRootParticle();
    }

    private void CacheParticles()
    {
        if (_rootParticle == null)
            _rootParticle = GetComponent<ParticleSystem>();

        if (_visualParticle != null)
            return;

        // Chỉ dùng VFX con (fx_Trail_2). PS trên root là đường thừa.
        ParticleSystem[] all = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < all.Length; i++)
        {
            ParticleSystem ps = all[i];
            if (ps != null && ps.gameObject != gameObject)
            {
                _visualParticle = ps;
                break;
            }
        }
    }

    public void PrepareInactive()
    {
        _onComplete = null;
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        CacheParticles();
        DisableRootParticle();
        StopVisualParticle();
        if (_visualParticle != null)
        {
            _visualParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _visualParticle.Clear(true);
            ParticleSystemRenderer renderer = _visualParticle.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.enabled = false;
        }
    }

    public void Play(
        RectTransform startPoint,
        RectTransform endPoint,
        Action onComplete)
    {
        _onComplete = onComplete;

        if (startPoint == null || endPoint == null)
        {
            Complete();
            return;
        }

        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        _movementRoot = _rect.parent as RectTransform;
        if (_movementRoot == null)
        {
            Complete();
            return;
        }

        CacheParticles();
        DisableRootParticle();

        float scale = Mathf.Max(0.01f, visualScale);
        transform.localScale = new Vector3(scale, scale, scale);

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
            Complete();
            return;
        }

        _rect.anchoredPosition = startLocal;
        PlayVisualParticle();

        _moveCoroutine = StartCoroutine(MoveRoutine(startLocal, endLocal));
    }

    private void DisableRootParticle()
    {
        if (_rootParticle == null)
            return;

        _rootParticle.playOnAwake = false;
        _rootParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _rootParticle.Clear(true);

        ParticleSystemRenderer renderer =
            _rootParticle.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    private void PlayVisualParticle()
    {
        if (_visualParticle == null)
            return;

        // Giữ ParticleSystemRenderer tắt: UIParticle trên root đã render rồi.
        // Bật cả hai sẽ ra 2 đường particle chồng nhau.
        ParticleSystemRenderer renderer =
            _visualParticle.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
            renderer.enabled = false;

        _visualParticle.playOnAwake = false;
        _visualParticle.Clear(true);
        _visualParticle.Play(false);
    }

    private void StopVisualParticle()
    {
        if (_visualParticle == null)
            return;

        _visualParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _visualParticle.Clear(true);
    }

    private IEnumerator MoveRoutine(Vector2 start, Vector2 end)
    {
        float elapsed = 0f;
        Vector2 control = (start + end) * 0.5f + Vector2.up * curveHeight;

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

        StopVisualParticle();
        Complete();
    }

    private void Complete()
    {
        Action callback = _onComplete;
        _onComplete = null;
        callback?.Invoke();
    }

    private void OnDisable()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        DisableRootParticle();
        StopVisualParticle();
        _onComplete = null;
    }
}
