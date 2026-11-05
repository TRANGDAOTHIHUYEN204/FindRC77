using UnityEngine;
using UnityEngine.InputSystem;

public class ZoomMiniGameManager : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 4f;
    [SerializeField] private float mouseWheelSpeed = 0.3f;
    [SerializeField] private float pinchSpeed = 0.005f;

    [Header("References")]
    [SerializeField] private RectTransform frame;  // khung hiển thị (có Mask / RectMask2D)
    [SerializeField] private RectTransform image;  // ảnh gameplay (con của frame)

    private Canvas canvas;
    private bool isDragging;
    private Vector2 lastPos;
    private readonly Vector3[] frameCorners = new Vector3[4];
    private readonly Vector3[] imageCorners = new Vector3[4];

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (frame == null) frame = (RectTransform)transform;
    }

    private void Update()
    {
        if (HandlePinch()) return;
        HandleDrag();
        HandleMouseWheel();
    }

    private Camera GetCamera()
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera;
    }

    private bool InFrame(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(frame, screenPos, GetCamera());
    }

    // ---------- KÉO ẢNH ----------
    private void HandleDrag()
    {
        var ts = Touchscreen.current;
        var mouse = Mouse.current;

        Vector2 pos;
        bool pressed, down;

        if (ts != null && ts.primaryTouch.press.isPressed)
        {
            pos = ts.primaryTouch.position.ReadValue();
            pressed = true;
            down = ts.primaryTouch.press.wasPressedThisFrame;
        }
        else if (mouse != null)
        {
            pos = mouse.position.ReadValue();
            pressed = mouse.leftButton.isPressed;
            down = mouse.leftButton.wasPressedThisFrame;
        }
        else return;

        if (down)
        {
            isDragging = InFrame(pos);
            lastPos = pos;
            return;
        }

        if (!pressed) { isDragging = false; return; }
        if (!isDragging) return;

        Camera cam = GetCamera();
        RectTransformUtility.ScreenPointToWorldPointInRectangle(frame, lastPos, cam, out Vector3 prevWorld);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(frame, pos, cam, out Vector3 curWorld);

        image.position += curWorld - prevWorld;
        lastPos = pos;

        ClampImage();
    }
    
    private bool HandlePinch()
    {
        var ts = Touchscreen.current;
        if (ts == null) return false;

        int count = 0;
        Vector2 p0 = default, p1 = default, d0 = default, d1 = default;

        foreach (var t in ts.touches)
        {
            if (!t.press.isPressed) continue;

            if (count == 0) { p0 = t.position.ReadValue(); d0 = t.delta.ReadValue(); }
            else if (count == 1) { p1 = t.position.ReadValue(); d1 = t.delta.ReadValue(); }
            count++;
        }

        if (count < 2) return false;

        isDragging = false;

        float prevDist = ((p0 - d0) - (p1 - d1)).magnitude;
        float currDist = (p0 - p1).magnitude;
        float delta = (currDist - prevDist) * pinchSpeed;

        Vector2 center = (p0 + p1) * 0.5f;
        if (InFrame(center)) ZoomAt(center, delta);
        return true;
    }

    private void HandleMouseWheel()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        Vector2 mousePos = mouse.position.ReadValue();
        if (InFrame(mousePos))
            ZoomAt(mousePos, Mathf.Sign(scroll) * mouseWheelSpeed);
    }
    
    private void ZoomAt(Vector2 screenPoint, float delta)
    {
        float oldScale = image.localScale.x;
        float newScale = Mathf.Clamp(oldScale + delta, minScale, maxScale);
        if (Mathf.Approximately(oldScale, newScale)) return;

        Camera cam = GetCamera();

        RectTransformUtility.ScreenPointToWorldPointInRectangle(image, screenPoint, cam, out Vector3 before);
        image.localScale = new Vector3(newScale, newScale, 1f);
        RectTransformUtility.ScreenPointToWorldPointInRectangle(image, screenPoint, cam, out Vector3 after);

        image.position += before - after;

        ClampImage();
    }

    private void ClampImage()
    {
        frame.GetWorldCorners(frameCorners);
        image.GetWorldCorners(imageCorners);

        Vector3 offset = Vector3.zero;

        if (imageCorners[0].x > frameCorners[0].x) offset.x = frameCorners[0].x - imageCorners[0].x;
        else if (imageCorners[2].x < frameCorners[2].x) offset.x = frameCorners[2].x - imageCorners[2].x;

        if (imageCorners[0].y > frameCorners[0].y) offset.y = frameCorners[0].y - imageCorners[0].y;
        else if (imageCorners[2].y < frameCorners[2].y) offset.y = frameCorners[2].y - imageCorners[2].y;

        image.position += offset;
    }
}