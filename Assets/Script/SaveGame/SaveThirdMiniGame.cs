using UnityEngine;

public enum ThirdResult { None = 0, Won = 1, Lost = 2 }

public static class SaveThirdMiniGame
{
    private const string Key = "save_clean_result";

    public static ThirdResult Result
    {
        get => (ThirdResult)PlayerPrefs.GetInt(Key, 0);
        set { PlayerPrefs.SetInt(Key, (int)value); PlayerPrefs.Save(); }
    }

    public static bool HasPlayed => Result != ThirdResult.None;
    public static bool Completed => Result == ThirdResult.Won;

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}