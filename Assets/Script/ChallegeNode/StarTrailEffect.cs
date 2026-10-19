using System;
using System.Collections;
using UnityEngine;

public class StarTrailEffect : MonoBehaviour
{
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private ParticleSystem sparkle;
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float arcHeight = 0.8f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [SerializeField] private float startRadius = 0.5f;   
    [SerializeField] private float endRadius = 0.02f;    
    [SerializeField] private float startSizeMul = 1f;
    [SerializeField] private float endSizeMul = 0.3f;
    [SerializeField] private float startTrailWidth = 0.3f;
    [SerializeField] private float endTrailWidth = 0.02f;

    private Coroutine _routine;

    public void Play(Vector3 from, Vector3 to, Action onArrived)
    {
        if (_routine != null) StopCoroutine(_routine);

        transform.position = from;
        ApplyConverge(0f);

        if (trail != null) trail.Clear();
        if (sparkle != null) sparkle.Play();

        _routine = StartCoroutine(Fly(from, to, onArrived));
    }

    private void ApplyConverge(float t)
    {
        if (sparkle != null)
        {
            var shape = sparkle.shape;
            shape.radius = Mathf.Lerp(startRadius, endRadius, t);

            var main = sparkle.main;
            main.startSizeMultiplier = Mathf.Lerp(startSizeMul, endSizeMul, t);
        }

        if (trail != null)
            trail.widthMultiplier = Mathf.Lerp(startTrailWidth, endTrailWidth, t);
    }

    private IEnumerator Fly(Vector3 from, Vector3 to, Action onArrived)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = ease.Evaluate(Mathf.Clamp01(elapsed / duration));

            transform.position = Vector3.LerpUnclamped(from, to, t);

            ApplyConverge(t);
            yield return null;
        }

        transform.position = to;
        ApplyConverge(1f);

        if (sparkle != null) sparkle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        onArrived?.Invoke();

        float wait = 0.1f;
        if (trail != null) wait = Mathf.Max(wait, trail.time);
        if (sparkle != null) wait = Mathf.Max(wait, sparkle.main.startLifetime.constantMax);
        yield return new WaitForSeconds(wait);

        _routine = null;
        Destroy(gameObject);
    }
}