using UnityEngine;
using System;

public class LevelManager : MonoBehaviour
{
    // Chỉ giữ đường dẫn level; LevelData (và ảnh A/B) được load khi cần,
    // level cũ được giải phóng khi chuyển level để giảm RAM trên máy yếu.
    [SerializeField] private LevelCatalog catalog;

    private int _currentIndex;
    private LevelData _levelData;
    private bool _completed;

    public bool IsTutorial { get; private set; }

    public Sprite LevelSprite => _levelData.RawSprite;
    public Sprite ErrorSprite => _levelData.ErrorSprite;
    public int NumberLevel => _levelData.NumberLevel;
    public int CountDifferent => _levelData.CountDifferent;
    public float TimeLimit => _levelData.TimeLimit;
    public bool HasNextLevel => _currentIndex < CountLevel - 1;
    public int CurrentIndex => _currentIndex;
    public string TextWin => _levelData.TextWin;
    public event Action<LevelData> OnLevelLoaded;
    public bool LastWinWasTutorial { get; private set; }
    public int CountLevel => catalog != null ? catalog.Count : 0;
    public bool IsLastLevel => !IsTutorial && CurrentIndex >= CountLevel - 1;
    public event Action OnLevelReset;

    private void Start()
    {
        if (catalog != null && catalog.Tutorial.IsValid && !SaveGame.TutorialCompleted)
            LoadTutorial();
        else
            LoadLevel(SaveGame.CurrentLevel);
    }

    public void MarkWin()
    {
        LastWinWasTutorial = IsTutorial;
        _completed = true;
    }
    public void CommitProgress()
    {
        if (!_completed || IsTutorial) return;
        int next = Mathf.Min(_currentIndex + 1, CountLevel - 1);
        SaveGame.CurrentLevel = Mathf.Max(SaveGame.CurrentLevel, next);
    }

    public void LoadNextLevel()
    {
        CommitProgress();
        if (LastWinWasTutorial) LoadLevel(SaveGame.CurrentLevel);
        else LoadLevel(_currentIndex + 1);
    }

    public void LoadTutorial()
    {
        if (catalog == null) return;
        LevelData data = catalog.Tutorial.Load();
        if (data == null) return;

        IsTutorial = true;
        SetLevel(data);
    }

    public void ResetLevel()
    {
        _completed = false;
        OnLevelReset?.Invoke();
    }

    public void LoadLevel(int index)
    {
        if (CountLevel == 0) return;
        int clamped = Mathf.Clamp(index, 0, CountLevel - 1);
        LevelData data = catalog[clamped].Load();
        if (data == null) return;

        IsTutorial = false;
        _currentIndex = clamped;
        SetLevel(data);
    }

    private void SetLevel(LevelData data)
    {
        bool switched = _levelData != null && _levelData != data;
        _levelData = data;
        _completed = false;
        OnLevelLoaded?.Invoke(_levelData);

        // Mọi listener đã chuyển sang level mới -> ảnh của level cũ không còn ai dùng.
        if (switched)
            MemoryRelease.UnloadUnusedAssets();
    }

    public void CompleteCurrentLevel()
    {
        if (_completed) return;
        _completed = true;
        CommitProgress();
    }

    public void ReloadLevel()
    {
        if (IsTutorial) LoadTutorial();
        else LoadLevel(_currentIndex);
    }
}
