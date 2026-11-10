using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Raccoon;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RCKit.Ads.Main.Editor
{
    public class RaccoonAdsSetupWindow : EditorWindow
    {
        private static string ManifestPath =>
            Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");

        // PackageInfo.FindForAssembly resolve đường dẫn hiện tại của package mỗi lần gọi,
        // nên không bị stale khi Package Manager update package sang git commit mới
        // (folder Library/PackageCache/com.raccoon.adpack@<hash cũ> bị xoá và thay bằng
        // @<hash mới>). [CallerFilePath] chỉ dùng làm fallback cho trường hợp package
        // được nhúng thẳng vào Assets mà không khai báo qua manifest.json.
        private static string GetThisScriptDirectory([CallerFilePath] string path = null)
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(RaccoonAdsSetupWindow).Assembly);
            return packageInfo != null
                ? Path.Combine(packageInfo.resolvedPath, "Editor")
                : Path.GetDirectoryName(path);
        }

        private Vector2 _windowScroll;

        #region Window Lifecycle

        [MenuItem("Raccoon/AdKit Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<RaccoonAdsSetupWindow>(true, "Raccoon Ads Setup");
            window.minSize = new Vector2(380, 360);
        }

        private void OnEnable()
        {
            LoadDefineToggles();
            LoadFieldVisibilityToggles();
            LoadAdMobMediationConfig();
            LoadFirebaseModulesConfig();
        }

        private void OnGUI()
        {
            _windowScroll = EditorGUILayout.BeginScrollView(_windowScroll);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Expand All")) SetAllSectionsExpanded(true);
            if (GUILayout.Button("Collapse All")) SetAllSectionsExpanded(false);
            EditorGUILayout.EndHorizontal();

            DrawScopeUpmSection();
            DrawScriptingDefineSymbolsSection();
            DrawFieldVisibilitySection();
            DrawJNativeSection();
            DrawAdMobSection();
            DrawFirebaseSection();
            DrawRaccoonAdsPackageSection();
            DrawAppsFlyerSection();
            DrawFacebookSection();

            DrawBaseSetupSection();
            DrawAppsFlyerSetupSection();
            DrawUnityAdsSetupSection();
            DrawTikTokSetupSection();

            EditorGUILayout.Space(16);
            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region DRAW GUI

        // Trạng thái expand/collapse của từng section chính trong OnGUI - chỉ tồn tại
        // trong phiên làm việc của window (không lưu qua EditorPrefs), mặc định collapse hết.
        private bool _scopeUpmExpanded = false;
        private bool _scriptingDefinesExpanded = false;
        private bool _fieldVisibilityExpanded = false;
        private bool _jnativeExpanded = false;
        private bool _adMobExpanded = false;
        private bool _firebaseExpanded = false;
        private bool _raccoonAdsPackageExpanded = false;
        private bool _appsFlyerExpanded = false;
        private bool _facebookExpanded = false;

        private void SetAllSectionsExpanded(bool expanded)
        {
            _scopeUpmExpanded = expanded;
            _scriptingDefinesExpanded = expanded;
            _fieldVisibilityExpanded = expanded;
            _jnativeExpanded = expanded;
            _adMobExpanded = expanded;
            _firebaseExpanded = expanded;
            _raccoonAdsPackageExpanded = expanded;
            _appsFlyerExpanded = expanded;
            _facebookExpanded = expanded;
        }

        private static bool DrawSectionHeader(string title, ref bool expanded)
        {
            EditorGUILayout.Space(16);
            expanded = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);
            return expanded;
        }

        private void DrawScopeUpmSection()
        {
            if (!DrawSectionHeader("Raccoon Ads - Package Dependencies", ref _scopeUpmExpanded)) return;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Thêm scoped registries (OpenUPM, AppLovin) vào Packages/manifest.json để Unity Package Manager resolve được các dependency của RaccoonAds.",
                MessageType.Info);
            EditorGUILayout.Space();

            if (GUILayout.Button("Add Scope UPM", GUILayout.Height(32)))
                AddScopeUpm();
        }

        private void DrawScriptingDefineSymbolsSection()
        {
            if (!DrawSectionHeader("Raccoon Ads - Scripting Define Symbols", ref _scriptingDefinesExpanded)) return;

            EditorGUILayout.HelpBox(
                "Chọn các network sẽ bật cho Android/iOS/Standalone (Player Settings > Scripting Define Symbols).",
                MessageType.Info);
            EditorGUILayout.Space();

            _admobEnabled = EditorGUILayout.ToggleLeft($"AdMob ({DefineAdMob})", _admobEnabled);
            _jnativeEnabled = EditorGUILayout.ToggleLeft($"JNative ({DefineJNative})", _jnativeEnabled);
            _maxEnabled = EditorGUILayout.ToggleLeft($"Max ({DefineMax})", _maxEnabled);
            _appsflyerDefineEnabled = EditorGUILayout.ToggleLeft($"AppsFlyer ({DefineAppsFlyer})", _appsflyerDefineEnabled);
            _unityAdsEnabled = EditorGUILayout.ToggleLeft($"Unity Ads ({DefineUnityAds})", _unityAdsEnabled);
            _tiktokEnabled = EditorGUILayout.ToggleLeft($"TikTok ({DefineTikTok})", _tiktokEnabled);

            EditorGUILayout.Space();
            if (GUILayout.Button("Add Scripting Define Symbol", GUILayout.Height(32)))
                ApplyScriptingDefineSymbols();
        }

        private void DrawFieldVisibilitySection()
        {
            if (!DrawSectionHeader("Raccoon Ads - Field Visibility (RaccoonAdsManager)", ref _fieldVisibilityExpanded)) return;

            EditorGUILayout.HelpBox(
                "Bật để hiện field trên Inspector của RaccoonAdsManager (giữ nguyên giá trị đã set). " +
                "Tắt: field không còn serialize, code dùng thẳng giá trị mặc định (const), Inspector không hiện field đó nữa.\n" +
                "RACCOON_DEBUG_LAYOUT_NATIVE: bật để ép layout/template Native luôn về giá trị mặc định (bỏ qua enum); " +
                "tắt để dùng đúng mapping theo enum trong GetQuitLayoutNative/GetLayoutTemplateNative.",
                MessageType.Info);
            EditorGUILayout.Space();

            _showAdScaleBtn = EditorGUILayout.ToggleLeft($"Show ad_scale_btn ({DefineShowAdScaleBtn})", _showAdScaleBtn);
            _showAdRefreshTime = EditorGUILayout.ToggleLeft($"Show ad_refesh_time ({DefineShowAdRefreshTime})", _showAdRefreshTime);
            _showReloadOnClick = EditorGUILayout.ToggleLeft($"Show reload_on_click ({DefineShowReloadOnClick})", _showReloadOnClick);
            _showCloseAdOnClick = EditorGUILayout.ToggleLeft($"Show close_ad_on_click ({DefineShowCloseAdOnClick})", _showCloseAdOnClick);
            _debugLayoutNative = EditorGUILayout.ToggleLeft($"Debug layout native ({DefineDebugLayoutNative})", _debugLayoutNative);
            _multiAppOpenAd = EditorGUILayout.ToggleLeft($"Multi App Open Ad ({DefineMultiAppOpenAd})", _multiAppOpenAd);

            EditorGUILayout.Space();
            if (GUILayout.Button("Apply Field Visibility", GUILayout.Height(32)))
                ApplyFieldVisibilityDefines();
        }

        private void DrawJNativeSection()
        {
            if (!DrawSectionHeader("Raccoon Ads - Git Dependency (JNative)", ref _jnativeExpanded)) return;

            EditorGUILayout.HelpBox(
                "Chọn nhánh và version (tuỳ chọn) để thêm/cập nhật dependency git jnativeads vào Packages/manifest.json.\n" +
                "- Bỏ trống version: pull theo tên nhánh (release / beta_test / alpha_test).\n" +
                "- Nhập version cho nhánh release: dùng đúng version (0.0.xx).\n" +
                "- Nhập version cho nhánh beta_test: version sẽ tự thêm hậu tố -beta (0.0.xx-beta).\n" +
                "- Nhập version cho nhánh alpha_test: version sẽ tự thêm hậu tố -alpha (0.0.xx-alpha).",
                MessageType.Info);
            EditorGUILayout.Space();

            _jnativeBranch = (GitReleaseBranch)EditorGUILayout.EnumPopup("Branch", _jnativeBranch);
            _jnativeVersion = EditorGUILayout.TextField(
                new GUIContent("Version (optional)", "Để trống để pull theo nhánh mặc định"), _jnativeVersion);
            _jnativeUseSsh = EditorGUILayout.ToggleLeft(
                new GUIContent("Use SSH", "Bật: dùng dạng git@github.com:... — Tắt: dùng dạng https://github.com/..."),
                _jnativeUseSsh);

            var previewRef = BuildReleaseGitRef(_jnativeBranch, _jnativeVersion);
            var previewBaseUrl = _jnativeUseSsh ? JNativeGitUrlSsh : JNativeGitUrlHttps;
            EditorGUILayout.LabelField("Preview:", $"{previewBaseUrl}#{previewRef}", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Git JNative", GUILayout.Height(32)))
                AddGitJNative();
            if (GUILayout.Button("Update JNative", GUILayout.Height(32)))
                UpdateJNativePackage();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRaccoonAdsPackageSection()
        {
            if (!DrawSectionHeader("Raccoon Ads - Package chính (RaccoonAds)", ref _raccoonAdsPackageExpanded)) return;

            EditorGUILayout.HelpBox(
                "Chọn nhánh và version (tuỳ chọn) cho chính package RaccoonAds (com.raccoon.adpack).\n" +
                "- Add Git RaccoonAds: ghi/cập nhật entry trong Packages/manifest.json.\n" +
                "- Update RaccoonAds: gọi Package Manager cập nhật ngay lập tức (dùng khi URL#ref không đổi nhưng muốn pull commit mới nhất).",
                MessageType.Info);
            EditorGUILayout.Space();

            _raccoonAdsBranch = (GitReleaseBranch)EditorGUILayout.EnumPopup("Branch", _raccoonAdsBranch);
            _raccoonAdsVersion = EditorGUILayout.TextField(
                new GUIContent("Version (optional)", "Để trống để pull theo nhánh mặc định"), _raccoonAdsVersion);
            _raccoonAdsUseSsh = EditorGUILayout.ToggleLeft(
                new GUIContent("Use SSH", "Bật: dùng dạng git@github.com:... — Tắt: dùng dạng https://github.com/..."),
                _raccoonAdsUseSsh);

            var raccoonAdsPreviewRef = BuildReleaseGitRef(_raccoonAdsBranch, _raccoonAdsVersion);
            var raccoonAdsPreviewBaseUrl = _raccoonAdsUseSsh ? RaccoonAdsGitUrlSsh : RaccoonAdsGitUrlHttps;
            EditorGUILayout.LabelField("Preview:", $"{raccoonAdsPreviewBaseUrl}#{raccoonAdsPreviewRef}",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Git RaccoonAds", GUILayout.Height(32)))
                AddGitRaccoonAds();
            if (GUILayout.Button("Update RaccoonAds", GUILayout.Height(32)))
                UpdateRaccoonAdsPackage();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAdMobSection()
        {
            if (!DrawSectionHeader("Raccoon Ads - AdMob (Google Mobile Ads for Unity)", ref _adMobExpanded)) return;

            if (_adMobMediations == null)
            {
                EditorGUILayout.HelpBox($"Không đọc được file config: {AdMobMediationConfigPath}", MessageType.Error);
                if (GUILayout.Button("Reload config", GUILayout.Height(32)))
                    LoadAdMobMediationConfig();
                return;
            }

            EditorGUILayout.HelpBox(
                $"Core: {_adMobCoreName} @ {_adMobCoreVersion}\nChọn mediation adapter muốn thêm (mỗi mediation kèm sẵn version).",
                MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Admob SDK version ({_adMobCoreVersion})", GUILayout.ExpandWidth(true));
            _adMobCoreVersionOverride = EditorGUILayout.TextField(_adMobCoreVersionOverride,
                GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            _adMobMediationScroll = EditorGUILayout.BeginScrollView(_adMobMediationScroll, GUILayout.Height(140));
            foreach (var mediation in _adMobMediations)
            {
                EditorGUILayout.BeginHorizontal();
                mediation.Enabled = EditorGUILayout.ToggleLeft(
                    $"{mediation.DisplayName} ({mediation.Name} @ {mediation.Version})", mediation.Enabled,
                    GUILayout.ExpandWidth(true));
                mediation.CustomVersion = EditorGUILayout.TextField(mediation.CustomVersion,
                    GUILayout.Width(90));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox(
                "Nhập version custom (tuỳ chọn) ở ô bên phải mỗi mediation để override version mặc định khi Add AdMob. " +
                "Version custom chỉ dùng tạm thời, không lưu lại vào AdMobMediationConfig.json.",
                MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add AdMob", GUILayout.Height(32)))
                AddAdMob();
            if (GUILayout.Button("Reload config", GUILayout.Height(32)))
                LoadAdMobMediationConfig();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFirebaseSection()
        {
            if (!DrawSectionHeader("Raccoon Ads - Firebase (Analytics / Remote Config / Crashlytics)", ref _firebaseExpanded)) return;

            if (_firebaseModules == null)
            {
                EditorGUILayout.HelpBox($"Không đọc được file config: {FirebaseModulesConfigPath}", MessageType.Error);
                if (GUILayout.Button("Reload config", GUILayout.Height(32)))
                    LoadFirebaseModulesConfig();
                return;
            }

            EditorGUILayout.HelpBox(
                $"Git: {_firebaseGitUrl}\nCore: {_firebaseCoreName} @ {_firebaseCoreVersion}\nChọn module Firebase muốn thêm (mỗi module kèm sẵn branch release).",
                MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"Firebase core branch ({_firebaseCoreVersion})");
            EditorGUILayout.Space();

            foreach (var module in _firebaseModules)
            {
                module.Enabled = EditorGUILayout.ToggleLeft(
                    $"{module.DisplayName} ({module.Name} @ {module.Version})", module.Enabled);
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Firebase", GUILayout.Height(32)))
                AddFirebase();
            if (GUILayout.Button("Reload config", GUILayout.Height(32)))
                LoadFirebaseModulesConfig();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAppsFlyerSection()
        {
            if (!DrawSectionHeader("Raccoon Ads - Git Dependency (AppsFlyer)", ref _appsFlyerExpanded)) return;

            EditorGUILayout.HelpBox(
                "Nhập version (tuỳ chọn) để thêm/cập nhật dependency git appsflyer-unity-plugin vào Packages/manifest.json.\n" +
                "- Bỏ trống version: pull theo nhánh mặc định (upm).\n" +
                "- Nhập version: dùng đúng version/tag đã nhập.",
                MessageType.Info);
            EditorGUILayout.Space();

            _appsflyerVersion = EditorGUILayout.TextField(
                new GUIContent("Version (optional)", "Để trống để pull theo nhánh mặc định (upm)"), _appsflyerVersion);

            var appsflyerPreviewRef = BuildAppsFlyerGitRef(_appsflyerVersion);
            EditorGUILayout.LabelField("Preview:", $"{AppsFlyerGitUrl}#{appsflyerPreviewRef}",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            if (GUILayout.Button("Add Git AppsFlyer", GUILayout.Height(32)))
                AddGitAppsFlyer();
        }

        private void DrawFacebookSection()
        {
            if (!DrawSectionHeader("Raccoon Ads - Git Dependency (Facebook SDK)", ref _facebookExpanded)) return;

            EditorGUILayout.HelpBox(
                "Thêm/cập nhật dependency git com.raccoon.facebooksdk vào Packages/manifest.json.\n" +
                "- Add Git Facebook SDK: ghi/cập nhật entry trong manifest.json.\n" +
                "- Update Facebook SDK: gọi Package Manager cập nhật ngay lập tức (dùng khi muốn pull commit mới nhất trên nhánh release).",
                MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Preview:", $"{FacebookGitUrl}#{FacebookDefaultRef}",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Git Facebook SDK", GUILayout.Height(32)))
                AddGitFacebook();
            if (GUILayout.Button("Update Facebook SDK", GUILayout.Height(32)))
                UpdateFacebookPackage();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Scope UPM

        private class ScopedRegistryInfo
        {
            public string Name;
            public string Url;
            public string[] Scopes;
        }

        private static readonly ScopedRegistryInfo[] RequiredScopedRegistries =
        {
            new ScopedRegistryInfo
            {
                Name = "package.openupm.com",
                Url = "https://package.openupm.com",
                Scopes = new[] { "com.google", "com.google.external-dependency-manager" }
            },
            new ScopedRegistryInfo
            {
                Name = "AppLovin MAX Unity",
                Url = "https://unity.packages.applovin.com/",
                Scopes = new[] { "com.applovin.mediation.ads", "com.applovin.mediation.adapters", "com.applovin.mediation.dsp" }
            }
        };

        private static void AddScopeUpm(bool showDialog = true)
        {
            if (!File.Exists(ManifestPath))
            {
                Debug.LogError($"[RaccoonAds] Không tìm thấy manifest.json tại: {ManifestPath}");
                return;
            }

            var json = File.ReadAllText(ManifestPath);
            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> manifest))
            {
                Debug.LogError("[RaccoonAds] manifest.json không đúng định dạng JSON.");
                return;
            }

            if (!(manifest.TryGetValue("scopedRegistries", out var scopedRegistriesObj) &&
                  scopedRegistriesObj is List<object> scopedRegistries))
            {
                scopedRegistries = new List<object>();
                manifest["scopedRegistries"] = scopedRegistries;
            }

            var addedCount = 0;
            var updatedCount = 0;

            foreach (var required in RequiredScopedRegistries)
            {
                var existing = scopedRegistries
                    .OfType<Dictionary<string, object>>()
                    .FirstOrDefault(r =>
                        r.TryGetValue("url", out var urlObj) &&
                        string.Equals(urlObj as string, required.Url, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    scopedRegistries.Add(new Dictionary<string, object>
                    {
                        ["name"] = required.Name,
                        ["url"] = required.Url,
                        ["scopes"] = required.Scopes.Cast<object>().ToList()
                    });
                    addedCount++;
                    continue;
                }

                if (!(existing.TryGetValue("scopes", out var scopesObj) && scopesObj is List<object> scopesList))
                {
                    scopesList = new List<object>();
                    existing["scopes"] = scopesList;
                }

                var existingScopes = new HashSet<string>(scopesList.OfType<string>());
                var missingScopes = required.Scopes.Where(s => !existingScopes.Contains(s)).ToList();
                if (missingScopes.Count == 0) continue;

                scopesList.AddRange(missingScopes);
                updatedCount++;
            }

            if (addedCount == 0 && updatedCount == 0)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("Raccoon Ads Setup", "Scoped registries đã đầy đủ, không cần thay đổi.", "OK");
                return;
            }

            File.WriteAllText(ManifestPath, MiniJson.Serialize(manifest));
            AssetDatabase.Refresh();

            if (showDialog)
                EditorUtility.DisplayDialog("Raccoon Ads Setup",
                    $"Đã cập nhật Packages/manifest.json\nThêm mới: {addedCount} registry\nCập nhật thêm scope: {updatedCount} registry",
                    "OK");
        }

        #endregion

        #region Scripting Define Symbols

        private const string DefineAdMob = "ENABLE_RACCOON_ADMOB";
        private const string DefineJNative = "ENABLE_RACCOON_JNATIVE";
        private const string DefineMax = "ENABLE_RACCOON_MAX";
        private const string DefineAppsFlyer = "ENABLE_APPSFLYER";
        private const string DefineUnityAds = "ENABLE_UNITY_AD_SDK";
        private const string DefineTikTok = "ENABLE_TIKTOK_SDK";

        private static readonly NamedBuildTarget[] TargetGroups =
        {
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Standalone
        };

        private bool _admobEnabled;
        private bool _jnativeEnabled;
        private bool _maxEnabled;
        private bool _appsflyerDefineEnabled;
        private bool _unityAdsEnabled;
        private bool _tiktokEnabled;

        private void LoadDefineToggles()
        {
            var defines = GetDefineSymbols(TargetGroups[0]);
            _admobEnabled = defines.Contains(DefineAdMob);
            _jnativeEnabled = defines.Contains(DefineJNative);
            _maxEnabled = defines.Contains(DefineMax);
            _appsflyerDefineEnabled = defines.Contains(DefineAppsFlyer);
            _unityAdsEnabled = defines.Contains(DefineUnityAds);
            _tiktokEnabled = defines.Contains(DefineTikTok);
        }

        private void ApplyScriptingDefineSymbols(bool showDialog = true)
        {
            var desiredState = new Dictionary<string, bool>
            {
                [DefineAdMob] = _admobEnabled,
                [DefineJNative] = _jnativeEnabled,
                [DefineMax] = _maxEnabled,
                [DefineAppsFlyer] = _appsflyerDefineEnabled,
                [DefineUnityAds] = _unityAdsEnabled,
                [DefineTikTok] = _tiktokEnabled
            };

            foreach (var group in TargetGroups)
            {
                var defines = GetDefineSymbols(group);

                foreach (var kvp in desiredState)
                {
                    if (kvp.Value)
                        defines.Add(kvp.Key);
                    else
                        defines.Remove(kvp.Key);
                }

                PlayerSettings.SetScriptingDefineSymbols(group, string.Join(";", defines));
            }

            if (showDialog)
                EditorUtility.DisplayDialog("Raccoon Ads Setup",
                    "Đã cập nhật Scripting Define Symbols cho Android / iOS / Standalone.", "OK");
        }

        private static HashSet<string> GetDefineSymbols(NamedBuildTarget target)
        {
            var raw = PlayerSettings.GetScriptingDefineSymbols(target);
            return new HashSet<string>(raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        #endregion

        #region Unity Ads Setup

        private const string UnityAdsPackageName = "com.unity.ads";

        // SessionState giữ được qua domain reload: đánh dấu cần tạo UnityAdsSettings sau khi
        // thêm define, vì class UnityAdsSettings chỉ được compile khi đã có ENABLE_UNITY_AD_SDK.
        private const string PendingCreateUnityAdsSettingsKey = "RaccoonAds.PendingCreateUnityAdsSettings";

        // Đánh dấu đang chờ Client.Add(com.unity.ads): nếu domain reload xảy ra trước khi callback
        // của request chạy thì ResumeUnityAdsSetup (DidReloadScripts) sẽ làm tiếp các bước sau.
        private const string PendingUnityAdsSetupKey = "RaccoonAds.PendingUnityAdsSetup";

        private static bool IsUnityAdsPackageInstalled =>
            UnityEditor.PackageManager.PackageInfo.FindForAssetPath($"Packages/{UnityAdsPackageName}") != null;

        private void DrawUnityAdsSetupSection()
        {
            EditorGUILayout.Space(24);
            EditorGUILayout.HelpBox(
                $"Setup Unity Ads (UA only): add package {UnityAdsPackageName} vào UPM, thêm define {DefineUnityAds} " +
                "cho Android / iOS / Standalone và tạo Resources/UnityAdsSettings.asset để nhập Game ID.\n" +
                $"- Cần bật AdMob ({DefineAdMob}) thì module Unity Ads mới được compile.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.55f, 0.85f, 0.55f);
            if (GUILayout.Button("Setup Unity SDK (One Click)", GUILayout.Height(48)))
                SetupUnityAdsOneClick();
            GUI.backgroundColor = previousColor;
        }

        private static void SetupUnityAdsOneClick()
        {
            if (IsUnityAdsPackageInstalled)
            {
                ContinueUnityAdsSetup();
                return;
            }

            // Phải add package xong mới thêm define, nếu không project lỗi compile (thiếu UnityEngine.Advertisements).
            Debug.Log($"[RaccoonAds] Đang add {UnityAdsPackageName} qua Package Manager...");
            SessionState.SetBool(PendingUnityAdsSetupKey, true);

            var request = Client.Add(UnityAdsPackageName);
            EditorApplication.CallbackFunction handler = null;
            handler = () =>
            {
                if (!request.IsCompleted) return;
                EditorApplication.update -= handler;

                if (request.Status == StatusCode.Success)
                {
                    Debug.Log($"[RaccoonAds] Đã add {request.Result.packageId}");
                    ContinueUnityAdsSetup();
                }
                else
                {
                    SessionState.EraseBool(PendingUnityAdsSetupKey);
                    Debug.LogError($"[RaccoonAds] Add {UnityAdsPackageName} thất bại: {request.Error?.message}");
                    EditorUtility.DisplayDialog("Raccoon Ads Setup",
                        $"Add {UnityAdsPackageName} thất bại:\n{request.Error?.message}", "OK");
                }
            };
            EditorApplication.update += handler;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void ResumeUnityAdsSetup()
        {
            if (!SessionState.GetBool(PendingUnityAdsSetupKey, false) || !IsUnityAdsPackageInstalled) return;
            ContinueUnityAdsSetup();
        }

        private static void ContinueUnityAdsSetup()
        {
            SessionState.EraseBool(PendingUnityAdsSetupKey);

#if ENABLE_UNITY_AD_SDK && ENABLE_RACCOON_ADMOB
            SelectOrCreateUnityAdsSettings();
#else
            foreach (var group in TargetGroups)
            {
                var defines = GetDefineSymbols(group);
                defines.Add(DefineUnityAds);
                PlayerSettings.SetScriptingDefineSymbols(group, string.Join(";", defines));
            }

            SessionState.SetBool(PendingCreateUnityAdsSettingsKey, true);

            var message = $"Đã thêm define {DefineUnityAds}. UnityAdsSettings.asset sẽ được tạo tự động sau khi Unity compile xong.";
            if (!GetDefineSymbols(TargetGroups[0]).Contains(DefineAdMob))
                message += $"\n\nLưu ý: chưa bật {DefineAdMob}, asset chỉ được tạo sau khi bật AdMob.";
            EditorUtility.DisplayDialog("Raccoon Ads Setup", message, "OK");
#endif
        }

#if ENABLE_UNITY_AD_SDK && ENABLE_RACCOON_ADMOB
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void CreatePendingUnityAdsSettings()
        {
            if (!SessionState.GetBool(PendingCreateUnityAdsSettingsKey, false)) return;
            SessionState.EraseBool(PendingCreateUnityAdsSettingsKey);
            SelectOrCreateUnityAdsSettings();
        }

        // Cùng folder Resources với RaccoonAdUnit.asset (RaccoonAdsManager.LoadOrCreateAdsData).
        private const string UnityAdsSettingsFolder = "Assets/RaccoonAds/Resources";
        private const string UnityAdsSettingsPath =
            UnityAdsSettingsFolder + "/" + UnityAds.UnityAdsSettings.ResourcesName + ".asset";

        private static void SelectOrCreateUnityAdsSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<UnityAds.UnityAdsSettings>(UnityAdsSettingsPath);
            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/RaccoonAds"))
                    AssetDatabase.CreateFolder("Assets", "RaccoonAds");
                if (!AssetDatabase.IsValidFolder(UnityAdsSettingsFolder))
                    AssetDatabase.CreateFolder("Assets/RaccoonAds", "Resources");

                settings = CreateInstance<UnityAds.UnityAdsSettings>();
                AssetDatabase.CreateAsset(settings, UnityAdsSettingsPath);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
#endif

        #endregion

        #region TikTok Setup

        // Pangle/TikTok Combined SDK: pag-sdk >= 7.7 đã kèm module TikTok App Events (tiktok-business-android-sdk-comp).
        // Không import TikTokBusinessSDK.unitypackage vì gói đó kèm sẵn tiktok-business-android-sdk.aar => trùng class.
        private const string PangleSdkVersion = "8.2.0.8";

        // Ghi vào Assets/Editor của project (không ghi vào package) để chỉ project bật TikTok mới kéo pag-sdk.
        private const string TikTokDependenciesFolder = "Assets/Editor";
        private const string TikTokDependenciesPath = TikTokDependenciesFolder + "/RaccoonTikTokDependencies.xml";

        private const string TikTokDependenciesXml =
            "<dependencies>\n" +
            "  <androidPackages>\n" +
            "    <repositories>\n" +
            "      <repository>https://artifact.bytedance.com/repository/pangle</repository>\n" +
            "    </repositories>\n" +
            "    <androidPackage spec=\"com.pangle.global:pag-sdk:" + PangleSdkVersion + "\" />\n" +
            "    <androidPackage spec=\"androidx.lifecycle:lifecycle-process:2.3.1\" />\n" +
            "    <androidPackage spec=\"androidx.lifecycle:lifecycle-common-java8:2.3.1\" />\n" +
            "    <androidPackage spec=\"com.android.installreferrer:installreferrer:2.2\" />\n" +
            "  </androidPackages>\n" +
            "</dependencies>\n";

        // Giống Unity Ads: TikTokSettings chỉ được compile khi đã có ENABLE_TIKTOK_SDK.
        private const string PendingCreateTikTokSettingsKey = "RaccoonAds.PendingCreateTikTokSettings";

        private void DrawTikTokSetupSection()
        {
            EditorGUILayout.Space(24);
            EditorGUILayout.HelpBox(
                $"Setup TikTok SDK (UA only, Android): tạo {TikTokDependenciesPath} (pag-sdk {PangleSdkVersion} - " +
                $"Pangle/TikTok Combined SDK), thêm define {DefineTikTok} cho Android / iOS / Standalone và tạo " +
                "Resources/TikTokSettings.asset để nhập TikTok App ID + App Secret.\n" +
                $"- Cần bật AdMob ({DefineAdMob}) thì module TikTok mới được compile.\n" +
                "- Sau đó chạy Assets > External Dependency Manager > Android Resolver > Force Resolve.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.55f, 0.85f, 0.55f);
            if (GUILayout.Button("Setup TikTok SDK (One Click)", GUILayout.Height(48)))
                SetupTikTokOneClick();
            GUI.backgroundColor = previousColor;
        }

        private static void SetupTikTokOneClick()
        {
            if (!AssetDatabase.IsValidFolder(TikTokDependenciesFolder))
                AssetDatabase.CreateFolder("Assets", "Editor");
            File.WriteAllText(TikTokDependenciesPath, TikTokDependenciesXml);
            AssetDatabase.ImportAsset(TikTokDependenciesPath);
            Debug.Log($"[RaccoonAds] Đã tạo {TikTokDependenciesPath}");

#if ENABLE_TIKTOK_SDK && ENABLE_RACCOON_ADMOB
            SelectOrCreateTikTokSettings();
#else
            foreach (var group in TargetGroups)
            {
                var defines = GetDefineSymbols(group);
                defines.Add(DefineTikTok);
                PlayerSettings.SetScriptingDefineSymbols(group, string.Join(";", defines));
            }

            SessionState.SetBool(PendingCreateTikTokSettingsKey, true);

            var message = $"Đã tạo {TikTokDependenciesPath} và thêm define {DefineTikTok}. " +
                          "TikTokSettings.asset sẽ được tạo tự động sau khi Unity compile xong.\n\n" +
                          "Nhớ chạy Force Resolve (External Dependency Manager > Android Resolver).";
            if (!GetDefineSymbols(TargetGroups[0]).Contains(DefineAdMob))
                message += $"\n\nLưu ý: chưa bật {DefineAdMob}, asset chỉ được tạo sau khi bật AdMob.";
            EditorUtility.DisplayDialog("Raccoon Ads Setup", message, "OK");
#endif
        }

#if ENABLE_TIKTOK_SDK && ENABLE_RACCOON_ADMOB
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void CreatePendingTikTokSettings()
        {
            if (!SessionState.GetBool(PendingCreateTikTokSettingsKey, false)) return;
            SessionState.EraseBool(PendingCreateTikTokSettingsKey);
            SelectOrCreateTikTokSettings();
        }

        // Cùng folder Resources với RaccoonAdUnit.asset / UnityAdsSettings.asset.
        private const string TikTokSettingsFolder = "Assets/RaccoonAds/Resources";
        private const string TikTokSettingsPath =
            TikTokSettingsFolder + "/" + TikTok.TikTokSettings.ResourcesName + ".asset";

        private static void SelectOrCreateTikTokSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TikTok.TikTokSettings>(TikTokSettingsPath);
            if (settings == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/RaccoonAds"))
                    AssetDatabase.CreateFolder("Assets", "RaccoonAds");
                if (!AssetDatabase.IsValidFolder(TikTokSettingsFolder))
                    AssetDatabase.CreateFolder("Assets/RaccoonAds", "Resources");

                settings = CreateInstance<TikTok.TikTokSettings>();
                AssetDatabase.CreateAsset(settings, TikTokSettingsPath);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
#endif

        #endregion

        #region Field Visibility (RaccoonAdsManager)

        private const string DefineShowAdScaleBtn = "RACCOON_SHOW_AD_SCALE_BTN";
        private const string DefineShowAdRefreshTime = "RACCOON_SHOW_AD_REFRESH_TIME";
        private const string DefineShowReloadOnClick = "RACCOON_SHOW_RELOAD_ON_CLICK";
        private const string DefineShowCloseAdOnClick = "RACCOON_SHOW_CLOSE_AD_ON_CLICK";
        private const string DefineDebugLayoutNative = "RACCOON_DEBUG_LAYOUT_NATIVE";
        private const string DefineMultiAppOpenAd = "RACCOON_MULTI_APPOPEN_AD";

        private bool _showAdScaleBtn;
        private bool _showAdRefreshTime;
        private bool _showReloadOnClick;
        private bool _showCloseAdOnClick;
        private bool _debugLayoutNative;
        private bool _multiAppOpenAd;

        private void LoadFieldVisibilityToggles()
        {
            var defines = GetDefineSymbols(TargetGroups[0]);
            _showAdScaleBtn = defines.Contains(DefineShowAdScaleBtn);
            _showAdRefreshTime = defines.Contains(DefineShowAdRefreshTime);
            _showReloadOnClick = defines.Contains(DefineShowReloadOnClick);
            _showCloseAdOnClick = defines.Contains(DefineShowCloseAdOnClick);
            _debugLayoutNative = defines.Contains(DefineDebugLayoutNative);
            _multiAppOpenAd = defines.Contains(DefineMultiAppOpenAd);
        }

        private void ApplyFieldVisibilityDefines()
        {
            var desiredState = new Dictionary<string, bool>
            {
                [DefineShowAdScaleBtn] = _showAdScaleBtn,
                [DefineShowAdRefreshTime] = _showAdRefreshTime,
                [DefineShowReloadOnClick] = _showReloadOnClick,
                [DefineShowCloseAdOnClick] = _showCloseAdOnClick,
                [DefineDebugLayoutNative] = _debugLayoutNative,
                [DefineMultiAppOpenAd] = _multiAppOpenAd
            };

            foreach (var group in TargetGroups)
            {
                var defines = GetDefineSymbols(group);

                foreach (var kvp in desiredState)
                {
                    if (kvp.Value)
                        defines.Add(kvp.Key);
                    else
                        defines.Remove(kvp.Key);
                }

                PlayerSettings.SetScriptingDefineSymbols(group, string.Join(";", defines));
            }

            EditorUtility.DisplayDialog("Raccoon Ads Setup",
                "Đã cập nhật Field Visibility cho RaccoonAdsManager (Android / iOS / Standalone).", "OK");
        }

        #endregion

        #region Git Dependency Shared

        private enum GitReleaseBranch
        {
            Release,
            BetaTest,
            AlphaTest
        }

        private static string BuildReleaseGitRef(GitReleaseBranch branch, string version)
        {
            var branchName = branch switch
            {
                GitReleaseBranch.Release => "release",
                GitReleaseBranch.BetaTest => "beta_test",
                GitReleaseBranch.AlphaTest => "alpha_test",
                _ => "release"
            };

            if (string.IsNullOrWhiteSpace(version))
                return branchName;

            var trimmed = version.Trim();
            if (branch == GitReleaseBranch.BetaTest)
                return trimmed.EndsWith("-beta", StringComparison.OrdinalIgnoreCase) ? trimmed : $"{trimmed}-beta";
            if (branch == GitReleaseBranch.AlphaTest)
                return trimmed.EndsWith("-alpha", StringComparison.OrdinalIgnoreCase) ? trimmed : $"{trimmed}-alpha";

            return trimmed;
        }

        private static void UpdatePackageViaGit(string gitUrl, string displayName)
        {
            Debug.Log($"[RaccoonAds] Đang yêu cầu Package Manager cập nhật {displayName}: {gitUrl}");

            var request = Client.Add(gitUrl);
            EditorApplication.CallbackFunction handler = null;
            handler = () =>
            {
                if (!request.IsCompleted) return;
                EditorApplication.update -= handler;

                if (request.Status == StatusCode.Success)
                {
                    Debug.Log($"[RaccoonAds] Đã cập nhật {displayName}: {request.Result.packageId}");
                    EditorUtility.DisplayDialog("Raccoon Ads Setup",
                        $"Đã cập nhật {displayName} qua Package Manager:\n{request.Result.packageId}", "OK");
                }
                else
                {
                    Debug.LogError($"[RaccoonAds] Cập nhật {displayName} thất bại: {request.Error?.message}");
                    EditorUtility.DisplayDialog("Raccoon Ads Setup",
                        $"Cập nhật {displayName} thất bại:\n{request.Error?.message}", "OK");
                }
            };
            EditorApplication.update += handler;
        }

        #endregion

        #region JNative Git Dependency

        private const string JNativePackageName = "com.raccoon.jnativeads";
        private const string JNativeGitUrlSsh = "git@github.com:raccoon-studio-tripglobal/jnativeadsplugin.git";
        private const string JNativeGitUrlHttps = "https://github.com/raccoon-studio-tripglobal/jnativeadsplugin.git";

        private GitReleaseBranch _jnativeBranch = GitReleaseBranch.Release;
        private string _jnativeVersion = string.Empty;
        private bool _jnativeUseSsh = true;

        private void AddGitJNative(bool showDialog = true)
        {
            var gitRef = BuildReleaseGitRef(_jnativeBranch, _jnativeVersion);
            var baseUrl = _jnativeUseSsh ? JNativeGitUrlSsh : JNativeGitUrlHttps;
            var gitUrl = $"{baseUrl}#{gitRef}";

            if (!File.Exists(ManifestPath))
            {
                Debug.LogError($"[RaccoonAds] Không tìm thấy manifest.json tại: {ManifestPath}");
                return;
            }

            var json = File.ReadAllText(ManifestPath);
            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> manifest))
            {
                Debug.LogError("[RaccoonAds] manifest.json không đúng định dạng JSON.");
                return;
            }

            if (!(manifest.TryGetValue("dependencies", out var dependenciesObj) &&
                  dependenciesObj is Dictionary<string, object> dependencies))
            {
                dependencies = new Dictionary<string, object>();
                manifest["dependencies"] = dependencies;
            }

            dependencies[JNativePackageName] = gitUrl;

            File.WriteAllText(ManifestPath, MiniJson.Serialize(manifest));
            AssetDatabase.Refresh();

            if (showDialog)
                EditorUtility.DisplayDialog("Raccoon Ads Setup",
                    $"Đã cập nhật Packages/manifest.json\n{JNativePackageName} = {gitUrl}", "OK");
        }

        private void UpdateJNativePackage()
        {
            var gitRef = BuildReleaseGitRef(_jnativeBranch, _jnativeVersion);
            var baseUrl = _jnativeUseSsh ? JNativeGitUrlSsh : JNativeGitUrlHttps;
            UpdatePackageViaGit($"{baseUrl}#{gitRef}", $"JNative ({JNativePackageName})");
        }

        #endregion

        #region RaccoonAds Package

        private const string RaccoonAdsPackageName = "com.raccoon.adpack";
        private const string RaccoonAdsGitUrlSsh = "git@github.com:raccoon-studio-tripglobal/raccoon-ads-kit.git";
        private const string RaccoonAdsGitUrlHttps = "https://github.com/raccoon-studio-tripglobal/raccoon-ads-kit.git";

        private GitReleaseBranch _raccoonAdsBranch = GitReleaseBranch.Release;
        private string _raccoonAdsVersion = string.Empty;
        private bool _raccoonAdsUseSsh = true;

        private void AddGitRaccoonAds()
        {
            var gitRef = BuildReleaseGitRef(_raccoonAdsBranch, _raccoonAdsVersion);
            var baseUrl = _raccoonAdsUseSsh ? RaccoonAdsGitUrlSsh : RaccoonAdsGitUrlHttps;
            var gitUrl = $"{baseUrl}#{gitRef}";

            if (!File.Exists(ManifestPath))
            {
                Debug.LogError($"[RaccoonAds] Không tìm thấy manifest.json tại: {ManifestPath}");
                return;
            }

            var json = File.ReadAllText(ManifestPath);
            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> manifest))
            {
                Debug.LogError("[RaccoonAds] manifest.json không đúng định dạng JSON.");
                return;
            }

            if (!(manifest.TryGetValue("dependencies", out var dependenciesObj) &&
                  dependenciesObj is Dictionary<string, object> dependencies))
            {
                dependencies = new Dictionary<string, object>();
                manifest["dependencies"] = dependencies;
            }

            dependencies[RaccoonAdsPackageName] = gitUrl;

            File.WriteAllText(ManifestPath, MiniJson.Serialize(manifest));
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Raccoon Ads Setup",
                $"Đã cập nhật Packages/manifest.json\n{RaccoonAdsPackageName} = {gitUrl}", "OK");
        }

        private void UpdateRaccoonAdsPackage()
        {
            var gitRef = BuildReleaseGitRef(_raccoonAdsBranch, _raccoonAdsVersion);
            var baseUrl = _raccoonAdsUseSsh ? RaccoonAdsGitUrlSsh : RaccoonAdsGitUrlHttps;
            UpdatePackageViaGit($"{baseUrl}#{gitRef}", $"RaccoonAds ({RaccoonAdsPackageName})");
        }

        #endregion

        #region AdMob

        private class AdMobMediationInfo
        {
            public string Name;
            public string DisplayName;
            public string Version;
            public bool Enabled;
            public string CustomVersion;

            public string EffectiveVersion =>
                string.IsNullOrWhiteSpace(CustomVersion) ? Version : CustomVersion.Trim();
        }

        private static string AdMobMediationConfigPath =>
            Path.Combine(GetThisScriptDirectory(), "AdMobMediationConfig.json");

        private string _adMobCoreName;
        private string _adMobCoreVersion;
        private string _adMobCoreVersionOverride = string.Empty;
        private List<AdMobMediationInfo> _adMobMediations;
        private Vector2 _adMobMediationScroll;

        private void LoadAdMobMediationConfig()
        {
            _adMobMediations = null;

            if (!File.Exists(AdMobMediationConfigPath))
            {
                Debug.LogError($"[RaccoonAds] Không tìm thấy file config: {AdMobMediationConfigPath}");
                return;
            }

            if (!(MiniJson.Deserialize(File.ReadAllText(AdMobMediationConfigPath)) is Dictionary<string, object> config))
            {
                Debug.LogError("[RaccoonAds] AdMobMediationConfig.json không đúng định dạng JSON.");
                return;
            }

            var installedDependencies = ReadManifestDependencies();

            if (config.TryGetValue("core", out var coreObj) && coreObj is Dictionary<string, object> core)
            {
                _adMobCoreName = core.TryGetValue("name", out var nameObj) ? nameObj as string : string.Empty;
                _adMobCoreVersion = core.TryGetValue("version", out var verObj) ? verObj as string : string.Empty;
            }

            var mediations = new List<AdMobMediationInfo>();
            if (config.TryGetValue("mediations", out var mediationsObj) && mediationsObj is List<object> mediationList)
            {
                foreach (var item in mediationList.OfType<Dictionary<string, object>>())
                {
                    var mediationName = item.TryGetValue("name", out var n) ? n as string : null;
                    if (string.IsNullOrEmpty(mediationName)) continue;

                    mediations.Add(new AdMobMediationInfo
                    {
                        Name = mediationName,
                        DisplayName = item.TryGetValue("displayName", out var d) ? d as string : mediationName,
                        Version = item.TryGetValue("version", out var v) ? v as string : string.Empty,
                        Enabled = installedDependencies.Contains(mediationName),
                        CustomVersion = string.Empty
                    });
                }
            }

            _adMobMediations = mediations;
        }

        private static HashSet<string> ReadManifestDependencies()
        {
            if (!File.Exists(ManifestPath)) return new HashSet<string>();

            if (!(MiniJson.Deserialize(File.ReadAllText(ManifestPath)) is Dictionary<string, object> manifest))
                return new HashSet<string>();

            if (!(manifest.TryGetValue("dependencies", out var depsObj) && depsObj is Dictionary<string, object> deps))
                return new HashSet<string>();

            return new HashSet<string>(deps.Keys);
        }

        private void AddAdMob(bool showDialog = true)
        {
            if (_adMobMediations == null)
            {
                Debug.LogError("[RaccoonAds] Chưa load được AdMob mediation config.");
                return;
            }

            if (!File.Exists(ManifestPath))
            {
                Debug.LogError($"[RaccoonAds] Không tìm thấy manifest.json tại: {ManifestPath}");
                return;
            }

            var json = File.ReadAllText(ManifestPath);
            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> manifest))
            {
                Debug.LogError("[RaccoonAds] manifest.json không đúng định dạng JSON.");
                return;
            }

            if (!(manifest.TryGetValue("dependencies", out var dependenciesObj) &&
                  dependenciesObj is Dictionary<string, object> dependencies))
            {
                dependencies = new Dictionary<string, object>();
                manifest["dependencies"] = dependencies;
            }

            var effectiveCoreVersion = string.IsNullOrWhiteSpace(_adMobCoreVersionOverride)
                ? _adMobCoreVersion
                : _adMobCoreVersionOverride.Trim();
            dependencies[_adMobCoreName] = effectiveCoreVersion;

            foreach (var mediation in _adMobMediations)
            {
                if (mediation.Enabled)
                    dependencies[mediation.Name] = mediation.EffectiveVersion;
                else
                    dependencies.Remove(mediation.Name);
            }

            File.WriteAllText(ManifestPath, MiniJson.Serialize(manifest));
            AssetDatabase.Refresh();

            if (showDialog)
            {
                var enabledSummary = string.Join("\n",
                    _adMobMediations.Where(m => m.Enabled).Select(m => $"- {m.Name} @ {m.EffectiveVersion}"));
                EditorUtility.DisplayDialog("Raccoon Ads Setup",
                    $"Đã cập nhật Packages/manifest.json\nCore: {_adMobCoreName} @ {effectiveCoreVersion}\nMediation:\n{enabledSummary}",
                    "OK");
            }
        }

        #endregion

        #region Firebase

        private static string FirebaseModulesConfigPath =>
            Path.Combine(GetThisScriptDirectory(), "FirebaseModulesConfig.json");

        private string _firebaseGitUrl;
        private string _firebaseCoreName;
        private string _firebaseCoreVersion;
        private List<AdMobMediationInfo> _firebaseModules;

        private void LoadFirebaseModulesConfig()
        {
            _firebaseModules = null;

            if (!File.Exists(FirebaseModulesConfigPath))
            {
                Debug.LogError($"[RaccoonAds] Không tìm thấy file config: {FirebaseModulesConfigPath}");
                return;
            }

            if (!(MiniJson.Deserialize(File.ReadAllText(FirebaseModulesConfigPath)) is Dictionary<string, object> config))
            {
                Debug.LogError("[RaccoonAds] FirebaseModulesConfig.json không đúng định dạng JSON.");
                return;
            }

            var installedDependencies = ReadManifestDependencies();

            _firebaseGitUrl = config.TryGetValue("gitUrl", out var gitUrlObj) ? gitUrlObj as string : string.Empty;

            if (config.TryGetValue("core", out var coreObj) && coreObj is Dictionary<string, object> core)
            {
                _firebaseCoreName = core.TryGetValue("name", out var nameObj) ? nameObj as string : string.Empty;
                _firebaseCoreVersion = core.TryGetValue("ref", out var refObj) ? refObj as string : string.Empty;
            }

            var modules = new List<AdMobMediationInfo>();
            if (config.TryGetValue("modules", out var modulesObj) && modulesObj is List<object> moduleList)
            {
                foreach (var item in moduleList.OfType<Dictionary<string, object>>())
                {
                    var moduleName = item.TryGetValue("name", out var n) ? n as string : null;
                    if (string.IsNullOrEmpty(moduleName)) continue;

                    modules.Add(new AdMobMediationInfo
                    {
                        Name = moduleName,
                        DisplayName = item.TryGetValue("displayName", out var d) ? d as string : moduleName,
                        Version = item.TryGetValue("ref", out var v) ? v as string : string.Empty,
                        Enabled = installedDependencies.Contains(moduleName),
                        CustomVersion = string.Empty
                    });
                }
            }

            _firebaseModules = modules;
        }

        private void AddFirebase(bool showDialog = true)
        {
            if (_firebaseModules == null)
            {
                Debug.LogError("[RaccoonAds] Chưa load được Firebase modules config.");
                return;
            }

            if (!File.Exists(ManifestPath))
            {
                Debug.LogError($"[RaccoonAds] Không tìm thấy manifest.json tại: {ManifestPath}");
                return;
            }

            var json = File.ReadAllText(ManifestPath);
            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> manifest))
            {
                Debug.LogError("[RaccoonAds] manifest.json không đúng định dạng JSON.");
                return;
            }

            if (!(manifest.TryGetValue("dependencies", out var dependenciesObj) &&
                  dependenciesObj is Dictionary<string, object> dependencies))
            {
                dependencies = new Dictionary<string, object>();
                manifest["dependencies"] = dependencies;
            }

            dependencies[_firebaseCoreName] = $"{_firebaseGitUrl}#{_firebaseCoreVersion}";

            foreach (var module in _firebaseModules)
            {
                if (module.Enabled)
                    dependencies[module.Name] = $"{_firebaseGitUrl}#{module.Version}";
                else
                    dependencies.Remove(module.Name);
            }

            File.WriteAllText(ManifestPath, MiniJson.Serialize(manifest));
            AssetDatabase.Refresh();

            if (showDialog)
            {
                var enabledSummary = string.Join("\n",
                    _firebaseModules.Where(m => m.Enabled).Select(m => $"- {m.Name} @ {m.Version}"));
                EditorUtility.DisplayDialog("Raccoon Ads Setup",
                    $"Đã cập nhật Packages/manifest.json\nCore: {_firebaseCoreName} @ {_firebaseCoreVersion}\nModules:\n{enabledSummary}",
                    "OK");
            }
        }

        #endregion

        #region AppsFlyer Git Dependency

        private const string AppsFlyerPackageName = "appsflyer-unity-plugin";
        private const string AppsFlyerGitUrl = "https://github.com/AppsFlyerSDK/appsflyer-unity-plugin.git";
        private const string AppsFlyerDefaultRef = "v6.18.1";

        private string _appsflyerVersion = string.Empty;
        private string _appsflyerAppIdInput = string.Empty;

        private static string BuildAppsFlyerGitRef(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return AppsFlyerDefaultRef;
            else
            {
                var trimmed = version.Trim();
                return trimmed.StartsWith("v") ? trimmed : "v" + trimmed;
            }
        }

        private void AddGitAppsFlyer(bool showDialog = true)
        {
            var gitRef = BuildAppsFlyerGitRef(_appsflyerVersion);
            var gitUrl = $"{AppsFlyerGitUrl}#{gitRef}";

            if (!File.Exists(ManifestPath))
            {
                Debug.LogError($"[RaccoonAds] Không tìm thấy manifest.json tại: {ManifestPath}");
                return;
            }

            var json = File.ReadAllText(ManifestPath);
            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> manifest))
            {
                Debug.LogError("[RaccoonAds] manifest.json không đúng định dạng JSON.");
                return;
            }

            if (!(manifest.TryGetValue("dependencies", out var dependenciesObj) &&
                  dependenciesObj is Dictionary<string, object> dependencies))
            {
                dependencies = new Dictionary<string, object>();
                manifest["dependencies"] = dependencies;
            }

            dependencies[AppsFlyerPackageName] = gitUrl;

            File.WriteAllText(ManifestPath, MiniJson.Serialize(manifest));
            AssetDatabase.Refresh();

            if (showDialog)
                EditorUtility.DisplayDialog("Raccoon Ads Setup",
                    $"Đã cập nhật Packages/manifest.json\n{AppsFlyerPackageName} = {gitUrl}", "OK");
        }

        #endregion

        #region AppsFlyer Component Setup (GameAppsFlyer)

        private void DrawAppsFlyerSetupSection()
        {
            EditorGUILayout.Space(24);
            EditorGUILayout.HelpBox(
                "Setup GameAppsFlyer trên scene: tìm GameObject chứa RaccoonAdsManager trong scene hiện tại, " +
                "add (hoặc lấy nếu đã có) component GameAppsFlyer, rồi set AppID.\n" +
                "- Nếu nhập AppID bên dưới: dùng đúng giá trị đã nhập.\n" +
                "- Nếu để trống: Android tự lấy package name (Player Settings); iOS sẽ yêu cầu nhập AppID.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            _appsflyerAppIdInput = EditorGUILayout.TextField(
                new GUIContent("AppID", "Android: để trống để tự lấy package name. iOS: bắt buộc nhập App Store ID."),
                _appsflyerAppIdInput);

            EditorGUILayout.Space(4);
            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.55f, 0.85f, 0.55f);
            if (GUILayout.Button("Setup AppsFlyer (One Click)", GUILayout.Height(48)))
                SetupAppsFlyerOneClick();
            GUI.backgroundColor = previousColor;
        }

        private void SetupAppsFlyerOneClick()
        {
#if ENABLE_RACCOON_ADMOB
            var manager = FindObjectOfType<RaccoonAdsManager>();
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Raccoon Ads Setup",
                    "Không tìm thấy GameObject nào chứa RaccoonAdsManager trong scene hiện tại. Chưa thể setup AppsFlyer.",
                    "OK");
                return;
            }

            var go = manager.gameObject;

#if ENABLE_APPSFLYER
            var appsFlyer = go.GetComponent<GameAppsFlyer>();
            if (appsFlyer == null)
                appsFlyer = Undo.AddComponent<GameAppsFlyer>(go);

            if (!string.IsNullOrWhiteSpace(_appsflyerAppIdInput))
            {
                Undo.RecordObject(appsFlyer, "Set AppsFlyer AppID");
                appsFlyer.appID = _appsflyerAppIdInput.Trim();
            }
            else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                EditorUtility.DisplayDialog("Raccoon Ads Setup",
                    "Target hiện tại là iOS: cần nhập AppID (App Store ID) vào ô AppID rồi bấm lại Setup AppsFlyer.",
                    "OK");
                return;
            }
            else
            {
                Undo.RecordObject(appsFlyer, "Set AppsFlyer AppID");
                appsFlyer.appID = PlayerSettings.applicationIdentifier;
            }

            EditorUtility.SetDirty(appsFlyer);
            EditorSceneManager.MarkSceneDirty(go.scene);

            EditorUtility.DisplayDialog("Raccoon Ads Setup",
                $"Đã setup GameAppsFlyer trên GameObject '{go.name}'.\nAppID: {appsFlyer.appID}",
                "OK");
#else
            EditorUtility.DisplayDialog("Raccoon Ads Setup",
                "Cần bật Scripting Define Symbol ENABLE_APPSFLYER trước khi setup AppsFlyer (mục Scripting Define Symbols phía trên).",
                "OK");
#endif
#else
            EditorUtility.DisplayDialog("Raccoon Ads Setup",
                "Cần bật Scripting Define Symbol ENABLE_RACCOON_ADMOB trước khi setup AppsFlyer (mục Scripting Define Symbols phía trên).",
                "OK");
#endif
        }

        #endregion

        #region Facebook Git Dependency

        private const string FacebookPackageName = "com.raccoon.facebooksdk";
        private const string FacebookGitUrl = "https://github.com/namtuoc91/unity_facebook_upm.git";
        private const string FacebookDefaultRef = "release";

        private void AddGitFacebook()
        {
            var gitUrl = $"{FacebookGitUrl}#{FacebookDefaultRef}";

            if (!File.Exists(ManifestPath))
            {
                Debug.LogError($"[RaccoonAds] Không tìm thấy manifest.json tại: {ManifestPath}");
                return;
            }

            var json = File.ReadAllText(ManifestPath);
            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> manifest))
            {
                Debug.LogError("[RaccoonAds] manifest.json không đúng định dạng JSON.");
                return;
            }

            if (!(manifest.TryGetValue("dependencies", out var dependenciesObj) &&
                  dependenciesObj is Dictionary<string, object> dependencies))
            {
                dependencies = new Dictionary<string, object>();
                manifest["dependencies"] = dependencies;
            }

            dependencies[FacebookPackageName] = gitUrl;

            File.WriteAllText(ManifestPath, MiniJson.Serialize(manifest));
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Raccoon Ads Setup",
                $"Đã cập nhật Packages/manifest.json\n{FacebookPackageName} = {gitUrl}", "OK");
        }

        private void UpdateFacebookPackage()
        {
            UpdatePackageViaGit($"{FacebookGitUrl}#{FacebookDefaultRef}", $"Facebook SDK ({FacebookPackageName})");
        }

        #endregion

        #region Base Setup (all-in-one)

        private const string MetaAudienceNetworkMediationName = "com.google.ads.mobile.mediation.metaaudiencenetwork";
        private const string FirebaseAnalyticsModuleName = "com.google.firebase.analytics";
        private const string FirebaseCrashlyticsModuleName = "com.google.firebase.crashlytics";

        private void DrawBaseSetupSection()
        {
            EditorGUILayout.Space(24);
            EditorGUILayout.HelpBox(
                "Chạy toàn bộ setup cơ bản theo thứ tự: Add Scope UPM -> Add Define (AdMob, JNative, AppsFlyer) -> " +
                "Add Git JNative (tự nhận branch release/beta theo package RaccoonAds hiện tại) -> Add AdMob + Meta mediation -> " +
                "Add Firebase (Analytics + Crashlytics) -> Add Git AppsFlyer.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.55f, 0.85f, 0.55f);
            if (GUILayout.Button("Base Setup", GUILayout.Height(48)))
                RunBaseSetup();
            GUI.backgroundColor = previousColor;
        }

        private GitReleaseBranch DetectCurrentRaccoonAdsBranch()
        {
            var dependencies = ReadManifestGitRefs();
            if (dependencies.TryGetValue(RaccoonAdsPackageName, out var gitUrl) && !string.IsNullOrEmpty(gitUrl))
            {
                var hashIndex = gitUrl.LastIndexOf('#');
                var gitRef = hashIndex >= 0 ? gitUrl.Substring(hashIndex + 1) : gitUrl;

                if (gitRef.IndexOf("alpha", StringComparison.OrdinalIgnoreCase) >= 0)
                    return GitReleaseBranch.AlphaTest;
                if (gitRef.IndexOf("beta", StringComparison.OrdinalIgnoreCase) >= 0)
                    return GitReleaseBranch.BetaTest;
            }

            return GitReleaseBranch.Release;
        }

        private static Dictionary<string, string> ReadManifestGitRefs()
        {
            var result = new Dictionary<string, string>();
            if (!File.Exists(ManifestPath)) return result;

            if (!(MiniJson.Deserialize(File.ReadAllText(ManifestPath)) is Dictionary<string, object> manifest))
                return result;

            if (!(manifest.TryGetValue("dependencies", out var depsObj) && depsObj is Dictionary<string, object> deps))
                return result;

            foreach (var kvp in deps)
                if (kvp.Value is string strValue)
                    result[kvp.Key] = strValue;

            return result;
        }

        private void RunBaseSetup()
        {
            if (!EditorUtility.DisplayDialog("Raccoon Ads Setup",
                    "Base Setup sẽ chạy toàn bộ các bước setup (scope, define, JNative, AdMob, Firebase, AppsFlyer) và ghi đè Packages/manifest.json. Tiếp tục?",
                    "Chạy Base Setup", "Huỷ"))
                return;

            // 1. Scope UPM
            AddScopeUpm(false);

            // 2. Scripting define symbols
            _admobEnabled = true;
            _jnativeEnabled = true;
            _appsflyerDefineEnabled = true;
            ApplyScriptingDefineSymbols(false);

            // 3. Git JNative - tự nhận branch theo package RaccoonAds hiện tại
            _jnativeBranch = DetectCurrentRaccoonAdsBranch();
            _jnativeVersion = string.Empty;
            AddGitJNative(false);

            // 4. AdMob + Meta mediation
            if (_adMobMediations == null) LoadAdMobMediationConfig();
            if (_adMobMediations != null)
            {
                foreach (var mediation in _adMobMediations)
                    if (mediation.Name == MetaAudienceNetworkMediationName)
                        mediation.Enabled = true;
                AddAdMob(false);
            }
            else
            {
                Debug.LogError("[RaccoonAds] Base Setup: bỏ qua AdMob vì không đọc được AdMobMediationConfig.json.");
            }

            // 5. Firebase (Analytics + Crashlytics)
            if (_firebaseModules == null) LoadFirebaseModulesConfig();
            if (_firebaseModules != null)
            {
                foreach (var module in _firebaseModules)
                    if (module.Name == FirebaseAnalyticsModuleName || module.Name == FirebaseCrashlyticsModuleName)
                        module.Enabled = true;
                AddFirebase(false);
            }
            else
            {
                Debug.LogError("[RaccoonAds] Base Setup: bỏ qua Firebase vì không đọc được FirebaseModulesConfig.json.");
            }

            // 6. Git AppsFlyer
            _appsflyerVersion = string.Empty;
            AddGitAppsFlyer(false);

            // Ép Package Manager resolve lại manifest.json vừa ghi đè nhiều lần ở trên,
            // chỉ báo "hoàn tất" sau khi Package Manager resolve xong (Client.Resolve() trả về void,
            // nên phải nghe qua Events.registeredPackages thay vì poll IsCompleted như Client.Add).
            Action<PackageRegistrationEventArgs> registrationHandler = null;
            registrationHandler = args =>
            {
                Events.registeredPackages -= registrationHandler;
                EditorUtility.DisplayDialog("Raccoon Ads Setup", "Base Setup hoàn tất.", "OK");
            };
            Events.registeredPackages += registrationHandler;
            Client.Resolve();
        }

        #endregion
    }
}
