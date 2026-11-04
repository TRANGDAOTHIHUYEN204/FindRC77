using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HiddenEntry
{
    public FindHiddenData data;
    public PointSpawnItem points;   
    public int count = 0;

    public int SpawnCount
    {
        get
        {
            if (points == null) return 0;
            int n = points.Points.Count;
            return count <= 0 ? n : Mathf.Min(count, n);
        }
    }
}

[CreateAssetMenu(fileName = "FindHiddenLevelData", menuName = "Scriptable Objects/FindHiddenLevelData")]
public class FindHiddenLevelData : ScriptableObject
{
    [SerializeField] private Sprite background;
    [SerializeField] private HiddenEntry[] entries;

    [SerializeField] private float time;

    public Sprite Background => background;
    public IReadOnlyList<HiddenEntry> Entries => entries;

    public int TotalCount
    {
        get
        {
            int sum = 0;
            foreach (var e in entries) sum += e.SpawnCount;
            return sum;
        }
    }

    public float Time => time;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (entries == null) return;
        foreach (var e in entries)
        {
            if (e.data == null || e.points == null) continue;
            if (e.count > e.points.Points.Count)
                Debug.LogWarning($"{name}: {e.data.name} cần {e.count} điểm nhưng {e.points.name} chỉ có {e.points.Points.Count}", this);
        }
    }
#endif
}