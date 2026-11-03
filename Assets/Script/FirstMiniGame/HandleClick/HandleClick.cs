using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandleClick : MonoBehaviour
{
    [SerializeField] private FindHiddenLevelSpawner levelSpawner;
    [SerializeField] private CollectTargetRegistry targets;
    [SerializeField] private RectTransform backgroundImage;
    [SerializeField] private Camera cam; 

    public event Action<Vector2> OnWrongClick;
    public event Action<FindHiddenBehaviour> OnHitItem;
    public bool InputEnabled { get; set; } = true;

    private void Update()
    {
        if (!InputEnabled) return;

        var pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;

        Vector2 screenPoint = pointer.position.ReadValue();

        if (!RectTransformUtility.RectangleContainsScreenPoint(backgroundImage, screenPoint, cam))
            return;

        if (levelSpawner.TryGetHit(screenPoint, cam, out var hit))
        {
            Vector3 dest = hit.transform.position;
            if (hit.Data != null && targets != null &&
                targets.TryGetWorldPos(hit.Data.TargetId, out var p))
                dest = p;

            hit.Collect(dest);
            OnHitItem?.Invoke(hit);
        }
        else
        {
            OnWrongClick?.Invoke(screenPoint);
        }
    }
}