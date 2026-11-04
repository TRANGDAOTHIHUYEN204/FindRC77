using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MiniGameManager : MonoBehaviour
{
    [SerializeField] private FindHiddenLevelSpawner levelSpawner;
    [SerializeField] private HandleClick handleClick;
    private Sprite backgroundImage;
    
    public bool IsEnded { get; private set; }

    private int collectedCount;

    public event Action<int, int> OnProgress;
    public event Action<int> OnLivesChanged;
    public event Action OnWin;
    public event Action OnLose;
    [SerializeField] private TimeManager timeManager;
    private void Awake()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }
    private void Start()
    {
        handleClick.OnHitItem += HandleHitItem;
        handleClick.OnWrongClick += HandleWrongClick;
        timeManager.OnTimeUp += HandleTimeUp;  
        StartGame();
    }

    private void OnDestroy()
    {
        handleClick.OnHitItem -= HandleHitItem;
        handleClick.OnWrongClick -= HandleWrongClick;
        timeManager.OnTimeUp -= HandleTimeUp;  
        levelSpawner.DespawnAll();
    }

    public void StartGame()
    {
        IsEnded = false;
        collectedCount = 0;
        handleClick.InputEnabled = true;
        timeManager.ResetTimer();                      
        timeManager.StartTimer();
        levelSpawner.SpawnAll();
        backgroundImage = levelSpawner.BackgroundImage; 
        Debug.Log($"[Game] START | Total={levelSpawner.Total}");
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
        Debug.Log($"[Game] WRONG CLICK");
    }

    private void HandleTimeUp()
    {
        if (IsEnded) return;
        EndGame(false);
    }

    private IEnumerator EndGame(bool win)
    {
        IsEnded = true;
        handleClick.InputEnabled = false;
        timeManager.PauseTimer();                   
        yield return new WaitForSeconds(2f);
        if (win) Debug.Log("[Game] WIN");
        else Debug.Log("[Game] LOSE");

        if (win) OnWin?.Invoke();
        else OnLose?.Invoke();
    }
}