using UnityEngine;

public class DifferentPair : MonoBehaviour
{
    [SerializeField] private DifferentPoint pointA;
    [SerializeField] private DifferentPoint pointB;

    private static readonly Vector2 DesignSize = new Vector2(1080f, 720f);

    private RectTransform _rootA, _rootB;
    private Vector2 _pos, _size;
    private ShapeMark _shape;

    public bool IsFound { get; private set; }
    public DifferentPoint PointA => pointA;
    public DifferentPoint PointB => pointB;
    public RectTransform RootA => _rootA;
    public RectTransform RootB => _rootB;
    public ShapeMark Shape => _shape;
    public void SetShape(ShapeMark shape) => _shape = shape;
    private Vector2 _markCenter;
    private bool _hasMarkCenter;
    private bool _hasCustomRadius;
    private float _markRadius;

    public void Init(DifferentPointInfo data, Transform rootA, Transform rootB)
    {
        IsFound = false;
        _rootA = rootA as RectTransform;
        _rootB = rootB as RectTransform;
        Canvas.ForceUpdateCanvases();

        Vector2 rect = _rootA != null ? _rootA.rect.size : DesignSize;
        if (rect.x < 1f || rect.y < 1f)
            rect = DesignSize;

        Vector2 scale = new Vector2(rect.x / DesignSize.x, rect.y / DesignSize.y);
        _pos  = Vector2.Scale((Vector2)data.Position, scale);
        _size = Vector2.Scale(data.Size, scale);
        _hasMarkCenter = data.HasMarkCenter;
        _markCenter = Vector2.Scale((Vector2)data.MarkCenter, scale);

        _hasCustomRadius = data.OverrideMarkRadius && data.MarkRadius > 0f;
        _markRadius = _hasCustomRadius ? data.MarkRadius * scale.x : 0f;

        Vector3[] outline = data.GetScaledOutline(Vector2.one);

        if (pointA != null && pointA.gameObject.scene.IsValid())
            pointA.Init(_pos, _size, outline, rootA);
        if (pointB != null && pointB.gameObject.scene.IsValid())
            pointB.Init(_pos, _size, outline, rootB);
    }

    public bool IsHit(Vector2 screenPoint, Camera cam)
        => !IsFound && ContainsPoint(screenPoint, cam);

    public bool ContainsPoint(Vector2 screenPoint, Camera cam)
        => pointA.IsHit(screenPoint, cam) || pointB.IsHit(screenPoint, cam);

    public bool MarkFound()
    {
        if (IsFound) return false;
        IsFound = true;

        // Tắt box collider của cả 2 point khi đã tìm thấy
        if (pointA != null) pointA.SetColliderEnabled(false);
        if (pointB != null) pointB.SetColliderEnabled(false);
        return true;
    }

    public void ShowOutline()
    {
        pointA.ShowOutline();
        pointB.ShowOutline();
    }

    public void HideOutline(float fadeDuration = -1f)
    {
        pointA.HideOutline(fadeDuration);
        pointB.HideOutline(fadeDuration);
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

    public void GetMarkWorldRect(RectTransform root, out Vector3 center, out Vector2 size)
    {
        GetShapeWorldRect(root, out center, out size);
        if (_hasMarkCenter)
            center = root.TransformPoint(_markCenter);
    }

    public bool TryGetMarkWorldRadius(RectTransform root, out float radiusWorld)
    {
        radiusWorld = 0f;
        if (!_hasCustomRadius || root == null) return false;

        Vector3 v = root.TransformVector(new Vector3(_markRadius, 0f, 0f));
        radiusWorld = Mathf.Abs(v.x);
        return radiusWorld > 0.0001f;
    }

    public void ResetFound()
    {
        IsFound = false;
        
        if (pointA != null) pointA.SetColliderEnabled(true);
        if (pointB != null) pointB.SetColliderEnabled(true);

        HideOutline(0f);
    }
}