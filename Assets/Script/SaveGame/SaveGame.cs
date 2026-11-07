using UnityEngine;

public static class SaveGame
{
    private const string KeyLevel = "save_current_level";
    private const string KeyMusic = "save_music_on";
    private const string KeySfx = "save_sfx_on";
    private const string KeyTutorial = "isCompletedTutorial";
    private const string KeyTutorialShown = "isTutorialShown";
    private const string KeyAllCompleted = "save_all_completed";
    private const string KeyHintBalance = "save_hint_balance";
    private const string KeyFindHiddenCompleted = "save_find_hidden_completed";

    public static bool AllCompleted
    {
        get => PlayerPrefs.GetInt(KeyAllCompleted, 0) == 1;
        set { PlayerPrefs.SetInt(KeyAllCompleted, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static int HintBalance
    {
        get => PlayerPrefs.GetInt(KeyHintBalance, 0);
        set { PlayerPrefs.SetInt(KeyHintBalance, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static bool TryClaimFindHiddenReward()
    {
        if (PlayerPrefs.GetInt(KeyFindHiddenCompleted, 0) == 1)
            return false;

        PlayerPrefs.SetInt(KeyFindHiddenCompleted, 1);
        PlayerPrefs.SetInt(KeyHintBalance, HintBalance + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(KeyLevel);
        PlayerPrefs.DeleteKey(KeyAllCompleted);
        PlayerPrefs.DeleteKey(KeyHintBalance);
        PlayerPrefs.DeleteKey(KeyFindHiddenCompleted);
        PlayerPrefs.Save();
    }

    public static bool TutorialShown
    {
        get => PlayerPrefs.GetInt(KeyTutorialShown, 0) == 1;
        set
        {
            Debug.Log($"[SaveGame] TutorialShown = {value}\n{System.Environment.StackTrace}");
            PlayerPrefs.SetInt(KeyTutorialShown, value ? 1 : 0); PlayerPrefs.Save();
        }
    }

    public static bool TutorialCompleted
    {
        get => PlayerPrefs.GetInt(KeyTutorial, 0) == 1;
        set
        {
            Debug.Log($"[SaveGame] TutorialCompleted = {value}\n{System.Environment.StackTrace}");
            PlayerPrefs.SetInt(KeyTutorial, value ? 1 : 0); PlayerPrefs.Save();
        }
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

    public static void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(KeyTutorial);
        PlayerPrefs.DeleteKey(KeyTutorialShown);
        PlayerPrefs.Save();
    }
}