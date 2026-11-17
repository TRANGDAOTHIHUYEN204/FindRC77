using System;
using DG.Tweening;
using UnityEngine;

public class IconCollectionAnimation : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private float spawnDuration = 0.4f;
    [SerializeField] private float bounceStrength = 0.2f;
    [SerializeField] private float bounceDuration = 0.3f;
    [SerializeField] private float despawnDuration = 0.3f;
    [SerializeField] private float liftHeight = 30f;
    [SerializeField] private float liftDuration = 0.15f;

    private Tween current;
    private Tween liftTween;
    private Vector3 baseLocalPos;
    private Transform dragRoot;
    
    private bool CanMovePosition => visual != dragRoot;

    private void Awake()
    {
        var drag = GetComponentInParent<DragHandler2D>();
        dragRoot = drag != null ? drag.transform : transform;

        if (visual == null) visual = transform;
        baseLocalPos = visual.localPosition;
    }

    private void OnDisable() => KillAll();
    private void OnDestroy() => KillAll();

    private void KillAll()
    {
        current?.Kill();
        liftTween?.Kill();
    }

    public void Spawn(float delay = 0f, Action onComplete = null)
    {
        KillAll();
        if (CanMovePosition) visual.localPosition = baseLocalPos;
        visual.localScale = Vector3.zero;
        visual.localRotation = Quaternion.identity;

        current = visual.DOScale(Vector3.one, spawnDuration)
            .SetDelay(delay)
            .SetEase(Ease.OutBack)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void Lift()
    {
        if (!CanMovePosition) return;

        liftTween?.Kill();
        liftTween = visual.DOLocalMoveY(baseLocalPos.y + liftHeight, liftDuration)
            .SetEase(Ease.OutQuad);
    }

    public void Drop()
    {
        if (!CanMovePosition) return;

        liftTween?.Kill();
        liftTween = visual.DOLocalMoveY(baseLocalPos.y, liftDuration)
            .SetEase(Ease.OutQuad);
    }

    public void ResetPose()
    {
        KillAll();
        if (CanMovePosition) visual.localPosition = baseLocalPos;
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one;
    }

    public void PlayComplete(Action onComplete = null)
    {
        current?.Kill();
        visual.localScale = Vector3.one;
        visual.localRotation = Quaternion.identity;

        Sequence seq = DOTween.Sequence();
        seq.Append(visual.DOPunchScale(Vector3.one * bounceStrength, bounceDuration, 5, 0.5f));
        seq.Append(visual.DOScale(Vector3.zero, despawnDuration).SetEase(Ease.InBack));
        seq.OnComplete(() => onComplete?.Invoke());
        current = seq;
    }
}