using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class ProcessingBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float duration = 3f;

    private Coroutine routine;

    public void Play(Action onComplete = null)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run(onComplete));
    }

    private IEnumerator Run(Action onComplete)
    {
        SetValue(0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetValue(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        SetValue(1f);
        routine = null;
        onComplete?.Invoke();
    }

    private void SetValue(float v)
    {
        if (fillImage != null) fillImage.fillAmount = v;
    }
}