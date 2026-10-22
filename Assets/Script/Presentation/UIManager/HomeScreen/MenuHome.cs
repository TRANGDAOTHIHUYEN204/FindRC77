using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; 

public class MenuHome : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject settingPanel;
    private void Awake()
    {
        
        PendingReward.Clear();
        Wallets.Coin.Reset();
        SaveGame.Reset();
        
    }


    private void Start()
    {
        levelText.text = "Level " + (SaveGame.CurrentLevel + 1);
        /*
        PendingReward.Add(50);
        FindFirstObjectByType<CoinPresentation>().TryClaim();
        */
    }

    public void JoinGamePlay(string screenName)
    {
        StartCoroutine(GamePlay(screenName));
    }

    private IEnumerator GamePlay(string screenName)
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(screenName);
    }
    public void OpenSetting()
    {
        settingPanel.SetActive(true);

        PanelAnimation anim =
            settingPanel.GetComponentInChildren<PanelAnimation>(true);

        if (anim != null)
            anim.Show();
    }

    public void CloseSetting()
    {
        PanelAnimation anim = settingPanel.GetComponentInChildren<PanelAnimation>(true);

        if (anim != null)
        {
            anim.Close(() => settingPanel.SetActive(false));
        }
        else
        {
            settingPanel.SetActive(false);
        }
    }
    
}