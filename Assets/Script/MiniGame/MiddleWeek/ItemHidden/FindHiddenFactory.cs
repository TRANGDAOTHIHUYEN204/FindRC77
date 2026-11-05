using System.Collections.Generic;
using UnityEngine;

public class FindHiddenFactory : MonoBehaviour
{
    [SerializeField] private FindHiddenBehaviour prefab;
    [SerializeField] private RectTransform container; 

    private readonly PoolManager pool = new PoolManager();
    private readonly List<FindHiddenBehaviour> items = new();
    public IReadOnlyList<FindHiddenBehaviour> Items => items;

    public FindHiddenBehaviour Spawn(FindHiddenData data, Vector3 point, int slotIndex)
    {
        if (prefab == null)    { Debug.LogError("FindHiddenFactory: Prefab chưa gán", this); return null; }
        if (container == null) { Debug.LogError("FindHiddenFactory: Container chưa gán", this); return null; }

        GameObject go = pool.CreateGameObject(prefab.gameObject, Vector3.zero, Quaternion.identity);
        go.transform.SetParent(container, false);

        var item = go.GetComponent<FindHiddenBehaviour>();
        item.Init(container, point, data, slotIndex);

        item.OnArrived -= Despawn;
        item.OnArrived += Despawn;

        items.Add(item);
        return item;
    }

    public void Despawn(FindHiddenBehaviour item)
    {
        if (item == null) return;
        item.OnArrived -= Despawn;
        items.Remove(item);
        pool.ReturnObject(item.gameObject);
    }

    public void DespawnAll()
    {
        for (int i = items.Count - 1; i >= 0; i--)
            Despawn(items[i]);
    }
}