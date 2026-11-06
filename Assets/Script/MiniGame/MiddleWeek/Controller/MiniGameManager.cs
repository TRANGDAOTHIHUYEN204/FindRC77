using System;
using System.Collections;
using UnityEngine;

public class MiniGameManager : MonoBehaviour
{
    [SerializeField] private FindHiddenLevelSpawner levelSpawner;
    [SerializeField] private HandleClick handleClick;
    [SerializeField] private CollectTargetRegistry registry;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;
    [SerializeField] private AudioClip clickRightSound;
    [SerializeField] private AudioClip clickWrongSound;
    public FindHiddenGameState State { get; } = new FindHiddenGameState();

    [Header("Error Mark")]
    [SerializeField] private ErrorMark errorMarkPrefab;  
    [SerializeField] private Sprite errorSprite;
    [SerializeField] private float markSize = 1f;
    [SerializeField] private float markLifeTime = 0.8f;
    [SerializeField] private Camera cam;

    private readonly PoolManager _pool = new PoolManager();
    public bool IsEnded => State.IsEnded;

    public event Action<int, int> OnProgress;
    public event Action OnWin;
    public event Action OnLose;
    public event Action OnWrongClick;  
    private void Start()
    {
        if (cam == null) cam = Camera.main;

        levelSpawner.OnTypeProgress += HandleTypeProgress;
        handleClick.OnHitItem += HandleHitItem;
        handleClick.OnWrongClick += HandleWrongClick;
        State.OnProgress += HandleStateProgress;
        State.OnPhaseChanged += HandlePhaseChanged;

        levelSpawner.SetLevel(SaveMiniGame.CurrentLevel);
        StartGame(resume: true);
    }

    private void OnDestroy()
    {
        levelSpawner.OnTypeProgress -= HandleTypeProgress;
        handleClick.OnHitItem -= HandleHitItem;
        handleClick.OnWrongClick -= HandleWrongClick;
        State.OnProgress -= HandleStateProgress;
        State.OnPhaseChanged -= HandlePhaseChanged;

        SaveProgress();
        levelSpawner.DespawnAll();
        for (int i = _pool.ActiveGameObjects.Count - 1; i >= 0; i--)
            _pool.ReturnObject(_pool.ActiveGameObjects[i]);
    }

    private void OnApplicationPause(bool paused) { if (paused) SaveProgress(); }
    private void OnApplicationQuit() => SaveProgress();


    public void StartGame() => StartGame(false);

    public void StartGame(bool resume)
    {
        int level = levelSpawner.CurrentLevel;
        FindHiddenSaveData saved = null;
        if (resume) FindHiddenSaveStore.TryLoad(level, out saved);
        else FindHiddenSaveStore.Clear();

        registry.Setup(levelSpawner.CurrentLevelData); 
        levelSpawner.SpawnAll(saved?.foundIds);
        State.Begin(level, levelSpawner.Total, saved?.foundIds);
        Debug.Log($"[Game] START | Level={level} Found={State.FoundCount}/{State.Total}");
    }

    public void Pause()
    {
        if (State.Phase != GamePhase.Playing) return;
        State.SetPhase(GamePhase.Paused);
    }

    public void Resume()
    {
        if (State.Phase != GamePhase.Paused) return;
        State.SetPhase(GamePhase.Playing);
    }
    
    private void HandlePhaseChanged(GamePhase phase)
    {
        handleClick.InputEnabled = (phase == GamePhase.Playing);
    }

    private void HandleStateProgress(int found, int total) => OnProgress?.Invoke(found, total);
    

    private void HandleTypeProgress(FindHiddenData data, int found, int total)
        => registry.SetProgress(data.TargetId, found, total);

    private void HandleHitItem(FindHiddenBehaviour hit)
    {
        if (State.IsEnded) return;

        PlaySfx(clickRightSound);

        hit.OnArrived += HandleItemArrived;
    }

    private void HandleItemArrived(FindHiddenBehaviour item)
    {
        item.OnArrived -= HandleItemArrived;
        if (State.IsEnded) return;

        if (!State.MarkFound(item.Id)) return;
        levelSpawner.MarkFound(item.Data);

        if (State.IsCompleted) StartCoroutine(EndGame(true));
        else SaveProgress();
    }

    private void HandleWrongClick(Vector2 p)
    {
        Debug.Log("[Game] WRONG CLICK");
        PlaySfx(clickWrongSound);
        SpawnErrorMark(p);
        OnWrongClick?.Invoke();
    }

    private void SpawnErrorMark(Vector2 screenPos)
    {
        if (errorMarkPrefab == null || cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(screenPos);
        world.z = 0f;

        GameObject obj = _pool.CreateGameObject(errorMarkPrefab.gameObject, world, Quaternion.identity);
        if (obj == null) return;

        ErrorMark mark = obj.GetComponent<ErrorMark>();
        mark.Init(markSize, errorSprite, markLifeTime, _pool.ReturnObject);
    }

    private IEnumerator EndGame(bool win)
    {
        State.SetPhase(win ? GamePhase.Won : GamePhase.Lost);
        
        PlaySfx(win ? winSound : loseSound);

        if (win)
        {
            FindHiddenSaveStore.Clear();
            bool isLastLevel = State.Level >= levelSpawner.LevelCount - 1;
            if (isLastLevel)
            {
                SaveMiniGame.AllCompleted = true;
                SaveMiniGame.CurrentLevel = levelSpawner.LevelCount - 1;
            }
            else
            {
                SaveMiniGame.CurrentLevel = State.Level + 1;
            }
        }

        yield return new WaitForSeconds(0.5f);
        if (win) OnWin?.Invoke(); else OnLose?.Invoke();
    }

    public void SaveProgress()
    {
        if (!State.IsEnded) FindHiddenSaveStore.Save(State);
    }

    private void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null || SoundManager.Instance == null) return;
        SoundManager.Instance.PlaySfx(clip, volume);
    }
}