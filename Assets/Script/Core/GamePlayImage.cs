using System.Collections;
using UnityEngine;

public class GamePlayImage : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private LevelImageView levelImageView;
    [SerializeField] private DifferentClickHandler clickHandler;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private FactoryManager factoryManager;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private StateGame stateGame;
    [SerializeField] private SpawnChallegeNode spawnChallegeNode;
    [SerializeField] private HeartManager heartManager;
    private bool _isPlaying;
    [SerializeField] private HintPresenter hintPresenter;
    
    private void OnEnable()  => levelManager.OnLevelLoaded += Build;
    private void OnDisable() => levelManager.OnLevelLoaded -= Build;

    private void Build(LevelData level)
    {
        _isPlaying = false;
        hintPresenter.ClearHint();      
        factoryManager.ReleaseAll();
        heartManager.ResetLives(); 
        levelImageView.ShowLevel(level);
        pairManagerHolder.Build(level, levelImageView.RootA, levelImageView.RootB);
        Debug.Log($"RootA = {levelImageView.RootA.name}, RootB = {levelImageView.RootB.name}");
        stateGame.Init(level.TimeLimit);
        spawnChallegeNode.Init(level.CountDifferent);
        _isPlaying = true;
    }

    private void Update()
    {
        if (!_isPlaying) return;
        if (!inputManager.ClickedThisFrame) return;

        if (clickHandler.HandleClick(inputManager.ClickScreenPosition))
            StartCoroutine(Win());
    }

    private IEnumerator Win()
    {
        _isPlaying = false;
        yield return new WaitForSeconds(1f);
        Debug.Log("Đã tìm đủ!");
        stateGame.WinGame();
    }
}