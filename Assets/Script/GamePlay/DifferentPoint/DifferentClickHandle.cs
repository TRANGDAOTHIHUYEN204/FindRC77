using UnityEngine;
using System.Collections;

public class DifferentClickHandler : MonoBehaviour
{
    [SerializeField] private LevelImageView levelImageView;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private MarkPresenter markPresenter;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private StateGame stateGame;
    [SerializeField] private SpawnChallengeNode spawnChallegeNode;
    [SerializeField] private Sprite spriteChange;
    [SerializeField] private Camera uiCamera;
    [SerializeField] private HeartManager heartManager;
    [SerializeField] private HintPresenter hintPresenter;
    [SerializeField] private ErrorSpawnManager  errorSpawnManager;
    [SerializeField] private AudioClip clickErrorSound;
    [SerializeField] private AudioClip loseGameSound;
    [SerializeField] private AudioClip clickSuccessSound;
    
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
            if (hintPresenter.HintedPair == entry.Pair) hintPresenter.ClearHint();
            markPresenter.DrawPair(entry.Pair, entry.Mark);
            SoundManager.Instance.PlaySfx(clickSuccessSound);
            spawnChallegeNode.ChangeNext(spriteChange);
            return manager.IsComplete;
        }

        if (manager.IsInFoundArea(screenPoint, uiCamera)) return false;
        Debug.Log("Click sai");
        errorSpawnManager.SpawnAtScreen(screenPoint);
        SoundManager.Instance.PlaySfx(clickErrorSound);
        bool outOfLives = heartManager.SubstractLives();
        if (outOfLives)
        {
            hintPresenter.ClearHint();
            StartCoroutine(LoseGame());
        }
        return false;
    }

    private IEnumerator LoseGame()
    {
        yield return new WaitForSeconds(2f);
        stateGame.LoseGame();
        SoundManager.Instance.PlaySfx(loseGameSound);
    }
}