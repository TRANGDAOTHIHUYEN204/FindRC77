using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandleClick : MonoBehaviour
{
    [SerializeField] private FindHiddenLevelSpawner levelSpawner;
    [SerializeField] private CollectTargetRegistry targets;
    [SerializeField] private RectTransform backgroundImage;
    [SerializeField] private Camera cam;
    
    [SerializeField] private float clickMoveThreshold = 20f;
    [SerializeField] private Transform flyLayer;
    public event Action<Vector2> OnWrongClick;
    public event Action<FindHiddenBehaviour> OnHitItem;
    public bool InputEnabled { get; set; } = true;

    private Vector2 pressStartPos;
    private bool isPressing;

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
            
            isPressing = RectTransformUtility.RectangleContainsScreenPoint(backgroundImage, downPos, cam);
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

    private void ProcessClick(Vector2 screenPoint)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(backgroundImage, screenPoint, cam))
            return;

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