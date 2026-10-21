using System.Collections;
using UnityEngine;

public class BounceEffect : MonoBehaviour
{
    [SerializeField] private float scaleAmount = 0.5f;   // mạnh hơn (trước: 0.2)
    [SerializeField] private float duration = 0.8f;      // lâu hơn (trước: 0.35)
    [SerializeField] private int bounces = 3;            // số lần nảy
    [SerializeField] private float damping = 2.5f;       // càng lớn càng tắt nhanh

    private Vector3 _baseScale;
    private Coroutine _routine;

    public void Play()
    {
        if (!gameObject.activeInHierarchy) return;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            transform.localScale = _baseScale;
        }
        _baseScale = transform.localScale;
        _routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // dao động tắt dần: phình -> thụt -> phình nhỏ dần...
            float wave = Mathf.Sin(t * Mathf.PI * 2f * bounces);
            float fade = Mathf.Exp(-damping * t);
            float k = wave * fade;

            transform.localScale = _baseScale * (1f + k * scaleAmount);
            yield return null;
        }
        transform.localScale = _baseScale;
        _routine = null;
    }

    private void OnDisable()
    {
        if (_routine != null)
        {
            transform.localScale = _baseScale;
            _routine = null;
        }
    }
}