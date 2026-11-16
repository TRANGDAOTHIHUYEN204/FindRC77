using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speedMove;
    [SerializeField] private ScreenBoudaries screenBoudaries;
    private Vector2 _movement;

    private float _targetX;
    private bool _hasTarget;
    private Camera _cam;
    private Vector2 _startPos;

    private bool _pressStartedOnUI;
    private readonly List<RaycastResult> _uiResults = new List<RaycastResult>();

    public bool CanMove { get; set; } = true;

    private void Awake()
    {
        _cam = Camera.main;
        _startPos = transform.position;
    }

    private void Start()
    {
        if (speedMove == 0f)
        {
            Debug.LogWarning("Speed Move chưa được gán");
            enabled = false;
        }
    }

    private void Update()
    {
        if (!CanMove)
        {
            _movement = Vector2.zero;
            _hasTarget = false;
            _pressStartedOnUI = false;
            return;
        }

        float inputX = 0f;

        if (Keyboard.current != null)
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                inputX -= 1f;

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                inputX += 1f;
        }

        bool hasKeyInput = Mathf.Abs(inputX) > 0.01f;

        if (hasKeyInput)
        {
            _hasTarget = false;
            _movement = new Vector2(inputX, 0f);
        }
        else
        {
            ReadPointer();
        }

        if (_hasTarget)
        {
            float directionX = _targetX - transform.position.x;

            if (Mathf.Abs(directionX) < 0.001f)
            {
                _movement = Vector2.zero;
                _hasTarget = false;
            }
            else
            {
                _movement = new Vector2(Mathf.Sign(directionX), 0f);
            }
        }
        else if (!hasKeyInput)
        {
            _movement = Vector2.zero;
        }

        MovePlayer();
    }

    private void ReadPointer()
    {
        var pointer = Pointer.current;

        if (pointer == null || !pointer.press.isPressed)
        {
            _pressStartedOnUI = false;
            return;
        }

        Vector2 screenPos = pointer.position.ReadValue();
        if (pointer.press.wasPressedThisFrame)
            _pressStartedOnUI = UIInputBlocker.IsBlocked || IsPointerOverInteractiveUI(screenPos);

        if (_pressStartedOnUI || _cam == null)
            return;

        Vector3 sp = new Vector3(screenPos.x, screenPos.y, Mathf.Abs(_cam.transform.position.z));
        float x = _cam.ScreenToWorldPoint(sp).x;

        _targetX = screenBoudaries != null
            ? Mathf.Clamp(x, screenBoudaries.MinX, screenBoudaries.MaxX)
            : x;
        _hasTarget = true;
    }

    private bool IsPointerOverInteractiveUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        var data = new PointerEventData(EventSystem.current) { position = screenPos };
        _uiResults.Clear();
        EventSystem.current.RaycastAll(data, _uiResults);

        foreach (var r in _uiResults)
        {
            var selectable = r.gameObject.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.IsInteractable())
                return true;
        }

        return false;
    }

    private void MovePlayer()
    {
        Vector3 pos = transform.position;

        float stepX = _movement.x * speedMove * Time.deltaTime;

        if (_hasTarget)
        {
            float toTargetX = _targetX - pos.x;

            if (Mathf.Abs(stepX) > Mathf.Abs(toTargetX))
                stepX = toTargetX;
        }

        pos.x += stepX;
        pos.y = _startPos.y;

        if (screenBoudaries != null)
            pos.x = Mathf.Clamp(pos.x, screenBoudaries.MinX, screenBoudaries.MaxX);

        transform.position = pos;
    }

    public void ResetMovement()
    {
        _movement = Vector2.zero;
        _hasTarget = false;
        _pressStartedOnUI = false;

        transform.position = _startPos;
    }
}