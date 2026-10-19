using UnityEngine;
using UnityEngine.InputSystem;

public class ZoomManager : MonoBehaviour
{
    [SerializeField] private RectTransform frameA;
    [SerializeField] private RectTransform frameB;

    [SerializeField] private RectTransform contentA;
    [SerializeField] private RectTransform contentB;

    [SerializeField] private Camera uiCamera;
    [SerializeField] private float minZoom = 1f;
    [SerializeField] private float maxZoom = 4f;
    [SerializeField] private float scrollStep = 0.1f;
    [SerializeField] private float dragThreshold = 20f;
    
    [SerializeField] private float focusZoom = 2.5f;
    [SerializeField] private float focusDuration = 0.4f;
    public float Zoom { get; private set; } = 1f;
    public Vector2 Offset { get; private set; }

    private bool _pinching, _pinchBlocked, _dragging, _startedOnFrame;
    private float _lastPinchDist;
    private Vector2 _pressStart, _lastPos;

    private Coroutine _focusCo;
    public bool IsFocusing { get; private set; }
    public event System.Action OnViewChanged;
    
    public bool Locked { get; private set; }

    private void Update()
    {
        if (Locked) return;
        HandleScroll();
        HandlePinch();
        if (!_pinching) HandleDrag();
    }

    public void Unlock() => Locked = false;
    public void ResetZoomSmooth()
    {
        if (_focusCo != null) StopCoroutine(_focusCo);
        _focusCo = StartCoroutine(FocusTween(1f, Vector2.zero));
    }

    private void HandleScroll()
    {
        Mouse mouse = Mouse.current;

        float s = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(s, 0f)) return;

        Vector2 pos = mouse.position.ReadValue();
        RectTransform f = GetFrameAt(pos);
        if (f == null) return;

        ZoomAt(pos, Zoom * (1f + Mathf.Sign(s) * scrollStep));
    }

    private void HandlePinch()
    {
        Touchscreen ts = Touchscreen.current;
        if (ts == null) { _pinching = false; return; }

        var t0 = ts.touches[0];
        var t1 = ts.touches[1];

        if (t0.press.isPressed && t1.press.isPressed)
        {
            Vector2 p0 = t0.position.ReadValue();
            Vector2 p1 = t1.position.ReadValue();
            float dist = Vector2.Distance(p0, p1);

            if (!_pinching)
            {
                _pinching = true;
                _pinchBlocked = true;
                _dragging = false;
                _lastPinchDist = dist;
            }
            else if (_lastPinchDist > 1f)
            {
                ZoomAt((p0 + p1) * 0.5f, Zoom * dist / _lastPinchDist);
                _lastPinchDist = dist;
            }
        }
        else _pinching = false;
    }

    private void HandleDrag()
    {
        Pointer p = Pointer.current;
        if (p == null) return;

        if (_pinchBlocked)
        {
            if (!p.press.isPressed) _pinchBlocked = false;
            return;
        }

        Vector2 pos = p.position.ReadValue();

        if (p.press.wasPressedThisFrame)
        {
            _pressStart = _lastPos = pos;
            _dragging = false;
            _startedOnFrame = GetFrameAt(pos) != null;
        }
        else if (p.press.isPressed && _startedOnFrame)
        {
            if (!_dragging && (pos - _pressStart).magnitude > dragThreshold)
                _dragging = true;

            if (_dragging)
            {
                Pan(_lastPos, pos);
                _lastPos = pos;
            }
        }
    }
    
    private void ZoomAt(Vector2 screenPos, float newZoom)
    {
        newZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);
        RectTransform frame = GetFrameAt(screenPos);
        if (frame == null || !ToLocal(frame, screenPos, out Vector2 f)) return;

        float k = newZoom / Zoom;
        Offset = f - k * (f - Offset);
        Zoom = newZoom;
        Apply();
    }

    private void Pan(Vector2 prevScreen, Vector2 curScreen)
    {
        if (!ToLocal(frameA, prevScreen, out Vector2 a) ||
            !ToLocal(frameA, curScreen, out Vector2 b)) return;

        Offset += b - a; 
        Apply();
    }

    private void Apply()
    {
        Vector2 max = frameA.rect.size * ((Zoom - 1f) * 0.5f);
        Offset = new Vector2(
            Mathf.Clamp(Offset.x, -max.x, max.x),
            Mathf.Clamp(Offset.y, -max.y, max.y));

        Vector3 scale = Vector3.one * Zoom;
        contentA.localScale = contentB.localScale = scale;
        contentA.anchoredPosition = contentB.anchoredPosition = Offset;
        OnViewChanged?.Invoke();
    }
    
    private RectTransform GetFrameAt(Vector2 screenPos)
    {
        if (RectTransformUtility.RectangleContainsScreenPoint(frameA, screenPos, uiCamera)) return frameA;
        if (RectTransformUtility.RectangleContainsScreenPoint(frameB, screenPos, uiCamera)) return frameB;
        return null;
    }


    private bool ToLocal(RectTransform frame, Vector2 screenPos, out Vector2 local)
    {
        bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(frame, screenPos, uiCamera, out local);
        local -= frame.rect.center;
        return ok;
    }
    public void FocusOn(RectTransform root, Vector3 worldCenter)
    {
        Locked = true;
        RectTransform content = root.IsChildOf(contentA) ? contentA : contentB;
        Vector2 local = content.InverseTransformPoint(worldCenter);
        float newZoom = Mathf.Clamp(focusZoom, minZoom, maxZoom);
        Vector2 newOffset = -local * newZoom;

        if (_focusCo != null) StopCoroutine(_focusCo);
        _focusCo = StartCoroutine(FocusTween(newZoom, newOffset));
    }

    private System.Collections.IEnumerator FocusTween(float toZoom, Vector2 toOffset)
    {
        IsFocusing = true;
        float fromZoom = Zoom;
        Vector2 fromOffset = Offset;

        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / focusDuration)
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            Zoom = Mathf.Lerp(fromZoom, toZoom, k);
            Offset = Vector2.Lerp(fromOffset, toOffset, k);
            Apply();
            yield return null;
        }
        Zoom = toZoom;
        Offset = toOffset;
        Apply();
        IsFocusing = false;
        if (Mathf.Approximately(toZoom, 1f)) Locked = false;
    }
}
