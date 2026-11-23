using System;
using System.Collections;
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
    public event Action<RewardType, int> OnRewardGranted;

    private Coroutine routine;

    private void OnDisable()
    {
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
            done = false;
            giftReward.Play(() => done = true);
            yield return new WaitUntil(() => done);

            GrantRandomReward();
        }

        routine = null;
        onDone?.Invoke();
    }

    public void ResetState()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        starReward.Stop();
        starReward.ResetStars();
        processReward.Hide();
        giftReward.Hide();
    }

    private void GrantRandomReward()
    {
        if (Random.Range(1, 3) == 1)
        {
            int amount = rewardData.RewardHint;
            AddHint(amount);
            OnRewardGranted?.Invoke(RewardType.Hint, amount);
        }
        else
        {
            int amount = rewardData.RewardCoin;
            AddCoin(amount);
            OnRewardGranted?.Invoke(RewardType.Coin, amount);
        }
    }

    private void AddCoin(int amount) => PendingReward.Add(amount);

    private void AddHint(int amount)
    {
        if (amount <= 0) return;
        if (hintPresenter != null) hintPresenter.AddHint(amount);
        else SaveGame.HintBalance += amount;
    }
}