using System;
using DG.Tweening;
using UnityEngine;

public class MoveToTarget : MonoBehaviour
{
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private Ease ease = Ease.OutQuad;
    [SerializeField, Min(0f)] private float arcHeightRatio = 0f;
    [SerializeField] private bool unscaledTime = true;

    private Tween tween;

    public float Duration => duration;

    public Tween Play(Vector3 startPos, RectTransform target, Action onComplete = null)
    {
        Stop();

        Vector3 end = target.position;
        float height = Vector3.Distance(startPos, end) * arcHeightRatio;
        transform.position = startPos;

        tween = DOVirtual.Float(0f, 1f, duration, p =>
            {
                Vector3 pos = Vector3.LerpUnclamped(startPos, end, p);
                pos.y += 4f * height * p * (1f - p);
                transform.position = pos;
            })
            .SetEase(ease)
            .SetUpdate(unscaledTime)
            .SetTarget(transform)
            .OnComplete(() =>
            {
                transform.position = end;
                onComplete?.Invoke();
            });

        return tween;
    }
    
    public void Stop()
    {
        if (tween != null && tween.IsActive()) tween.Kill();
        tween = null;
    }

    private void OnDisable() => Stop();
}