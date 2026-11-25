using System.Collections;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private HintPresenter hintPresenter;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private StateGame stateGame;

    [SerializeField] private ZoomManager zoom;
    [SerializeField] private SpawnChallengeNode challengeNodes;

    [SerializeField, Min(0f)] private float delayBetweenHints = 0.6f;
    [SerializeField] private GameObject[] hideDuringTutorial;
    [SerializeField, Min(1)] private int maxHints = 3;

    [Header("Nhịp tutorial")]
    [Tooltip("Thời gian chờ khi mới vào level (khoá tương tác) trước khi zoom vào điểm đầu tiên.")]
    [SerializeField, Min(0f)] private float startDelay = 1f;
    [Tooltip("Tỉ lệ phần phóng to so với zoom gợi ý thường. 0.5 = zoom một nửa (2.5x → 1.75x).")]
    [SerializeField, Range(0.1f, 1f)] private float zoomScale = 0.5f;
    [Tooltip("Chờ thêm sau khi dấu tích hiện ra (cho hiệu ứng nảy chạy xong) rồi mới sang điểm tiếp theo.")]
    [SerializeField, Min(0f)] private float afterTickDelay = 0.5f;
    [Tooltip("Thời gian tối đa chờ khoanh tròn + particle + dấu tích (phòng khi hiệu ứng bị lỗi).")]
    [SerializeField, Min(0.5f)] private float maxWaitFx = 4f;

    public bool IsRunning { get; private set; }

    private bool _quitting;
    private bool _inputBlocked;
    private System.Func<bool> _blockAdsFunc;

    private void OnEnable()
    {
        if (levelManager != null)
            levelManager.OnLevelLoaded += OnLevelLoaded;

        // Chặn inter ad trong lúc tutorial đang chạy
        _blockAdsFunc = () => IsRunning;
        if (AdsController.Instance != null)
            AdsController.Instance.IsBlocked = _blockAdsFunc;
    }

    private void Start() => StartCoroutine(RunTutorial());

    private void OnLevelLoaded(LevelData data)
    {
        if (!levelManager.IsTutorial)
            SetUIVisible(true);
    }

    private IEnumerator RunTutorial()
    {
        yield return null;
        if (!levelManager.IsTutorial) yield break;
        
        yield return new WaitUntil(() => pairManagerHolder.Manager != null);

        SaveGame.TutorialCompleted = true;
        PlayerPrefs.Save();

        IsRunning = true;
        SetUIVisible(false);

        if (zoom == null) zoom = FindFirstObjectByType<ZoomManager>();
        if (challengeNodes == null) challengeNodes = FindFirstObjectByType<SpawnChallengeNode>();

        // 1) Mới vào level: đứng yên một chút, không cho bấm / zoom.
        BlockInput(true);
        if (zoom != null) zoom.Lock();
        yield return new WaitForSecondsRealtime(startDelay);
        BlockInput(false);

        hintPresenter.KeepZoomOnClear = true;
        float tutorialZoom = zoom != null ? 1f + (zoom.FocusZoom - 1f) * zoomScale : -1f;

        int hintsShown = 0;

        while (hintsShown < maxHints)
        {
            var manager = pairManagerHolder.Manager;
            if (manager != null && manager.IsComplete) break;

            // 2) Zoom (nhẹ) vào điểm khác biệt + bàn tay chỉ.
            hintPresenter.ShowTutorialHint(tutorialZoom);
            hintsShown++;

            while (hintPresenter.HintedPair != null)
                yield return null;

            // 3) Đã bấm trúng: khoá tương tác, giữ khung zoom, chờ khoanh tròn
            //    → particle bay xuống → dấu tích hiện ra.
            BlockInput(true);
            yield return WaitForTicks();
            yield return new WaitForSecondsRealtime(afterTickDelay);

            manager = pairManagerHolder.Manager;
            bool done = manager != null && manager.IsComplete;

            // 4) Zoom ra rồi mới chuyển sang điểm tiếp theo.
            hintPresenter.ResetView();
            if (zoom != null)
            {
                yield return new WaitUntil(() => !zoom.IsFocusing);
                if (!done && hintsShown < maxHints) zoom.Lock();
            }

            if (done) { BlockInput(false); break; }

            yield return new WaitForSecondsRealtime(delayBetweenHints);
            BlockInput(false);
        }

        hintPresenter.KeepZoomOnClear = false;
        if (zoom != null && !zoom.IsFocusing) zoom.Unlock();

        yield return new WaitUntil(() =>
        {
            var m = pairManagerHolder.Manager;
            return m != null && m.IsComplete;
        });

        EndTutorial(completed: true);
    }

    /// Chờ cho tới khi số dấu tích đã hiện bằng số điểm đã tìm được.
    private IEnumerator WaitForTicks()
    {
        float t = 0f;
        while (t < maxWaitFx)
        {
            var m = pairManagerHolder.Manager;
            if (m == null || challengeNodes == null) yield break;
            if (challengeNodes.AppliedCount >= m.FoundCount) yield break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void BlockInput(bool block)
    {
        if (block == _inputBlocked) return;
        _inputBlocked = block;
        if (block) UIInputBlocker.RegisterOpen();
        else UIInputBlocker.RegisterClose(0f);
    }

    private void EndTutorial(bool completed)
    {
        if (!IsRunning) return;
        IsRunning = false;

        BlockInput(false);
        if (hintPresenter != null)
        {
            hintPresenter.KeepZoomOnClear = false;
            hintPresenter.ResetView();
        }

        if (!completed)
            SetUIVisible(true);
    }

    private void SetUIVisible(bool visible)
    {
        if (hideDuringTutorial == null) return;

        foreach (var go in hideDuringTutorial)
            if (go != null) go.SetActive(visible);
    }

    private void OnDisable()
    {
        if (levelManager != null)
            levelManager.OnLevelLoaded -= OnLevelLoaded;

        var ads = AdsController.Instance;
        if (ads != null && ads.IsBlocked == _blockAdsFunc)
            ads.IsBlocked = null;

        if (!IsRunning) return;
        IsRunning = false;
        StopAllCoroutines();
        BlockInput(false);
        if (_quitting) return;

        if (hintPresenter != null)
        {
            hintPresenter.KeepZoomOnClear = false;
            hintPresenter.ClearHint();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) PlayerPrefs.Save();
    }

    private void OnApplicationQuit() => _quitting = true;
}