using System;
using UnityEngine;

public class ScoreMiniGameSecondManager : MonoBehaviour
{
    [SerializeField] private HandlePlayerCollisionItem collisionHandler;

    public int TotalScore { get; private set; }
    public event Action<int, int> ScoreChanged;

    private void OnEnable()
    {
        collisionHandler.Caught += OnItemCaught;
    }

    private void OnDisable()
    {
        collisionHandler.Caught -= OnItemCaught;
    }

    private void OnItemCaught(Item item)
    {
        AddScore(item.Score);
    }

    public void AddScore(int amount)
    {
        TotalScore += amount;
        ScoreChanged?.Invoke(TotalScore, amount);
    }

    public void ResetScore()
    {
        TotalScore = 0;
        ScoreChanged?.Invoke(TotalScore, 0);
    }
}