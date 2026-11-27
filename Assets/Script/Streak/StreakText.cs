using TMPro;
using UnityEngine;

public class StreakText : MonoBehaviour
{
    [SerializeField] private StreakManager streakManager;
    [SerializeField] private TMP_Text currentText;
    [SerializeField] private TMP_Text moreWinText;
    [SerializeField] private TMP_Text progressText;

    private void OnEnable()
    {
        if (streakManager == null) return;

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

        if (currentText != null)
            currentText.text = current.ToString();

        if (progressText != null)
            progressText.text = $"{current}/{max}";
    }

    public void SetProgressText(int streak)
    {
        Refresh();
    }

    public void ShowMoreWin(int streak)
    {
        if (moreWinText == null || streakManager == null)
            return;

        bool atMilestone = streak > 0 && streakManager.IsMilestone(streak);
        bool hasNext = streakManager.TryGetNextMilestone(streak, out var next);

        if (atMilestone || !hasNext)
        {
            moreWinText.gameObject.SetActive(false);
            return;
        }

        int remaining = next.streak - streak;

        moreWinText.text = remaining == 1
            ? "1 more win"
            : $"{remaining} more wins";

        moreWinText.gameObject.SetActive(true);
    }

    public void HideMoreWin()
    {
        if (moreWinText != null)
            moreWinText.gameObject.SetActive(false);
    }
}