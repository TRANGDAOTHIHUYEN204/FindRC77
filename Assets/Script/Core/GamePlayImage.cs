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
    [SerializeField] private SpawnChallengeNode spawnChallegeNode;
    [SerializeField] private HeartManager heartManager;
    private bool _isPlaying;
    [SerializeField] private HintPresenter hintPresenter;
    [SerializeField] private AudioClip winSound;
    
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
        yield return new WaitForSeconds(2f);
        Debug.Log("Đã tìm đủ!");
        SoundManager.Instance.PlaySfx(winSound);
        stateGame.WinGame(levelManager.TextWin);
        
    }
}