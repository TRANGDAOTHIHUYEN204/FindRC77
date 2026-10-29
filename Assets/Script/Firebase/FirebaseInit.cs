using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseInit : MonoBehaviour
{
    public static bool IsReady { get; private set; }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                IsReady = true;
                Debug.Log("Firebase ready");
                FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventAppOpen);
            }
            else
            {
                Debug.LogError("Firebase loi: " + task.Result);
            }
        });
    }
}