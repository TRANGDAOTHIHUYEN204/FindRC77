using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField] private LevelManager leveManager;
    [SerializeField] private HintPresenter hintPresenter;
    [SerializeField] private string homeSceneName = "HomeScene";
    
   
    public void NextLevel()
    {
        if (leveManager.LastWinWasTutorial || leveManager.IsLastLevel)
        {
            ReturnHomeScene(homeSceneName);
            return;
        }

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

    
    public void ReturnHomeScene(string sceneName)
    {
        StartCoroutine(ReturnHome(sceneName));
    }
    private IEnumerator ReturnHome(string sceneName)
    {
        yield return new WaitForSecondsRealtime(0.2f);
        SceneManager.LoadSceneAsync(sceneName);
    }
}
