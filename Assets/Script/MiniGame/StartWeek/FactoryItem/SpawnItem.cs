using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnItem : MonoBehaviour
{
    [SerializeField] private FactoryItem factory;
    [SerializeField] private RectTransform spawnParent;
    [SerializeField] private ItemConfig[] configs;
    [SerializeField] private float spawnHeightAboveScreen = 300f;
    [SerializeField] private float horizontalPadding = 80f;

    [SerializeField] private TimeManager timeManager;
    [SerializeField] private float[] intervalPerMinute = { 2f, 1.5f, 0.75f };

    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float despawnOffsetBelow = 1f;
    [SerializeField] private HandlePlayerCollisionItem checker;
    private Coroutine spawnRoutine;
    private bool paused;
    private readonly List<Item> _spawned = new List<Item>();

    private void OnEnable()
    {
        if (timeManager != null) timeManager.OnMinutePassed += HandleMinutePassed;
    }

    private void OnDisable()
    {
        if (timeManager != null) timeManager.OnMinutePassed -= HandleMinutePassed;
        StopSpawn();
    }

    private void HandleMinutePassed(int minute)
    {
        ApplyIntervalForMinute(minute);
    }

    private void ApplyIntervalForMinute(int minute)
    {
        if (intervalPerMinute == null || intervalPerMinute.Length == 0) return;

        int index = Mathf.Clamp(minute, 0, intervalPerMinute.Length - 1);
        spawnInterval = intervalPerMinute[index];
    }

    public void StartSpawn()
    {
        StopSpawn();
        paused = false;
        ApplyIntervalForMinute(0);
        spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawn()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    public void PauseSpawn()  => paused = true;
    public void ResumeSpawn() => paused = false;

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            Spawn();

            float t = 0f;
            while (t < spawnInterval)
            {
                if (!paused) t += Time.deltaTime;
                yield return null;
            }
        }
    }

    public Item Spawn()
    {
        if (configs == null || configs.Length == 0) return null;

        _spawned.RemoveAll(i => i == null || !i.gameObject.activeSelf);

        Rect rect = spawnParent.rect;

        float topY = rect.yMax;
        float x = Random.Range(rect.xMin + horizontalPadding, rect.xMax - horizontalPadding);
        float y = Random.Range(topY, topY + spawnHeightAboveScreen);

        Item item = factory.CreateItem(Vector3.zero);
        if (item == null) return null;

        item.transform.SetParent(spawnParent, false);
        item.transform.localPosition = new Vector3(x, y, 0f);

        item.Setup(configs[Random.Range(0, configs.Length)], factory, checker);
        item.StartMove(rect.yMin - despawnOffsetBelow);
        _spawned.Add(item);
        return item;
    }

    public void ClearAll()
    {
        var snapshot = _spawned.ToArray();
        _spawned.Clear();

        for (int i = 0; i < snapshot.Length; i++)
        {
            var item = snapshot[i];
            if (item == null || !item.gameObject.activeSelf) continue;

            item.ReturnToPool();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnParent == null) return;

        Rect rect = spawnParent.rect;
        Vector3 localCenter = new Vector3((rect.xMin + rect.xMax) * 0.5f, rect.yMax + spawnHeightAboveScreen * 0.5f, 0f);
        Vector3 size = new Vector3(rect.width - horizontalPadding * 2f, spawnHeightAboveScreen, 0f);

        Gizmos.color = Color.cyan;
        Gizmos.matrix = spawnParent.localToWorldMatrix;
        Gizmos.DrawWireCube(localCenter, size);
    }
}