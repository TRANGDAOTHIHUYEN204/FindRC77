using UnityEngine;
using System.Collections;



public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private float delay = 1f;
    [SerializeField] private GameObject panelLoading;
    private void OnEnable()
    {
        StartCoroutine(HideAfterDelay());
    }
    private void Awake()
    {
        SaveGame.Reset();
    }
    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(delay);

        panelLoading.SetActive(false);
    }
}
