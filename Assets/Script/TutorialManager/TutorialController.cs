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
    public bool IsRunning { get; private set; }

    private void Start() => StartCoroutine(RunTutorial());

    private IEnumerator RunTutorial()
    {
        yield return null;
        if (!levelManager.IsTutorial) yield break;

        yield return new WaitUntil(() =>
            pairManagerHolder.Manager != null && stateGame.IsPlaying);

        IsRunning = true;
        SetUIVisible(false);

        while (true)
        {
            var manager = pairManagerHolder.Manager;
            if (manager != null && manager.IsComplete) break;

            hintPresenter.ShowTutorialHint();

            while (hintPresenter.HintedPair != null)
                yield return null;

            manager = pairManagerHolder.Manager;
            if (manager != null && manager.IsComplete) break;

            yield return new WaitForSecondsRealtime(delayBetweenHints);
        }

        EndTutorial(completed: true);
        yield return new WaitForSecondsRealtime(delayBeforeHome);
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }

    private void EndTutorial(bool completed)
    {
        IsRunning = false;
        hintPresenter.ClearHint();
        if (!completed) SetUIVisible(true); 
        if (completed) SaveGame.TutorialCompleted = true;
    }

    private void SetUIVisible(bool visible)
    {
        foreach (var go in hideDuringTutorial)
            if (go != null) go.SetActive(visible);
    }

    private void OnDisable()
    {
        if (IsRunning) EndTutorial(completed: false);
    }
}