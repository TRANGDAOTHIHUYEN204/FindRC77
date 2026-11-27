using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StreakUI : MonoBehaviour
{
    [SerializeField] private StreakManager streakManager;
    [SerializeField] private TMP_Text moreWinText;
    [SerializeField] private ProcessStreakReward processReward;
    [SerializeField] private ProcessStreak processStreak;
    [SerializeField] private List<Image> mileStone;
    [SerializeField] private Sprite streakReceived;
    [SerializeField] private Sprite nextStreak;
    [SerializeField] private GameObject streakObject;
    [SerializeField] private GameObject streakEnd;
    [SerializeField] private TextMeshProUGUI rewardStreak;
    [SerializeField] private float popScale = 1.35f;
    [SerializeField] private float popUpDuration = 0.15f;
    [SerializeField] private float popBackDuration = 0.25f;
    [SerializeField] private GameObject rewardStreakParent;
    [SerializeField] private float rewardPopDuration = 0.3f;
    [SerializeField] private float rewardStartScale = 0.5f;
    [SerializeField] private AudioClip milestoneClip;
    [SerializeField] private AudioClip rewardPopClip;

    private Sprite[] _defaultSprites;
    private Vector3[] _baseScales;
    private Vector3 _rewardParentBaseScale = Vector3.one;
    private Vector3 _rewardTextBaseScale = Vector3.one;
    private Coroutine _routine;

    private void Awake()
    {
        _defaultSprites = new Sprite[mileStone.Count];
        _baseScales = new Vector3[mileStone.Count];

        for (int i = 0; i < mileStone.Count; i++)
        {
            if (mileStone[i] == null) continue;

            _defaultSprites[i] = mileStone[i].sprite;
            _baseScales[i] = mileStone[i].transform.localScale;
        }

        if (rewardStreakParent != null)
            _rewardParentBaseScale = rewardStreakParent.transform.localScale;

        if (rewardStreak != null)
            _rewardTextBaseScale = rewardStreak.transform.localScale;
    }

    private void OnEnable() => Prepare();

    private void OnDisable() => Stop();

    public void Stop()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (processStreak != null)
            processStreak.Stop();

        ResetScales();
    }

    private bool ShouldShowStreakEnd()
    {
        return streakManager.IsCompleted
            && streakManager.PreviousStreak >= streakManager.MaxStreak;
    }

    public void Prepare()
    {
        Stop();

        if (streakManager == null)
        {
            Debug.LogError("[StreakUI] StreakManager reference is missing.", this);
            return;
        }

        int prev = streakManager.PreviousStreak;
        int cur = streakManager.CurrentStreak;

        if (rewardStreakParent != null)
            rewardStreakParent.SetActive(false);

        if (ShouldShowStreakEnd())
        {
            ApplyInstant(cur, true);
            return;
        }

        if (cur <= prev)
        {
            ApplyInstant(cur, false);
            return;
        }

        ApplyInstant(prev, false);
    }
    public void RefreshMoreWin()
    {
        if (streakManager == null) return;

        ShowMoreWin(streakManager.CurrentStreak);
    }
    public void PlayStreak(Action onDone = null)
    {
        if (streakManager == null)
        {
            Debug.LogError("[StreakUI] StreakManager reference is missing.", this);

            onDone?.Invoke();
            return;
        }

        int prev = streakManager.PreviousStreak;
        int cur = streakManager.CurrentStreak;

        if (ShouldShowStreakEnd())
        {
            ApplyInstant(cur, true);

            onDone?.Invoke();
            return;
        }

        if (cur <= prev)
        {
            onDone?.Invoke();
            return;
        }

        if (_routine != null)
            StopCoroutine(_routine);

        streakManager.MarkStreakShown();

        _routine = StartCoroutine(PlayRoutine(prev, cur, onDone));
    }

    private IEnumerator PlayRoutine(int prev, int cur, Action onDone)
    {
        if (streakManager.IsMilestone(cur))
            HideMoreWin();

        int pending = 2;

        if (processStreak != null)
            processStreak.Play(cur, () => pending--);
        else
            pending--;

        if (processReward != null)
            processReward.Play(cur, () => pending--);
        else
            pending--;

        while (pending > 0)
            yield return null;

        bool hitMilestone = false;
        StreakManager.Milestone hitMs = default;

        var ms = streakManager.Milestones;
        int count = Mathf.Min(mileStone.Count, ms.Count);

        for (int i = 0; i < count; i++)
        {
            if (mileStone[i] == null) continue;

            if (!(prev < ms[i].streak && ms[i].streak <= cur))
                continue;

            hitMilestone = true;
            hitMs = ms[i];

            Transform t = mileStone[i].transform;
            Vector3 baseScale = _baseScales[i];

            Sequence seq = DOTween.Sequence().SetUpdate(true);

            seq.Append(
                t.DOScale(baseScale * popScale, popUpDuration)
                    .SetEase(Ease.OutQuad)
            );

            seq.AppendCallback(() =>
            {
                ApplySprites(cur);
                PlaySfx(milestoneClip);
            });

            seq.Append(
                t.DOScale(baseScale, popBackDuration)
                    .SetEase(Ease.OutBack)
            );

            yield return seq.WaitForCompletion();
        }

        ApplyInstant(cur, false);

        if (hitMilestone)
            SetRewardText(hitMs);

        if (rewardStreakParent != null)
            rewardStreakParent.SetActive(false);

        if (hitMilestone)
        {
            Tween rewardPop = PopRewardObject();

            if (rewardPop != null)
                yield return rewardPop.WaitForCompletion();
        }

        _routine = null;

        onDone?.Invoke();
    }

    private Tween PopRewardObject()
    {
        if (rewardStreakParent == null)
            return null;

        rewardStreakParent.transform.DOKill();
        rewardStreakParent.SetActive(true);

        rewardStreakParent.transform.localScale =
            _rewardParentBaseScale * rewardStartScale;

        PlaySfx(rewardPopClip);

        Tween t = rewardStreakParent.transform
            .DOScale(_rewardParentBaseScale, rewardPopDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        if (rewardStreak != null)
        {
            rewardStreak.transform.DOKill();

            rewardStreak.transform.localScale =
                _rewardTextBaseScale * rewardStartScale;

            rewardStreak.transform
                .DOScale(_rewardTextBaseScale, rewardPopDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        return t;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.PlaySfx(clip);
    }

    private void ShowMoreWin(int streak)
    {
        if (moreWinText == null) return;

        bool atMilestone = streak > 0 && streakManager.IsMilestone(streak);
        bool hasNext = streakManager.TryGetNextMilestone(streak, out var next);

        if (atMilestone || !hasNext)
        {
            moreWinText.gameObject.SetActive(false);
            return;
        }

        int remaining = next.streak - streak;

        moreWinText.text = remaining == 1
            ? "1 more win"
            : $"{remaining} more wins";

        moreWinText.gameObject.SetActive(true);
    }

    private void HideMoreWin()
    {
        if (moreWinText != null)
            moreWinText.gameObject.SetActive(false);
    }

    private void ApplySprites(int current)
    {
        var ms = streakManager.Milestones;
        int count = Mathf.Min(mileStone.Count, ms.Count);
        bool nextAssigned = false;

        for (int i = 0; i < count; i++)
        {
            if (mileStone[i] == null) continue;

            if (current >= ms[i].streak)
            {
                mileStone[i].sprite = streakReceived;
            }
            else if (!nextAssigned)
            {
                mileStone[i].sprite = nextStreak;
                nextAssigned = true;
            }
            else
            {
                mileStone[i].sprite = _defaultSprites[i];
            }
        }
    }

    private void UpdateRewardText(int current)
    {
        if (rewardStreak == null) return;

        if (!streakManager.TryGetNextMilestone(current, out var next))
        {
            rewardStreak.text = "";
            return;
        }

        SetRewardText(next);
    }
    private void ApplyInstant(int streak, bool completed)
    {
        if (streakObject != null)
            streakObject.SetActive(!completed);

        if (streakEnd != null)
            streakEnd.SetActive(completed);

        ApplySprites(streak);
        UpdateRewardText(streak);

        ShowMoreWin(streakManager.CurrentStreak);

        if (processReward != null)
            processReward.SetInstant(streak);

        if (processStreak != null)
            processStreak.SetInstant(streak);
    }
    private void SetRewardText(StreakManager.Milestone m)
    {
        if (rewardStreak == null) return;

        int coin = m.hintAsCoin * streakManager.HintPrice;
        string text = "";

        if (m.hint > 0)
            text = m.hint == 1 ? "+1 Hint" : $"+{m.hint} Hints";

        if (coin > 0)
            text += (text.Length > 0 ? " " : "") + $"+{coin} Coins";

        rewardStreak.text = text;
    }

    private void ResetScales()
    {
        if (_baseScales == null) return;

        for (int i = 0; i < mileStone.Count; i++)
        {
            if (mileStone[i] == null) continue;

            mileStone[i].transform.DOKill();
            mileStone[i].transform.localScale = _baseScales[i];
        }

        if (rewardStreakParent != null)
        {
            rewardStreakParent.transform.DOKill();

            rewardStreakParent.transform.localScale =
                _rewardParentBaseScale;
        }

        if (rewardStreak != null)
        {
            rewardStreak.transform.DOKill();

            rewardStreak.transform.localScale =
                _rewardTextBaseScale;
        }
    }
}