using UnityEngine;
using System;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelData[] levels;
    private int _currentIndex;
    private LevelData _levelData;

    public Sprite LevelSprite => _levelData.RawSprite;
    public Sprite ErrorSprite => _levelData.ErrorSprite;
    public int NumberLevel => _levelData.NumberLevel;
    public int CountDifferent => _levelData.CountDifferent;
    public float TimeLimit => _levelData.TimeLimit;
    public bool HasNextLevel => _currentIndex < levels.Length - 1;
    public int CurrentIndex => _currentIndex;
    public event Action<LevelData> OnLevelLoaded;
    public string TextWin => _levelData.TextWin;
    
    private void Start() => LoadLevel(SaveGame.CurrentLevel);

    public void LoadLevel(int index)
    {
        if (levels == null || levels.Length == 0) return;
        _currentIndex = Mathf.Clamp(index, 0, levels.Length - 1);
        _levelData = levels[_currentIndex];
        SaveGame.CurrentLevel = _currentIndex;
        OnLevelLoaded?.Invoke(_levelData);
    }

    public void LoadNextLevel()
    {
        if (HasNextLevel) LoadLevel(_currentIndex + 1);
    }

    public void ReloadLevel() => LoadLevel(_currentIndex);
}