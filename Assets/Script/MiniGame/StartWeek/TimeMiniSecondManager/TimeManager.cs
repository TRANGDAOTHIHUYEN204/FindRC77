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
    public event Action<int> OnMinutePassed;

    private float _timeLeft;
    private bool _running;
    private bool _warningStarted;
    private int _lastWarningSecond = -1;
    private int _lastMinutePassed;

    public float TimeLeft => _timeLeft;
    public bool IsRunning => _running;
    public bool IsWarning => _timeLeft <= warningTime && _timeLeft > 0f;

    void Awake()
    {
        _timeLeft = totalTime;
        _lastMinutePassed = 0;
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
            HandleMinutePassed();
            OnTimeUp?.Invoke();
            return;
        }

        OnTimeChanged?.Invoke(_timeLeft);
        HandleMinutePassed();
        HandleWarning();
    }

    private void HandleMinutePassed()
    {
        int elapsedMinutes = Mathf.FloorToInt((totalTime - _timeLeft) / 60f);
        while (_lastMinutePassed < elapsedMinutes)
        {
            _lastMinutePassed++;
            OnMinutePassed?.Invoke(_lastMinutePassed);
        }
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
        _lastMinutePassed = 0;
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
        int elapsedMinutes = Mathf.Max(0, Mathf.FloorToInt((totalTime - _timeLeft) / 60f));
        _lastMinutePassed = Mathf.Min(_lastMinutePassed, elapsedMinutes);

        OnTimeChanged?.Invoke(_timeLeft);
    }

    public static string Format(float seconds)
    {
        int s = Mathf.CeilToInt(seconds);
        return $"{s / 60:00}:{s % 60:00}";
    }
}