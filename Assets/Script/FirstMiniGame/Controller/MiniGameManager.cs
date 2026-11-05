using System;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameManager : MonoBehaviour
{
    [SerializeField] private FindHiddenLevelSpawner levelSpawner;
    [SerializeField] private HandleClick handleClick;
    private Sprite backgroundImage;
    [SerializeField] private int maxLives = 5;

    public int Lives { get; private set; }
    public bool IsEnded { get; private set; }

    private int collectedCount;

    public event Action<int, int> OnProgress;
    public event Action<int> OnLivesChanged;
    public event Action OnWin;
    public event Action OnLose;

    // [SerializeField] private TimeManager timeManager;
    private void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        handleClick.OnHitItem += HandleHitItem;
        handleClick.OnWrongClick += HandleWrongClick;
        StartGame();
    }

    private void OnDestroy()
    {
        handleClick.OnHitItem -= HandleHitItem;
        handleClick.OnWrongClick -= HandleWrongClick;
        levelSpawner.DespawnAll();
    }

    public void StartGame()
    {
        IsEnded = false;
        Lives = maxLives;
        collectedCount = 0;
        handleClick.InputEnabled = true;

        levelSpawner.SpawnAll();
        backgroundImage = levelSpawner.BackgroundImage; 
        Debug.Log($"[Game] START | Lives={Lives} | Total={levelSpawner.Total}");

        OnLivesChanged?.Invoke(Lives);
        OnProgress?.Invoke(0, levelSpawner.Total);
    }
    
    private void HandleHitItem(FindHiddenBehaviour hit)
    {
        if (IsEnded) return;
        hit.OnArrived += HandleItemArrived;
    }

    private void HandleItemArrived(FindHiddenBehaviour item)
    {
        item.OnArrived -= HandleItemArrived;
        if (IsEnded) return;

        collectedCount++;
        OnProgress?.Invoke(collectedCount, levelSpawner.Total);

        if (collectedCount >= levelSpawner.Total)
            EndGame(win: true);
    }

    private void HandleWrongClick(Vector2 screenPoint)
    {
        if (IsEnded) return;

        Lives--;
        Debug.Log($"[Game] WRONG CLICK | Lives={Lives}");

        OnLivesChanged?.Invoke(Lives);

        if (Lives <= 0)
            EndGame(win: false);
    }

    private void EndGame(bool win)
    {
        IsEnded = true;
        handleClick.InputEnabled = false;

        if (win) Debug.Log("[Game] WIN");
        else Debug.Log("[Game] LOSE");

        if (win) OnWin?.Invoke();
        else OnLose?.Invoke();
    }
}