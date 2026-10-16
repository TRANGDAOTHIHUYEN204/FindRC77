using UnityEngine;

public static class SaveGame
{
    private const string KeyLevel = "save_current_level";
    private const string KeyScore = "save_score";
    private const string KeyHint  = "save_hint_count";
    private const int DefaultHint = 3;

    public static int CurrentLevel
    {
        get => PlayerPrefs.GetInt(KeyLevel, 0);
        set { PlayerPrefs.SetInt(KeyLevel, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static int Score
    {
        get => PlayerPrefs.GetInt(KeyScore, 0);
        set { PlayerPrefs.SetInt(KeyScore, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static int HintCount
    {
        get => PlayerPrefs.GetInt(KeyHint, DefaultHint);
        set { PlayerPrefs.SetInt(KeyHint, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static void Save() => PlayerPrefs.Save();
    
    public static void Reset()
    {
        PlayerPrefs.DeleteKey(KeyLevel);
        PlayerPrefs.DeleteKey(KeyScore);
        PlayerPrefs.Save();
    }
    public static void ResetHint()
    {
        PlayerPrefs.DeleteKey(KeyHint);
    }
}