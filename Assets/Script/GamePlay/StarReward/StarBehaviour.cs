using System.Collections;
using UnityEngine;

public class StarBehaviour : MonoBehaviour
{
    [SerializeField] private Vector3 targetScale = Vector3.one;
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private float delay = 0f;

    [SerializeField] private float overshoot = 1.70158f;
    [SerializeField] private bool spin = true;
    [SerializeField] private float spinAngle = 360f;

    [SerializeField] private AudioClip popSound;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.1f);

    private Coroutine playRoutine;
    private Quaternion baseRotation;

    private void Awake()
    {
        baseRotation = transform.localRotation;
        transform.localScale = Vector3.zero;
    }

    private void OnEnable()
    {
        transform.localScale = Vector3.zero;
    }

    private void OnDisable()
    {
        playRoutine = null;  
    }

    public void Play()
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(ScaleRoutine());
    }

    public void ResetStar()
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

        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        PlaySound();

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = EaseOutBack(p);

            transform.localScale = targetScale * eased;

            if (spin)
            {
                float rot = spinAngle * (1f - Mathf.Pow(1f - p, 3f));
                transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, rot);
            }
            yield return null;
        }

        transform.localScale = targetScale;
        transform.localRotation = baseRotation;
        playRoutine = null;
    }

    private void PlaySound()
    {
        SoundManager.Instance?.PlaySfxRandomPitch(popSound, pitchRange, volume);
    }

    private float EaseOutBack(float x)
    {
        float c1 = overshoot;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}