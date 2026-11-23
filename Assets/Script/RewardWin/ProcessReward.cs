using System;
using System.Collections;
using UnityEngine;

public class ProcessReward : MonoBehaviour
{
    [SerializeField] private GameObject processPanel;
    [SerializeField] private ProcessBar processBar;
    [SerializeField] private int levelsPerReward = 3;
    [SerializeField] private float waitAfterFill = 0.5f;

    private Coroutine routine;

    private int GetCurrent(int levelNumber)
    {
        int current = levelNumber % levelsPerReward;
        return current == 0 ? levelsPerReward : current;
    }
    
    public void Show(int levelNumber)
    {
        processPanel.SetActive(true);
        processBar.SetProgress(GetCurrent(levelNumber) - 1, levelsPerReward, false);
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
        processBar.SetProgress(GetCurrent(levelNumber), levelsPerReward, true);
        yield return new WaitForSecondsRealtime(waitAfterFill);
        routine = null;
        onDone?.Invoke();
    }
}