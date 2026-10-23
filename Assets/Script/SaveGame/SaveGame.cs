using UnityEngine;

public static class SaveGame
{
    private const string KeyLevel = "save_current_level";
    private const string KeyMusic = "save_music_on";
    private const string KeySfx = "save_sfx_on";
    private const string KeyTutorial = "isCompletedTutorial";
    private const string KeyTutorialShown = "isTutorialShown";

    public static bool TutorialShown
    {
        get => PlayerPrefs.GetInt(KeyTutorialShown, 0) == 1;
        set { PlayerPrefs.SetInt(KeyTutorialShown, value ? 1 : 0); PlayerPrefs.Save(); }
    }
    
    public static int CurrentLevel
    {
        get => PlayerPrefs.GetInt(KeyLevel, 0);
        set { PlayerPrefs.SetInt(KeyLevel, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt(KeyMusic, 1) == 1;
        set { PlayerPrefs.SetInt(KeyMusic, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static bool SfxOn
    {
        get => PlayerPrefs.GetInt(KeySfx, 1) == 1;
        set { PlayerPrefs.SetInt(KeySfx, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static void Save() => PlayerPrefs.Save();

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(KeyLevel);
        PlayerPrefs.Save();
    }
    public static bool TutorialCompleted
    {
        get => PlayerPrefs.GetInt(KeyTutorial, 0) == 1;
        set { PlayerPrefs.SetInt(KeyTutorial, value ? 1 : 0); PlayerPrefs.Save(); }
    }
    public static void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(KeyTutorial);
        PlayerPrefs.DeleteKey(KeyTutorialShown);
        PlayerPrefs.Save();
    }
}