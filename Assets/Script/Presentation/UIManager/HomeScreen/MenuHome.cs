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
    [SerializeField] private Image playButtonImage;
    [SerializeField] private Sprite completedSprite;
    [SerializeField] private string nameGame;
    [SerializeField] private TMP_Text playButtonText;

    private void Start()
    {
        numberLevelText.text = "Level " + (SaveGame.CurrentLevel + 1);
        RefreshPlayButton();
    }

    private void RefreshPlayButton()
    {
        if (SaveMiniGame.AllCompleted)
        {
            playButton.interactable = false;
            playButtonImage.sprite = completedSprite;
            playButtonText.text = "Completed";
        }
        else
        {
            playButton.interactable = true;
            playButtonText.text = nameGame;
        }
    }
    public void JoinFindHidden(string sceneName)
    {
        float t0 = Time.realtimeSinceStartup;
        bool done = SaveMiniGame.AllCompleted;
        Debug.Log($"[MenuHome] AllCompleted mất {(Time.realtimeSinceStartup - t0) * 1000f:F0} ms");
        if (done) return;
        StartCoroutine(GamePlay(sceneName));
    }

    private IEnumerator GamePlay(string screenName)
    {
        yield return null;
        yield return null;                       // bỏ qua frame đứng
        float t = 0f;
        while (t < 0.3f)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);  // frame đứng không ăn hết thời gian
            yield return null;
        }
        SceneManager.LoadScene(screenName);
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