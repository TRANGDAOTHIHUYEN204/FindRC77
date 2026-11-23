public static class RewardRule
{
    private static readonly int[] GiftLevels = { 3, 6, 9, 12, 15 };

    public static bool IsGiftLevel(int level) => System.Array.IndexOf(GiftLevels, level) >= 0;
}