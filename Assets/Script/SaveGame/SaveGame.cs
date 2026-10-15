using UnityEngine;

public static class SaveGame
{
    private const string KeyLevel = "save_current_level";
    private const string KeyScore = "save_score";
    
    public static int CurrentLevel
    {
        get => PlayerPrefs.GetInt(KeyLevel, 0);
        set { PlayerPrefs.SetInt(KeyLevel, Mathf.Max(0, value)); }
    }

    public static int Score
    {
        get => PlayerPrefs.GetInt(KeyScore, 0);
        set { PlayerPrefs.SetInt(KeyScore, Mathf.Max(0, value)); }
    }

    public static void Save() => PlayerPrefs.Save();

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(KeyLevel);
        PlayerPrefs.DeleteKey(KeyScore);
        PlayerPrefs.Save();
    }
}