using UnityEngine;

public class DifferentPair : MonoBehaviour
{
    [SerializeField] private DifferentPoint pointA;
    [SerializeField] private DifferentPoint pointB;

    private DifferentPointInfo _info;
    private RectTransform _rootA, _rootB;

    public bool IsFound { get; private set; }
    public DifferentPoint PointA => pointA;
    public DifferentPoint PointB => pointB;
    public Vector3 LocalPosition => _info.Position;
    public Vector2 LocalSize => _info.Size;
    public void Init(DifferentPointInfo data, Transform rootA, Transform rootB)
    {
        IsFound = false;
        _info = data;
        _rootA = rootA as RectTransform;
        _rootB = rootB as RectTransform;
    }
    public bool IsHit(Vector2 screenPoint, Camera cam)
    {
        RectTransform frameA = _rootA.parent as RectTransform;
        RectTransform frameB = _rootB.parent as RectTransform;

        if (RectTransformUtility.RectangleContainsScreenPoint(frameA, screenPoint, cam))
            return Contains(_rootA, screenPoint, cam);

        if (RectTransformUtility.RectangleContainsScreenPoint(frameB, screenPoint, cam))
            return Contains(_rootB, screenPoint, cam);

        return false;
    }

    private bool Contains(RectTransform root, Vector2 screenPoint, Camera cam)
    {
        if (root == null) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, cam, out Vector2 local))
            return false;

        Vector2 d = local - (Vector2)_info.Position;
        return Mathf.Abs(d.x) <= _info.Size.x * 0.5f &&
               Mathf.Abs(d.y) <= _info.Size.y * 0.5f;
    }

    public bool MarkFound()
    {
        if (IsFound) return false;
        IsFound = true;
        return true;
    }
    public void GetWorldRect(RectTransform root, out Vector3 center, out Vector2 size)
    {
        Vector3 c = root.TransformPoint(new Vector3(_info.Position.x, _info.Position.y, 0f));
        Vector3 s = root.TransformVector(new Vector3(_info.Size.x, _info.Size.y, 0f));
        center = c;
        size = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.y));
    }

    public RectTransform RootA => _rootA;
    public RectTransform RootB => _rootB;
    public void GetWorldRect(RectTransform root, out Vector2 center, out Vector2 size)
    {
        Vector3 c = root.TransformPoint(new Vector3(_info.Position.x, _info.Position.y, 0f));
        Vector3 s = root.TransformVector(new Vector3(_info.Size.x, _info.Size.y, 0f));
        center = c;
        size = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.y));
    }
}