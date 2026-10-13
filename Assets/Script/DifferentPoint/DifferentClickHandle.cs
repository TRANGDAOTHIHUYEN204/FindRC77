using System;
using UnityEngine;

public class DifferentClickHandler : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private LevelImageView levelImageView;
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private MarkPresenter markPresenter;
    [SerializeField] private Camera worldCamera;

    private bool _listening;

    public event Action<DifferentPair, MarkData> OnPairFound;
    public event Action OnAllFound;

    public void StartListening()
    {
        if (_listening)
            return;

        inputManager.OnClick += HandleClick;
        _listening = true;
    }

    public void StopListening()
    {
        if (!_listening)
            return;

        inputManager.OnClick -= HandleClick;
        _listening = false;
    }

    private void OnDisable()
    {
        StopListening();
    }

    private void HandleClick(Vector2 screenPoint)
    {
        if (!levelImageView.IsInsideImages(screenPoint))
            return;

        Vector2 worldPoint =
            levelImageView.ScreenToWorldPoint(screenPoint, worldCamera);

        DifferentPairManager manager = pairManagerHolder.Manager;

        if (manager.TryFindRemaining(worldPoint, out int index, out _))
        {
            if (!manager.MarkFound(index, worldPoint, out var entry))
                return;

            markPresenter.DrawPair(entry.Pair, entry.Mark);

            Debug.Log(
                $"Click ĐÚNG ({manager.FoundCount}/{manager.TotalCount})");

            OnPairFound?.Invoke(entry.Pair, entry.Mark);

            if (manager.IsComplete)
            {
                StopListening();
                Debug.Log("Đã tìm đủ!");
                OnAllFound?.Invoke();
            }

            return;
        }

        if (manager.IsInFoundArea(worldPoint))
            return;

        Debug.Log("Click SAI");
    }
}
