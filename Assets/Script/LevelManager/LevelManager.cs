using UnityEngine;
using System;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelData[] _levels;
    private int _currentIndex;
    private LevelData _levelData;

    public Sprite LevelSprite => _levelData.RawSprite;
    public Sprite ErrorSprite => _levelData.ErrorSprite;
    public int NumberLevel => _levelData.NumberLevel;
    public int CountDifferent => _levelData.CountDifferent;
    public float TimeLimit => _levelData.TimeLimit;
    public bool HasNextLevel => _currentIndex < _levels.Length - 1;
    public event Action<LevelData> OnLevelLoaded;

    private void Start() => LoadLevel(0);

    public void LoadLevel(int index)
    {
        if (_levels == null || _levels.Length == 0) return;

        _currentIndex = Mathf.Clamp(index, 0, _levels.Length - 1);
        _levelData = _levels[_currentIndex];
        OnLevelLoaded?.Invoke(_levelData);
    }

    public void LoadNextLevel()
    {
        if (HasNextLevel) LoadLevel(_currentIndex + 1);
    }

    public void ReloadLevel() => LoadLevel(_currentIndex);
}