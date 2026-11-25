using UnityEngine;

public enum RewardType { Hint = 1, Coin = 2 }

[CreateAssetMenu(fileName = "RewardWin", menuName = "Scriptable Objects/RewardWin")]
public class RewardWin : ScriptableObject
{
    [SerializeField, Min(1)] private int firstLoopLevels = 2;
    [SerializeField, Min(0)] private int firstLoopCoin = 100;   
    [SerializeField, Min(0)] private int firstLoopHint = 1;

    [SerializeField, Min(1)] private int otherLoopLevels = 3;
    [SerializeField, Min(0)] private int otherLoopCoin = 100;  
    [SerializeField, Min(0)] private int otherLoopHint = 1;
    [SerializeField, Range(0f, 2f)] private float coinMinPercent = 0.9f;
    [SerializeField, Range(0f, 2f)] private float coinMaxPercent = 1.1f;
    [SerializeField, Range(0f, 1f)] private float hintChance = 0.5f;

    public int FirstLoopCoin => firstLoopCoin;
    public int FirstLoopHint => firstLoopHint;
    public int OtherLoopHint => otherLoopHint;

    public bool IsFirstLoop(int level) => level <= firstLoopLevels;
    
    public void GetLoop(int level, out int current, out int total)
    {
        level = Mathf.Max(1, level);

        if (level <= firstLoopLevels)
        {
            total = firstLoopLevels;
            current = level;
            return;
        }

        total = otherLoopLevels;
        int offset = level - firstLoopLevels;
        current = (offset - 1) % total + 1;
    }

    public bool IsGiftLevel(int level)
    {
        GetLoop(level, out int current, out int total);
        return current == total;
    }

    public int RollOtherLoopCoin()
    {
        float p = Random.Range(coinMinPercent, coinMaxPercent);
        return Mathf.Max(0, Mathf.RoundToInt(otherLoopCoin * p));
    }

    public bool RollIsHint() => Random.value < hintChance;
}