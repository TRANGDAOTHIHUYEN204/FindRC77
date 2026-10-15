using UnityEngine;

public class DifferentPairManagerHolder : MonoBehaviour
{
    [SerializeField] private SpawnDifferentPoint spawner;
    public DifferentPairManager Manager { get; private set; }

    public void Build(LevelData level, Transform rootA, Transform rootB)
    {
        spawner.Spawn(level, rootA, rootB);
        Manager = new DifferentPairManager(spawner.Pairs, level.MarkData);
    }

    public void Clear()
    {
        Manager = null;
        spawner.Clear();
    }
}