using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private float delay = 3f;
    [SerializeField] private string homeSceneName = "HomeScene";
    [SerializeField] private string gameplaySceneName = "GamePlay";
    [SerializeField] private ProcessingBar processingBar;

    [SerializeField] private bool resetAllOnStart = false;
    [SerializeField] private bool resetTutorialOnStart = false;

    private void Awake()
    {
#if UNITY_EDITOR
        if (resetAllOnStart)
        {
            PendingReward.Clear();
            Wallets.Coin.Reset();
            SaveGame.Reset();
        }
        if (resetTutorialOnStart)
            SaveGame.ResetTutorial();
#endif
    }

    private void OnEnable() => StartCoroutine(LoadNextScene());

    private IEnumerator LoadNextScene()
    {
        if (processingBar != null)
        {
            bool done = false;
            processingBar.Play(() => done = true);
            while (!done)
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(delay);
        }

        string target = SaveGame.TutorialShown ? homeSceneName : gameplaySceneName;
        SceneManager.LoadScene(target);
    }
}
