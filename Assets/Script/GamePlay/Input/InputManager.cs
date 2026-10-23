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
    private PointerEventData _pointerData;

    private void Awake()
    {
        _pointerData = new PointerEventData(EventSystem.current);
    }

    private void Update()
    {
        ClickedThisFrame = false;

        Pointer pointer = Pointer.current;
        if (pointer == null) return;

        Vector2 pos = pointer.position.ReadValue();

        if (pointer.press.wasPressedThisFrame)
        {
            _pressing = true;
            _cancelled = IsOverButton(pos);
            _pressStart = pos;
        }

        if (!_pressing) return;

        if (Touchscreen.current != null &&
            Touchscreen.current.touches[1].press.isPressed)
            _cancelled = true;

        if ((pos - _pressStart).sqrMagnitude > tapMaxDistance * tapMaxDistance)
            _cancelled = true;

        if (pointer.press.wasReleasedThisFrame)
        {
            if (!_cancelled)
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

        if (_pointerData == null)
            _pointerData = new PointerEventData(EventSystem.current);

        _pointerData.Reset();
        _pointerData.position = screenPos;
        _hits.Clear();
        EventSystem.current.RaycastAll(_pointerData, _hits);

        if (_hits.Count == 0) return false;
        return _hits[0].gameObject.GetComponentInParent<Selectable>() != null;
    }
}