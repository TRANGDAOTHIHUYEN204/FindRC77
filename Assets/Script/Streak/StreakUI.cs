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
    [SerializeField] private ProcessStreakReward processReward;
    [SerializeField] private ProcessStreak processStreak;
    [SerializeField] private Transform streakPanel;          
    [SerializeField] private List<Image> mileStone;
    [SerializeField] private Sprite streakReceived;
    [SerializeField] private Sprite nextStreak;
    [SerializeField] private GameObject streakObject;
    [SerializeField] private GameObject streakEnd;
    [SerializeField] private TextMeshProUGUI rewardStreak;
    [SerializeField] private float panelPopDuration = 0.35f;
    [SerializeField] private float popScale = 1.35f;
    [SerializeField] private float popUpDuration = 0.15f;
    [SerializeField] private float popBackDuration = 0.25f;

    private Sprite[] _defaultSprites;
    private Vector3[] _baseScales;
    private Vector3 _panelBaseScale = Vector3.one;
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
        if (streakPanel != null) _panelBaseScale = streakPanel.localScale;
    }

    private void OnEnable() => Prepare();

    private void OnDisable() => Stop();

    public void Stop()
    {
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        if (streakPanel != null) streakPanel.DOKill();
        ResetScales();
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

        if (cur <= prev)
        {
            ApplyInstant(cur, streakManager.IsCompleted);
            SetPanelScale(_panelBaseScale);
            return;
        }

        ApplyInstant(prev, false);
        SetPanelScale(Vector3.zero);
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

        if (cur <= prev)
        {
            SetPanelScale(_panelBaseScale);
            onDone?.Invoke();
            return;
        }

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(PlayRoutine(prev, cur, onDone));
    }

    private IEnumerator PlayRoutine(int prev, int cur, Action onDone)
    {
        if (streakPanel != null)
        {
            Tween pop = streakPanel.DOScale(_panelBaseScale, panelPopDuration)
                .SetEase(Ease.OutBack).SetUpdate(true);
            yield return pop.WaitForCompletion();
        }
        
        int pending = 2;
        if (processStreak != null) processStreak.Play(cur, () => pending--); else pending--;
        if (processReward != null) processReward.Play(cur, () => pending--); else pending--;
        while (pending > 0) yield return null;
        
        var ms = streakManager.Milestones;
        int count = Mathf.Min(mileStone.Count, ms.Count);
        for (int i = 0; i < count; i++)
        {
            if (mileStone[i] == null) continue;
            if (!(prev < ms[i].streak && ms[i].streak <= cur)) continue;

            Transform t = mileStone[i].transform;
            Vector3 baseScale = _baseScales[i];

            Sequence seq = DOTween.Sequence().SetUpdate(true);
            seq.Append(t.DOScale(baseScale * popScale, popUpDuration).SetEase(Ease.OutQuad));
            seq.AppendCallback(() => ApplySprites(cur));
            seq.Append(t.DOScale(baseScale, popBackDuration).SetEase(Ease.OutBack));
            yield return seq.WaitForCompletion();
        }

        ApplyInstant(cur, streakManager.IsCompleted);
        _routine = null;
        onDone?.Invoke();
    }

    private void SetPanelScale(Vector3 s)
    {
        if (streakPanel != null) streakPanel.localScale = s;
    }

    private void ApplyInstant(int streak, bool completed)
    {
        if (streakObject != null) streakObject.SetActive(!completed);
        if (streakEnd != null) streakEnd.SetActive(completed);

        ApplySprites(streak);
        UpdateRewardText(streak);

        if (processReward != null) processReward.SetInstant(streak);
        if (processStreak != null) processStreak.SetInstant(streak);
    }

    private void ApplySprites(int current)
    {
        var ms = streakManager.Milestones;
        int count = Mathf.Min(mileStone.Count, ms.Count);
        bool nextAssigned = false;

        for (int i = 0; i < count; i++)
        {
            if (mileStone[i] == null) continue;

            if (current >= ms[i].streak) mileStone[i].sprite = streakReceived;
            else if (!nextAssigned) { mileStone[i].sprite = nextStreak; nextAssigned = true; }
            else mileStone[i].sprite = _defaultSprites[i];
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

        int coin = next.hintAsCoin * streakManager.HintPrice;
        string text = "";
        if (next.hint > 0) text = next.hint == 1 ? "+1 Hint" : $"+{next.hint} Hints";
        if (coin > 0) text += (text.Length > 0 ? " " : "") + $"+{coin} Coins";
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
    }
}