using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class HandleClick : MonoBehaviour
{
    [SerializeField] private FindHiddenLevelSpawner levelSpawner;
    [SerializeField] private CollectTargetRegistry targets;
    [SerializeField] private RectTransform backgroundImage;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private Camera cam;
    [SerializeField] private float clickMoveThreshold = 20f;
    [SerializeField] private Transform flyLayer;

    public event Action<Vector2> OnWrongClick;
    public event Action<FindHiddenBehaviour> OnHitItem;
    public bool InputEnabled { get; set; } = true;

    private Vector2 pressStartPos;
    private bool isPressing;
    private bool cancelled;

    private readonly List<RaycastResult> results = new List<RaycastResult>();

    private RectTransform GameArea => viewport != null ? viewport : backgroundImage;
    private Camera UICam
    {
        get
        {
            var canvas = GameArea.GetComponentInParent<Canvas>();
            if (canvas == null) return cam;
            canvas = canvas.rootCanvas;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (canvas.worldCamera != null ? canvas.worldCamera : cam);
        }
    }

    private void Update()
    {
        var pointer = Pointer.current;
        if (pointer == null)
        {
            isPressing = false;
            return;
        }

        if (!InputEnabled)
        {
            isPressing = false;
            return;
        }

        Vector2 pos = pointer.position.ReadValue();
        bool uiBlocked = UIInputBlocker.IsBlocked;

        if (pointer.press.wasPressedThisFrame)
        {
            isPressing = true;
            pressStartPos = pos;
            cancelled = uiBlocked || !IsInsideGameArea(pos) || IsBlockedByOtherUI(pos);
            return;
        }

        if (!isPressing) return;
        
        if (uiBlocked) cancelled = true;

        var ts = Touchscreen.current;
        if (ts != null && ts.touches.Count > 1 && ts.touches[1].press.isPressed)
            cancelled = true;

        if ((pos - pressStartPos).sqrMagnitude > clickMoveThreshold * clickMoveThreshold)
            cancelled = true;

        if (pointer.press.wasReleasedThisFrame)
        {
            isPressing = false;
            
            if (cancelled || uiBlocked || IsBlockedByOtherUI(pos) || !IsInsideGameArea(pos))
                return;

            ProcessClick(pos);
        }
    }

    private bool IsInsideGameArea(Vector2 screenPos)
    {
        Camera c = UICam;
        return RectTransformUtility.RectangleContainsScreenPoint(GameArea, screenPos, c)
            && RectTransformUtility.RectangleContainsScreenPoint(backgroundImage, screenPos, c);
    }

    private bool IsBlockedByOtherUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        var data = new PointerEventData(EventSystem.current) { position = screenPos };
        results.Clear();
        EventSystem.current.RaycastAll(data, results);

        if (results.Count == 0) return false;

        Transform top = results[0].gameObject.transform;
        return !top.IsChildOf(GameArea);
    }

    private void ProcessClick(Vector2 screenPoint)
    {
        if (levelSpawner.TryGetHit(screenPoint, cam, out var hit))
        {
            Vector3 dest = hit.transform.position;
            if (hit.Data != null && targets != null &&
                targets.TryGetWorldPos(hit.Data.TargetId, out var p))
                dest = p;

            hit.Collect(dest, flyLayer);
            OnHitItem?.Invoke(hit);
        }
        else
        {
            OnWrongClick?.Invoke(screenPoint);
        }
    }
}