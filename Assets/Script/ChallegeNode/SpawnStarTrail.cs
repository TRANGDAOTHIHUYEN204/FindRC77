
using System;
using UnityEngine;

public class SpawnStarTrail : MonoBehaviour
{
    [SerializeField] private FactoryManager factoryManager;
    [SerializeField] private RectTransform container;
    [SerializeField] private int initialCount = 2;

    private StarTrailEffect[] _trails;
    private bool[] _isUsing;

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        if (_trails != null)
            return;

        int count = Mathf.Max(1, initialCount);

        _trails = new StarTrailEffect[count];
        _isUsing = new bool[count];

        if (factoryManager == null)
        {
            Debug.LogError(
                "[SpawnStarTrail] FactoryManager chưa được gán.", this);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            StarTrailEffect trail = factoryManager.CreateStarTrail();

            if (trail == null)
            {
                Debug.LogError(
                    "[SpawnStarTrail] Không tạo được StarTrail.", this);
                continue;
            }

            _trails[i] = trail;

            if (container != null)
                trail.transform.SetParent(container, false);

            trail.gameObject.SetActive(false);
        }
    }

    public bool Play(
        RectTransform startPoint,
        RectTransform endPoint,
        Action onComplete)
    {
        if (startPoint == null || endPoint == null)
            return false;

        if (_trails == null)
            InitializePool();

        if (_trails == null)
            return false;

        for (int i = 0; i < _trails.Length; i++)
        {
            if (_isUsing[i] || _trails[i] == null)
                continue;

            int index = i;
            StarTrailEffect trail = _trails[index];

            _isUsing[index] = true;
            trail.gameObject.SetActive(true);
            trail.transform.SetAsLastSibling();

            trail.Play(startPoint, endPoint, () =>
            {
                _isUsing[index] = false;

                if (trail != null)
                    trail.gameObject.SetActive(false);

                onComplete?.Invoke();
            });

            return true;
        }
        
        return false;
    }
}
