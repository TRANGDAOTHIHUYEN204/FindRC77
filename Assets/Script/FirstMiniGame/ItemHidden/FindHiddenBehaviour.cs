using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider2D))]
public class FindHiddenBehaviour : MonoBehaviour
{
    private const float FlyDuration = 0.6f;
    private static readonly AnimationCurve FlyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Image _image;
    private RectTransform _rect;
    private BoxCollider2D _col;
    private Vector3 _originalScale;

    public bool IsFound { get; private set; }
    public int SlotIndex { get; private set; }
    public event Action<FindHiddenBehaviour> OnArrived;
    public FindHiddenData Data { get; private set; }

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _col = GetComponent<BoxCollider2D>();
        _image = GetComponentInChildren<Image>(true);
        _originalScale = _rect.localScale;
    }

    public void Init(Vector3 anchoredPosition, FindHiddenData data, int slotIndex)
    {
        OnArrived = null;
        Data = data;
        SlotIndex = slotIndex;
        StopAllCoroutines();
        gameObject.SetActive(true);  
        _rect.anchoredPosition3D = anchoredPosition;
        _rect.localScale = _originalScale;
        IsFound = false;

        _image.sprite = data.SpriteItem;
        _rect.sizeDelta = data.Size;    

        _col.size = data.Size;              
        _col.offset = _rect.rect.center;
        _col.enabled = true;
    }

    public bool IsHit(Vector2 screenPoint, Camera cam)
    {
        if (IsFound) return false;

        Vector2 worldPoint = cam != null
            ? (Vector2)cam.ScreenToWorldPoint(screenPoint)
            : screenPoint;

        return _col.OverlapPoint(worldPoint);
    }

    public void Collect(Vector3 targetWorldPos)
    {
        if (IsFound) return;
        IsFound = true;
        _col.enabled = false;
        StartCoroutine(FlyRoutine(targetWorldPos));
    }

    private IEnumerator FlyRoutine(Vector3 target)
    {
        Vector3 start = _rect.position;
        Vector3 startScale = _rect.localScale;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / FlyDuration;
            float k = FlyCurve.Evaluate(Mathf.Clamp01(t));

            _rect.position = Vector3.Lerp(start, target, k);
            _rect.localScale = Vector3.Lerp(startScale, startScale * 0.5f, k);
            yield return null;
        }

        _rect.position = target;
        OnArrived?.Invoke(this);
    }
}