using UnityEngine;

public class ThirdMiniGameManager : MonoBehaviour
{
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private CompletionCounter counter;
    [SerializeField] private IconSpawner spawner;
    [SerializeField] private CleanUpUI ui;
 
    public MiniThirdStateGame State { get; private set; }

    private bool _adPending;
    private bool _blocked;   

    private void Awake()
    {
        State = new MiniThirdStateGame();
        ui.Bind(State);
        DragHandler2D.Locked = false;
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        if (timeManager != null) timeManager.OnTimeUp += HandleTimeUp;
        if (counter != null) counter.OnAllCompleted += HandleAllCompleted;
        State.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        if (timeManager != null) timeManager.OnTimeUp -= HandleTimeUp;
        if (counter != null) counter.OnAllCompleted -= HandleAllCompleted;
        State.OnPhaseChanged -= HandlePhaseChanged;

        DragHandler2D.Locked = false;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if ( SaveThirdMiniGame.HasPlayed)
        {
            _blocked = true;
            DragHandler2D.Locked = true;
            timeManager.PauseTimer();
            ui.ShowSavedResult(SaveThirdMiniGame.Result);
            return;
        }

        spawner.Spawn();
        State.Begin();
    }

    private void HandleTimeUp() => State.Finish(counter.Remaining == 0);
    private void HandleAllCompleted() => State.Finish(true);

    private void HandlePhaseChanged(MiniThirdPhase phase)
    {
        switch (phase)
        {
            case MiniThirdPhase.Playing:
                Time.timeScale = 1f;
                DragHandler2D.Locked = false;
                timeManager.ResumeTimer();
                break;

            case MiniThirdPhase.Paused:
                Time.timeScale = 0f;
                DragHandler2D.Locked = true;
                timeManager.PauseTimer();
                break;

            case MiniThirdPhase.Won:
                Time.timeScale = 1f;
                DragHandler2D.Locked = true;
                timeManager.PauseTimer();           
                SaveThirdMiniGame.Result = ThirdResult.Won;
                SaveGame.TryClaimCleanWin();    
                break;

            case MiniThirdPhase.Lost:
                Time.timeScale = 1f;
                DragHandler2D.Locked = true;
                timeManager.PauseTimer();
                SaveThirdMiniGame.Result = ThirdResult.Lost;
                break;
        }
    }

    public void PauseGame()
    {
        if (_blocked) return;
        State.Pause();
    }

    public void ResumeGame()
    {
        if (_blocked) return;
        State.Resume();
    }

    public void RestartGame()
    {
        if (_blocked) return;

        _adPending = false;
        Time.timeScale = 1f;
        timeManager.ResetTimer();
        ui.ResetUI();
        spawner.Restart();  
        State.Begin();         
    }

    public void ClaimDoubleByAd()
    {
        if (_adPending || SaveGame.IsCleanDoubleClaimed()) return;
        if (State.Phase != MiniThirdPhase.Won && SaveThirdMiniGame.Result != ThirdResult.Won) return;

        var ads = AdsController.Instance;
        if (ads == null)
        {
            return;
        }

        _adPending = true;
        ui.SetDoubleButtonInteractable(false);

        ads.RequestReward(
            onRewarded: () =>
            {
                if (this == null) return;
                _adPending = false;
                if (SaveGame.TryClaimCleanDouble())
                    Debug.Log("[CleanUp] Double reward | +1 hint");
                ui.RefreshDoubleButton();
            },
            onFailed: () =>
            {
                if (this == null) return;
                _adPending = false;
                ui.SetDoubleButtonInteractable(true);
            });
    }
}