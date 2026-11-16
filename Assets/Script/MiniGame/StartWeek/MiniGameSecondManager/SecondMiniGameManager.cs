using UnityEngine;

public class SecondMiniGameManager : MonoBehaviour
{
    [SerializeField] private SpawnItem spawnItem;
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private HandlePlayerCollisionItem collisionHandler;
    [SerializeField] private PlayerMovement player;
    [SerializeField] private CatchItemUI ui;
    [SerializeField] private ScoreMiniGameSecondManager scoreManager;
    private bool _adPending;

    public MiniSecondStateGame State { get; private set; }

    private void Awake()
    {
        State = new MiniSecondStateGame();
        ui.Bind(State);
        ItemMoveMent.Paused = false;
    }

    private void OnEnable()
    {
        if (timeManager != null) timeManager.OnTimeUp += HandleTimeUp;
        if (collisionHandler != null) collisionHandler.Caught += HandleCaught;
        State.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        if (timeManager != null) timeManager.OnTimeUp -= HandleTimeUp;
        if (collisionHandler != null) collisionHandler.Caught -= HandleCaught;
        State.OnPhaseChanged -= HandlePhaseChanged;

        ItemMoveMent.Paused = false;
    }

    public void ClaimDoubleByAd()
    {
        if (_adPending || SaveGame.IsCatchDoubleClaimed()) return;

        var ads = AdsController.Instance;
        if (ads == null)
        {
            Debug.LogWarning("[CatchGame] AdsController null.");
            return;
        }

        _adPending = true;
        ui.SetDoubleButtonInteractable(false); 

        ads.RequestReward(
            onRewarded: () =>
            {
                if (this == null) return;
                _adPending = false;

                if (SaveGame.TryClaimCatchDouble())
                    Debug.Log("[CatchGame] Double reward | +1 hint");

                ui.RefreshDoubleButton();       
            },
            onFailed: () =>
            {
                if (this == null) return;
                _adPending = false;
                ui.SetDoubleButtonInteractable(true);  
                Debug.Log("[CatchGame] Reward ad not ready / cancelled.");
            });
    }
    private void Start()
    {
        State.Begin();
        spawnItem.StartSpawn();
    }

    private void HandleCaught(Item item) => State.AddScore(item.Score);

    private void HandleTimeUp() => State.Finish();

    private void HandlePhaseChanged(MiniSecondPhase phase)
    {
        switch (phase)
        {
            case MiniSecondPhase.Playing:
                timeManager.ResumeTimer();
                spawnItem.ResumeSpawn();
                ItemMoveMent.Paused = false;
                player.CanMove = true;
                break;

            case MiniSecondPhase.Paused:
                timeManager.PauseTimer();
                spawnItem.PauseSpawn();
                ItemMoveMent.Paused = true;
                player.CanMove = false;
                break;

            case MiniSecondPhase.Won:
                StopAll();
                SaveSecondMiniGame.Completed = true;
                SaveGame.TryClaimCatchWin();
                break;

            case MiniSecondPhase.Lost:
                StopAll();
                break;
        }
    }

    private void StopAll()
    {
        spawnItem.StopSpawn();
        ItemMoveMent.Paused = true;
        player.CanMove = false;
    }

    public void PauseGame()  => State.Pause();
    public void ResumeGame() => State.Resume();

    public void RestartGame()
    {
        _adPending = false;

        spawnItem.StopSpawn();
        spawnItem.ClearAll();
        timeManager.ResetTimer();
        player.ResetMovement();
        scoreManager.ResetScore();   
        ui.ResetUI();
        ItemMoveMent.Paused = false;
        State.Begin();
        spawnItem.StartSpawn();
    }
    
}