using System.Collections.Generic;
using UnityEngine;

public class FactoryManager : MonoBehaviour
{
    [SerializeField] private MarkFound prefabMarkFound;

    private readonly PoolManager _pool = new PoolManager();
    private readonly Dictionary<GameObject, MarkFound> _cache = new();

    public MarkFound Create()
    {
        GameObject obj = _pool.CreateGameObject(prefabMarkFound.gameObject, Vector3.zero, Quaternion.identity);

        if (!_cache.TryGetValue(obj, out var mark))
        {
            mark = obj.GetComponent<MarkFound>();  
            _cache[obj] = mark;
        }
        return mark;
    }

    public void Release(MarkFound mark) => _pool.ReturnObject(mark.gameObject);

    public void ReleaseAll()
    {
        var active = _pool.ActiveGameObjects;
        for (int i = active.Count - 1; i >= 0; i--)
            _pool.ReturnObject(active[i]);
    }
}