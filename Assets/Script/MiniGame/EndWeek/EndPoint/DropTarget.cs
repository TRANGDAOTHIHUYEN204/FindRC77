using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

public class DropTarget : MonoBehaviour
{

    private static readonly List<DropTarget> all = new();
    [SerializeField] private string acceptedType;
    [SerializeField] private RectTransform dropArea;  
    [SerializeField] private RectTransform snapPoint; 
    [SerializeField] private Vector2 areaPadding = Vector2.zero;

    public UnityEvent onAccepted;
    public UnityEvent onRejected;
    public static event Action<DropTarget> AnyAccepted;
    public static event Action<DropTarget> AnyRejected;

    private RectTransform rect;

    private RectTransform Area
    {
        get
        {
            return snapPoint ;
        }
    }

    public Vector3 SnapPosition => snapPoint != null ? snapPoint.position : Area.position;

    private void Awake() => rect = (RectTransform)transform;
    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    public bool Accepts(string itemType) => acceptedType == itemType;

    public void NotifyAccepted()
    {
        onAccepted?.Invoke();
        AnyAccepted?.Invoke(this);
    }

    public void NotifyRejected()
    {
        AnyRejected?.Invoke(this);
        onRejected?.Invoke();
    }

    public bool Contains(Vector2 screenPoint, Camera cam)
    {
        var area = Area;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPoint, cam, out var local))
            return false;
        Rect r = area.rect;
        r.xMin -= areaPadding.x; r.xMax += areaPadding.x;
        r.yMin -= areaPadding.y; r.yMax += areaPadding.y;
        return r.Contains(local);
    }

    public static DropTarget FindAt(Vector2 screenPoint, Camera cam, string itemType = null)
    {
        DropTarget best = null;
        float bestDist = float.MaxValue;
        bool bestAccepts = false;

        for (int i = 0; i < all.Count; i++)
        {
            var t = all[i];
            if (!t.Contains(screenPoint, cam)) continue;

            bool accepts = itemType != null && t.Accepts(itemType);
            Vector2 center = RectTransformUtility.WorldToScreenPoint(cam, t.SnapPosition);
            float d = (center - screenPoint).sqrMagnitude;
            if ((accepts && !bestAccepts) || (accepts == bestAccepts && d < bestDist))
            {
                best = t;
                bestDist = d;
                bestAccepts = accepts;
            }
        }
        return best;
    }
    
}