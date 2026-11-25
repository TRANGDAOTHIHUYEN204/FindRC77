public static class PendingReward
{
    public static int Amount => Wallets.Coin.PendingAmount;

    public static void Add(int n) => Wallets.Coin.AddPending(n);

    public static void Clear() => Wallets.Coin.ClearPending();
}