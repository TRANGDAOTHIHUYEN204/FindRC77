using System;
using DG.Tweening;
using UnityEngine;

public class IconCollectionMover : MonoBehaviour
{
    private Tween tween;
    private bool fxActive;
    private Vector3 fxScale;
    private Quaternion fxRot;

    public void MoveTo(Vector3 worldPosition, float duration, Ease ease = Ease.OutQuad, Action onComplete = null)
    {
        Kill();
        tween = transform.DOMove(worldPosition, duration)
            .SetEase(ease).SetLink(gameObject)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void MoveToLocal(Vector3 localPosition, float duration, Ease ease = Ease.OutQuad, Action onComplete = null)
    {
        Kill();
        tween = transform.DOLocalMove(localPosition, duration)
            .SetEase(ease).SetLink(gameObject)
            .OnComplete(() => onComplete?.Invoke());
    }
    private Sequence BuildJump(Vector3 end, float power, float duration, Ease moveEase, bool local)
    {
        Vector3 start = local ? transform.localPosition : transform.position;
        float p = 0f;
        float arc = 0f;

        void Apply()
        {
            Vector3 pos = Vector3.LerpUnclamped(start, end, p);
            pos.y += arc;
            if (local) transform.localPosition = pos;
            else transform.position = pos;
        }

        float half = duration * 0.5f;
        Sequence seq = DOTween.Sequence().SetLink(gameObject);

        seq.Insert(0f, DOTween.To(v => { p = v; Apply(); }, 0f, 1f, duration).SetEase(moveEase));
        seq.Insert(0f, DOTween.To(v => { arc = v; Apply(); }, 0f, power, half).SetEase(Ease.OutQuad));
        seq.Insert(half, DOTween.To(v => { arc = v; Apply(); }, power, 0f, half).SetEase(Ease.InQuad));

        return seq;
    }

    public void JumpTo(Vector3 worldPosition, float jumpPower, float duration,
        Ease ease = Ease.OutQuad, Action onComplete = null)
    {
        Kill();
        Sequence seq = BuildJump(worldPosition, jumpPower, duration, ease, false);
        seq.OnComplete(() => onComplete?.Invoke());
        tween = seq;
    }

    public void JumpToLocal(Vector3 localPosition, float jumpPower, float duration,
        Ease ease = Ease.OutQuad, Action onComplete = null)
    {
        Kill();
        Sequence seq = BuildJump(localPosition, jumpPower, duration, ease, true);
        seq.OnComplete(() => onComplete?.Invoke());
        tween = seq;
    }

    public void JumpAndScaleTo(Vector3 worldPosition, float jumpPower, float duration, Ease jumpEase,
        Vector3 endScale, Ease scaleEase, Action onComplete = null)
    {
        Kill();

        Sequence seq = BuildJump(worldPosition, jumpPower, duration, jumpEase, false);
        seq.Insert(0f, transform.DOScale(endScale, duration).SetEase(scaleEase));
        seq.OnComplete(() => onComplete?.Invoke());

        tween = seq;
    }

    public void ScaleBounceInPlace(Vector3 homeScale, float amount, int bounces, float firstDuration,
        float decay = 0.5f, Action onComplete = null)
    {
        Kill();

        if (bounces <= 0)
        {
            transform.localScale = homeScale;
            onComplete?.Invoke();
            return;
        }

        fxScale = homeScale;
        fxRot = transform.localRotation;
        fxActive = true;

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        float a = amount;
        float d = firstDuration;

        for (int i = 0; i < bounces; i++)
        {
            seq.Append(transform.DOScale(
                new Vector3(homeScale.x * (1f + a), homeScale.y * (1f - a), homeScale.z), d * 0.3f)
                .SetEase(Ease.OutQuad));
            seq.Append(transform.DOScale(
                new Vector3(homeScale.x * (1f - a * 0.5f), homeScale.y * (1f + a * 0.5f), homeScale.z), d * 0.35f)
                .SetEase(Ease.InOutSine));
            seq.Append(transform.DOScale(homeScale, d * 0.35f).SetEase(Ease.OutSine));

            a *= decay;
            d *= Mathf.Sqrt(decay);
        }

        seq.OnComplete(() =>
        {
            transform.localScale = homeScale;
            fxActive = false;
            onComplete?.Invoke();
        });

        tween = seq;
    }
    public void BounceAndWobble(Vector3 homeLocal, Vector3 homeScale, float height, int bounces,
        float firstDuration, float wobbleAngle, float decay = 0.6f, float stretch = 0.08f,
        Action onComplete = null)
    {
        Kill();

        if (bounces <= 0)
        {
            transform.localPosition = homeLocal;
            transform.localScale = homeScale;
            onComplete?.Invoke();
            return;
        }

        Quaternion homeRot = transform.localRotation;
        fxScale = homeScale;
        fxRot = homeRot;
        fxActive = true;

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        float t = 0f;
        float h = height;
        float d = firstDuration;
        float ang = wobbleAngle;
        float s = stretch;
        float sign = 1f;

        for (int i = 0; i < bounces; i++)
        {
            float half = d * 0.5f;

            Vector3 stretched = new Vector3(
                homeScale.x * (1f - s * 0.5f),
                homeScale.y * (1f + s),
                homeScale.z);
            seq.Insert(t, transform.DOLocalMoveY(homeLocal.y + h, half).SetEase(Ease.OutQuad));
            seq.Insert(t, transform.DOLocalRotateQuaternion(
                homeRot * Quaternion.Euler(0f, 0f, sign * ang), half).SetEase(Ease.OutSine));
            seq.Insert(t, transform.DOScale(stretched, half).SetEase(Ease.OutSine));
            seq.Insert(t + half, transform.DOLocalMoveY(homeLocal.y, half).SetEase(Ease.InQuad));
            seq.Insert(t + half, transform.DOLocalRotateQuaternion(homeRot, half).SetEase(Ease.InSine));
            seq.Insert(t + half, transform.DOScale(homeScale, half).SetEase(Ease.InSine));

            sign = -sign;
            t += d;
            h *= decay;
            d *= Mathf.Sqrt(decay);
            ang *= decay;
            s *= decay;
        }

        seq.OnComplete(() =>
        {
            transform.localPosition = homeLocal;
            transform.localRotation = homeRot;
            transform.localScale = homeScale;
            fxActive = false;
            onComplete?.Invoke();
        });

        tween = seq;
    }

    public void Kill()
    {
        tween?.Kill();
        if (fxActive)
        {
            transform.localScale = fxScale;
            transform.localRotation = fxRot;
            fxActive = false;
        }
    }

    private void OnDisable() => Kill();
    private void OnDestroy() => tween?.Kill();
}