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
    private void Start()
    {
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
        collectedCount = 0;
        handleClick.InputEnabled = true;
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
            StartCoroutine(EndGame(true));
    }

    private void HandleTimeUp()
    {
        if (IsEnded) return;
        StartCoroutine(EndGame(false));
    }
    private void HandleWrongClick(Vector2 screenPoint)
    {
        Debug.Log($"[Game] WRONG CLICK");
    }
    
    private IEnumerator EndGame(bool win)
    {
        IsEnded = true;
        handleClick.InputEnabled = false;
        yield return new WaitForSeconds(0.5f);
        if (win) Debug.Log("[Game] WIN");
        else Debug.Log("[Game] LOSE");

        if (win) OnWin?.Invoke();
        else OnLose?.Invoke();
    }
}