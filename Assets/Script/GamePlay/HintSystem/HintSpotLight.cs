using UnityEngine;

public class HintSpotlight : MonoBehaviour
{
    [SerializeField] private ZoomManager zoom;
    [SerializeField] private HintOverlay overlayA;
    [SerializeField] private HintOverlay overlayB;
    [Tooltip("Khi hint chỉ vào cả một đồ vật: vòng sáng chiếm tối đa tỉ lệ này của khung ảnh khi zoom.")]
    [SerializeField, Range(0.3f, 1f)] private float areaFitRatio = 0.85f;
    [Tooltip("Đường kính vòng sáng so với cạnh ngắn của đồ vật (vòng đặt ở tâm đồ vật).")]
    [SerializeField, Range(0.5f, 2f)] private float areaCircleMul = 1.1f;

    private DifferentPair _pair;
    private bool _animated;

    // Hint vào một vùng (vd: đồ vật tương tác đang đóng) thay vì một điểm khác.
    private bool _useArea;
    private RectTransform _areaRootA, _areaRootB;
    private Vector2 _areaCenter, _areaSize;

    private void OnEnable()  => zoom.OnViewChanged += Refresh;
    private void OnDisable() => zoom.OnViewChanged -= Refresh;

    public void Show(DifferentPair pair) => ShowInternal(pair, false, -1f);
    public void ShowAnimated(DifferentPair pair) => ShowInternal(pair, true, -1f);
    /// zoomLevel &lt;= 0 → dùng focusZoom mặc định của ZoomManager.
    public void ShowAnimated(DifferentPair pair, float zoomLevel) => ShowInternal(pair, true, zoomLevel);

    private void ShowInternal(DifferentPair pair, bool animated, float zoomLevel)
    {
        _useArea = false;
        _pair = pair;
        _animated = animated;
        pair.GetWorldRect(pair.RootA, out Vector3 c, out Vector2 _);
        if (zoomLevel > 0f) zoom.FocusOn(pair.RootA, c, zoomLevel);
        else zoom.FocusOn(pair.RootA, c);
        Refresh();
    }

    /// Khoanh + zoom vào một vùng (toạ độ local của ảnh A/B, gốc ở pivot).
    /// Mức zoom tự giảm để cả vùng vẫn lọt trong khung.
    public void ShowArea(RectTransform rootA, RectTransform rootB, Vector2 localCenter, Vector2 localSize,
        bool animated, float zoomLevel = -1f)
    {
        _pair = null;
        _useArea = true;
        _areaRootA = rootA;
        _areaRootB = rootB;
        _areaCenter = localCenter;
        _animated = animated;

        // Lỗ sáng là hình tròn: lấy theo cạnh ngắn của đồ vật, nếu lấy cạnh dài thì đồ vật cao
        // (tủ quần áo) sẽ cho vòng to hơn cả khung ảnh và không còn thấy lớp tối.
        float d = Mathf.Min(localSize.x, localSize.y) * areaCircleMul;
        _areaSize = new Vector2(d, d);

        float z = zoomLevel > 0f ? zoomLevel : zoom.FocusZoom;
        Rect r = rootA.rect;
        if (d > 0.01f)
            z = Mathf.Min(z, Mathf.Min(r.width, r.height) / d * areaFitRatio);
        z = Mathf.Max(1f, z);

        zoom.FocusOn(rootA, rootA.TransformPoint(localCenter), z);
        Refresh();
    }

    public void Hide()
    {
        HideOverlay();
        zoom.Unlock();
        zoom.ResetZoomSmooth();
    }

    /// Chỉ ẩn lớp tối + khoanh gợi ý, giữ nguyên khung zoom hiện tại.
    public void HideOverlay()
    {
        _pair = null;
        _useArea = false;
        _animated = false;
        overlayA.Hide();
        overlayB.Hide();
    }

    private void Refresh()
    {
        Vector3 ca, cb;
        Vector2 sa, sb;

        if (_useArea)
        {
            if (_areaRootA == null || _areaRootB == null) return;
            GetAreaWorldRect(_areaRootA, out ca, out sa);
            GetAreaWorldRect(_areaRootB, out cb, out sb);
        }
        else
        {
            if (_pair == null) return;
            _pair.GetWorldRect(_pair.RootA, out ca, out sa);
            _pair.GetWorldRect(_pair.RootB, out cb, out sb);
        }

        if (_animated)
        {
            overlayA.ShowAnimatedHint(ca, sa);
            overlayB.ShowAnimatedHint(cb, sb);
        }
        else
        {
            overlayA.Show(ca, sa);
            overlayB.Show(cb, sb);
        }
    }

    private void GetAreaWorldRect(RectTransform root, out Vector3 center, out Vector2 size)
    {
        center = root.TransformPoint(_areaCenter);
        Vector3 s = root.TransformVector(new Vector3(_areaSize.x, _areaSize.y, 0f));
        size = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.y));
    }
}
