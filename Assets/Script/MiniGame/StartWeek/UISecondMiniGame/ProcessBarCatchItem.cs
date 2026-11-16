using UnityEngine;

public class ProcessBarCatchItem : MonoBehaviour
{
    [SerializeField] private ScoreMiniGameSecondManager scoreManager;
    [SerializeField] private ProcessBar processBar;
    [SerializeField] private int maxScore = 30;

    private void OnEnable()
    {
        scoreManager.ScoreChanged += OnScoreChanged;
        processBar.SetProgress(scoreManager.TotalScore, maxScore, false);
    }

    private void OnDisable()
    {
        scoreManager.ScoreChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int totalScore, int addedAmount)
    {
        bool animate = addedAmount != 0;
        int clamped = Mathf.Min(totalScore, maxScore);
        processBar.SetProgress(clamped, maxScore, animate);
    }
}