
using System;
using UnityEngine;

public class SpawnStarTrail : MonoBehaviour
{
    [SerializeField] private FactoryManager factoryManager;
    [SerializeField] private RectTransform container;
    [SerializeField] private int initialCount = 2;

    private StarTrailEffect[] _trails;
    private bool[] _isUsing;
    private Action[] _callbacks;

    private void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        if (_trails != null)
            return;

        // Pool 2: 1 đang dùng + 1 standby (false) cho click nhanh.
        int count = Mathf.Max(2, initialCount);

        _trails = new StarTrailEffect[count];
        _isUsing = new bool[count];
        _callbacks = new Action[count];

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
            factoryManager.Detach(trail.gameObject);

            if (container != null)
                trail.transform.SetParent(container, false);

            trail.PrepareInactive();
            trail.gameObject.SetActive(false);
            _isUsing[i] = false;
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

        // Mỗi lần Play chỉ bật ĐÚNG 1 trail đang false.
        // Click nhanh: trail đang chạy vẫn giữ; lấy trail standby (false) mang ra dùng.
        int freeIndex = FindFreeTrail();
        if (freeIndex < 0)
            return false;

        StartTrail(freeIndex, startPoint, endPoint, onComplete);
        return true;
    }

    private int FindFreeTrail()
    {
        for (int i = 0; i < _trails.Length; i++)
        {
            StarTrailEffect trail = _trails[i];
            if (trail == null)
                continue;

            // Đang dùng hoặc vẫn đang active → bỏ qua, lấy cái false khác.
            if (_isUsing[i] || trail.gameObject.activeSelf)
                continue;

            return i;
        }

        return -1;
    }

    private void StartTrail(
        int index,
        RectTransform startPoint,
        RectTransform endPoint,
        Action onComplete)
    {
        StarTrailEffect trail = _trails[index];

        _isUsing[index] = true;
        _callbacks[index] = onComplete;

        trail.gameObject.SetActive(true);
        trail.transform.SetAsLastSibling();

        int captured = index;
        trail.Play(startPoint, endPoint, () =>
        {
            ReturnToPool(captured);
        });
    }

    private void ReturnToPool(int index)
    {
        if (index < 0 || index >= _trails.Length)
            return;

        Action callback = _callbacks[index];
        _callbacks[index] = null;
        _isUsing[index] = false;

        StarTrailEffect trail = _trails[index];
        if (trail != null)
        {
            trail.PrepareInactive();
            trail.gameObject.SetActive(false);
        }

        callback?.Invoke();
    }
}
