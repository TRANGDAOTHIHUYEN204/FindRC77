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

    private readonly List<RaycastResult> results = new List<RaycastResult>();

    private RectTransform GameArea => viewport != null ? viewport : backgroundImage;

    private void Update()
    {
        var pointer = Pointer.current;
        if (pointer == null) return;

        if (!InputEnabled)
        {
            isPressing = false;
            return;
        }

        if (pointer.press.wasPressedThisFrame)
        {
            Vector2 downPos = pointer.position.ReadValue();

            if (!IsInsideGameArea(downPos) || IsBlockedByOtherUI(downPos))
            {
                isPressing = false;
                return;
            }

            isPressing = true;
            pressStartPos = downPos;
            return;
        }

        if (pointer.press.wasReleasedThisFrame && isPressing)
        {
            isPressing = false;

            Vector2 upPos = pointer.position.ReadValue();
            if ((upPos - pressStartPos).sqrMagnitude > clickMoveThreshold * clickMoveThreshold)
                return; 

            ProcessClick(upPos);
        }
    }


    private bool IsInsideGameArea(Vector2 screenPos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(GameArea, screenPos, cam)
            && RectTransformUtility.RectangleContainsScreenPoint(backgroundImage, screenPos, cam);
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