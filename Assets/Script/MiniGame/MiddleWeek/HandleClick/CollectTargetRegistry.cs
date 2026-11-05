using System;
using UnityEngine;

public class CollectTargetRegistry : MonoBehaviour
{
    [Serializable]
    private class Entry
    {
        public string id;      
        public RectTransform target;
    }

    [SerializeField] private Entry[] entries;

    public bool TryGetWorldPos(string id, out Vector3 worldPos)
    {
        foreach (var e in entries)
        {
            if (e.id == id && e.target != null)
            {
                worldPos = e.target.position;
                return true;
            }
        }
        Debug.LogWarning($"Không tìm thấy đích cho id '{id}'");
        worldPos = default;
        return false;
    }
}