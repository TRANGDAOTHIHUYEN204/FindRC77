using UnityEngine;

public class DifferentPair : MonoBehaviour
{
    [SerializeField] private DifferentPoint pointA;
    [SerializeField] private DifferentPoint pointB;

    private static readonly Vector2 DesignSize = new Vector2(1040f, 650f);

    private DifferentPointInfo _info;
    private RectTransform _rootA, _rootB;
    private Vector2 _pos, _size;

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

        // Layout phải sẵn trước khi đọc rect (tránh size 0 lúc Start).
        Canvas.ForceUpdateCanvases();

        Vector2 rect = _rootA != null ? _rootA.rect.size : DesignSize;
        if (rect.x < 1f || rect.y < 1f)
            rect = DesignSize;

        Vector2 scale = new Vector2(rect.x / DesignSize.x, rect.y / DesignSize.y);
        _pos  = Vector2.Scale((Vector2)data.Position, scale);
        _size = Vector2.Scale(data.Size, scale);

        // pointA/B trên prefab hiện đang trỏ asset dùng chung — không dùng cho hit-test.
        // Vẫn Init nếu có instance riêng (sau khi sửa prefab nest children).
        if (pointA != null && pointA.gameObject.scene.IsValid())
            pointA.Init(_pos, _size, rootA);
        if (pointB != null && pointB.gameObject.scene.IsValid())
            pointB.Init(_pos, _size, rootB);
    }

    public bool IsHit(Vector2 screenPoint, Camera cam)
        => !IsFound && ContainsPoint(screenPoint, cam);

    public bool ContainsPoint(Vector2 screenPoint, Camera cam)
        => HitRoot(_rootA, screenPoint, cam) || HitRoot(_rootB, screenPoint, cam);

    private bool HitRoot(RectTransform root, Vector2 screenPoint, Camera cam)
    {
        if (root == null) return false;

        var frame = root.parent as RectTransform;
        if (frame != null &&
            !RectTransformUtility.RectangleContainsScreenPoint(frame, screenPoint, cam))
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                root, screenPoint, cam, out Vector2 local))
            return false;

        Vector2 d = local - _pos;

        if (_shape == ShapeMark.Circle)
        {
            float r = Mathf.Min(_size.x, _size.y) * 0.5f;
            return d.sqrMagnitude <= r * r;
        }

        return Mathf.Abs(d.x) <= _size.x * 0.5f &&
               Mathf.Abs(d.y) <= _size.y * 0.5f;
    }

    public bool MarkFound()
    {
        if (IsFound) return false;
        IsFound = true;
        return true;
    }

    public void GetWorldRect(RectTransform root, out Vector3 center, out Vector2 size)
    {
        center = root.TransformPoint(_pos);
        Vector3 s = root.TransformVector(new Vector3(_size.x, _size.y, 0f));
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
