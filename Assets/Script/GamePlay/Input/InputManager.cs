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

        Pointer pointer = Pointer.current;
        if (pointer == null) return;

        Vector2 pos = pointer.position.ReadValue();
        
        bool uiBlocked = UIInputBlocker.IsBlocked;

        if (pointer.press.wasPressedThisFrame)
        {
            _pressing = true;
            _cancelled = uiBlocked || IsOverButton(pos);
            _pressStart = pos;
        }

        if (!_pressing) return;

        if (uiBlocked)
            _cancelled = true;

        if (Touchscreen.current != null &&
            Touchscreen.current.touches[1].press.isPressed)
            _cancelled = true;

        if ((pos - _pressStart).sqrMagnitude > tapMaxDistance * tapMaxDistance)
            _cancelled = true;

        if (pointer.press.wasReleasedThisFrame)
        {
            if (!_cancelled && !uiBlocked && !IsOverButton(pos))
            {
                ClickedThisFrame = true;
                ClickScreenPosition = pos;
            }
            _pressing = false;
        }
    }

    private bool IsOverButton(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        var data = new PointerEventData(EventSystem.current) { position = screenPos };
        _hits.Clear();
        EventSystem.current.RaycastAll(data, _hits);

        foreach (var hit in _hits)
            if (hit.gameObject.GetComponentInParent<Selectable>() != null)
                return true;

        return false;
    }
}