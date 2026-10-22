public static class CoinReward
{
    public static int Get(DifficultyLevel d) => d switch
    {
        DifficultyLevel.Easy   => 5,
        DifficultyLevel.Normal => 10,
        DifficultyLevel.Hard   => 15,
        _ => 0
    };
}