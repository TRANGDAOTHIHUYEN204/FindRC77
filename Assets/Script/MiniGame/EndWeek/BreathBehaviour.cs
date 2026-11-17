using DG.Tweening;
using UnityEngine;

public class BreathBehaviour : MonoBehaviour
{
    private float squashAmount = 0.05f;
    private float phaseDuration = 1f;
    private Ease ease = Ease.InOutSine; 
    private float wobbleAngle = 3f; 
    private bool playOnStart = true;   

    private Vector3 baseScale;
    private Vector3 baseEuler;

    private void Awake()
    {
        baseScale = transform.localScale;
        baseEuler = transform.localEulerAngles;
    }

    private void Start()
    {
        if (playOnStart) StartBreathing();
    }

    private void OnDisable() => DOTween.Kill(this);
    private void OnDestroy() => DOTween.Kill(this);

    public void StartBreathing()
    {
        DOTween.Kill(this);
        ResetPose();

        Vector3 stretch = new Vector3(baseScale.x * (1f - squashAmount * 0.6f), baseScale.y * (1f + squashAmount), baseScale.z);
        Vector3 squash = new Vector3(baseScale.x * (1f + squashAmount * 0.6f), baseScale.y * (1f - squashAmount), baseScale.z);

        Vector3 tiltLeft = baseEuler + new Vector3(0, 0, wobbleAngle);
        Vector3 tiltRight = baseEuler + new Vector3(0, 0, -wobbleAngle);

        Sequence seq = DOTween.Sequence().SetTarget(this).SetLoops(-1, LoopType.Restart);

        seq.Append(transform.DOScale(stretch, phaseDuration).SetEase(ease));
        seq.Join(transform.DOLocalRotate(tiltLeft, phaseDuration).SetEase(ease));

        seq.Append(transform.DOScale(squash, phaseDuration).SetEase(ease));
        seq.Join(transform.DOLocalRotate(tiltRight, phaseDuration).SetEase(ease));

        seq.Append(transform.DOScale(baseScale, phaseDuration).SetEase(ease));
        seq.Join(transform.DOLocalRotate(baseEuler, phaseDuration).SetEase(ease));
    }

    public void StopBreathing()
    {
        DOTween.Kill(this);
        ResetPose();
    }

    private void ResetPose()
    {
        transform.localScale = baseScale;
        transform.localEulerAngles = baseEuler;
    }
}