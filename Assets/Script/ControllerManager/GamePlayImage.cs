using System;
using UnityEngine;

public class GamePlayImage : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private LevelImageView levelImageView;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private DifferentClickHandler clickHandler;
    [SerializeField] private FactoryManager factoryManager;

    public event Action<DifferentPair, MarkData> OnPairFound;
    public event Action OnAllFound;

    private void OnEnable()
    {
        levelManager.OnLevelLoaded += Build;

        clickHandler.OnPairFound += HandlePairFound;
        clickHandler.OnAllFound += HandleAllFound;
    }

    private void OnDisable()
    {
        levelManager.OnLevelLoaded -= Build;

        clickHandler.OnPairFound -= HandlePairFound;
        clickHandler.OnAllFound -= HandleAllFound;

        clickHandler.StopListening();
    }

    private void Build(LevelData level)
    {
        clickHandler.StopListening();
        factoryManager.ReleaseAll();

        int currentIndex = levelManager.CurrentIndex;

        levelImageView.ShowLevel(level, currentIndex);

        levelImageView.GetCurrentRoots(
            currentIndex,
            out GameObject rootA,
            out GameObject rootB
        );

        pairManagerHolder.Manager.Build(
            rootA,
            rootB,
            level.MarkData
        );

        clickHandler.StartListening();
    }

    private void HandlePairFound(DifferentPair pair, MarkData mark)
    {
        OnPairFound?.Invoke(pair, mark);
    }

    private void HandleAllFound()
    {
        OnAllFound?.Invoke();
    }
}
