using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class StarRewardManager : MonoBehaviour
{
    [SerializeField] private HeartLiveManager heartManager;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private StarBehaviour[] stars = new StarBehaviour[3];
    [SerializeField] private int[] heartsRequired = { 1, 2, 3 };
    [SerializeField] private bool tutorialAlwaysFullStars = true;

    [SerializeField] private float startDelay = 0.3f;
    [SerializeField] private float delayBetweenStars = 0.35f;
    public UnityEvent<int> onRewardFinished;

    private Coroutine routine;

    private bool IgnoreHearts =>
        tutorialAlwaysFullStars && levelManager != null && levelManager.IsTutorial;

    private void Awake()
    {
        if (heartsRequired.Length != stars.Length)
        {
            int[] fixedArray = new int[stars.Length];
            for (int i = 0; i < fixedArray.Length; i++)
                fixedArray[i] = i < heartsRequired.Length ? heartsRequired[i] : i + 1;
            heartsRequired = fixedArray;
        }
    }

    private void OnDisable()
    {
        Stop();
    }
    
    public void Play(Action<int> onDone = null)
    {
        int hearts = heartManager != null ? heartManager.CurrentLives : 0;
        Play(hearts, onDone);
    }

    public void Play(int heartsLeft, Action<int> onDone = null)
    {
        Stop();
        routine = StartCoroutine(ShowRoutine(heartsLeft, onDone));
    }

    public void Stop()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
    }

    public bool CanActivateStar(int index, int heartsLeft)
    {
        if (index < 0 || index >= stars.Length) return false;
        if (IgnoreHearts) return true;
        return heartsLeft >= heartsRequired[index];
    }

    public int CountEarnedStars(int heartsLeft)
    {
        int count = 0;
        for (int i = 0; i < stars.Length; i++)
            if (CanActivateStar(i, heartsLeft)) count++;
        return count;
    }

    private IEnumerator ShowRoutine(int heartsLeft, Action<int> onDone)
    {
        yield return null;
        ResetStars();

        if (startDelay > 0f) yield return new WaitForSecondsRealtime(startDelay);

        int earned = 0;
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            if (!CanActivateStar(i, heartsLeft)) continue;

            stars[i].Play();
            earned++;
            yield return new WaitForSecondsRealtime(delayBetweenStars);
        }

        routine = null;
        onRewardFinished?.Invoke(earned);
        onDone?.Invoke(earned);
    }

    public void ResetStars()
    {
        foreach (var star in stars)
            if (star != null) star.ResetStar();
    }
}