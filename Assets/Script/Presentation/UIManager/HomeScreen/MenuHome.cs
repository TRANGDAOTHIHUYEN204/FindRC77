using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuHome : MonoBehaviour
{
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private TextMeshProUGUI numberLevelText;

    [SerializeField] private Button playButton;
    [SerializeField] private TMP_Text playButtonText;
    [SerializeField] private GameObject cafeTimePanel;
    [SerializeField] private float cafeTimeDuration = 1.5f;

    private bool _isStarting;
    [SerializeField] private float adTimeout = 20f;

    private void Start()
    {
        numberLevelText.text = "Level " + (SaveGame.CurrentLevel + 1);
        RefreshPlayButton();

        if (cafeTimePanel) cafeTimePanel.SetActive(false);
        if (AdsController.Instance != null)
            AdsController.Instance.PreloadSecond();
    }

    private void RefreshPlayButton()
    {
        if (SaveMiniGame.AllCompleted)
        {
            playButton.interactable = false;
            playButtonText.text = "Completed";
        }
        else
        {
            playButton.interactable = true;
        }
    }

    private IEnumerator GamePlay(string sceneName)
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        
        var ads = AdsController.Instance;
        if (SaveGame.TutorialCompleted && ads != null && ads.ConsumeFirstStart())
        {
            bool done = false;
            ads.RequestSecond(() => done = true);

            float t = 0f;
            while (!done && t < adTimeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        op.allowSceneActivation = true;
    }

    public void JoinFindHidden(string sceneName)
    {
        if (SaveMiniGame.AllCompleted) return;
        StartGame(sceneName, false);
    }

    public void JoinGamePlay(string sceneName)
    {
        StartGame(sceneName, true);
    }

    private void StartGame(string sceneName, bool isGamePlay)
    {
        if (_isStarting) return;

        _isStarting = true;

        if (isGamePlay)
            StartCoroutine(GamePlay(sceneName));
        else
            StartCoroutine(EventGame(sceneName));
    }

    private IEnumerator EventGame(string sceneName)
    {
        yield return new WaitForSeconds(0.1f);
        SceneManager.LoadScene(sceneName);
    }

    public void OpenSetting()
    {
        settingPanel.SetActive(true);

        PanelAnimation anim = settingPanel.GetComponentInChildren<PanelAnimation>(true);
        if (anim != null)
            anim.Show();
    }

    public void CloseSetting()
    {
        PanelAnimation anim = settingPanel.GetComponentInChildren<PanelAnimation>(true);

        if (anim != null)
            anim.Close(() => settingPanel.SetActive(false));
        else
            settingPanel.SetActive(false);
    }
}