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
    public RectTransform RootA => _rootA;
    public RectTransform RootB => _rootB;
    private ShapeMark _shape;
    public void SetShape(ShapeMark shape) => _shape = shape;
    public ShapeMark Shape => _shape;

    public void Init(DifferentPointInfo data, Transform rootA, Transform rootB)
    {
        IsFound = false;
        _info = data;
        _rootA = rootA as RectTransform;
        _rootB = rootB as RectTransform;

        pointA.Init(data, rootA);
        pointB.Init(data, rootB);
    }

    public bool IsHit(Vector2 screenPoint, Camera cam)
        => !IsFound && ContainsPoint(screenPoint, cam);

    public bool ContainsPoint(Vector2 screenPoint, Camera cam)
        => HitRoot(_rootA, screenPoint, cam) || HitRoot(_rootB, screenPoint, cam);

    private bool HitRoot(RectTransform root, Vector2 screenPoint, Camera cam)
    {
        if (root == null) return false;

        var frame = root.parent as RectTransform;
        if (!RectTransformUtility.RectangleContainsScreenPoint(frame, screenPoint, cam))
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, cam, out Vector2 local))
            return false;
        Vector2 d = local - (Vector2)_info.Position;

        if (_shape == ShapeMark.Rectangle)
            return Mathf.Abs(d.x) <= _info.Size.x * 0.5f &&
                   Mathf.Abs(d.y) <= _info.Size.y * 0.5f;

        float r = Mathf.Min(_info.Size.x, _info.Size.y) * 0.5f;
        return d.sqrMagnitude <= r * r;
    }

    

    public bool MarkFound()
    {
        if (IsFound) return false;
        IsFound = true;
        return true;
    }

    public void GetWorldRect(RectTransform root, out Vector3 center, out Vector2 size)
    {
        center = root.TransformPoint(_info.Position);
        Vector3 s = root.TransformVector(new Vector3(_info.Size.x, _info.Size.y, 0f));
        size = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.y));
    }

    public void GetShapeWorldRect(RectTransform root, out Vector3 center, out Vector2 size)
    {
        GetWorldRect(root, out center, out Vector2 boxSize);
        if (_shape == ShapeMark.Rectangle)
            size = boxSize;
        else
        {
            float d = Mathf.Min(boxSize.x, boxSize.y);
            size = new Vector2(d, d);
        }
    }
}