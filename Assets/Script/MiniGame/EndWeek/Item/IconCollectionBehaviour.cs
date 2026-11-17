using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class IconCollectionBehaviour : MonoBehaviour
{
    public enum State { Spawning, Idle, Dragging, Returning, Completing, Completed }

    [SerializeField] private IconCollectionAnimation anim;
    [SerializeField] private BreathBehaviour breath;
    [SerializeField] private Image image;

    [SerializeField] private float snapDuration = 0.2f;
    [SerializeField] private float returnDuration = 0.15f;

    [SerializeField] private DragHandler2D drag;
    [SerializeField] private IconCollectionMover mover;

    public string ItemType { get; private set; }
    public State CurrentState { get; private set; } = State.Spawning;
    public event Action<IconCollectionBehaviour> OnCompleted;

    private Canvas rootCanvas;
    private Vector3 homePosition;
    private float spawnDelay;
    private Vector3 homeLocalPosition;
    private Camera UICamera =>
        rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

    private void Awake()
    {
        if (drag == null) drag = GetComponent<DragHandler2D>();
        if (mover == null) mover = GetComponent<IconCollectionMover>();
        if (anim == null) anim = GetComponentInChildren<IconCollectionAnimation>();
        if (breath == null) breath = GetComponentInChildren<BreathBehaviour>();
        if (image == null) image = GetComponentInChildren<Image>();
    }

    private void OnEnable()
    {
        rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

        drag.OnDragStarted += HandleDragStarted;
        drag.OnDragEnded += HandleDragEnded;
        drag.OnDragCancelled += HandleDragCancelled;
    }

    private void OnDisable()
    {
        drag.OnDragStarted -= HandleDragStarted;
        drag.OnDragEnded -= HandleDragEnded;
        drag.OnDragCancelled -= HandleDragCancelled;
    }

    public void Setup(string type, Sprite sprite, float delay = 0f)
    {
        ItemType = type;
        spawnDelay = delay;
        if (image != null) image.sprite = sprite;
    }

    public void Begin()
    {
        mover.Kill();
        homeLocalPosition = transform.localPosition;

        anim.ResetPose();
        breath.StopBreathing();
        SetState(State.Spawning);

        anim.Spawn(spawnDelay, () =>
        {
            SetState(State.Idle);
            breath.StartBreathing();
        });
    }

    private void SetState(State state)
    {
        CurrentState = state;
        drag.CanDrag = state == State.Idle;
    }

    private void HandleDragStarted()
    {
        if (CurrentState != State.Idle) return;

        SetState(State.Dragging);
        transform.SetAsLastSibling();
        breath.StopBreathing();
        anim.Lift();
    }

    private void HandleDragEnded()
    {
        if (CurrentState != State.Dragging) return;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(UICamera, transform.position);
        DropTarget target = DropTarget.FindAt(screenPoint, UICamera);

        if (target != null && target.Accepts(ItemType))
            Accept(target);
        else
            Reject(target);
    }
    
    private void HandleDragCancelled()
    {
        if (CurrentState != State.Dragging) return;
        Reject(null);
    }

    private void Accept(DropTarget target)
    {
        SetState(State.Completing);
        anim.Drop();
        mover.MoveTo(target.SnapPosition, snapDuration, Ease.OutQuad, () =>
        {
            target.NotifyAccepted();
            anim.PlayComplete(Complete);
        });
    }

    private void Reject(DropTarget target)
    {
        if (target != null) target.NotifyRejected();

        SetState(State.Returning);
        anim.Drop();
        mover.MoveToLocal(homeLocalPosition, returnDuration, Ease.OutBack, () =>
        {
            SetState(State.Idle);
            breath.StartBreathing();
        });
    }

    private void Complete()
    {
        SetState(State.Completed);
        OnCompleted?.Invoke(this);
    }
}