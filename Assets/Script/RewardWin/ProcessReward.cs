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

    private Sequence sequence;

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

    public void ResetVisual()
    {
        StopAnimation();

        fillImage.fillAmount = 0f;
        levelImage.sprite = graySprite;
        levelVisual.localScale = Vector3.one * normalScale;
    }

    public void StopAnimation()
    {
        sequence?.Kill();
        sequence = null;
    }

    private void OnDisable()
    {
        StopAnimation();
    }
}