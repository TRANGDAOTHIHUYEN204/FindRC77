using TMPro;
using UnityEngine;

public class StreakText : MonoBehaviour
{
    [SerializeField] private StreakManager streakManager;
    [SerializeField] private TMP_Text currentText;   // số trong ngọn lửa
    [SerializeField] private TMP_Text moreWinText;   // "X more win(s)"
    [SerializeField] private TMP_Text progressText;  // "5/10"

    private void OnEnable()
    {
        if (streakManager == null)
        {
            Debug.LogError("[StreakText] StreakManager reference is missing.", this);
            return;
        }

        streakManager.OnStreakChanged += OnStreakChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (streakManager != null)
            streakManager.OnStreakChanged -= OnStreakChanged;
    }

    private void OnStreakChanged(int _) => Refresh();

    private void Refresh()
    {
        if (streakManager == null) return;

        int current = streakManager.CurrentStreak;
        int max = streakManager.MaxStreak;

        if (currentText != null) currentText.text = current.ToString();
        if (progressText != null) progressText.text = $"{Mathf.Min(current, max)}/{max}";

        if (moreWinText != null && streakManager.TryGetNextMilestone(current, out var next))
        {
            int remaining = next.streak - current;
            moreWinText.text = remaining == 1 ? "1 more win" : $"{remaining} more wins";
        }
    }
}