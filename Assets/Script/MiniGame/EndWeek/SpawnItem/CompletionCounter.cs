using UnityEngine;
using UnityEngine.Events;

public class CompletionCounter : MonoBehaviour
{
    [SerializeField] private UnityEvent onAllCompleted;

    private int remaining;

    public void Init(int total) => remaining = total;

    public void NotifyCompleted()
    {
        if (remaining <= 0) return;
        if (--remaining == 0)
            onAllCompleted?.Invoke();
    }
}