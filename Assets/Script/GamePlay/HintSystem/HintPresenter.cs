using System.Collections.Generic;
using UnityEngine;

public class HintPresenter : MonoBehaviour
{
    [SerializeField] private FactoryManager factoryManager;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private StateGame stateGame;
    [SerializeField] private MarkData hintMarkData;
    
    [SerializeField] private int defaultHint = 3;   
    [SerializeField] private int currentHint;    

    private const string KeyHint = "HintCount";

    private readonly List<MarkFound> _marks = new List<MarkFound>();
    public DifferentPair HintedPair { get; private set; }
    public int CountHint => currentHint;

    private void Awake()
    {
        currentHint = PlayerPrefs.GetInt(KeyHint, defaultHint);
    }

    [ContextMenu("Reset Hint Count")]
    public void ResetHint()
    {
        PlayerPrefs.DeleteKey(KeyHint);
        currentHint = defaultHint;
    }

    public void ShowHint()
    {
        if (currentHint <= 0)
        {
            Debug.Log("hết key rồi");
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
        SetCount(currentHint - 1);
    }

    public void AddHint(int amount) => SetCount(currentHint + amount);

    private void SetCount(int value)
    {
        currentHint = Mathf.Max(0, value);
        PlayerPrefs.SetInt(KeyHint, currentHint);
        PlayerPrefs.Save();
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