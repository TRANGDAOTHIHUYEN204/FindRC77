using System;
using DG.Tweening;
using UnityEngine;

public class GiftReward : MonoBehaviour
{
    [SerializeField] private RectTransform gift;
    [SerializeField] private RectTransform targetPos;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float jumpPower = 150f;
    [SerializeField] private float jumpDuration = 0.7f;
    [SerializeField] private float squashDuration = 0.35f;

    private Sequence seq;
    private Vector2 originPos;
    private Vector3 originScale;

    private void Awake()
    {
        originPos = gift.anchoredPosition;
        originScale = gift.localScale;
    }

    private void OnDisable()
    {
        seq?.Kill();
        ResetTransform();
    }

    private void ResetTransform()
    {
        if (gift == null) return;
        gift.anchoredPosition = originPos;
        gift.localScale = originScale;
    }

    public void Show()
    {
        seq?.Kill();
        ResetTransform();
        gift.gameObject.SetActive(true);
    }

    public void Play(Action onDone = null)
    {
        seq?.Kill();
        ResetTransform();

        seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(gift.DOPunchScale(Vector3.one * 0.3f, shakeDuration, 6, 0.5f));
        seq.Append(gift.DOJumpAnchorPos(targetPos.anchoredPosition, jumpPower, 1, jumpDuration)
            .SetEase(Ease.OutQuad));
        seq.Append(gift.DOScale(new Vector3(1.25f, 0.75f, 1f), squashDuration * 0.3f).SetEase(Ease.OutQuad));
        seq.Append(gift.DOScale(new Vector3(0.85f, 1.15f, 1f), squashDuration * 0.3f).SetEase(Ease.InOutQuad));
        seq.Append(gift.DOScale(originScale, squashDuration * 0.4f).SetEase(Ease.OutBack));
        seq.OnComplete(() => onDone?.Invoke());
    }

    public void Hide()
    {
        seq?.Kill();
        ResetTransform();
        if (gift != null) gift.gameObject.SetActive(false);
    }
}