using UnityEngine;


public static class UIInputBlocker
{
    private static int openCount;
    private static float blockUntil;

    public static bool IsBlocked => openCount > 0 || Time.unscaledTime < blockUntil;

    public static void RegisterOpen()
    {
        openCount++;
    }

    public static void RegisterClose(float holdSeconds = 0.25f)
    {
        openCount = Mathf.Max(0, openCount - 1);
        blockUntil = Mathf.Max(blockUntil, Time.unscaledTime + holdSeconds);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        openCount = 0;
        blockUntil = 0f;
    }
}