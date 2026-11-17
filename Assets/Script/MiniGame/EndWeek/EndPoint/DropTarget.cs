using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DropTarget : MonoBehaviour
{
    private static readonly List<DropTarget> all = new();

    [SerializeField] private string acceptedType;
    [SerializeField] private RectTransform snapPoint;
    public UnityEvent onAccepted;
    public UnityEvent onRejected;

    private RectTransform rect;

    public Vector3 SnapPosition => snapPoint != null ? snapPoint.position : rect.position;

    private void Awake() => rect = (RectTransform)transform;
    private void OnEnable() => all.Add(this);
    private void OnDisable() => all.Remove(this);

    public bool Accepts(string itemType) => acceptedType == itemType;
    public void NotifyAccepted() => onAccepted?.Invoke();
    public void NotifyRejected() => onRejected?.Invoke();

    public static DropTarget FindAt(Vector2 screenPoint, Camera cam)
    {
        for (int i = all.Count - 1; i >= 0; i--)
        {
            var t = all[i];
            if (RectTransformUtility.RectangleContainsScreenPoint(t.rect, screenPoint, cam))
                return t;
        }
        return null;
    }
}