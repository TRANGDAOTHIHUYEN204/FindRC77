using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; 

public class MenuHome : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText; 
    private void Start()
    {
        levelText.text = "Level " + (SaveGame.CurrentLevel + 1);
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
    
}