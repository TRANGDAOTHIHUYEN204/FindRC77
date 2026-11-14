using System.Collections;
using UnityEngine;

public class ReviewTrigger : MonoBehaviour
{
    [SerializeField] private GameObject safeScreen;
    [SerializeField] private float delay = 0.5f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(delay);

        if (GameReviewManager.Instance == null)
            yield break;

        GameObject screen = safeScreen != null ? safeScreen : gameObject;

        GameReviewManager.Instance.TryRequestAfterTutorial(screen);
    }
}