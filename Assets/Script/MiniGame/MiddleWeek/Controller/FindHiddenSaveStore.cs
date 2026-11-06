using UnityEngine;

public static class FindHiddenSaveStore
{
    private const string Key = "find_hidden_progress";

    public static void Save(FindHiddenGameState state)
    {
        if (state.Phase == GamePhase.Won || state.FoundCount == 0)
        {
            Clear();
            return;
        }
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(state.ToSaveData()));
        PlayerPrefs.Save();
    }


    public static bool TryLoad(int level, out FindHiddenSaveData data)
    {
        data = null;
        if (!PlayerPrefs.HasKey(Key)) return false;

        var loaded = JsonUtility.FromJson<FindHiddenSaveData>(PlayerPrefs.GetString(Key));
        if (loaded == null || loaded.level != level) return false;

        data = loaded;
        return true;
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}