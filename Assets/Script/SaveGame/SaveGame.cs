using UnityEngine;

public static class SaveGame
{
    private const string KeyLevel = "save_current_level";
    private const int DefaultHint = 10;

    public static int CurrentLevel
    {
        get => PlayerPrefs.GetInt(KeyLevel, 0);
        set { PlayerPrefs.SetInt(KeyLevel, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }
    
    

    public static void Save() => PlayerPrefs.Save();
    
    public static void Reset()
    {
        PlayerPrefs.DeleteKey(KeyLevel);
        PlayerPrefs.Save();
    }
}