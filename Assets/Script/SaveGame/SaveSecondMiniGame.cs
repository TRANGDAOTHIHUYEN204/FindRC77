using UnityEngine;

public static class SaveSecondMiniGame
{
    private const string KeyCompleted = "sh_completed";

    public static bool Completed
    {
        get => PlayerPrefs.GetInt(KeyCompleted, 0) == 1;
        set { PlayerPrefs.SetInt(KeyCompleted, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(KeyCompleted);
        PlayerPrefs.Save();
    }
}