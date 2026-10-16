using System.Collections.Generic;
using UnityEngine;

public class SpawnDifferentPoint : MonoBehaviour
{
    [SerializeField] private FactoryManager factoryManager;
    private Transform _pairContainer; 
    
    private readonly List<DifferentPair> pairs = new List<DifferentPair>();
    public IReadOnlyList<DifferentPair> Pairs => pairs;

    public void Spawn(LevelData levelData, Transform rootA, Transform rootB)
    {
        Clear();
        if (levelData == null || levelData.DifferentPoints == null) return;

        foreach (DifferentPointInfo info in levelData.DifferentPoints)
        {
            DifferentPair pair = factoryManager.CreateDifferentPair();
            if (pair == null) continue;

            pair.transform.SetParent(_pairContainer != null ? _pairContainer : transform, false);
            pair.Init(info, rootA, rootB);
            pairs.Add(pair);
        }
        Debug.Log($"Spawn {pairs.Count}/{levelData.DifferentPoints.Count} pair");
        Physics2D.SyncTransforms();
    }

    public void Clear()
    {
        for (int i = 0; i < pairs.Count; i++)
        {
            if (pairs[i] != null && pairs[i].gameObject.activeInHierarchy)
                factoryManager.Release(pairs[i].gameObject);
        }
        pairs.Clear();
    }
}