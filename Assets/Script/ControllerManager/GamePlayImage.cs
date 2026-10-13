using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayImage : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private FactoryManager factoryManager;
    [SerializeField] private Image rawImage;
    [SerializeField] private Image errorImage;
    [SerializeField] private Camera cam;

    private struct Entry
    {
        public DifferentPair Pair;
        public MarkData Mark;
    }

    private readonly List<Entry> _remaining = new();   
    private readonly List<DifferentPoint> _listA = new();
    private readonly List<DifferentPoint> _listB = new();
    private int _totalCount;
    private int _foundCount;
    private bool _listening;

    public event Action<DifferentPair, MarkData> OnPairFound;
    public event Action OnAllFound;

    private void OnEnable() => levelManager.OnLevelLoaded += Build;

    private void OnDisable()
    {
        levelManager.OnLevelLoaded -= Build;
        StopListening();
    }

    private void Build(LevelData level)
    {
        factoryManager.ReleaseAll();

        _remaining.Clear();
        _foundCount = 0;

        rawImage.sprite = level.RawSprite;
        errorImage.sprite = level.ErrorSprite;

        rawImage.GetComponentsInChildren(true, _listA);
        errorImage.GetComponentsInChildren(true, _listB);

        int count = Mathf.Min(_listA.Count, _listB.Count);
        int markLast = level.MarkData.Length - 1;

        for (int i = 0; i < count; i++)
        {
            _listA[i].ResetPoint();
            _listB[i].ResetPoint();

            _remaining.Add(new Entry
            {
                Pair = new DifferentPair(_listA[i], _listB[i]), 
                Mark = level.MarkData[Mathf.Min(i, markLast)]
            });
        }

        _totalCount = _remaining.Count;
        StartListening();
    }

    private void HandleClick(Vector2 screenPoint)
    {
        float dist = Mathf.Abs(cam.transform.position.z - rawImage.canvas.transform.position.z);
        Vector2 worldPoint = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, dist));

        for (int i = 0; i < _remaining.Count; i++)
        {
            Entry e = _remaining[i];
            if (!e.Pair.TryHit(worldPoint)) continue;

            _remaining.RemoveAt(i);   
            _foundCount++;
            Log($"Click ĐÚNG ({_foundCount}/{_totalCount})");

            DrawMark(e.Pair.PointA, e.Mark);
            DrawMark(e.Pair.PointB, e.Mark);
            OnPairFound?.Invoke(e.Pair, e.Mark);

            if (_remaining.Count == 0)
            {
                StopListening();     
                Log("Đã tìm đủ!");
                OnAllFound?.Invoke();
            }
            return;
        }

        Log("Click SAI");
    }

    private void DrawMark(DifferentPoint point, MarkData d)
    {
        MarkFound mark = factoryManager.Create();
        mark.Init(d.ShapeMark, point.Center, point.Size, d.LineWidth, d.ColorLine, d.Segments);
    }

    private void StartListening()
    {
        if (_listening) return;
        inputManager.OnClick += HandleClick;
        _listening = true;
    }

    private void StopListening()
    {
        if (!_listening) return;
        inputManager.OnClick -= HandleClick;
        _listening = false;
    }
    [Conditional("UNITY_EDITOR")]
    private static void Log(string message) => UnityEngine.Debug.Log(message);
}