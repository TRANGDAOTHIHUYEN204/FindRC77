using System;
using UnityEngine;

/// Tham chiếu "mềm" tới một LevelData nằm trong thư mục Resources.
/// Chỉ lưu đường dẫn (không phải object reference), nên scene/asset giữ LevelRef
/// KHÔNG kéo ảnh A/B của level vào RAM lúc load scene; ảnh chỉ được load khi gọi Load().
/// Trên máy 32-bit 2-3GB RAM, giữ cả 26 level (~52 ảnh 1536x1024) trong RAM là nguyên nhân
/// góp phần gây crash native khi thiếu bộ nhớ.
[Serializable]
public struct LevelRef
{
    [SerializeField] private string assetGuid;
    [SerializeField] private string path;

    public string Path => path;
    public bool IsValid => !string.IsNullOrEmpty(path);

    public LevelData Load()
    {
        if (!IsValid) return null;
        LevelData data = Resources.Load<LevelData>(path);
        if (data == null) Debug.LogError($"[Level] Không tìm thấy LevelData tại Resources/{path}");
        return data;
    }

#if UNITY_EDITOR
    public string Guid => assetGuid;

    /// Tạo LevelRef từ asset. Trả về ref rỗng nếu asset không nằm trong thư mục Resources.
    public static LevelRef FromAsset(LevelData level)
    {
        var result = new LevelRef();
        if (level == null) return result;

        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(level);
        if (!TryGetResourcePath(assetPath, out string resourcePath)) return result;

        result.assetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
        result.path = resourcePath;
        return result;
    }

    /// "Assets/SOData/Resources/Levels/Level_01_Data.asset" -> "Levels/Level_01_Data"
    public static bool TryGetResourcePath(string assetPath, out string resourcePath)
    {
        resourcePath = null;
        if (string.IsNullOrEmpty(assetPath)) return false;

        const string marker = "/Resources/";
        int i = assetPath.LastIndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return false;

        string relative = assetPath.Substring(i + marker.Length);
        resourcePath = relative.Substring(0, relative.Length - System.IO.Path.GetExtension(relative).Length);
        return resourcePath.Length > 0;
    }
#endif
}
