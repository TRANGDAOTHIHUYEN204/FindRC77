using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemGroup
{
    public string type;

    [Min(1)] public int count = 2; 
}

public readonly struct ItemEntry
{
    public readonly string Type;
    public readonly Sprite Sprite;

    public ItemEntry(string type, Sprite sprite)
    {
        Type = type;
        Sprite = sprite;
    }
}

[CreateAssetMenu(fileName = "ItemDataThirdMiniGame", menuName = "Scriptable Objects/ItemDataThirdMiniGame")]
public class IconCollectionData : ScriptableObject
{
    [SerializeField] private List<ItemGroup> groups = new();
    [SerializeField] private List<Sprite> sprites = new();

    public int TotalCount
    {
        get
        {
            int total = 0;
            foreach (var g in groups) total += g.count;
            return total;
        }
    }
    public List<ItemEntry> BuildEntries()
    {
        var result = new List<ItemEntry>(TotalCount);
        int index = 0;

        foreach (var group in groups)
        {
            for (int i = 0; i < group.count; i++)
            {
                Sprite sprite = index < sprites.Count ? sprites[index] : null;
                result.Add(new ItemEntry(group.type, sprite));
                index++;
            }
        }
        return result;
    }
    public bool Validate(out string error)
    {
        int total = TotalCount;

        if (total == 0)
        {
            error = $"{name}: chưa có group nào";
            return false;
        }

        if (sprites.Count < total)
        {
            error = $"{name}: cần {total} sprite nhưng chỉ có {sprites.Count}";
            return false;
        }

        for (int i = 0; i < total; i++)
        {
            if (sprites[i] == null)
            {
                error = $"{name}: sprite thứ {i} đang null";
                return false;
            }
        }

        error = null;
        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Validate(out string error))
            Debug.LogWarning(error, this);
    }
#endif
}