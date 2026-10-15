using System.Collections.Generic;
using UnityEngine;

public class DifferentPairManager
{
    public class Entry
    {
        public DifferentPair Pair;
        public MarkData Mark;
    }
    private readonly List<Entry> entries = new List<Entry>();
    public int TotalCount => entries.Count;
    public int FoundCount { get; private set; }
    public bool IsComplete => TotalCount > 0 && FoundCount >= TotalCount;

    public DifferentPairManager(IReadOnlyList<DifferentPair> pairs, MarkData[] marks)
    {
        for (int i = 0; i < pairs.Count; i++)
        {
            MarkData mark = null;
            if (marks != null && marks.Length > 0)
                mark = marks[Mathf.Min(i, marks.Length - 1)];

            entries.Add(new Entry { Pair = pairs[i], Mark = mark });
        }
    }
    
    public Entry FindRemaining(Vector2 screenPoint, Camera cam)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (!e.Pair.IsFound && e.Pair.IsHit(screenPoint, cam)) return e;
        }
        return null;
    }
    
    public bool MarkFound(Entry entry)
    {
        if (entry == null || !entry.Pair.MarkFound()) return false;
        FoundCount++;
        return true;
    }
    
    public bool IsInFoundArea(Vector2 screenPoint, Camera cam)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (e.Pair.IsFound && e.Pair.IsHit(screenPoint, cam)) return true;
        }
        return false;
    }
    
}