using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Hiển thị LevelRef như một ô kéo-thả LevelData; tự lưu guid + đường dẫn Resources.
[CustomPropertyDrawer(typeof(LevelRef))]
public class LevelRefDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty guidProp = property.FindPropertyRelative("assetGuid");
        SerializedProperty pathProp = property.FindPropertyRelative("path");

        string assetPath = string.IsNullOrEmpty(guidProp.stringValue) ? null : AssetDatabase.GUIDToAssetPath(guidProp.stringValue);
        LevelData current = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);

        // Asset bị đổi tên/di chuyển trong Resources -> cập nhật lại đường dẫn.
        if (current != null && LevelRef.TryGetResourcePath(assetPath, out string resourcePath) && resourcePath != pathProp.stringValue)
            pathProp.stringValue = resourcePath;

        bool broken = !string.IsNullOrEmpty(pathProp.stringValue) && (current == null || !LevelRef.TryGetResourcePath(assetPath, out _));

        EditorGUI.BeginProperty(position, label, property);
        Color oldColor = GUI.color;
        if (broken) GUI.color = new Color(1f, 0.5f, 0.5f);

        EditorGUI.BeginChangeCheck();
        var picked = (LevelData)EditorGUI.ObjectField(position, label, current, typeof(LevelData), false);
        if (EditorGUI.EndChangeCheck())
        {
            LevelRef r = LevelRef.FromAsset(picked);
            if (picked != null && !r.IsValid)
            {
                Debug.LogError($"[Level] {picked.name} phải nằm trong thư mục Resources (vd Assets/SOData/Resources/Levels).", picked);
            }
            else
            {
                guidProp.stringValue = r.Guid ?? "";
                pathProp.stringValue = r.Path ?? "";
            }
        }

        GUI.color = oldColor;
        EditorGUI.EndProperty();
    }
}

/// Chặn build nếu LevelCatalog có level không load được bằng Resources.Load.
public class LevelCatalogBuildCheck : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:LevelCatalog"))
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guid));
            if (catalog == null) continue;

            if (catalog.Count == 0)
                throw new BuildFailedException($"[Level] {catalog.name} không có level nào.");

            for (int i = 0; i < catalog.Count; i++)
                Check(catalog, catalog[i], $"level #{i}");
            if (catalog.Tutorial.IsValid)
                Check(catalog, catalog.Tutorial, "tutorial");
        }
    }

    private static void Check(LevelCatalog catalog, LevelRef r, string what)
    {
        if (!r.IsValid || Resources.Load<LevelData>(r.Path) == null)
            throw new BuildFailedException($"[Level] {catalog.name}: {what} không load được (Resources/{r.Path}).");
    }
}
