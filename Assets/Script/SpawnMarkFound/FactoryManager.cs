using UnityEngine;

public class FactoryManager : MonoBehaviour
{
    [SerializeField] private MarkFound prefabMarkFound;
    [SerializeField] private ChallengeNode challengeNodePrefab;

    private readonly PoolManager _pool = new PoolManager();

    public MarkFound Create()
    {
        return CreateObject(prefabMarkFound);
    }

    public ChallengeNode CreateChallengeNode()
    {
        return CreateObject(challengeNodePrefab);
    }

    private T CreateObject<T>(T prefab) where T : Component
    {
        if (prefab == null) return null;

        GameObject obj = _pool.CreateGameObject(prefab.gameObject, Vector3.zero, Quaternion.identity);

        return obj.GetComponent<T>();
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
        {
            _pool.ReturnObject(active[i]);
        }
    }
}
