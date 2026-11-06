using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollectTargetRegistry : MonoBehaviour
{
    [SerializeField] private ItemSlot slotPrefab; 
    [SerializeField] private RectTransform listParent; 
    [SerializeField] private int maxSlots = 5;         

    private readonly Dictionary<string, ItemSlot> map = new();
    private readonly List<ItemSlot> spawned = new();

    public void Setup(FindHiddenLevelData level)
    {
        
        Clear();

        int nextId = 1; 
        foreach (var e in level.Entries)
        {
            if (e.data == null || e.points == null) continue;
            if (maxSlots > 0 && spawned.Count >= maxSlots) break;

            var slot = Instantiate(slotPrefab, listParent);
            slot.Setup(nextId++, e.data.SpriteItem);
            slot.SetProgress(0, e.SpawnCount); 

            spawned.Add(slot);
            map[e.data.TargetId] = slot;
        }
        
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(listParent);
    }

    private void Clear()
    {
        foreach (var s in spawned)
        {
            if (s == null) continue;
            s.gameObject.SetActive(false); 
            Destroy(s.gameObject);
        }
        spawned.Clear();
        map.Clear();
    }

    public bool TryGetWorldPos(string id, out Vector3 worldPos)
    {
        if (map.TryGetValue(id, out var slot) && slot.FlyTarget != null)
        {
            worldPos = slot.FlyTarget.position;
            return true;
        }
        Debug.LogWarning($"Không tìm thấy đích cho id '{id}'");
        worldPos = default;
        return false;
    }

    public void SetProgress(string id, int found, int total)
    {
        if (map.TryGetValue(id, out var slot))
            slot.SetProgress(found, total);
    }
}