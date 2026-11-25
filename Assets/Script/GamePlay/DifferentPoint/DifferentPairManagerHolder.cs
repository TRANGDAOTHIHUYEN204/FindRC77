using UnityEngine;

public class DifferentPairManagerHolder : MonoBehaviour
{
    [SerializeField] private SpawnDifferentPoint spawner;
    public DifferentPairManager Manager { get; private set; }

    private LevelData _level;
    private Transform _rootA, _rootB;
    private InteractableController _interactables;

    public InteractableController Interactables
    {
        get
        {
            if (_interactables == null)
            {
                _interactables = GetComponent<InteractableController>();
                if (_interactables == null)
                    _interactables = gameObject.AddComponent<InteractableController>();
            }
            return _interactables;
        }
    }

    public void Build(LevelData level, Transform rootA, Transform rootB)
    {
        _level = level; _rootA = rootA; _rootB = rootB;
        spawner.Spawn(level, rootA, rootB);
        Manager = new DifferentPairManager(spawner.Pairs, level.MarkData);
        Interactables.Build(level, rootA as RectTransform, rootB as RectTransform, spawner.Pairs);
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
        Interactables.Clear();
    }
}
