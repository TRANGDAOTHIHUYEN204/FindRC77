using System;
using System.Collections;
using UnityEngine;

public class ProcessStreak : MonoBehaviour
{
    [SerializeField] private StreakManager streakManager;
    [SerializeField] private ProcessBar bar;
    [SerializeField] private float waitDuration = 0.25f;

    private Coroutine _routine;

    private void OnDisable()
    {
        Stop();
    }

    private float Ratio(int streak)
    {
        int max = streakManager.MaxStreak;

        return max <= 0
            ? 0f
            : Mathf.Clamp01(streak / (float)max);
    }

    private bool HasStreakManager()
    {
        if (streakManager != null) return true;

        Debug.LogError("[ProcessStreak] StreakManager reference is missing.", this);
        return false;
    }

    public void SetInstant(int streak)
    {
        if (!HasStreakManager()) return;

        Stop();

        if (bar != null)
            bar.SetProgress(Ratio(streak), false);
    }

    public void Stop()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    public void Play(int streak, Action onDone)
    {
        if (!HasStreakManager())
        {
            onDone?.Invoke();
            return;
        }

        Stop();

        if (!isActiveAndEnabled || bar == null)
        {
            onDone?.Invoke();
            return;
        }

        float target = Ratio(streak);
        bool needRun = !Mathf.Approximately(bar.Ratio, target);

        bar.SetProgress(target, true);

        if (!needRun)
        {
            onDone?.Invoke();
            return;
        }

        _routine = StartCoroutine(Wait(onDone));
    }

    private IEnumerator Wait(Action onDone)
    {
        yield return new WaitForSecondsRealtime(waitDuration);

        _routine = null;

        onDone?.Invoke();
    }
}