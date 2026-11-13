using System.Collections;
using UnityEngine;

public class ProcessBar : MonoBehaviour
{
    [SerializeField] private MiniGameManager gameManager;
    [SerializeField] private RectTransform fillMask;
    [SerializeField] private RectTransform fill;
    [SerializeField] private float fillDuration = 0.25f;

    private float fullWidth;
    private float currentRatio;
    private Coroutine fillRoutine;
    private bool initialized;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<MiniGameManager>();
    }

    private void OnEnable()
    {
        if (gameManager == null) return;
        gameManager.OnProgress += HandleProgress;
        
        Init();
        var state = gameManager.State;
        SetProgress(state.FoundCount, state.Total, false);
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnProgress -= HandleProgress;

        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }
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

    private void HandleProgress(int found, int total) => SetProgress(found, total, true);

    private void SetProgress(int found, int total, bool animate)
    {
        Init();
        float target = total <= 0 ? 0f : Mathf.Clamp01((float)found / total);

        if (fillRoutine != null) StopCoroutine(fillRoutine);

        if (animate && isActiveAndEnabled)
            fillRoutine = StartCoroutine(FillTo(target));
        else
            ApplyRatio(target);
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