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

            if (mark != null) pairs[i].SetShape(mark.ShapeMark);
            entries.Add(new Entry { Pair = pairs[i], Mark = mark });
        }
    }
    private Entry FindOwner(Vector2 screenPoint, Camera cam)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].Pair.ContainsPoint(screenPoint, cam)) return entries[i];
        return null;
    }

    public Entry FindRemaining(Vector2 screenPoint, Camera cam)
    {
        Entry owner = FindOwner(screenPoint, cam);
        return (owner != null && !owner.Pair.IsFound) ? owner : null;
    }

    public bool IsInFoundArea(Vector2 screenPoint, Camera cam)
    {
        Entry owner = FindOwner(screenPoint, cam);
        return owner != null && owner.Pair.IsFound;
    }
    
    public bool MarkFound(Entry entry)
    {
        if (entry == null || !entry.Pair.MarkFound()) return false;
        FoundCount++;
        return true;
    }
    

    public Entry GetHintEntry()
    {
        var remaining = new List<Entry>();
        for (int i = 0; i < entries.Count ; i++)
        {
            if (!entries[i].Pair.IsFound) remaining.Add(entries[i]); 
        }

        if (remaining.Count == 0) return null;
        return remaining[Random.Range(0, remaining.Count)];
    }
}