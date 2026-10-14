using UnityEngine;

public class FactoryManager : MonoBehaviour
{
    [SerializeField] private MarkFound prefabMarkFound;

    private readonly PoolManager _pool = new PoolManager();
    
    public MarkFound Create()
    {
        if (prefabMarkFound == null) return null;
        GameObject markFound = _pool.CreateGameObject(prefabMarkFound.gameObject, Vector3.zero, Quaternion.identity);
        return markFound.GetComponent<MarkFound>();
    }

    public void Release(GameObject obj)
    {
        if (obj == null) return;
        _pool.ReturnObject(obj);
    }

    public void ReleaseAll()
    {
        var active = _pool.ActiveGameObjects;

        for (int i = active.Count - 1; i >= 0; i--)
            _pool.ReturnObject(active[i]);
    }
}