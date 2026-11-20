using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_APPSFLYER
using Raccoon;
#endif

namespace RaccoonAds.Editor
{
    /// <summary>
    /// Chặn build iOS khi AppsFlyer bị cấu hình sai theo kiểu "SDK có trong Xcode nhưng không bao giờ gửi dữ liệu":
    /// - Có plugin AppsFlyer nhưng thiếu define ENABLE_APPSFLYER cho iOS (GameAppsFlyer bị compile thành stub, không init/start).
    /// - GameAppsFlyer trong scene build có App ID iOS không phải App Store ID dạng số (ví dụ còn để package name Android).
    /// </summary>
    public class AppsFlyerBuildValidator : IPreprocessBuildWithReport, IProcessSceneWithReport, IPostprocessBuildWithReport
    {
        private const string Define = "ENABLE_APPSFLYER";
        private const string Tag = "[RaccoonAds][AppsFlyer]";

        private static int _componentCount;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            _componentCount = 0;
            if (report.summary.platform != BuildTarget.iOS) return;

            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.iOS)
                .Split(';').Select(d => d.Trim());
            var hasDefine = defines.Contains(Define);

            if (!hasDefine && IsAppsFlyerPluginInstalled())
            {
                throw new BuildFailedException(
                    $"{Tag} Project có plugin AppsFlyer nhưng iOS thiếu Scripting Define Symbol {Define}. " +
                    "GameAppsFlyer sẽ là stub, AppsFlyer không init/start nên không có install/postback nào. " +
                    $"Bật {Define} cho iOS trong Raccoon Ads Setup (hoặc Player Settings > iOS), hoặc gỡ plugin AppsFlyer nếu game không dùng.");
            }
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // report == null khi vào Play Mode trong Editor → bỏ qua.
            if (report == null || report.summary.platform != BuildTarget.iOS) return;
#if ENABLE_APPSFLYER
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var af in root.GetComponentsInChildren<GameAppsFlyer>(true))
                {
                    _componentCount++;
                    var appId = af.GetAppId(true);
                    if (!GameAppsFlyer.IsValidIOSAppId(appId))
                    {
                        throw new BuildFailedException(
                            $"{Tag} GameAppsFlyer trên '{GetPath(af.transform)}' (scene '{scene.path}') có App ID iOS không hợp lệ: '{appId}'. " +
                            "Cần App Store ID dạng số (ví dụ 6759906257) ở field appIdIOS. " +
                            "Dùng sai ID (ví dụ package name Android) thì AppsFlyer không ghi nhận install và không postback sang Google Ads.");
                    }
                }
            }
#endif
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
#if ENABLE_APPSFLYER
            if (_componentCount == 0)
            {
                Debug.LogWarning(
                    $"{Tag} Đã bật {Define} nhưng không thấy GameAppsFlyer nào trong các scene build. " +
                    "Nếu không add component này lúc runtime thì AppsFlyer sẽ không được init/start.");
            }
#endif
        }

        private static bool IsAppsFlyerPluginInstalled()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetType("AppsFlyerSDK.AppsFlyer", false) != null);
        }

        private static string GetPath(Transform t)
        {
            var path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
