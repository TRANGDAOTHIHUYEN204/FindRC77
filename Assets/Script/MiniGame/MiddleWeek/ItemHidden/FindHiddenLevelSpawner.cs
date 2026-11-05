using System;
using System.Collections.Generic;
using UnityEngine;

public class FindHiddenLevelSpawner : MonoBehaviour
{
    [SerializeField] private FindHiddenLevelData[] levels;
    [SerializeField] private FindHiddenFactory spawner;
    public Sprite BackgroundImage => levels[CurrentLevel].Background;
    public int CurrentLevel { get; private set; }
    public int Total { get; private set; }
    public int LevelCount => levels != null ? levels.Length : 0;
    public event Action<int> OnLevelLoaded;

    public void SetLevel(int index) =>
        CurrentLevel = Mathf.Clamp(index, 0, levels.Length - 1);

    public void SpawnAll()
    {
        spawner.DespawnAll();
        Total = 0;

        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("FindHiddenLevelSpawner: chưa gán levels", this);
            return;
        }

        var level = levels[CurrentLevel];

        foreach (var entry in level.Entries)
        {
            if (entry.data == null || entry.points == null) continue;

            int n = entry.SpawnCount;
            for (int k = 0; k < n; k++)
            {
                var item = spawner.Spawn(entry.data, entry.points.Points[k], k);
                if (item != null) Total++;
            }
        }

        Debug.Log($"[SpawnAll] spawned={Total}/{level.TotalCount}");
        OnLevelLoaded?.Invoke(CurrentLevel);
    }

    public void DespawnAll()
    {
        spawner.DespawnAll();
        Total = 0;
    }

    public bool TryGetHit(Vector2 screenPoint, Camera cam, out FindHiddenBehaviour hit)
    {
        var items = spawner.Items;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];
            if (item.IsFound) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)item.transform, screenPoint, cam))
            {
                hit = item;
                return true;
            }
        }
        hit = null;
        return false;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}