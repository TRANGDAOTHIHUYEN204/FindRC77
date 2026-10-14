using UnityEngine;

public class GamePlayImage : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private LevelImageView levelImageView;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private DifferentClickHandler clickHandler;
    [SerializeField] private FactoryManager factoryManager;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private StateGame stateGame;
    
    private bool _isPlaying;

    private void OnEnable()
    {
        levelManager.OnLevelLoaded += Build;
    }

    private void OnDisable()
    {
        levelManager.OnLevelLoaded -= Build;
    }

    private void Build(LevelData level)
    {
        factoryManager.ReleaseAll();

        int currentIndex = levelManager.CurrentIndex;

        levelImageView.ShowLevel(level, currentIndex);
        levelImageView.GetCurrentRoots(currentIndex, out GameObject rootA, out GameObject rootB);
        pairManagerHolder.Manager.Build(rootA, rootB, level.MarkData);
        stateGame.Init(level.TimeLimit);
        _isPlaying = true;
    }

    private void Update()
    {
        if (!_isPlaying) return;
        if (!inputManager.ClickedThisFrame) return;

        bool allFound = clickHandler.HandleClick(inputManager.ClickScreenPosition);

        if (allFound)
            Win();
    }

    private void Win()
    {
        _isPlaying = false;
        Debug.Log("Đã tìm đủ!");
        stateGame.WinGame();
    }
}