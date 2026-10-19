using UnityEngine;

public class ErrorSpawnManager : MonoBehaviour
{
    [SerializeField] private FactoryManager factoryManager;
    [SerializeField] private Sprite errorSprite;
    [SerializeField] private float markSize = 1f;
    [SerializeField] private float lifeTime = 0.8f;
    [SerializeField] private Camera cam;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    public void SpawnAtScreen(Vector2 screenPos)
    {
        ErrorMark mark = factoryManager.CreateErrorManager();
        if (mark == null) return;

        Vector3 world = cam.ScreenToWorldPoint(screenPos);
        world.z = 0f;
        mark.transform.position = world;

        mark.Init(markSize, errorSprite, lifeTime, factoryManager.Release);
    }
}