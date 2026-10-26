using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class DifferentPoint : MonoBehaviour
{
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private OutlineDifferentPoint outline;   

    private RectTransform _root;
    private Vector2 _position;
    private Vector2 _size;
    private DifferentPointInfo _outlineInfo;
    private Vector3[] _outlinePoints;

    public void Init(Vector2 position, Vector2 size, Transform root, Vector3[] outlinePoints)
    {
        _root = root as RectTransform;
        _position = position;
        _size = size;
        _outlinePoints = outlinePoints;

        transform.position = root.TransformPoint(position);

        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        Vector3 rootScale = root.lossyScale;
        transform.localScale = new Vector3(rootScale.x / parentScale.x, rootScale.y / parentScale.y, 1f);

        boxCollider.offset = Vector2.zero;
        boxCollider.size = size;

        if (outline != null) outline.Clear();
    }

    public void ShowOutline() { if (outline != null) outline.Draw(_outlinePoints, _size); }
    public void HideOutline() { if (outline != null) outline.Clear(); }

    public bool IsHit(Vector2 screenPoint, Camera cam)
    {
        if (_root == null) return false;

        var frame = _root.parent as RectTransform;
        if (!RectTransformUtility.RectangleContainsScreenPoint(frame, screenPoint, cam))
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPoint, cam, out Vector2 local))
            return false;

        Vector2 d = local - _position;
        return Mathf.Abs(d.x) <= _size.x * 0.5f && Mathf.Abs(d.y) <= _size.y * 0.5f;
    }
}