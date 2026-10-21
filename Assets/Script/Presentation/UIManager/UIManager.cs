using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private LevelManager leveManager;
    [SerializeField] private HintPresenter hintPresenter;
    public void NextLevel()
    {

        StartCoroutine(Continue());
    }

    public void Replay()
    {
        Debug.Log("Replay");
        StartCoroutine(ReplayGame());
    }

    private IEnumerator ReplayGame()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        Debug.Log("Load Scene");
        leveManager.ReloadLevel();
    }

    private IEnumerator Continue()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        Debug.Log("Load Scene");
        leveManager.LoadNextLevel();
    }
    public void Hint()
    {
        hintPresenter.ShowHint();
    }
}
