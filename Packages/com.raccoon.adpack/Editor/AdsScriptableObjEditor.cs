using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RCKit.Ads.Main.Editor
{
    [CustomEditor(typeof(AdsScriptableObj))]
    public class AdsScriptableObjEditor : UnityEditor.Editor
    {
        private enum AdIdPlatformView { All, Android, iOS }

        private SerializedProperty _useTestAdProp;
        private SerializedProperty _adUnitConfigsProp;
        private AdIdPlatformView _platformView;

        private void OnEnable()
        {
            _useTestAdProp = serializedObject.FindProperty("use_test_ad");
            _adUnitConfigsProp = serializedObject.FindProperty("AdUnitConfigs");
            _platformView = EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS
                ? AdIdPlatformView.iOS
                : AdIdPlatformView.Android;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_useTestAdProp, new GUIContent("Use Test Ad"));
            if (_useTestAdProp.boolValue)
                EditorGUILayout.HelpBox(
                    "Đang bật Use Test Ad: lúc runtime ad_id sẽ tự đổi sang test ad unit id của AdMob, giá trị ad_id khai báo dưới đây không được dùng.",
                    MessageType.Info);

            _platformView = (AdIdPlatformView)EditorGUILayout.EnumPopup("View Ad Id For", _platformView);

            EditorGUILayout.Space();
            DrawDuplicateWarning();

            EditorGUILayout.LabelField($"Ad Units ({_adUnitConfigsProp.arraySize})", EditorStyles.boldLabel);

            var removeIndex = -1;
            for (var i = 0; i < _adUnitConfigsProp.arraySize; i++)
            {
                if (DrawAdUnitElement(i))
                    removeIndex = i;
            }

            if (removeIndex >= 0)
                _adUnitConfigsProp.DeleteArrayElementAtIndex(removeIndex);

            EditorGUILayout.Space();
            if (GUILayout.Button("+ Add Ad Unit"))
            {
                var wasEmpty = _adUnitConfigsProp.arraySize == 0;
                var newIndex = _adUnitConfigsProp.arraySize;
                _adUnitConfigsProp.InsertArrayElementAtIndex(newIndex);
                var newElement = _adUnitConfigsProp.GetArrayElementAtIndex(newIndex);
                newElement.isExpanded = true;

                // InsertArrayElementAtIndex trên array rỗng tạo phần tử "zero" thô (0/false),
                // không chạy qua field initializer của AdUnitConfig, nên phải set lại default thủ công.
                if (wasEmpty)
                {
                    newElement.FindPropertyRelative("reload_on_fail").boolValue = true;
                    // newElement.FindPropertyRelative("time_close_native").intValue = 3;
                    newElement.FindPropertyRelative("layout_support_media").boolValue = true;
                    newElement.FindPropertyRelative("has_close").boolValue = true;
                    newElement.FindPropertyRelative("use_only_portrait").boolValue = false;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        // Trả về true nếu người dùng bấm nút Remove của entry này (xóa được thực hiện sau khi
        // vẽ xong toàn bộ list, tránh làm lệch layout GUI khi xóa ngay trong lúc đang vẽ).
        private bool DrawAdUnitElement(int index)
        {
            var element = _adUnitConfigsProp.GetArrayElementAtIndex(index);
            var adTypeProp = element.FindPropertyRelative("ad_type");
            var adIdAndroidProp = element.FindPropertyRelative("ad_id_android");
            var adIdIosProp = element.FindPropertyRelative("ad_id_ios");
            var isMultiAdProp = element.FindPropertyRelative("is_multi_ad");
            var adIdAndroid2Prop = element.FindPropertyRelative("ad_id_android_2");
            var adIdIos2Prop = element.FindPropertyRelative("ad_id_ios_2");
            var removableProp = element.FindPropertyRelative("removable");
            var preloadAdProp = element.FindPropertyRelative("preload_ad");
            var reloadOnFailProp = element.FindPropertyRelative("reload_on_fail");
            var timeCloseProp = element.FindPropertyRelative("time_close_native");
            var layoutQuitProp = element.FindPropertyRelative("layout_quit_native");
            var layoutTemplateProp = element.FindPropertyRelative("layout_template_native");
            var layoutSupportMediaProp = element.FindPropertyRelative("layout_support_media");
            var hasCloseProp = element.FindPropertyRelative("has_close");
            var useOnlyPortraitProp = element.FindPropertyRelative("use_only_portrait");

            var adType = GetEnumValue(adTypeProp);
            var isNative = IsNative(adType);
            var isBannerFamily = IsBannerFamily(adType);
            var isBannerCollapseFamily = IsBannerCollapseFamily(adType);
            var supportsPreloadAd = !isBannerFamily;

            var didRemove = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, $"[{index}] {adType}", true);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
                didRemove = true;
            EditorGUILayout.EndHorizontal();

            if (element.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(adTypeProp);
                if (_platformView is AdIdPlatformView.All or AdIdPlatformView.Android)
                    EditorGUILayout.PropertyField(adIdAndroidProp, new GUIContent("Ad Id (Android)"));
                if (_platformView is AdIdPlatformView.All or AdIdPlatformView.iOS)
                    EditorGUILayout.PropertyField(adIdIosProp, new GUIContent("Ad Id (iOS)"));

                if (isNative && !isBannerFamily)
                {
                    EditorGUILayout.PropertyField(isMultiAdProp, new GUIContent("Is Multi Ad",
                        "Bật thêm 1 ad id thứ 2 cho cùng ad_type này."));
                    if (isMultiAdProp.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        if (_platformView is AdIdPlatformView.All or AdIdPlatformView.Android)
                        {
                            EditorGUILayout.PropertyField(adIdAndroid2Prop, new GUIContent("Ad Id 2 (Android)"));
                            if (string.IsNullOrEmpty(adIdAndroid2Prop.stringValue))
                                EditorGUILayout.HelpBox("Ad Id 2 (Android) không được để trống khi bật Is Multi Ad.",
                                    MessageType.Error);
                        }
                        if (_platformView is AdIdPlatformView.All or AdIdPlatformView.iOS)
                        {
                            EditorGUILayout.PropertyField(adIdIos2Prop, new GUIContent("Ad Id 2 (iOS)"));
                            if (string.IsNullOrEmpty(adIdIos2Prop.stringValue))
                                EditorGUILayout.HelpBox("Ad Id 2 (iOS) không được để trống khi bật Is Multi Ad.",
                                    MessageType.Error);
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.PropertyField(removableProp);

                if (supportsPreloadAd)
                {
                    EditorGUILayout.PropertyField(preloadAdProp, new GUIContent("Preload Ad",
                        "Tự reload ad ngay sau khi show xong, áp dụng cho mọi loại ad trừ họ Banner."));
                }

                EditorGUILayout.PropertyField(reloadOnFailProp, new GUIContent("Reload On Fail",
                    "Tự động load lại khi load ad thất bại. Banner luôn tự reload nên không cấu hình được."));

                if (isNative && isBannerCollapseFamily)
                {
                    EditorGUILayout.PropertyField(hasCloseProp, new GUIContent("Has Close",
                        "Banner Collapse có hiện nút đóng (quit) hay không."));
                }

                if (isNative && !isBannerFamily)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Native Interstitial Layout", EditorStyles.miniBoldLabel);
                    // EditorGUILayout.PropertyField(timeCloseProp);
                    EditorGUILayout.PropertyField(useOnlyPortraitProp, new GUIContent("Use Only Portrait",
                        "Native full chỉ hiển thị ở dạng orientation portrait."));
#if RACCOON_DEBUG_LAYOUT_NATIVE
                    EditorGUILayout.PropertyField(layoutQuitProp);
                    EditorGUILayout.PropertyField(layoutTemplateProp);
                    EditorGUILayout.PropertyField(layoutSupportMediaProp);
#endif

                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            return didRemove;
        }

        private void DrawDuplicateWarning()
        {
            var seenIndices = new HashSet<int>();
            var duplicateIndices = new HashSet<int>();
            for (var i = 0; i < _adUnitConfigsProp.arraySize; i++)
            {
                var enumIndex = _adUnitConfigsProp.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("ad_type").enumValueIndex;
                if (!seenIndices.Add(enumIndex))
                    duplicateIndices.Add(enumIndex);
            }

            if (duplicateIndices.Count == 0) return;

            var names = duplicateIndices.Select(idx => GetEnumValue(idx).ToString());
            EditorGUILayout.HelpBox(
                $"Trùng AD_TYPE: {string.Join(", ", names)}. RegisterAdUnits dùng ad_type làm key nên entry sau sẽ ghi đè entry trước.",
                MessageType.Error);
        }

        // enumValueIndex là vị trí khai báo trong enum, không phải giá trị int thực (AD_TYPE
        // có các giá trị không liên tục theo dải 10/20/... như 0,1,2,10,11,...), nên phải map
        // lại qua Enum.GetValues theo đúng thứ tự khai báo.
        private static AD_TYPE GetEnumValue(SerializedProperty enumProp) => GetEnumValue(enumProp.enumValueIndex);

        private static AD_TYPE GetEnumValue(int enumValueIndex) =>
            (AD_TYPE)Enum.GetValues(typeof(AD_TYPE)).GetValue(enumValueIndex);

        private static bool IsBannerFamily(AD_TYPE type)
        {
            var value = (int)type;
            return value is >= 80 and < 100 or >= 120 and < 140;
        }

        private static bool IsBannerCollapseFamily(AD_TYPE type)
        {
            var value = (int)type;
            return value is >= 120 and < 140;
        }

        // Cùng convention với AdsScriptableObj.GetTestAdUnitId / RaccoonAdsManager.RegisterAdUnits:
        // trong mỗi family AD_TYPE, biến thể native luôn ở vị trí +10 so với giá trị gốc.
        private static bool IsNative(AD_TYPE type) => (int)type / 10 % 2 == 1;
    }
}
