using UnityEngine;
using System.Collections;
using Char.Haptic;
using MoreMountains.NiceVibrations;

public class DifferentClickHandler : MonoBehaviour
{
    [SerializeField] private LevelImageView levelImageView;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private MarkPresenter markPresenter;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private StateGame stateGame;
    [SerializeField] private SpawnChallengeNode spawnChallengeNode;
    [SerializeField] private Sprite spriteChange;
    [SerializeField] private Camera uiCamera;
    [SerializeField] private HeartManager heartManager;
    [SerializeField] private HintPresenter hintPresenter;
    [SerializeField] private ErrorSpawnManager errorSpawnManager;
    [SerializeField] private AudioClip clickErrorSound;
    [SerializeField] private AudioClip clickSuccessSound;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private RectTransform trailStartPoint;
    [SerializeField] private float markDelay = 1f;
    [SerializeField] private float hideOutlineDelay = 0.5f;

    private bool _isLosing = false;
    private bool _isResetting = false;

    public bool HandleClick(Vector2 screenPoint)
    {
        if (_isResetting) return false;
        if (_isLosing) return false;
        if (!stateGame.IsPlaying) return false;
        if (!levelImageView.IsInsideImages(screenPoint)) return false;

        DifferentPairManager manager = pairManagerHolder.Manager;
        if (manager == null) return false;

        var entry = manager.FindRemaining(screenPoint, uiCamera);

        if (entry != null)
        {
            if (!manager.MarkFound(entry)) return false;

            if (hintPresenter.HintedPair == entry.Pair)
                hintPresenter.ClearHint();

            // chụp session ngay lúc click
            StartCoroutine(OutlineThenMark(entry.Pair, entry.Mark, markPresenter.Session));

            Haptics.Light();
            SoundManager.Instance.PlaySfx(clickSuccessSound);

            if (trailStartPoint != null &&
                trailStartPoint.parent is RectTransform canvasRect &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
            {
                trailStartPoint.anchoredPosition = localPoint;
                spawnChallengeNode.ChangeNext(spriteChange, trailStartPoint);
            }
            else
            {
                spawnChallengeNode.ChangeNext(spriteChange);
            }

            return manager.IsComplete;
        }

        if (manager.IsInFoundArea(screenPoint, uiCamera))
            return false;

        if (levelManager.IsTutorial)
            return false;

        Debug.Log("Click sai");

        errorSpawnManager.SpawnAtScreen(screenPoint);
        SoundManager.Instance.PlaySfx(clickErrorSound);

        bool outOfLives = heartManager.SubstractLives();

        if (outOfLives)
        {
            _isLosing = true;
            hintPresenter.ClearHint();

            Haptics.Play(HapticTypes.Failure);
            StartCoroutine(LoseGame());
        }
        else
        {
            Haptics.Heavy();
        }

        return false;
    }

    private IEnumerator OutlineThenMark(DifferentPair pair, MarkData mark, int session)
    {
        pair.ShowOutline();

        yield return new WaitForSecondsRealtime(markDelay);

        if (session != markPresenter.Session || pair == null) yield break;

        markPresenter.DrawPair(pair, mark, session);

        yield return new WaitForSecondsRealtime(hideOutlineDelay);

        if (session != markPresenter.Session || pair == null) yield break;

        pair.HideOutline();
    }

    private IEnumerator LoseGame()
    {
        yield return new WaitForSeconds(1f);
        stateGame.LoseGame();
    }
    
    public void OnResetBegin()
    {
        _isResetting = true;
        StopAllCoroutines();
    }
    public void OnResetEnd()
    {
        _isResetting = false;
        _isLosing = false;
    }

    public void ResetLoseState()
    {
        _isLosing = false;
    }
}