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
    
    public Entry FindRemaining(Vector2 screenPoint, Camera cam)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            var pair = entries[i].Pair;
            if (pair.IsFound) continue;
            if (pair.ContainsPoint(screenPoint, cam)) return entries[i];
        }
        return null;
    }
    
    public bool IsInFoundArea(Vector2 screenPoint, Camera cam)
    {
        if (FindRemaining(screenPoint, cam) != null) return false;

        for (int i = 0; i < entries.Count; i++)
        {
            var pair = entries[i].Pair;
            if (pair.IsFound && pair.ContainsPoint(screenPoint, cam)) return true;
        }
        return false;
    }
    public bool MarkFound(Entry entry)
    {
        if (entry == null || !entry.Pair.MarkFound()) return false;
        FoundCount++;
        return true;
    }
    

    public Entry GetHintEntry()
    {
        // Ưu tiên điểm đang bấm được; chỉ hint điểm bị giấu (trong tủ đóng)
        // khi không còn điểm nào khác → spotlight rơi vào đồ vật cần mở.
        var remaining = new List<Entry>();
        var locked = new List<Entry>();
        for (int i = 0; i < entries.Count ; i++)
        {
            if (entries[i].Pair.IsFound) continue;
            if (entries[i].Pair.IsLocked) locked.Add(entries[i]);
            else remaining.Add(entries[i]);
        }

        if (remaining.Count == 0) remaining = locked;
        if (remaining.Count == 0) return null;
        return remaining[Random.Range(0, remaining.Count)];
        
    }
    public void ResetAll()
    {
        for (int i = 0; i < entries.Count; i++)
            entries[i].Pair.ResetFound();

        FoundCount = 0;
    }

}