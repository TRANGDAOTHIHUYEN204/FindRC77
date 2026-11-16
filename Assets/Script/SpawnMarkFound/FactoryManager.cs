using UnityEngine;

public class FactoryManager
{
    private readonly PoolManager _pool = new PoolManager();

    public T Create<T>(T prefab) where T : Component
    {
        return Create(prefab, Vector3.zero, Quaternion.identity);
    }

    public T Create<T>(T prefab, Vector3 position) where T : Component
    {
        return Create(prefab, position, Quaternion.identity);
    }

    public T Create<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        if (prefab == null) return null;

        GameObject obj = _pool.CreateGameObject(prefab.gameObject, position, rotation);
        return obj != null ? obj.GetComponent<T>() : null;
    }
    
    public void Release(GameObject obj)
    {
        if (obj == null) return;
        _pool.ReturnObject(obj);
    }

    public void Detach(GameObject obj)
    {
        if (obj == null) return;
        _pool.Detach(obj);
    }

    public void ReleaseAll()
    {
        var active = _pool.ActiveGameObjects;
        for (int i = active.Count - 1; i >= 0; i--)
            _pool.ReturnObject(active[i]);
    }
}