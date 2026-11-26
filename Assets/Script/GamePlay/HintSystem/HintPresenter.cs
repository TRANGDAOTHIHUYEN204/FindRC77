using System;
using UnityEngine;

public class HintPresenter : MonoBehaviour
{
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private StateGame stateGame;
    [SerializeField] private HintSpotlight spotlight;
    [SerializeField] private FactoryGameplay factoryGameplay;

    private HintHand _handA;
    private HintHand _handB;
    private bool _adPending;

    public DifferentPair HintedPair { get; private set; }

    public int CountHint { get; private set; }

    public bool NeedAdForHint => CountHint <= 0;

    public event Action<int> OnHintChanged;

    private void Start()
    {
        GrantStartingHint();   // tặng 1 hint đầu game (chỉ 1 lần duy nhất)
        ResetForLevel();
    }

    public void ResetForLevel()
    {
        _adPending = false;
        ClearHint();
        RefreshCount();
    }

    /// <summary>Hint khởi đầu: ai cũng nhận 1 lần, bất kể tutorial.</summary>
    public void GrantStartingHint()
    {
        if (SaveGame.TryClaimStartingHint())
            RefreshCount();
    }

    /// <summary>Giữ lại để các script cũ gọi không bị lỗi. Dùng chung cờ nên không bị cộng trùng.</summary>
    public void GrantTutorialHint() => GrantStartingHint();

    public void ShowHint()
    {
        if (!CanShowHint()) return;

        RefreshCount();

        if (CountHint > 0)
        {
            ShowInternal(consume: true, tutorial: false);
            return;
        }

        RequestHintByAd();
    }

    public void ShowTutorialHint() => ShowInternal(consume: false, tutorial: true);

    public void AddHint(int amount)
    {
        if (amount <= 0) return;
        SaveGame.HintBalance += amount;
        RefreshCount();
    }

    public void ClaimMiniGameReward()
    {
        if (SaveGame.TryClaimFindHiddenReward())
            RefreshCount();
    }

    public void ClearHint()
    {
        HintedPair = null;
        if (spotlight != null) spotlight.Hide();
        ReleaseHands();
    }

    private void RequestHintByAd()
    {
        if (_adPending)
        {
            Debug.Log("[Hint] Ad đang pending, bỏ qua.");
            return;
        }

        var ads = AdsController.Instance;
        if (ads == null)
        {
            Debug.LogWarning("[Hint] AdsController null.");
            return;
        }

        _adPending = true;

        ads.RequestReward(
            onRewarded: () =>
            {
                if (this == null) return;
                _adPending = false;

                if (!ShowInternal(consume: false, tutorial: false))
                    Debug.Log("[Hint] Không hiện được hint sau khi xem ad.");
            },
            onFailed: () =>
            {
                if (this == null) return;
                _adPending = false;
                Debug.Log("[Hint] Ad chưa sẵn sàng / bị hủy.");
            });
    }

    private bool CanShowHint()
    {
        if (stateGame == null || !stateGame.IsPlaying) return false;

        var manager = pairManagerHolder.Manager;
        if (manager == null || manager.IsComplete) return false;

        if (HintedPair != null && !HintedPair.IsFound) return false;
        return true;
    }

    private bool ShowInternal(bool consume, bool tutorial)
    {
        if (!CanShowHint()) return false;

        var manager = pairManagerHolder.Manager;
        var entry = manager.GetHintEntry();
        if (entry == null) return false;

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
        return true;
    }

    private void ConsumeHint()
    {
        if (SaveGame.HintBalance > 0)
            SaveGame.HintBalance--;

        RefreshCount();
    }

    private void RefreshCount()
    {
        CountHint = SaveGame.HintBalance;
        OnHintChanged?.Invoke(CountHint);
    }

    private HintHand SpawnHand(RectTransform root)
    {
        if (root == null) return null;

        var hand = factoryGameplay.CreateHand();
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
        if (hand == null) return;

        hand.Detach();
        if (factoryGameplay != null) factoryGameplay.Release(hand.gameObject);
        hand = null;
    }
}