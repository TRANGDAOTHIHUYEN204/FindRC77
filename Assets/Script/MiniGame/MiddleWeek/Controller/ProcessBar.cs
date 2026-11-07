using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ProcessBar : MonoBehaviour
{
    [SerializeField] private MiniGameManager gameManager;
    [SerializeField] private RectTransform fillMask;
    [SerializeField] private RectTransform fill;    
    [SerializeField] private float fillDuration = 0.25f;

    private float fullWidth;
    private float currentRatio;
    private Coroutine fillRoutine;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<MiniGameManager>();
    }

    private void OnEnable()  => gameManager.OnProgress += HandleProgress;
    private void OnDisable() => gameManager.OnProgress -= HandleProgress;

    private void Start()
    {
        fullWidth = fill.rect.width;               
        fill.anchorMin = new Vector2(0, fill.anchorMin.y);
        fill.anchorMax = new Vector2(0, fill.anchorMax.y);
        fill.pivot = new Vector2(0, fill.pivot.y);
        fill.sizeDelta = new Vector2(fullWidth, fill.sizeDelta.y);

        var state = gameManager.State;
        SetProgress(state.FoundCount, state.Total, false);
    }

    private void HandleProgress(int found, int total) => SetProgress(found, total, true);

    private void SetProgress(int found, int total, bool animate)
    {
        float target = total <= 0 ? 0f : Mathf.Clamp01((float)found / total);

        if (fillRoutine != null) StopCoroutine(fillRoutine);

        if (animate && gameObject.activeInHierarchy)
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
            t += Time.deltaTime;
            ApplyRatio(Mathf.Lerp(start, target, t / fillDuration));
            yield return null;
        }
        ApplyRatio(target);
    }

    private void ApplyRatio(float ratio)
    {
        currentRatio = ratio;
        fillMask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullWidth * ratio);
    }
}