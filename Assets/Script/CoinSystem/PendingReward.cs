using System;

public static class PendingReward
{
    public static event Action OnAdded;

    public static int Amount => Wallets.Coin.PendingAmount;

    public static void Add(int n)
    {
        if (n <= 0) return;
        Wallets.Coin.AddPending(n);
        OnAdded?.Invoke();
    }
}