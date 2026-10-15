using UnityEngine;
using System.Collections;



public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private float _delay = 2.5f;
    [SerializeField] private GameObject panelLoading;
    private void OnEnable()
    {
        StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(_delay);

        panelLoading.SetActive(false);
    }
}
