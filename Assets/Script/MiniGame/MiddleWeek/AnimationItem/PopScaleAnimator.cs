using System;
using System.Collections;
using UnityEngine;

public class PopScaleAnimator : MonoBehaviour
{
    [Header("Scale")]
    [SerializeField] private bool useCurrentScaleAsTarget = true;
    [SerializeField] private Vector3 targetScale = Vector3.one;
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float delay = 0f;
    [SerializeField] private float overshoot = 1.70158f;

    [Header("Spin")]
    [SerializeField] private bool spin = false;
    [SerializeField] private float spinAngle = 360f;

    [Header("Options")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool startFromZero = true;

    public event Action OnStarted;
    public event Action OnCompleted;

    private Coroutine playRoutine;
    private Quaternion baseRotation;
    private Vector3 finalScale;

    public bool IsPlaying => playRoutine != null;

    private void Awake()
    {
        baseRotation = transform.localRotation;
        finalScale = useCurrentScaleAsTarget ? transform.localScale : targetScale;
    }

    private void OnEnable()
    {
        if (startFromZero) transform.localScale = Vector3.zero;
        if (playOnEnable) Play();
    }

    private void OnDisable()
    {
        playRoutine = null;
        transform.localScale = finalScale;
        transform.localRotation = baseRotation;
    }

    public void Play()
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(ScaleRoutine());
    }

    public void ResetState()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
        transform.localScale = Vector3.zero;
        transform.localRotation = baseRotation;
    }

    private IEnumerator ScaleRoutine()
    {
        transform.localScale = Vector3.zero;
        transform.localRotation = baseRotation;

        if (delay > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(delay);
            else yield return new WaitForSeconds(delay);
        }

        OnStarted?.Invoke();

        float t = 0f;
        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);

            transform.localScale = finalScale * EaseOutBack(p);

            if (spin)
            {
                float rot = spinAngle * (1f - Mathf.Pow(1f - p, 3f));
                transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, rot);
            }
            yield return null;
        }

        transform.localScale = finalScale;
        transform.localRotation = baseRotation;
        playRoutine = null;

        OnCompleted?.Invoke();
    }

    private float EaseOutBack(float x)
    {
        float c1 = overshoot;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}