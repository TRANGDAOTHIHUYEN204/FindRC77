using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragHandler2D : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public bool CanDrag { get; set; } = true;
    public ScreenBoudaries Boundaries { get; set; }

    public event Action OnDragStarted;
    public event Action OnDragEnded;
    public event Action OnDragCancelled;

    [SerializeField] private float liftAmount = 3f;
    private RectTransform rect;
    private RectTransform parentRect;
    private Vector2 offset;
    private bool dragging;
    public static bool Locked { get; set; } 

    private void Awake()
    {
        rect = (RectTransform)transform;
        parentRect = rect.parent as RectTransform;
    }

    private void OnDisable()
    {
        dragging = false;
    }

    private bool Pointer(PointerEventData e, out Vector2 local, out Vector3 world)
    {
        local = default;
        world = default;

        if (parentRect == null) parentRect = rect.parent as RectTransform;
        if (parentRect == null) return false;

        var cam = e.pressEventCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, e.position, cam, out local)
            && RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, e.position, cam, out world);
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!CanDrag || Locked) return;  
        if (!Pointer(e, out var local, out _)) return;

        dragging = true;

        Vector3 pos = rect.localPosition;
        pos.y += liftAmount;
        rect.localPosition = pos;

        offset = (Vector2)pos - local;

        OnDragStarted?.Invoke();
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging) return;
        if (Locked) { CancelDrag(); return; }
        if (!Pointer(e, out var local, out var world)) return;

        if (Boundaries != null && !Boundaries.IsInside(world))
        {
            CancelDrag();
            return;
        }

        Vector2 target = local + offset;
        rect.localPosition = new Vector3(target.x, target.y, rect.localPosition.z);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;
        OnDragEnded?.Invoke();
    }

    private void CancelDrag()
    {
        dragging = false;
        OnDragCancelled?.Invoke();
    }
}