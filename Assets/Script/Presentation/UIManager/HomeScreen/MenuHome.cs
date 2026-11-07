using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuHome : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private TextMeshProUGUI numberLevelText;

    [SerializeField] private Button playButton;
    [SerializeField] private Image playButtonImage;      
    [SerializeField] private Sprite completedSprite;
    [SerializeField] private string nameGame;
   
    [SerializeField] private TMP_Text playButtonText;

    private void Start()
    {
        string label = "Level " + (SaveGame.CurrentLevel + 1);
        levelText.text = label;
        numberLevelText.text = label;

        RefreshPlayButton();
    }

    private void RefreshPlayButton()
    {
        if (SaveMiniGame.AllCompleted)
        {
            playButton.interactable = false;
            playButtonImage.sprite = completedSprite;
            playButtonImage.color = Color.white;
            playButtonText.text = "Completed";
        }
        else
        {
            playButtonText.text = nameGame;
            playButton.interactable = true;
        }
    }

    public void JoinFindHidden(string sceneName)
    {
        if (SaveMiniGame.AllCompleted) return;
        SceneManager.LoadScene(sceneName);
        
    }

    public void JoinGamePlay(string screenName)
    {
        StartCoroutine(GamePlay(screenName));
    }

    private IEnumerator GamePlay(string screenName)
    {
        yield return new WaitForSeconds(0.1f);
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