using System;
using System.Collections;
using UnityEngine;

public class ProcessStreakReward : MonoBehaviour
{
    [SerializeField] private StreakManager streakManager;
    [SerializeField] private ProcessBar bar;
    [Tooltip("Đặt bằng Fill Duration của ProcessBar")]
    [SerializeField] private float waitDuration = 0.25f;

    private Coroutine _routine;

    private void OnDisable()
    {
        _routine = null;
    }

    public void SetInstant(int streak)
    {
        if (bar == null) return;
        bar.SetProgress(CalcRatio(streak), false);
    }

    public void Play(int streak, Action onDone)
    {
        if (bar == null) { onDone?.Invoke(); return; }

        float target = CalcRatio(streak);
        bool needRun = !Mathf.Approximately(bar.Ratio, target);

        bar.SetProgress(target, true);

        if (!needRun || !isActiveAndEnabled)
        {
            onDone?.Invoke();
            return;
        }

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(WaitThen(onDone));
    }

    private IEnumerator WaitThen(Action onDone)
    {
        yield return new WaitForSecondsRealtime(waitDuration);
        _routine = null;
        onDone?.Invoke();
    }
    
    private float CalcRatio(int current)
    {
        var ms = streakManager.Milestones;
        int n = ms.Count;
        if (n < 2) return 0f;
        if (current <= ms[0].streak) return 0f;
        if (current >= ms[n - 1].streak) return 1f;

        for (int i = 0; i < n - 1; i++)
        {
            if (current >= ms[i + 1].streak) continue;
            float seg = (current - ms[i].streak) / (float)(ms[i + 1].streak - ms[i].streak);
            return (i + seg) / (n - 1);
        }
        return 1f;
    }
}