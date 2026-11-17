using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuHome : MonoBehaviour
{
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private TextMeshProUGUI numberLevelText;
    [SerializeField] private Button firstWeekButton;
    [SerializeField] private TMP_Text firstWeekText;
    [SerializeField] private Button midWeekButton;
    [SerializeField] private TMP_Text midWeekText;
    [SerializeField] private Button endWeekButton;
    [SerializeField] private TMP_Text endWeekText;

    [SerializeField] private float adTimeout = 20f;

    private bool _isStarting;

    private void Start()
    {
        numberLevelText.text = "Level " + (SaveGame.CurrentLevel + 1);
        RefreshEventButtons();
        if (AdsController.Instance != null)
            AdsController.Instance.PreloadSecond();
    }

    private void RefreshEventButtons()
    {
        Debug.Log($"[Home] SecondCompleted={SaveSecondMiniGame.Completed}, MidCompleted={SaveMiniGame.AllCompleted}");
        SetCompleted(firstWeekButton, firstWeekText, SaveSecondMiniGame.Completed);
        SetCompleted(midWeekButton, midWeekText, SaveMiniGame.AllCompleted);
    }
    
    private static void SetCompleted(Button btn, TMP_Text label, bool completed)
    {
        if (btn == null) return;

        btn.interactable = !completed;
        if (completed && label != null)
            label.text = "Completed";
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

    // Mini game
    public void JoinSecondMiniGame(string sceneName)
    {
        if (SaveSecondMiniGame.Completed) return;
        StartGame(sceneName, false);
    }

    // Giữa tuần
    public void JoinFindHidden(string sceneName)
    {
        if (SaveMiniGame.AllCompleted) return;
        StartGame(sceneName, false);
    }

    // Cuối tuần 
    public void Join(string sceneName)
    {
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