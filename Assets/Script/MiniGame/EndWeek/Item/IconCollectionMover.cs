using System;
using DG.Tweening;
using UnityEngine;

public class IconCollectionMover : MonoBehaviour
{
    private Tween tween;

    public void MoveTo(Vector3 worldPosition, float duration, Ease ease = Ease.OutQuad, Action onComplete = null)
    {
        tween?.Kill();
        tween = transform.DOMove(worldPosition, duration)
            .SetEase(ease)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void MoveToLocal(Vector3 localPosition, float duration, Ease ease = Ease.OutQuad, Action onComplete = null)
    {
        tween?.Kill();
        tween = transform.DOLocalMove(localPosition, duration)
            .SetEase(ease)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void Kill() => tween?.Kill();

    private void OnDisable() => tween?.Kill();
    private void OnDestroy() => tween?.Kill();
}