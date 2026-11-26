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
    private const string KeyCatchWin = "save_catch_win";
    private const string KeyCatchDouble = "save_catch_double";
    private const int MaxEventLevels = 1;
    private static string WinKey(int level) => $"save_fh_win_{level}";
    private static string DoubleKey(int level) => $"save_fh_double_{level}";
    private const string KeyCleanWin = "save_clean_win";
    private const string KeyCleanDouble = "save_clean_double";
    private const string KeyTitle = "save_title_unlocked";
    private const string KeyStroke = "save_stroke_unlocked";
    private const string KeyRewardChainDone = "save_reward_chain_done";
    private const string KeyStartingHint = "save_tutorial_hint_claimed"; 
    private const string KeyStreakState = "save_streak_state";
    public static bool TitleUnlocked
    {
        get => PlayerPrefs.GetInt(KeyTitle, 0) == 1;
        set { PlayerPrefs.SetInt(KeyTitle, value ? 1 : 0); PlayerPrefs.Save(); }
    }
    public static string StreakState
    {
        get => PlayerPrefs.GetString(KeyStreakState, "");
        set { PlayerPrefs.SetString(KeyStreakState, value); PlayerPrefs.Save(); }
    }

    public static bool StrokeRewardUnlocked
    {
        get => PlayerPrefs.GetInt(KeyStroke, 0) == 1;
        set { PlayerPrefs.SetInt(KeyStroke, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static bool RewardChainDone
    {
        get => PlayerPrefs.GetInt(KeyRewardChainDone, 0) == 1;
        set { PlayerPrefs.SetInt(KeyRewardChainDone, value ? 1 : 0); PlayerPrefs.Save(); }
    }
    
    public static bool TryClaimStartingHint()
    {
        if (PlayerPrefs.GetInt(KeyStartingHint, 0) == 1) return false;

        PlayerPrefs.SetInt(KeyStartingHint, 1);
        HintBalance += 1; 
        return true;
    }

    public static bool TryClaimCleanWin()
    {
        if (PlayerPrefs.GetInt(KeyCleanWin, 0) == 1) return false;
        PlayerPrefs.SetInt(KeyCleanWin, 1);
        HintBalance += 1;
        return true;
    }

    public static bool IsCleanDoubleClaimed()
        => PlayerPrefs.GetInt(KeyCleanDouble, 0) == 1;

    public static bool TryClaimCleanDouble()
    {
        if (IsCleanDoubleClaimed()) return false;
        PlayerPrefs.SetInt(KeyCleanDouble, 1);
        HintBalance += 1;
        return true;
    }

    public static bool TryClaimFindHiddenWin(int level)
    {
        string key = WinKey(level);
        if (PlayerPrefs.GetInt(key, 0) == 1) return false;

        PlayerPrefs.SetInt(key, 1);
        HintBalance += 1;
        return true;
    }

    public static bool IsFindHiddenDoubleClaimed(int level)
        => PlayerPrefs.GetInt(DoubleKey(level), 0) == 1;

    public static bool TryClaimFindHiddenDouble(int level)
    {
        string key = DoubleKey(level);
        if (PlayerPrefs.GetInt(key, 0) == 1) return false;

        PlayerPrefs.SetInt(key, 1);
        HintBalance += 1;
        return true;
    }

    public static bool TryClaimCatchWin()
    {
        if (PlayerPrefs.GetInt(KeyCatchWin, 0) == 1) return false;

        PlayerPrefs.SetInt(KeyCatchWin, 1);
        HintBalance += 1;
        return true;
    }

    public static bool IsCatchDoubleClaimed()
        => PlayerPrefs.GetInt(KeyCatchDouble, 0) == 1;

    public static bool TryClaimCatchDouble()
    {
        if (IsCatchDoubleClaimed()) return false;

        PlayerPrefs.SetInt(KeyCatchDouble, 1);
        HintBalance += 1;
        return true;
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(KeyLevel);
        PlayerPrefs.DeleteKey(KeyAllCompleted);
        PlayerPrefs.DeleteKey(KeyHintBalance);
        PlayerPrefs.DeleteKey(KeyCatchWin);
        PlayerPrefs.DeleteKey(KeyCatchDouble);
        PlayerPrefs.DeleteKey(KeyCleanWin);
        PlayerPrefs.DeleteKey(KeyStreakState);
        PlayerPrefs.DeleteKey(KeyCleanDouble);
        PlayerPrefs.DeleteKey(KeyTitle);
        PlayerPrefs.DeleteKey(KeyStroke);
        PlayerPrefs.DeleteKey(KeyRewardChainDone);
        PlayerPrefs.DeleteKey(KeyStartingHint);
        SaveThirdMiniGame.Reset();
        for (int i = 0; i < MaxEventLevels; i++)
        {
            PlayerPrefs.DeleteKey(WinKey(i));
            PlayerPrefs.DeleteKey(DoubleKey(i));
        }
        PlayerPrefs.Save();
    }

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