using System.Collections;
using UnityEngine;

public class ProcessBar : MonoBehaviour
{
    [SerializeField] private RectTransform fillMask;
    [SerializeField] private RectTransform fill;
    [SerializeField] private float fillDuration = 0.25f;

    private float fullWidth;
    private float currentRatio;
    private Coroutine fillRoutine;
    private bool initialized;

    public float Ratio => currentRatio;

    private void OnDisable()
    {
        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }
    }

    public void SetProgress(int current, int total, bool animate = true)
    {
        float ratio = total <= 0 ? 0f : (float)current / total;
        SetProgress(ratio, animate);
    }

    public void SetProgress(float ratio, bool animate = true)
    {
        Init();
        ratio = Mathf.Clamp01(ratio);

        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }

        if (animate && isActiveAndEnabled)
            fillRoutine = StartCoroutine(FillTo(ratio));
        else
            ApplyRatio(ratio);
    }

    private void Init()
    {
        if (initialized) return;

        Canvas.ForceUpdateCanvases();

        fillMask.anchorMin = new Vector2(0, fillMask.anchorMin.y);
        fillMask.anchorMax = new Vector2(0, fillMask.anchorMax.y);
        fillMask.pivot     = new Vector2(0, fillMask.pivot.y);

        fullWidth = fill.rect.width;
        if (fullWidth <= 0f) fullWidth = fillMask.rect.width;

        fill.anchorMin = new Vector2(0, fill.anchorMin.y);
        fill.anchorMax = new Vector2(0, fill.anchorMax.y);
        fill.pivot     = new Vector2(0, fill.pivot.y);
        fill.anchoredPosition = new Vector2(0, fill.anchoredPosition.y);
        fill.sizeDelta = new Vector2(fullWidth, fill.sizeDelta.y);

        initialized = true;
    }

    private IEnumerator FillTo(float target)
    {
        float start = currentRatio;
        float t = 0f;
        while (t < fillDuration)
        {
            t += Time.unscaledDeltaTime;
            ApplyRatio(Mathf.Lerp(start, target, t / fillDuration));
            yield return null;
        }
        ApplyRatio(target);
        fillRoutine = null;
    }

    private void ApplyRatio(float ratio)
    {
        currentRatio = ratio;
        fillMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullWidth * ratio);
    }
}