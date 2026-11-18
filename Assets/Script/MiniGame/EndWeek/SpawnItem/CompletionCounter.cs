using System;
using UnityEngine;
using UnityEngine.Events;

public class CompletionCounter : MonoBehaviour
{
    [SerializeField] private UnityEvent onAllCompleted;

    public event Action OnAllCompleted;
    public event Action<int, int> OnChanged;   

    public int Total { get; private set; }
    public int Remaining { get; private set; }
    public int Completed => Total - Remaining;

    public void Init(int total)
    {
        Total = total;
        Remaining = total;
        OnChanged?.Invoke(Completed, Total);
    }

    public void NotifyCompleted()
    {
        if (Remaining <= 0) return;
        Remaining--;
        OnChanged?.Invoke(Completed, Total);

        if (Remaining == 0)
        {
            onAllCompleted?.Invoke();
            OnAllCompleted?.Invoke();
        }
    }
}