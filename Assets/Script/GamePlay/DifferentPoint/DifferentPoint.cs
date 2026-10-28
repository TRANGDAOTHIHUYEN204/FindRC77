using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DifferentPoint : MonoBehaviour
{
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private OutlineDifferentPoint outline;

    private RectTransform _root;
    private Vector2 _position;
    private Vector2 _size;
    private Vector3[] _outlinePoints;
    private bool _outlineLoop;

    public void Init(Vector2 position, Vector2 size, Vector3[] outlinePoints, Transform root, bool outlineLoop = true)
    {
        _root = root as RectTransform;
        _position = position;
        _size = size;
        _outlinePoints = outlinePoints;
        _outlineLoop = outlineLoop;

        transform.position = root.TransformPoint(position);

        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        Vector3 rootScale = root.lossyScale;
        transform.localScale = new Vector3(rootScale.x / parentScale.x, rootScale.y / parentScale.y, 1f);

        boxCollider.offset = Vector2.zero;
        boxCollider.size = size;

        if (outline != null)
        {
            var rt = (RectTransform)outline.transform;
            rt.SetParent(_root, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.SetAsLastSibling(); 

            outline.Clear();
        }
    }

    private void OnDestroy()
    {
        if (outline != null) Destroy(outline.gameObject);
    }

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

    public void ShowOutline()
    {
        if (outline != null) outline.Draw(_outlinePoints, _root, _outlineLoop);
    }

    public void HideOutline(float fadeDuration = -1f)
    {
        if (outline != null) outline.Hide(fadeDuration);
    }
}