using System;
using System.Collections;
using UnityEngine;

public class ProcessStreak : MonoBehaviour
{
    [SerializeField] private StreakManager streakManager;
    [SerializeField] private ProcessBar bar;
    [SerializeField] private float waitDuration = 0.25f; 

    private Coroutine _routine;

    private void OnDisable() { _routine = null; }

    private float Ratio(int streak)
    {
        int max = streakManager.MaxStreak;
        return max <= 0 ? 0f : Mathf.Clamp01(streak / (float)max);
    }

    public void SetInstant(int streak)
    {
        if (bar != null) bar.SetProgress(Ratio(streak), false);
    }

    public void Play(int streak, Action onDone)
    {
        if (bar == null || !isActiveAndEnabled) { onDone?.Invoke(); return; }

        float target = Ratio(streak);
        bool needRun = !Mathf.Approximately(bar.Ratio, target);
        bar.SetProgress(target, true);

        if (!needRun) { onDone?.Invoke(); return; }
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Wait(onDone));
    }

    private IEnumerator Wait(Action onDone)
    {
        yield return new WaitForSecondsRealtime(waitDuration);
        _routine = null;
        onDone?.Invoke();
    }
}