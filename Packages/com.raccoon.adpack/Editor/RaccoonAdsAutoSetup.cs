using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;

namespace RCKit.Ads.Main.Editor
{
    // Tự mở Raccoon Ads Setup Window mỗi khi package RaccoonAds/JNative vừa được
    // Package Manager import mới hoặc update - registeredPackages chỉ fire khi tập package
    // thực sự thay đổi (add/update/remove), không fire ở mọi lần mở project/domain reload.
    [InitializeOnLoad]
    internal static class RaccoonAdsAutoSetup
    {
        private const string RaccoonAdsPackageName = "com.raccoon.adpack";

        // SessionState sống theo Editor process (mất khi đóng Unity, không mất qua domain
        // reload/recompile) - đảm bảo chỉ tự mở window 1 lần cho mỗi session, dù trong session
        // đó package có được import/update thêm bao nhiêu lần nữa.
        private const string ShownThisSessionKey = "RaccoonAdsAutoSetup_ShownThisSession";

        static RaccoonAdsAutoSetup()
        {
            Events.registeredPackages += OnPackagesRegistered;
        }

        private static void OnPackagesRegistered(PackageRegistrationEventArgs args)
        {
            if (SessionState.GetBool(ShownThisSessionKey, false)) return;

            var touched = args.added.Concat(args.changedTo)
                .Any(p => p.name == RaccoonAdsPackageName);

            if (!touched) return;

            SessionState.SetBool(ShownThisSessionKey, true);
            // EditorApplication.delayCall += RaccoonAdsSetupWindow.ShowWindow;
        }
    }
}
