using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;


public class RewardIntervalWin : MonoBehaviour
{
    [SerializeField] private RewardWin rewardData;
    [SerializeField] private StarRewardManager starReward;
    [SerializeField] private ProcessReward processReward;
    [SerializeField] private GiftReward giftReward;

    [SerializeField] private List<TextMeshProUGUI> textLevel;
    [SerializeField] private List<GameObject> imageReward;

    [SerializeField] private GameObject rewardObject;
    
    [SerializeField] private float rewardScaleDuration = 0.35f;
    private int countHint;
    private int countCoin;
    private int _claimedLevel = -1;
    private int _totalLevels = int.MaxValue;
    [SerializeField] private StreakUI streakUI;
    
    public bool IsGiftLevel(int levelNumber)
        => levelNumber >= 1
           && levelNumber <= _totalLevels
           && rewardData.IsGiftLevel(levelNumber);
    public void Setup(int totalLevels)
    {
        _totalLevels = Mathf.Max(1, totalLevels);
        _claimedLevel = -1;
    }
    public void StopAnimation()
    {
        starReward.Stop();
        processReward.StopAnimation();
        rewardObject.transform.DOKill();
        if (streakUI != null) streakUI.Stop();
    }

    public void Play(int levelNumber)
    {
        StopAnimation();

        UpdateLevelText(levelNumber);
        PrepareReward(levelNumber);

        processReward.ResetVisual();
        if (streakUI != null) streakUI.Prepare();   

        starReward.Play(earned =>
        {
            processReward.PlayAnimation(() =>
            {
                ShowReward(levelNumber);
            });
        });
    }

    private void ShowReward(int levelNumber)
    {
        if (!IsGiftLevel(levelNumber))
        {
            PlayStreak();
            return;
        }

        rewardObject.SetActive(true);

        Transform t = rewardObject.transform;
        t.DOKill();
        t.localScale = Vector3.one * 0.5f;
        t.DOScale(1f, rewardScaleDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true)
            .OnComplete(PlayStreak);
    }

    private void PlayStreak()
    {
        if (streakUI != null) streakUI.PlayStreak();
    }
    private void UpdateLevelText(int levelNumber)
    {
        int centerIndex = textLevel.Count / 2;

        for (int i = 0; i < textLevel.Count; i++)
        {
            int level = levelNumber + i - centerIndex;

            bool isValid = level >= 1 && level <= _totalLevels;
            bool isGift = isValid && rewardData.IsGiftLevel(level);

            textLevel[i].text = level.ToString();
            textLevel[i].gameObject.SetActive(isValid && !isGift);
            imageReward[i].SetActive(isGift);
        }
    }

    private void PrepareReward(int levelNumber)
    {
        rewardObject.SetActive(false);
        rewardObject.transform.localScale = Vector3.one * 0.5f;
        if (!IsGiftLevel(levelNumber))
        {
            countCoin = 0;
            countHint = 0;
            giftReward.SetReward(0, 0);
            return;
        }

        if (_claimedLevel != levelNumber)
        {
            rewardData.GetReward(levelNumber, out countCoin, out countHint);
            
            if (countCoin > 0) PendingReward.Add(countCoin);
            if (countHint > 0) SaveGame.HintBalance += countHint;

            _claimedLevel = levelNumber;
        }

        giftReward.SetReward(countCoin, countHint);
    }
    
    
    private void OnDisable()
    {
        StopAnimation();
    }
}