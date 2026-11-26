using System.Collections.Generic;
using UnityEngine;

/// Danh sách level theo thứ tự chơi, dùng chung cho HomeScene và GamePlay.
/// Chỉ chứa LevelRef (đường dẫn), nên load catalog không load ảnh của level nào.
[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Scriptable Objects/LevelCatalog")]
public class LevelCatalog : ScriptableObject
{
    [SerializeField] private LevelRef[] levels = new LevelRef[0];
    [SerializeField] private LevelRef tutorial;

    public int Count => levels == null ? 0 : levels.Length;
    public LevelRef Tutorial => tutorial;
    public LevelRef this[int index] => levels[index];

#if UNITY_EDITOR
    public int EditorIndexOf(LevelData level)
    {
        string guid = LevelRef.FromAsset(level).Guid;
        if (string.IsNullOrEmpty(guid) || levels == null) return -1;
        for (int i = 0; i < levels.Length; i++)
            if (levels[i].Guid == guid) return i;
        return -1;
    }

    /// Ghi lại danh sách level (bỏ qua level tutorial và level nằm ngoài Resources).
    /// Trả về số level KHÔNG ghi được vì không nằm trong thư mục Resources.
    public int EditorSetLevels(IList<LevelData> list)
    {
        var refs = new List<LevelRef>();
        int skipped = 0;
        foreach (LevelData level in list)
        {
            LevelRef r = LevelRef.FromAsset(level);
            if (!r.IsValid) { skipped++; continue; }
            if (tutorial.IsValid && r.Guid == tutorial.Guid) continue;
            refs.Add(r);
        }
        levels = refs.ToArray();
        UnityEditor.EditorUtility.SetDirty(this);
        return skipped;
    }
#endif
}
