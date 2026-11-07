using UnityEngine;

public static class SaveMiniGame 
{
    private const string KeyLevel = "fh_current_level";
    private const string KeyAllCompleted = "fh_all_completed";

    public static int CurrentLevel
    {
        get => PlayerPrefs.GetInt(KeyLevel, 0);
        set { PlayerPrefs.SetInt(KeyLevel, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static bool AllCompleted
    {
        get => PlayerPrefs.GetInt(KeyAllCompleted, 0) == 1;
        set { PlayerPrefs.SetInt(KeyAllCompleted, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(KeyLevel);
        PlayerPrefs.DeleteKey(KeyAllCompleted);
        PlayerPrefs.Save();
    }
}