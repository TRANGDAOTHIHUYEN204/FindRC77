using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ZoomMiniGameManager : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 4f;
    [SerializeField] private float mouseWheelSpeed = 0.3f;
    [SerializeField] private float pinchSpeed = 0.005f;

    [SerializeField] private ScrollRect scrollRect;
    private RectTransform content;
    private RectTransform viewport;
    private Canvas canvas;
    private bool wasPinching;

    private void Awake()
    {
        content = scrollRect.content;
        viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
        canvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        HandlePinch();
        HandleMouseWheel();
    }

    private void HandlePinch()
    {
        var ts = Touchscreen.current;
        if (ts == null) return;


        int count = 0;
        Vector2 p0 = default, p1 = default, d0 = default, d1 = default;

        foreach (var t in ts.touches)
        {
            if (!t.press.isPressed) continue;

            if (count == 0)
            {
                p0 = t.position.ReadValue();
                d0 = t.delta.ReadValue();
            }
            else if (count == 1)
            {
                p1 = t.position.ReadValue();
                d1 = t.delta.ReadValue();
            }
            count++;
        }

        if (count >= 2)
        {
            wasPinching = true;
            scrollRect.enabled = false;

            Vector2 prev0 = p0 - d0;
            Vector2 prev1 = p1 - d1;

            float prevDist = (prev0 - prev1).magnitude;
            float currDist = (p0 - p1).magnitude;
            float delta = (currDist - prevDist) * pinchSpeed;

            Vector2 center = (p0 + p1) * 0.5f;
            ZoomAt(center, delta);
            return;
        }
        
        if (wasPinching && count == 0)
        {
            wasPinching = false;
            scrollRect.enabled = true;
        }
    }

    private void HandleMouseWheel()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        Vector2 mousePos = mouse.position.ReadValue();

        if (RectTransformUtility.RectangleContainsScreenPoint(viewport, mousePos, GetCamera()))
        {
            ZoomAt(mousePos, Mathf.Sign(scroll) * mouseWheelSpeed);
        }
    }

    private void ZoomAt(Vector2 screenPoint, float delta)
    {
        float oldScale = content.localScale.x;
        float newScale = Mathf.Clamp(oldScale + delta, minScale, maxScale);
        if (Mathf.Approximately(oldScale, newScale)) return;

        Camera cam = GetCamera();

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            content, screenPoint, cam, out Vector3 worldBefore);

        content.localScale = new Vector3(newScale, newScale, 1f);

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            content, screenPoint, cam, out Vector3 worldAfter);

        content.position += worldBefore - worldAfter;

        scrollRect.StopMovement();
    }

    private Camera GetCamera()
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        return canvas.worldCamera;
    }
}