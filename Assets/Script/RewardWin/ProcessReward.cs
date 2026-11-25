using System;
using System.Collections;
using UnityEngine;

public class ProcessReward : MonoBehaviour
{
    [SerializeField] private RewardWin rewardData;
    [SerializeField] private GameObject processPanel;
    [SerializeField] private ProcessBar processBar;
    [SerializeField] private float waitAfterFill = 0.5f;

    private Coroutine routine;

    public void Show(int levelNumber)
    {
        rewardData.GetLoop(levelNumber, out int current, out int total);
        processPanel.SetActive(true);
        processBar.SetProgress(current - 1, total, false);
    }

    public void Fill(int levelNumber, Action onDone = null)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FillRoutine(levelNumber, onDone));
    }

    public void Hide()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        if (processPanel != null) processPanel.SetActive(false);
    }

    private IEnumerator FillRoutine(int levelNumber, Action onDone)
    {
        rewardData.GetLoop(levelNumber, out int current, out int total);
        processBar.SetProgress(current, total, true);
        yield return new WaitForSecondsRealtime(waitAfterFill);
        routine = null;
        onDone?.Invoke();
    }
}