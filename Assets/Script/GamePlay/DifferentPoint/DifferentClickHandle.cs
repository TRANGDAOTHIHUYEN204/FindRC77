using UnityEngine;

public class DifferentClickHandler : MonoBehaviour
{
    [SerializeField] private LevelImageView levelImageView;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private MarkPresenter markPresenter;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private StateGame stateGame;
    public bool HandleClick(Vector2 screenPoint)
    {
        if (!stateGame.IsPlaying) return false;
        if (!levelImageView.IsInsideImages(screenPoint))
            return false;

        Vector2 worldPoint = levelImageView.ScreenToWorldPoint(screenPoint, worldCamera);
        DifferentPairManager manager = pairManagerHolder.Manager;
        
        DifferentPairManager.Entry entry = manager.FindRemaining(worldPoint);

        if (entry != null)
        {
            if (!manager.MarkFound(entry, worldPoint))
                return false;

            markPresenter.DrawPair(entry.Pair, entry.Mark);
            Debug.Log($"Click ĐÚNG ({manager.FoundCount}/{manager.TotalCount})");

            return manager.IsComplete;
        }
        if (manager.IsInFoundArea(worldPoint))
            return false;
        Debug.Log("Click SAI");
        return false;
    }
}