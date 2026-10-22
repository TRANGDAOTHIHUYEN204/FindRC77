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
    private bool _completed; 
    
    private void Start() => LoadLevel(SaveGame.CurrentLevel);

    public void LoadLevel(int index)
    {
        if (levels == null || levels.Length == 0) return;
        _currentIndex = Mathf.Clamp(index, 0, levels.Length - 1);
        _levelData = levels[_currentIndex];
        _completed = false;

        OnLevelLoaded?.Invoke(_levelData);   
    }

    public void CompleteCurrentLevel()
    {
        if (_completed) return;
        _completed = true;
        SaveGame.CurrentLevel = Mathf.Min(_currentIndex + 1, levels.Length - 1);
    }

    public void LoadNextLevel() => LoadLevel(SaveGame.CurrentLevel);
    public void ReloadLevel()   => LoadLevel(_currentIndex);  
}