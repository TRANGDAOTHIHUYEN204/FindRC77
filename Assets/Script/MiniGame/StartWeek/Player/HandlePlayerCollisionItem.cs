using System;
using System.Collections.Generic;
using UnityEngine;

public class HandlePlayerCollisionItem : MonoBehaviour
{
    [SerializeField] private RectTransform spawnParent;
    [SerializeField] private Transform catchPoint; 
    [SerializeField] private float catchHalfWidth = 100f; 
    [SerializeField] private float catchHalfHeight = 30f;

    public event Action<Item> Caught;

    private readonly List<Item> active = new List<Item>(64);

    public void Register(Item item)
    {
        item.ActiveIndex = active.Count;
        active.Add(item);
    }

    public void Unregister(Item item)
    {
        int i = item.ActiveIndex;
        if (i < 0) return;

        int last = active.Count - 1;
        Item moved = active[last];
        active[i] = moved;
        moved.ActiveIndex = i;
        active.RemoveAt(last);
        item.ActiveIndex = -1;
    }

    private void LateUpdate()
    {
        if (active.Count == 0) return;
        
        Vector3 c = spawnParent.InverseTransformPoint(catchPoint.position);
        float minX = c.x - catchHalfWidth, maxX = c.x + catchHalfWidth;
        float minY = c.y - catchHalfHeight, maxY = c.y + catchHalfHeight;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            Item it = active[i];
            float x = it.X, y = it.Y;

            if (x + it.HalfWidth < minX || x - it.HalfWidth > maxX) continue;
            if (y + it.HalfHeight < minY || y - it.HalfHeight > maxY) continue;

            Caught?.Invoke(it);
            it.ReturnToPool();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnParent == null || catchPoint == null) return;
        Gizmos.color = Color.green;
        Gizmos.matrix = spawnParent.localToWorldMatrix;
        Vector3 c = spawnParent.InverseTransformPoint(catchPoint.position);
        Gizmos.DrawWireCube(c, new Vector3(catchHalfWidth * 2f, catchHalfHeight * 2f, 0f));
    }
}