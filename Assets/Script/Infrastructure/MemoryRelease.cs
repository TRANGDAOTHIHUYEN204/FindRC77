using System;
using UnityEngine;

/// Giải phóng asset không còn dùng (ảnh level cũ...) để giảm RAM trên máy yếu.
/// Tự đăng ký Application.lowMemory (Android onTrimMemory) khi game khởi động.
public static class MemoryRelease
{
    private const float LowMemoryCooldown = 5f;
    private static float _lastLowMemoryRelease = float.NegativeInfinity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        Application.lowMemory -= OnLowMemory;
        Application.lowMemory += OnLowMemory;
    }

    public static void UnloadUnusedAssets() => Resources.UnloadUnusedAssets();

    private static void OnLowMemory()
    {
        float now = Time.realtimeSinceStartup;
        if (now - _lastLowMemoryRelease < LowMemoryCooldown) return;
        _lastLowMemoryRelease = now;

        Debug.LogWarning("[Memory] Low memory warning: releasing unused assets.");
        GC.Collect();
        Resources.UnloadUnusedAssets();
    }
}
