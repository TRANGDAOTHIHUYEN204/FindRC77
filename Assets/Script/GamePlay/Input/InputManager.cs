using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    [SerializeField] private float tapMaxDistance = 20f;
    public bool ClickedThisFrame { get; private set; }
    public Vector2 ClickScreenPosition { get; private set; }

    private bool _pressing;
    private bool _cancelled;
    private Vector2 _pressStart;

    private readonly List<RaycastResult> _hits = new List<RaycastResult>();

    private void Update()
    {
        ClickedThisFrame = false;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var touch = touchscreen.primaryTouch;
            if (touch.press.isPressed ||
                touch.press.wasPressedThisFrame ||
                touch.press.wasReleasedThisFrame)
            {
                bool anotherTouchPressed = touchscreen.touches[1].press.isPressed;
                ProcessInput(
                    touch.position.ReadValue(),
                    touch.press.wasPressedThisFrame,
                    touch.press.wasReleasedThisFrame,
                    anotherTouchPressed);
                return;
            }
        }

        Pointer pointer = Pointer.current;
        if (pointer == null)
        {
            _pressing = false;
            return;
        }

        ProcessInput(
            pointer.position.ReadValue(),
            pointer.press.wasPressedThisFrame,
            pointer.press.wasReleasedThisFrame,
            touchscreen != null && touchscreen.touches[1].press.isPressed);
    }

    private void ProcessInput(Vector2 pos, bool pressedThisFrame, bool releasedThisFrame, bool anotherTouchPressed)
    {
        bool uiBlocked = UIInputBlocker.IsBlocked;

        if (pressedThisFrame)
        {
            _pressing = true;
            _cancelled = uiBlocked || IsOverBlockingUI(pos);
            _pressStart = pos;
        }

        if (!_pressing) return;

        if (uiBlocked)
            _cancelled = true;

        if (anotherTouchPressed)
            _cancelled = true;

        if ((pos - _pressStart).sqrMagnitude > tapMaxDistance * tapMaxDistance)
            _cancelled = true;

        if (releasedThisFrame)
        {
            if (!_cancelled && !uiBlocked && !IsOverBlockingUI(pos))
            {
                ClickedThisFrame = true;
                ClickScreenPosition = pos;
            }
            _pressing = false;
        }
    }

    private bool IsOverBlockingUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        var data = new PointerEventData(EventSystem.current) { position = screenPos };
        _hits.Clear();
        EventSystem.current.RaycastAll(data, _hits);

        foreach (var hit in _hits)
        {
            if (hit.gameObject.GetComponentInParent<Selectable>() != null)
                return true;

            var groups = hit.gameObject.GetComponentsInParent<CanvasGroup>();
            foreach (var group in groups)
                if (group.blocksRaycasts)
                    return true;
        }

        return false;
    }
}