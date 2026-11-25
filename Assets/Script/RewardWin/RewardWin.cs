using UnityEngine;

[CreateAssetMenu(fileName = "RewardWin", menuName = "Scriptable Objects/RewardWin")]
public class RewardWin : ScriptableObject
{
    [SerializeField, Min(1)] private int hintPrice = 100;  
    [SerializeField, Min(1)] private int firstLoopLevels = 2;
    [SerializeField, Min(0)] private int firstLoopHint = 1;
    [SerializeField, Min(1)] private int otherLoopLevels = 3;
    [SerializeField, Range(0f, 2f)] private float coinMinPercent = 0.9f;
    [SerializeField, Range(0f, 2f)] private float coinMaxPercent = 1.1f;

    public int FirstLoopCoin => hintPrice;
    public int FirstLoopHint => firstLoopHint;

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
        float min = Mathf.Min(coinMinPercent, coinMaxPercent);
        float max = Mathf.Max(coinMinPercent, coinMaxPercent);
        float p = Random.Range(min, max);
        return Mathf.Max(0, Mathf.RoundToInt(hintPrice * p));
    }

    public void GetReward(int level, out int coin, out int hint)
    {
        coin = 0;
        hint = 0;

        if (!IsGiftLevel(level)) return;

        if (IsFirstLoop(level))
        {
            coin = FirstLoopCoin;
            hint = FirstLoopHint;
            return;
        }

        if (Random.Range(0, 2) == 0)
            coin = RollOtherLoopCoin();
        else
            hint = 1;
    }
}