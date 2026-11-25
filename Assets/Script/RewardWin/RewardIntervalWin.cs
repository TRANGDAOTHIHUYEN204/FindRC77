using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class RewardIntervalWin : MonoBehaviour
{
    [SerializeField] private RewardWin rewardData;
    [SerializeField] private StarRewardManager starReward;
    [SerializeField] private ProcessReward processReward;
    [SerializeField] private GiftReward giftReward;
    [SerializeField] private HintPresenter hintPresenter;
    [SerializeField] private GameObject winPopup;
    [SerializeField] private GameObject coinObject;
    [SerializeField] private GameObject hintObject;
    [SerializeField] private GameObject openGift;
    [SerializeField] private RectTransform closedGift;

    [SerializeField] private RectTransform giftTarget;
    [SerializeField] private RectTransform rewardTarget;
    [SerializeField] private float anticipationTime = 0.12f;
    [SerializeField] private float bounceDuration = 0.4f;
    [SerializeField] private float bounceStrength = 0.3f;
    [SerializeField] private int bounceVibrato = 6;
    [SerializeField, Range(0f, 1f)] private float bounceElasticity = 0.5f;
    [SerializeField] private float rewardStartScale = 0.5f;
    [SerializeField] private float rewardEndScale = 1f;
    [SerializeField] private Ease rewardScaleEase = Ease.OutBack;
    [SerializeField] private float revealDuration = 0.5f;
    [SerializeField] private RectTransform rewardStart;
    public event Action<RewardType, int> OnRewardGranted;

    private Coroutine routine;

    private MoveToTarget giftMover, coinMover, hintMover;

    private Vector3 openGiftBaseScale = Vector3.one;
    private Vector3 closedGiftBaseLocalPos, closedGiftBaseScale = Vector3.one;
    private Quaternion closedGiftBaseRot = Quaternion.identity;
    private Vector3 coinBasePos, coinBaseScale = Vector3.one;
    private Vector3 hintBasePos, hintBaseScale = Vector3.one;

    private void Awake()
    {
        if (closedGift != null)
        {
            giftMover = GetOrAddMover(closedGift.gameObject);
            closedGiftBaseLocalPos = closedGift.localPosition;
            closedGiftBaseScale = closedGift.localScale;
            closedGiftBaseRot = closedGift.localRotation;
        }

        if (openGift != null) openGiftBaseScale = openGift.transform.localScale;

        if (coinObject != null)
        {
            coinMover = GetOrAddMover(coinObject);
            coinBasePos = coinObject.transform.position;
            coinBaseScale = coinObject.transform.localScale;
        }

        if (hintObject != null)
        {
            hintMover = GetOrAddMover(hintObject);
            hintBasePos = hintObject.transform.position;
            hintBaseScale = hintObject.transform.localScale;
        }
    }

    private static MoveToTarget GetOrAddMover(GameObject go)
    {
        var m = go.GetComponent<MoveToTarget>();
        return m != null ? m : go.AddComponent<MoveToTarget>();
    }

    private void OnDisable()
    {
        KillTweens();
        if (routine != null) { StopCoroutine(routine); routine = null; }
    }

    public void Play(int levelNumber, Action onDone = null)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run(levelNumber, onDone));
    }

    private IEnumerator Run(int levelNumber, Action onDone)
    {
        ResetState();
        processReward.Show(levelNumber);
        giftReward.Show();

        bool done = false;
        starReward.Play(_ => done = true);
        yield return new WaitUntil(() => done);

        done = false;
        processReward.Fill(levelNumber, () => done = true);
        yield return new WaitUntil(() => done);

        if (rewardData.IsGiftLevel(levelNumber))
        {
            if (giftMover != null && giftTarget != null)
            {
                yield return closedGift
                    .DOScale(new Vector3(closedGiftBaseScale.x * 1.12f, closedGiftBaseScale.y * 0.85f, closedGiftBaseScale.z), anticipationTime)
                    .SetEase(Ease.OutQuad).SetUpdate(true).SetTarget(closedGift)
                    .WaitForCompletion();

                yield return giftMover.Play(closedGift.position, giftTarget).WaitForCompletion();
            }

            if (closedGift != null)
            {
                closedGift.localScale = closedGiftBaseScale;
                yield return closedGift
                    .DOPunchScale(Vector3.one * bounceStrength, bounceDuration, bounceVibrato, bounceElasticity)
                    .SetUpdate(true).SetTarget(closedGift)
                    .WaitForCompletion();
            }

            giftReward.Hide();
            if (openGift != null)
            {
                if (giftTarget != null) openGift.transform.position = giftTarget.position;
                openGift.transform.localScale = openGiftBaseScale;
                openGift.SetActive(true);
            }

            // Loop đầu: coin + hint. Loop sau: random coin hoặc hint.
            List<RewardType> granted = GrantRewards(levelNumber);
            foreach (RewardType type in granted)
                yield return PlayRewardObject(type);

            if (revealDuration > 0f)
                yield return new WaitForSecondsRealtime(revealDuration);
        }

        routine = null;
        onDone?.Invoke();
    }

    private List<RewardType> GrantRewards(int levelNumber)
    {
        var result = new List<RewardType>();

        if (rewardData.IsFirstLoop(levelNumber))
        {
            GrantCoin(rewardData.FirstLoopCoin);
            result.Add(RewardType.Coin);

            GrantHint(rewardData.FirstLoopHint);
            result.Add(RewardType.Hint);
        }
        else if (rewardData.RollIsHint())
        {
            GrantHint(rewardData.OtherLoopHint);
            result.Add(RewardType.Hint);
        }
        else
        {
            GrantCoin(rewardData.RollOtherLoopCoin());
            result.Add(RewardType.Coin);
        }

        return result;
    }

    private void GrantCoin(int amount)
    {
        AddCoin(amount);
        Debug.Log($"[Reward] Coin +{amount}");
        OnRewardGranted?.Invoke(RewardType.Coin, amount);
    }

    private void GrantHint(int amount)
    {
        AddHint(amount);
        Debug.Log($"[Reward] Hint +{amount}");
        OnRewardGranted?.Invoke(RewardType.Hint, amount);
    }

    private IEnumerator PlayRewardObject(RewardType type)
    {
        bool isCoin = type == RewardType.Coin;

        GameObject obj = isCoin ? coinObject : hintObject;
        MoveToTarget mover = isCoin ? coinMover : hintMover;
        Vector3 baseScale = isCoin ? coinBaseScale : hintBaseScale;

        if (coinObject != null) coinObject.SetActive(isCoin);
        if (hintObject != null) hintObject.SetActive(!isCoin);

        if (obj == null || mover == null || rewardTarget == null) yield break;

        Transform t = obj.transform;

        RectTransform startRt = rewardStart != null ? rewardStart : giftTarget;
        Vector3 startPos = startRt != null ? startRt.position : t.position;

        if (Vector3.Distance(startPos, rewardTarget.position) < 1f)
            Debug.LogWarning("[Reward] Điểm bắt đầu và Reward Target trùng nhau nên sẽ không thấy di chuyển!", this);

        t.position = startPos;
        t.localScale = baseScale * rewardStartScale;

        Tween move = mover.Play(startPos, rewardTarget);
        t.DOScale(baseScale * rewardEndScale, mover.Duration)
            .SetEase(rewardScaleEase)
            .SetUpdate(true)
            .SetTarget(t);

        yield return move.WaitForCompletion();
    }

    public void ResetState()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        KillTweens();

        starReward.Stop();
        starReward.ResetStars();
        processReward.Hide();
        giftReward.Hide();
        HideRewardObjects();

        if (closedGift != null)
        {
            closedGift.localPosition = closedGiftBaseLocalPos;
            closedGift.localScale = closedGiftBaseScale;
            closedGift.localRotation = closedGiftBaseRot;
        }

        if (openGift != null)
        {
            openGift.transform.localScale = openGiftBaseScale;
            openGift.SetActive(false);
        }

        if (coinObject != null)
        {
            coinObject.transform.position = coinBasePos;
            coinObject.transform.localScale = coinBaseScale;
        }

        if (hintObject != null)
        {
            hintObject.transform.position = hintBasePos;
            hintObject.transform.localScale = hintBaseScale;
        }
    }

    private void KillTweens()
    {
        if (closedGift != null) closedGift.DOKill();
        if (giftReward != null) giftReward.transform.DOKill();
        if (openGift != null) openGift.transform.DOKill();
        if (coinObject != null) coinObject.transform.DOKill();
        if (hintObject != null) hintObject.transform.DOKill();
    }

    private void HideRewardObjects()
    {
        if (coinObject != null) coinObject.SetActive(false);
        if (hintObject != null) hintObject.SetActive(false);
    }

    private void AddCoin(int amount)
    {
        if (amount <= 0) return;
        PendingReward.Add(amount);
    }

    private void AddHint(int amount)
    {
        if (amount <= 0) return;
        if (hintPresenter != null) hintPresenter.AddHint(amount);
        else SaveGame.HintBalance += amount;
    }
}