using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private bool countDown = true;
    [SerializeField] private FindHiddenLevelSpawner spawnerLevel;
    private float startTime => spawnerLevel.Time;

    public float CurrentTime { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action OnTimeUp;

    void Update()
    {
        if (!IsRunning) return;

        if (countDown)
        {
            CurrentTime -= Time.deltaTime;
            if (CurrentTime <= 0f)
            {
                CurrentTime = 0f;
                IsRunning = false;
                OnTimeUp?.Invoke();
            }
        }
        else
        {
            CurrentTime += Time.deltaTime;
        }
    }

    public void StartTimer() => IsRunning = true;
    public void PauseTimer() => IsRunning = false;

    public void ResetTimer()
    {
        CurrentTime = countDown ? startTime : 0f;
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(CurrentTime / 60f);
        int seconds = Mathf.FloorToInt(CurrentTime % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}