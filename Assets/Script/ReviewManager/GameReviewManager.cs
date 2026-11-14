using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_RACCOON_ADMOB
using RCKit.Ads.Main;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.Review;
#endif

public sealed class GameReviewManager : MonoBehaviour
{
    public static GameReviewManager Instance { get; private set; }

    [Header("iOS")]
    [Tooltip("Numeric App Store ID, for example: 1234567890")]
    [SerializeField] private string iosAppId = "";
    private const string PendingKey = "review.pending";

    private bool attemptedThisLaunch;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void MarkTutorialCompleted()
    {
        string scene = SceneManager.GetActiveScene().name;
        int current = PlayerPrefs.GetInt(PendingKey, -1);

        if (current != -1)
        {
            Debug.Log($"[Review] MarkTutorialCompleted skipped | scene={scene} | pending={current}");
            return;
        }

        PlayerPrefs.SetInt(PendingKey, 1);
        PlayerPrefs.Save();
        Debug.Log($"[Review] MarkTutorialCompleted -> pending=1 | scene={scene}");
    }
    
    public void TryRequestAfterTutorial(GameObject safeScreen)
    {
        string scene = SceneManager.GetActiveScene().name;
        int pending = PlayerPrefs.GetInt(PendingKey, -1);

        Debug.Log($"[Review] TryRequestAfterTutorial | scene={scene} | pending={pending} | attempted={attemptedThisLaunch} | safeScreenActive={(safeScreen != null && safeScreen.activeInHierarchy)} | focused={Application.isFocused}");

        if (attemptedThisLaunch)
        {
            Debug.Log("[Review] BLOCKED: already attempted this launch");
            return;
        }

        if (pending != 1)
        {
            Debug.Log("[Review] BLOCKED: pending != 1 (chưa thắng tutorial hoặc đã review)");
            return;
        }

        if (!IsSafeScreenVisible(safeScreen))
        {
            Debug.Log("[Review] BLOCKED: safe screen not visible");
            return;
        }

        attemptedThisLaunch = true;

#if !TEST_REVIEW
        PlayerPrefs.SetInt(PendingKey, 0);
        PlayerPrefs.Save();
#endif

        Debug.Log($"[Review] PASSED -> launching review | scene={scene}");

#if UNITY_EDITOR
        Debug.Log("[Review] Editor: test the native dialog on a device.");

#elif UNITY_ANDROID
        StartCoroutine(RequestAndroidReview(safeScreen));

#elif UNITY_IOS
        SuppressAppOpenForReview();
        bool apiAvailable = UnityEngine.iOS.Device.RequestStoreReview();
        Debug.Log("[Review] iOS request sent, api available = " + apiAvailable);

#else
        Debug.Log("[Review] Unsupported platform.");
#endif
    }

    private static bool IsSafeScreenVisible(GameObject screen)
    {
        return screen != null
            && screen.activeInHierarchy
            && Application.isFocused;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator RequestAndroidReview(GameObject safeScreen)
    {
        var manager = new ReviewManager();
        Debug.Log("[Review] Android: RequestReviewFlow...");

        var preparation = manager.RequestReviewFlow();
        yield return preparation;

        if (preparation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning($"[Review] Preparation failed: {preparation.Error}");
            yield break;
        }

        if (!IsSafeScreenVisible(safeScreen))
        {
            Debug.Log("[Review] Android: safe screen lost after preparation, abort");
            yield break;
        }

        var reviewInfo = preparation.GetResult();
        Debug.Log("[Review] Android: LaunchReviewFlow (popup should show if Google allows)");

        SuppressAppOpenForReview();
        var launch = manager.LaunchReviewFlow(reviewInfo);
        yield return launch;

        if (launch.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning($"[Review] Launch failed: {launch.Error}");
            yield break;
        }

        Debug.Log("[Review] Flow finished. Dialog visibility and rating are unknown.");
    }
#endif

    public void OpenStoreReviewPage()
    {
#if UNITY_EDITOR
        Debug.Log("[Review] Open store page on a device.");

#elif UNITY_ANDROID
        SuppressAppOpenForReview();
        Application.OpenURL(
            "https://play.google.com/store/apps/details?id=" + Application.identifier);

#elif UNITY_IOS
        if (string.IsNullOrWhiteSpace(iosAppId))
        {
            Debug.LogWarning("[Review] Set the numeric iOS App ID.");
            return;
        }

        SuppressAppOpenForReview();
        Application.OpenURL(
            "https://apps.apple.com/app/id" + iosAppId.Trim() + "?action=write-review");

#else
        Debug.Log("[Review] Unsupported platform.");
#endif
    }

    private static void SuppressAppOpenForReview()
    {
#if ENABLE_RACCOON_ADMOB
        RaccoonAdsManager.Instance?.SetStopTrackingPaused();
#endif
    }
}