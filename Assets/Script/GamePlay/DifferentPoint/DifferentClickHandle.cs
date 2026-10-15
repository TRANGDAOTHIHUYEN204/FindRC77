using UnityEngine;

public class DifferentClickHandler : MonoBehaviour
{
    [SerializeField] private LevelImageView levelImageView;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private MarkPresenter markPresenter;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private StateGame stateGame;
    [SerializeField] private SpawnChallegeNode spawnChallegeNode;
    [SerializeField] private Sprite spriteChange;
    [SerializeField] private Camera uiCamera;
    [SerializeField] private HeartManager heartManager;

    public bool HandleClick(Vector2 screenPoint)
    {
        if (!stateGame.IsPlaying) return false;
        if (!levelImageView.IsInsideImages(screenPoint)) return false;

        DifferentPairManager manager = pairManagerHolder.Manager;
        if (manager == null) return false;

        var entry = manager.FindRemaining(screenPoint, uiCamera);
        if (entry != null)
        {
            if (!manager.MarkFound(entry)) return false;

            markPresenter.DrawPair(entry.Pair, entry.Mark);
            spawnChallegeNode.ChangeNext(spriteChange);
            return manager.IsComplete;
        }

        if (manager.IsInFoundArea(screenPoint, uiCamera)) return false;
        Debug.Log("Click sai");
        bool outOfLives = heartManager.SubstractLives();
        if (outOfLives)
            stateGame.LoseGame();

        return false;
    }
}