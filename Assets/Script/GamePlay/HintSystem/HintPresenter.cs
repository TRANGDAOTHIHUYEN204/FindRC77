using System;
using System.Collections.Generic;
using UnityEngine;

public class HintPresenter : MonoBehaviour
{
    [SerializeField] private FactoryManager factoryManager;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private StateGame stateGame;
    [SerializeField] private MarkData hintMarkData;
    private readonly List<MarkFound> _marks = new List<MarkFound>();

    public DifferentPair HintedPair { get; private set; }
    public int CountHint => SaveGame.HintCount;

    public event Action<int> OnHintChanged;

    private void Start() => OnHintChanged?.Invoke(CountHint);

    [ContextMenu("Reset Hint Count")]
    public void ResetHint()
    {
        SaveGame.ResetHint();
        OnHintChanged?.Invoke(CountHint);
    }

    public void ShowHint()
    {
        if (CountHint <= 0)
        {
            Debug.Log("Hết hint rồi");
            return;
        }
        if (!stateGame.IsPlaying) return;

        var manager = pairManagerHolder.Manager;
        if (manager == null || manager.IsComplete) return;
        if (HintedPair != null && !HintedPair.IsFound) return;

        var entry = manager.GetHintEntry();
        if (entry == null) return;

        HintedPair = entry.Pair;
        Draw(HintedPair.RootA);
        Draw(HintedPair.RootB);
        SetCount(CountHint - 1);
    }

    public void AddHint(int amount) => SetCount(CountHint + amount);

    private void SetCount(int value)
    {
        SaveGame.HintCount = value;
        OnHintChanged?.Invoke(CountHint);
    }

    private void Draw(RectTransform root)
    {
        MarkFound mark = factoryManager.Create();
        if (mark == null) return;

        HintedPair.GetWorldRect(root, out Vector2 center, out Vector2 size);
        mark.Init(hintMarkData.ShapeMark, center, size,
                  hintMarkData.LineWidth, hintMarkData.ColorLine, hintMarkData.Segments);
        _marks.Add(mark);
    }

    public void ClearHint()
    {
        foreach (var m in _marks)
            if (m != null) factoryManager.Release(m.gameObject);
        _marks.Clear();
        HintedPair = null;
    }
}