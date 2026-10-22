using UnityEngine;

public static class PendingReward
{
    private const string Key = "pending_coin";

    public static int Amount => PlayerPrefs.GetInt(Key, 0);

    public static void Add(int n)
    {
        if (n <= 0) return;

        PlayerPrefs.SetInt(Key, Amount + n);
        PlayerPrefs.Save();
    }
    
    public static void Clear()
    {
        PlayerPrefs.SetInt(Key, 0);
        PlayerPrefs.Save();
    }
}