using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuHome : MonoBehaviour
{
    public void JoinGamePlay(string screenName)
    {
        StartCoroutine(GamePlay(screenName));
    }

    private IEnumerator GamePlay(string screenName)
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene(screenName);
    }
}
