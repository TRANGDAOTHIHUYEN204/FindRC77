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
    public event Action<FindHiddenData, int, int> OnTypeProgress;

    private readonly Dictionary<FindHiddenData, int> _total = new();
    private readonly Dictionary<FindHiddenData, int> _found = new();
    public FindHiddenLevelData CurrentLevelData => levels[CurrentLevel];
    public void MarkFound(FindHiddenData data)
    {
        if (data == null || !_found.ContainsKey(data)) return;
        _found[data]++;
        OnTypeProgress?.Invoke(data, _found[data], _total[data]);
    }

    private void RefreshProgress()
    {
        foreach (var kv in _total)
            OnTypeProgress?.Invoke(kv.Key, _found[kv.Key], kv.Value);
    }

    public void SetLevel(int index) =>
        CurrentLevel = Mathf.Clamp(index, 0, levels.Length - 1);

    public void SpawnAll(IList<int> foundIds = null)
    {
        spawner.DespawnAll();
        Total = 0;
        _total.Clear();
        _found.Clear();

        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("FindHiddenLevelSpawner: chưa gán levels", this);
            return;
        }

        var foundSet = foundIds != null ? new HashSet<int>(foundIds) : null;
        var level = levels[CurrentLevel];
        int nextId = 0; 

        foreach (var entry in level.Entries)
        {
            if (entry.data == null || entry.points == null) continue;

            int n = entry.SpawnCount;
            _total[entry.data] = 0;
            _found[entry.data] = 0;

            for (int k = 0; k < n; k++)
            {
                int id = nextId++; 

                var item = spawner.Spawn(entry.data, entry.points.Points[k], k);
                if (item == null) continue;

                item.Id = id;
                Total++;
                _total[entry.data]++;

                if (foundSet != null && foundSet.Contains(id))
                {
                    item.SetFoundInstant();
                    _found[entry.data]++; 
                }
            }
        }

        RefreshProgress(); 

        Debug.Log($"[SpawnAll] spawned={Total}/{level.TotalCount} restored={foundSet?.Count ?? 0}");
        OnLevelLoaded?.Invoke(CurrentLevel);
    }

    public void DespawnAll()
    {
        spawner.DespawnAll();
        Total = 0;
        _total.Clear();
        _found.Clear();
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