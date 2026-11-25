using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public enum RewardType { Hint = 1, Coin = 2 }

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
    [SerializeField] private RectTransform targetPoint;
    [SerializeField] private RectTransform closedGift;
    
    [SerializeField] private float jumpDuration = 0.7f;
    [SerializeField, Range(0.1f, 1.5f)] private float jumpHeightRatio = 0.5f; 
    [SerializeField] private Ease moveEase = Ease.OutSine; 
    [SerializeField] private float anticipationTime = 0.12f;
    [SerializeField] private float landSquashTime = 0.08f; 
    [SerializeField] private float airStretch = 0.15f; 
    [SerializeField] private float airTilt = 10f;  
    
    [SerializeField] private float bounceDuration = 0.4f;
    [SerializeField] private float bounceStrength = 0.3f;
    [SerializeField] private int bounceVibrato = 6;
    [SerializeField, Range(0f, 1f)] private float bounceElasticity = 0.5f;
    [SerializeField] private float revealDuration = 1f;

    public event Action<RewardType, int> OnRewardGranted;

    private Coroutine routine;
    private Vector3 openGiftBaseScale = Vector3.one;
    private Vector3 closedGiftBaseLocalPos;
    private Vector3 closedGiftBaseScale = Vector3.one;
    private Quaternion closedGiftBaseRot = Quaternion.identity;

    private void Awake()
    {
        if (openGift != null) openGiftBaseScale = openGift.transform.localScale;
        if (closedGift != null)
        {
            closedGiftBaseLocalPos = closedGift.localPosition;
            closedGiftBaseScale = closedGift.localScale;
            closedGiftBaseRot = closedGift.localRotation;
        }
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

        if (RewardRule.IsGiftLevel(levelNumber))
        {
            Transform gift = closedGift != null ? closedGift : giftReward.transform;
            
            if (targetPoint != null)
                yield return JumpTo(gift, targetPoint.position);
            
            yield return gift
                .DOPunchScale(Vector3.one * bounceStrength, bounceDuration, bounceVibrato, bounceElasticity)
                .SetUpdate(true)
                .SetTarget(gift)
                .WaitForCompletion();
            
            giftReward.Hide();
            if (openGift != null)
            {
                if (targetPoint != null) openGift.transform.position = targetPoint.position;
                openGift.transform.localScale = openGiftBaseScale;
                openGift.SetActive(true);
            }
            RewardType type = GrantRandomReward();
            ShowRewardObject(type);

            if (revealDuration > 0f)
                yield return new WaitForSecondsRealtime(revealDuration);
        }

        routine = null;
        onDone?.Invoke();
    }

    private IEnumerator JumpTo(Transform gift, Vector3 end)
    {
        Vector3 baseScale = gift.localScale;
        Vector3 start = gift.position;
        float height = Vector3.Distance(start, end) * jumpHeightRatio;
        yield return gift
            .DOScale(new Vector3(baseScale.x * 1.12f, baseScale.y * 0.85f, baseScale.z), anticipationTime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetTarget(gift)
            .WaitForCompletion();
        yield return DOVirtual.Float(0f, 1f, jumpDuration, p =>
            {
                float px = DOVirtual.EasedValue(0f, 1f, p, moveEase);
                Vector3 pos = Vector3.Lerp(start, end, px);
                pos.y += 4f * height * p * (1f - p);
                gift.position = pos;

                float air = Mathf.Sin(p * Mathf.PI);           
                float sx = Mathf.Lerp(1.12f, 1f - airStretch * 0.5f, air);
                float sy = Mathf.Lerp(0.85f, 1f + airStretch, air);
                gift.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);

                gift.localRotation = Quaternion.Euler(0f, 0f,
                    closedGiftBaseRot.eulerAngles.z + Mathf.Sin(p * Mathf.PI * 2f) * airTilt);
            })
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(gift)
            .WaitForCompletion();

        gift.position = end;
        gift.localRotation = closedGiftBaseRot;
        yield return gift
            .DOScale(new Vector3(baseScale.x * 1.15f, baseScale.y * 0.82f, baseScale.z), landSquashTime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetTarget(gift)
            .WaitForCompletion();

        yield return gift
            .DOScale(baseScale, landSquashTime * 1.5f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .SetTarget(gift)
            .WaitForCompletion();
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
    }

    private void KillTweens()
    {
        if (closedGift != null) closedGift.DOKill();
        if (giftReward != null) giftReward.transform.DOKill();
        if (openGift != null) openGift.transform.DOKill();
    }

    private RewardType GrantRandomReward()
    {
        if (Random.Range(1, 3) == 1)
        {
            int amount = rewardData.RewardHint;
            AddHint(amount);
            Debug.Log($"[Reward] Hint +{amount}");
            OnRewardGranted?.Invoke(RewardType.Hint, amount);
            return RewardType.Hint;
        }
        else
        {
            int amount = rewardData.RewardCoin;
            AddCoin(amount);
            Debug.Log($"[Reward] Coin +{amount}");
            OnRewardGranted?.Invoke(RewardType.Coin, amount);
            return RewardType.Coin;
        }
    }

    private void ShowRewardObject(RewardType type)
    {
        if (coinObject != null) coinObject.SetActive(type == RewardType.Coin);
        if (hintObject != null) hintObject.SetActive(type == RewardType.Hint);
    }

    private void HideRewardObjects()
    {
        if (coinObject != null) coinObject.SetActive(false);
        if (hintObject != null) hintObject.SetActive(false);
    }

    private void AddCoin(int amount) => PendingReward.Add(amount);

    private void AddHint(int amount)
    {
        if (amount <= 0) return;
        if (hintPresenter != null) hintPresenter.AddHint(amount);
        else SaveGame.HintBalance += amount;
    }
}