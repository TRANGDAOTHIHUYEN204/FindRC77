using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ProcessReward : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Image levelImage;
    [SerializeField] private RectTransform levelVisual;
    [SerializeField] private Sprite graySprite;
    [SerializeField] private Sprite greenSprite;
    [SerializeField] private float delay = 0.3f;
    [SerializeField] private float fillDuration = 0.4f;
    [SerializeField] private float scaleUpDuration = 0.2f;
    [SerializeField] private float scaleDownDuration = 0.15f;
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float overshoot = 1.1f;
    [SerializeField] private GameObject effectReward;
    [SerializeField] private float effectStartScale = 0.5f;
    [SerializeField] private float effectScaleDuration = 0.35f;
    [SerializeField] private float effectFloatDistance = 0.5f;
    [SerializeField] private float effectFloatDuration = 1.2f;

    private Sequence sequence;
    private Sequence effectSequence;

    public void PlayAnimation(Action onDone = null)
    {
        ResetVisual();

        sequence = DOTween.Sequence();
        sequence.SetUpdate(true);

        sequence.AppendInterval(delay);
        sequence.Append(
            fillImage.DOFillAmount(1f, fillDuration)
                .SetEase(Ease.Linear)
        );

        sequence.Append(
            levelVisual.DOScale(normalScale * overshoot, scaleUpDuration)
                .SetEase(Ease.OutCubic)
        );
        sequence.AppendCallback(() =>
        {
            levelImage.sprite = greenSprite;
        });
        sequence.Append(
            levelVisual.DOScale(normalScale, scaleDownDuration)
                .SetEase(Ease.InOutSine)
        );

        sequence.OnComplete(() =>
        {
            sequence = null;
            onDone?.Invoke();
        });
    }
    
    public void PlayEffectReward()
    {
        if (effectReward == null) return;

        KillEffect();

        Transform t = effectReward.transform;
        t.localPosition = new Vector3(0f, 0f, t.localPosition.z);
        t.localScale = Vector3.one * effectStartScale;
        effectReward.SetActive(true);

        effectSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetTarget(t)
            .SetLink(effectReward);

        effectSequence.Append(
            t.DOScale(1f, effectScaleDuration).SetEase(Ease.OutBack)
        );
        effectSequence.Join(
            t.DOLocalMoveY(effectFloatDistance, effectFloatDuration).SetEase(Ease.OutSine)
        );
        effectSequence.OnComplete(() => effectSequence = null);
    }

    public void HideEffectReward()
    {
        KillEffect();

        if (effectReward == null) return;

        Transform t = effectReward.transform;
        t.localPosition = new Vector3(0f, 0f, t.localPosition.z);
        t.localScale = Vector3.one * effectStartScale;
        effectReward.SetActive(false);
    }

    public void ResetVisual()
    {
        StopAnimation();
        HideEffectReward();

        fillImage.fillAmount = 0f;
        levelImage.sprite = graySprite;
        levelVisual.localScale = Vector3.one * normalScale;
    }

    public void StopAnimation()
    {
        sequence?.Kill();
        sequence = null;
        KillEffect();
    }

    private void KillEffect()
    {
        effectSequence?.Kill();
        effectSequence = null;
    }

    private void OnDisable()
    {
        StopAnimation();
    }
}