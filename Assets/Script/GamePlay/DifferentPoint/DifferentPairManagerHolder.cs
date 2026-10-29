using UnityEngine;

public class DifferentPairManagerHolder : MonoBehaviour
{
    [SerializeField] private SpawnDifferentPoint spawner;
    public DifferentPairManager Manager { get; private set; }

    private LevelData _level;
    private Transform _rootA, _rootB;

    public void Build(LevelData level, Transform rootA, Transform rootB)
    {
        _level = level; _rootA = rootA; _rootB = rootB;
        spawner.Spawn(level, rootA, rootB);
        Manager = new DifferentPairManager(spawner.Pairs, level.MarkData);
    }

    public void Rebuild()
    {
        if (_level == null) return;
        var level = _level; var a = _rootA; var b = _rootB;
        Clear();
        Build(level, a, b);
    }

    public void Clear()
    {
        Manager = null;
        spawner.Clear();
    }
}