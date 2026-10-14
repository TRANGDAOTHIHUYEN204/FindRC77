using System.Collections.Generic;
using UnityEngine;

public class PoolManager
{
    private readonly Dictionary<int, Stack<GameObject>> _free = new();
    private readonly Dictionary<GameObject, int> _owner = new();
    private readonly List<GameObject> _active = new();

    public IReadOnlyList<GameObject> ActiveGameObjects => _active;

    public GameObject CreateGameObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();
        if (!_free.TryGetValue(key, out var stack))
        {
            stack = new Stack<GameObject>();
            _free[key] = stack;
        }

        GameObject obj = null;
        while (stack.Count > 0 && obj == null)
            obj = stack.Pop();

        if (obj == null)
        {
            obj = Object.Instantiate(prefab);
            _owner[obj] = key;
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        _active.Add(obj);
        return obj;
    }

    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;
        if (!_owner.TryGetValue(obj, out int key)) return;

        if (!_active.Remove(obj)) return;

        if (!_free.TryGetValue(key, out Stack<GameObject> stack)) return;

        obj.SetActive(false);
        stack.Push(obj);
    }
}