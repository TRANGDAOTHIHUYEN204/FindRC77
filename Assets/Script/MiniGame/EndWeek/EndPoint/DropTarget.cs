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
    [SerializeField] private Vector2 areaPadding = new Vector2(200, 200); 
    public UnityEvent onAccepted;
    public UnityEvent onRejected;
    public static event Action<DropTarget> AnyAccepted;
    public static event Action<DropTarget> AnyRejected;
    
    private RectTransform rect;
    private RectTransform Area => dropArea != null ? dropArea : rect;

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
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(Area, screenPoint, cam, out var local))
            return false;
        Rect r = Area.rect;
        r.xMin -= areaPadding.x; r.xMax += areaPadding.x;
        r.yMin -= areaPadding.y; r.yMax += areaPadding.y;
        return r.Contains(local);
    }

    public static DropTarget FindAt(Vector2 screenPoint, Camera cam)
    {
        for (int i = all.Count - 1; i >= 0; i--)
            if (all[i].Contains(screenPoint, cam))
                return all[i];
        return null;
    }
}