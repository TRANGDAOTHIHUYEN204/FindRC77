using UnityEngine;

public class FactoryItem : MonoBehaviour
{
    [SerializeField] private Item itemPrefab;

    private readonly FactoryManager _factory = new FactoryManager();

    public Item CreateItem(Vector3 position) => _factory.Create(itemPrefab, position);

    public void Release(GameObject obj) => _factory.Release(obj);
    public void Detach(GameObject obj) => _factory.Detach(obj);
    public void ReleaseAll() => _factory.ReleaseAll();
    private void OnDestroy() => _factory.ReleaseAll();
}