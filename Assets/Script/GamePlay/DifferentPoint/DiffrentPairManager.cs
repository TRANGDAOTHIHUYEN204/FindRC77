using System.Collections.Generic;
using UnityEngine;

public class DifferentPairManager
{
    public class Entry
    {
        public DifferentPair Pair;
        public MarkData Mark;

        public Entry(DifferentPair pair, MarkData mark)
        {
            Pair = pair;
            Mark = mark;
        }
    }

    private readonly List<Entry> _remaining = new();
    private readonly List<Entry> _found = new();
    private readonly List<DifferentPoint> _listA = new();
    private readonly List<DifferentPoint> _listB = new();

    public int TotalCount { get; private set; }
    public int FoundCount => _found.Count;
    public bool IsComplete => _remaining.Count == 0 && TotalCount > 0;

    public void Build(GameObject rootA, GameObject rootB, MarkData[] markData)
    {
        _remaining.Clear();
        _found.Clear();
        _listA.Clear();
        _listB.Clear();

        rootA.GetComponentsInChildren(true, _listA);
        rootB.GetComponentsInChildren(true, _listB);

        int count = Mathf.Min(_listA.Count, _listB.Count);

        if (markData == null || markData.Length == 0)
            count = 0;

        for (int i = 0; i < count; i++)
        {
            DifferentPoint pointA = _listA[i];
            DifferentPoint pointB = _listB[i];

            pointA.gameObject.SetActive(true);
            pointB.gameObject.SetActive(true);

            pointA.ResetPoint();
            pointB.ResetPoint();

            var pair = new DifferentPair(pointA, pointB);
            var mark = markData[Mathf.Min(i, markData.Length - 1)];

            _remaining.Add(new Entry(pair, mark));
        }

        TotalCount = _remaining.Count;
    }

    public Entry FindRemaining(Vector2 worldPoint)
    {
        foreach (Entry current in _remaining)
        {
            if (current.Pair.Contains(worldPoint))
                return current;
        }

        return null;
    }
    
    public bool IsInFoundArea(Vector2 worldPoint)
    {
        foreach (Entry current in _found)
        {
            if (current.Pair.Contains(worldPoint))
                return true;
        }

        return false;
    }
    
    public bool MarkFound(Entry entry, Vector2 worldPoint)
    {
        if (!entry.Pair.TryHit(worldPoint))
            return false;

        _remaining.Remove(entry);
        _found.Add(entry);
        return true;
    }
}