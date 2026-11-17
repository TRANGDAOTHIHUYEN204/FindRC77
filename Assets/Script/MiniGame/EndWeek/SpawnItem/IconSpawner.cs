using System.Collections.Generic;
using UnityEngine;

public class IconSpawner : MonoBehaviour
{
    [SerializeField] private IconCollectionData data;
    [SerializeField] private FactoryIconCollection factory;
    [SerializeField] private CompletionCounter counter;

    [SerializeField] private List<RectTransform> spawnPoints = new();

    [SerializeField] private bool shuffleEntries = true;
    [SerializeField] private float staggerDelay = 0.15f;

    private readonly Queue<ItemEntry> pendingEntries = new();
    private readonly Dictionary<IconCollectionBehaviour, int> slotOfIcon = new();
    private bool[] slotOccupied;

    private void Start() => Spawn();

    public void Spawn()
    {
        if (!data.Validate(out string error))
        {
            Debug.LogError($"Không spawn được: {error}", this);
            return;
        }

        if (spawnPoints.Count == 0)
        {
            return;
        }

        pendingEntries.Clear();
        slotOfIcon.Clear();
        slotOccupied = new bool[spawnPoints.Count];

        List<ItemEntry> entries = data.BuildEntries();
        if (shuffleEntries) Shuffle(entries);
        foreach (var entry in entries) pendingEntries.Enqueue(entry);

        counter.Init(entries.Count);

        int firstBatch = Mathf.Min(spawnPoints.Count, pendingEntries.Count);
        for (int i = 0; i < firstBatch; i++)
            SpawnIntoRandomSlot(i * staggerDelay);
    }

    private int GetRandomFreeSlot()
    {
        var free = new List<int>();
        for (int i = 0; i < slotOccupied.Length; i++)
            if (!slotOccupied[i]) free.Add(i);

        return free.Count == 0 ? -1 : free[Random.Range(0, free.Count)];
    }

    private void SpawnIntoRandomSlot(float delay)
    {
        int slot = GetRandomFreeSlot();
        if (slot < 0) return;
        SpawnIntoSlot(slot, delay);
    }

    private void SpawnIntoSlot(int slotIndex, float delay)
    {
        if (pendingEntries.Count == 0) return;

        ItemEntry entry = pendingEntries.Dequeue();
        Vector3 pos = spawnPoints[slotIndex].position;

        IconCollectionBehaviour icon = factory.Spawn(entry, pos, delay);
        if (icon == null) return;

        slotOccupied[slotIndex] = true;
        slotOfIcon[icon] = slotIndex;

        icon.OnCompleted -= HandleIconCompleted;
        icon.OnCompleted += HandleIconCompleted;
    }

    private void HandleIconCompleted(IconCollectionBehaviour icon)
    {
        icon.OnCompleted -= HandleIconCompleted;
        counter.NotifyCompleted();

        if (!slotOfIcon.TryGetValue(icon, out int slotIndex)) return;
        slotOfIcon.Remove(icon);
        slotOccupied[slotIndex] = false;

        SpawnIntoRandomSlot(0f);
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}