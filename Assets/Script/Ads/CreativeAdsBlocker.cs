#if CREATIVE
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_RACCOON_ADMOB
using RCKit.Ads.Main;
#endif

/// <summary>
/// Bản CREATIVE (build cho MKT quay video): tắt hẳn SDK quảng cáo Raccoon.
/// RaccoonAdsFirst ở LoadingScene gọi thẳng SDK (Initialize → first ad, rồi bật app-open khi quay lại app),
/// không đi qua AdsController nên define CREATIVE trong AdsController không chặn được.
/// Gỡ RaccoonAdsManager ngay sau Awake của scene đầu (trước Start của RaccoonAdsFirst);
/// mọi lời gọi trong RaccoonAdsFirst đều check Instance != null nên sẽ tự bỏ qua.
/// </summary>
public static class CreativeAdsBlocker
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        DisableAdsManager();
        SceneManager.sceneLoaded += (scene, mode) => DisableAdsManager();
    }

    private static void DisableAdsManager()
    {
#if ENABLE_RACCOON_ADMOB
        var mgr = RaccoonAdsManager.Instance;
        if (mgr == null) return;

        RaccoonAdsManager.Instance = null;
        mgr.enabled = false;
        Object.Destroy(mgr);

        Debug.Log("[Ads] CREATIVE: đã gỡ RaccoonAdsManager, không init SDK, không show ad.");
#endif
    }
}
#endif
