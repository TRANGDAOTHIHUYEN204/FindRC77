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
        GrantStartingHint();  
        ResetForLevel();
    }

    public void ResetForLevel()
    {
        _adPending = false;
        ClearHint();
        RefreshCount();
    }
    
    public void GrantStartingHint()
    {
        if (SaveGame.TryClaimStartingHint())
            RefreshCount();
    }
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

    public void ShowTutorialHint() => ShowTutorialHint(-1f);
    public void ShowTutorialHint(float zoomLevel)
    {
        _tutorialZoom = zoomLevel;
        ShowInternal(consume: false, tutorial: true);
    }
    
    public bool KeepZoomOnClear { get; set; }
    
    public void ResetView()
    {
        HintedPair = null;
        _hintOnArea = false;
        if (spotlight != null) spotlight.Hide();
        ReleaseHands();
    }

    private float _tutorialZoom = -1f;
    private bool _lastTutorial;
    private bool _hintOnArea;
    private Vector2 _areaCenter;

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
        _hintOnArea = false;
        if (spotlight != null)
        {
            if (KeepZoomOnClear) spotlight.HideOverlay();
            else spotlight.Hide();
        }
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
        _lastTutorial = tutorial;
        ShowTarget(tutorial);

        if (consume) ConsumeHint();
        return true;
    }
    private void ShowTarget(bool tutorial)
    {
        _hintOnArea = false;
        var interactables = pairManagerHolder.Interactables;
        if (HintedPair.IsLocked && interactables != null &&
            interactables.TryGetHotspotFor(HintedPair, out Vector2 areaCenter, out Vector2 areaSize))
        {
            _hintOnArea = true;
            _areaCenter = areaCenter;
            spotlight.ShowArea(HintedPair.RootA, HintedPair.RootB, areaCenter, areaSize,
                tutorial, tutorial ? _tutorialZoom : -1f);
        }
        else if (tutorial)
        {
            spotlight.ShowAnimated(HintedPair, _tutorialZoom);
        }
        else
        {
            spotlight.Show(HintedPair);
        }

        if (tutorial)
        {
            ReleaseHands();
            _handA = SpawnHand(HintedPair.RootA);
            _handB = SpawnHand(HintedPair.RootB);
        }
    }
    
    public void OnInteractableOpened()
    {
        if (!_hintOnArea || HintedPair == null || HintedPair.IsFound || HintedPair.IsLocked) return;
        ShowTarget(_lastTutorial);
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

        Vector3 center;
        if (_hintOnArea) center = root.TransformPoint(_areaCenter);
        else HintedPair.GetWorldRect(root, out center, out _);
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