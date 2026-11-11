using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private string homeSceneName = "HomeScene";
    [SerializeField] private string gameplaySceneName = "GamePlay";
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
            SaveMiniGame.Reset();
            FindHiddenSaveStore.Clear();
        }
        if (resetTutorialOnStart)
            SaveGame.ResetTutorial();
#endif
    }

    public void LoadNextSceneA()
    {
        string target = SaveGame.TutorialCompleted ? homeSceneName : gameplaySceneName;
        SceneManager.LoadScene(target);
    }
}
