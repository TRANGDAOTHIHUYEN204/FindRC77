using System;
using UnityEngine;

public class HintPresenter : MonoBehaviour
{
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private StateGame stateGame;
    [SerializeField] private HintSpotlight spotlight;
    [SerializeField] private FactoryManager factory;
    [SerializeField, Min(0)] private int hintPerLevel = 1;

    private HintHand _handA;
    private HintHand _handB;
    private int _levelHintsRemaining;

    public DifferentPair HintedPair { get; private set; }
    public int CountHint { get; private set; }

    public event Action<int> OnHintChanged;

    private void Start() => ResetForLevel();

    [ContextMenu("Reset Hint For Level")]
    public void ResetForLevel()
    {
        _levelHintsRemaining = hintPerLevel;
        ClearHint();
        RefreshCount();
    }

    public void ShowHint()
    {
        if (CountHint <= 0)
        {
            Debug.Log("Hết hint rồi");
            return;
        }
        ShowInternal(consume: true, tutorial: false);
    }

    public void ShowTutorialHint() => ShowInternal(consume: false, tutorial: true);

    private void ShowInternal(bool consume, bool tutorial)
    {
        if (!stateGame.IsPlaying) return;

        var manager = pairManagerHolder.Manager;
        if (manager == null || manager.IsComplete) return;
        if (HintedPair != null && !HintedPair.IsFound) return;

        var entry = manager.GetHintEntry();
        if (entry == null) return;

        HintedPair = entry.Pair;

        if (tutorial)
        {
            spotlight.ShowAnimated(HintedPair);
            ReleaseHands();

            _handA = SpawnHand(HintedPair.RootA);
            _handB = SpawnHand(HintedPair.RootB);
        }
        else
        {
            spotlight.Show(HintedPair);
        }

        if (consume) ConsumeHint();
    }

    public void AddHint(int amount)
    {
        if (amount <= 0) return;
        SaveGame.HintBalance += amount;
        RefreshCount();
    }

    private void ConsumeHint()
    {
        if (_levelHintsRemaining > 0)
            _levelHintsRemaining--;
        else
            SaveGame.HintBalance--;

        RefreshCount();
    }

    private void RefreshCount()
    {
        CountHint = _levelHintsRemaining + SaveGame.HintBalance;
        OnHintChanged?.Invoke(CountHint);
    }

    public void ClearHint()
    {
        HintedPair = null;
        if (spotlight != null) spotlight.Hide();
        ReleaseHands();
    }

    private HintHand SpawnHand(RectTransform root)
    {
        if (root == null) return null;

        var hand = factory.CreateHand();
        if (hand == null) return null;

        HintedPair.GetWorldRect(root, out Vector3 center, out _);
        hand.PointAt(root, center);
        return hand;
    }

    private void ReleaseHands()
    {
        ReleaseHand(ref _handA);
        ReleaseHand(ref _handB);
    }

    private void ReleaseHand(ref HintHand hand)
    {
        if (hand == null)
        {
            hand = null;
            return;
        }

        hand.Detach();
        if (factory != null) factory.Release(hand.gameObject);
        hand = null;
    }
}