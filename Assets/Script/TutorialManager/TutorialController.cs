using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private HintPresenter hintPresenter;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private StateGame stateGame;
    [SerializeField] private string homeSceneName = "HomeScene";

    [SerializeField, Min(0f)] private float delayBetweenHints = 0.6f;
    [SerializeField] private GameObject[] hideDuringTutorial;
    [SerializeField, Min(0f)] private float delayBeforeHome = 1f;
    [SerializeField, Min(1)] private int maxHints = 3;

    public bool IsRunning { get; private set; }

    private bool _quitting;

    private void OnEnable()
    {
        if (levelManager != null)
            levelManager.OnLevelLoaded += OnLevelLoaded;
    }

    private void Start() => StartCoroutine(RunTutorial());
    
    private void OnLevelLoaded(LevelData data)
    {
        if (!levelManager.IsTutorial)
            SetUIVisible(true);
    }

    private IEnumerator RunTutorial()
    {
        yield return null;
        if (!levelManager.IsTutorial) yield break;
        SaveGame.TutorialCompleted = true;
        PlayerPrefs.Save();

        yield return new WaitUntil(() => pairManagerHolder.Manager != null);

        IsRunning = true;
        SetUIVisible(false);

        int hintsShown = 0;

        while (hintsShown < maxHints)
        {
            var manager = pairManagerHolder.Manager;
            if (manager != null && manager.IsComplete) break;

            hintPresenter.ShowTutorialHint();
            hintsShown++;

            while (hintPresenter.HintedPair != null)
                yield return null;

            manager = pairManagerHolder.Manager;
            if (manager != null && manager.IsComplete) break;

            yield return new WaitForSecondsRealtime(delayBetweenHints);
        }

        yield return new WaitUntil(() =>
        {
            var m = pairManagerHolder.Manager;
            return m != null && m.IsComplete;
        });

        EndTutorial(completed: true);
    }

    private void EndTutorial(bool completed)
    {
        if (!IsRunning) return;
        IsRunning = false;

        if (hintPresenter != null) hintPresenter.ClearHint();

        if (completed)
        {
            SaveGame.TutorialCompleted = true; 
            PlayerPrefs.Save();
        }
        else
        {
            SetUIVisible(true);
        }
    }

    private void SetUIVisible(bool visible)
    {
        if (hideDuringTutorial == null) return;

        foreach (var go in hideDuringTutorial)
            if (go != null) go.SetActive(visible);
    }

    private void OnDisable()
    {
        if (levelManager != null)
            levelManager.OnLevelLoaded -= OnLevelLoaded;

        if (!IsRunning) return;
        IsRunning = false;
        StopAllCoroutines();
        if (_quitting) return;

        if (hintPresenter != null) hintPresenter.ClearHint();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) PlayerPrefs.Save();
    }

    private void OnApplicationQuit() => _quitting = true;
}