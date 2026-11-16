using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] private float totalTime = 240f;
    [SerializeField] private float warningTime = 10f;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private AudioClip warningClip;
    [Range(0f, 1f)] [SerializeField] private float warningVolume = 1f;

    public event Action<float> OnTimeChanged;
    public event Action OnWarningStart;
    public event Action OnTimeUp;

    private float _timeLeft;
    private bool _running;
    private bool _warningStarted;
    private int _lastWarningSecond = -1;

    public float TimeLeft => _timeLeft;
    public bool IsRunning => _running;
    public bool IsWarning => _timeLeft <= warningTime && _timeLeft > 0f;

    void Awake()
    {
        _timeLeft = totalTime;
    }

    void Start()
    {
        OnTimeChanged?.Invoke(_timeLeft);
        if (autoStart) StartTimer();
    }

    void Update()
    {
        if (!_running) return;

        _timeLeft -= Time.deltaTime;

        if (_timeLeft <= 0f)
        {
            _timeLeft = 0f;
            _running = false;
            OnTimeChanged?.Invoke(_timeLeft);
            OnTimeUp?.Invoke();
            return;
        }

        OnTimeChanged?.Invoke(_timeLeft);
        HandleWarning();
    }

    private void HandleWarning()
    {
        if (_timeLeft > warningTime) return;

        if (!_warningStarted)
        {
            _warningStarted = true;
            OnWarningStart?.Invoke();
        }

        int currentSecond = Mathf.CeilToInt(_timeLeft);
        if (currentSecond != _lastWarningSecond)
        {
            _lastWarningSecond = currentSecond;
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySfx(warningClip, warningVolume);
        }
    }

    public void StartTimer() => _running = true;

    public void PauseTimer() => _running = false;

    public void ResumeTimer()
    {
        if (_timeLeft > 0f) _running = true;
    }

    public void ResetTimer()
    {
        _timeLeft = totalTime;
        _running = false;
        _warningStarted = false;
        _lastWarningSecond = -1;
        OnTimeChanged?.Invoke(_timeLeft);
    }

    public void AddTime(float seconds)
    {
        _timeLeft = Mathf.Max(0f, _timeLeft + seconds);
        if (_timeLeft > warningTime)
        {
            _warningStarted = false;
            _lastWarningSecond = -1;
        }
        OnTimeChanged?.Invoke(_timeLeft);
    }

    public static string Format(float seconds)
    {
        int s = Mathf.CeilToInt(seconds);
        return $"{s / 60:00}:{s % 60:00}";
    }
}