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
    [SerializeField] private DragHandler2D drag;
    [SerializeField] private IconCollectionMover mover;

    [Header("đúng target")]
    [SerializeField] private float jumpPower = 100f;
    [SerializeField] private float jumpDuration = 0.5f;
    [SerializeField] private Ease jumpEase = Ease.InOutSine;
    [SerializeField] private float endScale = 0f;
    [SerializeField] private Ease scaleEase = Ease.InQuad;

    [Header("ra ngoài biên")]
    [SerializeField] private float returnJumpPower = 40f;
    [SerializeField] private float returnJumpDuration = 0.35f;
    [SerializeField] private Ease returnJumpEase = Ease.InOutSine;
    [SerializeField] private float scaleBounceAmount = 0.15f;
    [SerializeField] private int scaleBounceCount = 3;
    [SerializeField] private float scaleBounceDuration = 0.35f;
    [SerializeField, Range(0.1f, 1f)] private float scaleBounceDecay = 0.5f;

    [Header("kéo sai chỗ")]
    [SerializeField] private float wrongJumpPower = 60f;
    [SerializeField] private float wrongJumpDuration = 0.4f;
    [SerializeField] private Ease wrongJumpEase = Ease.InOutSine;
    [SerializeField] private float bounceHeight = 18f;
    [SerializeField] private int bounceCount = 3;
    [SerializeField] private float bounceDuration = 0.3f;
    [SerializeField, Range(0.1f, 1f)] private float bounceDecay = 0.6f;
    [SerializeField] private float wobbleAngle = 10f;
    [SerializeField, Range(0f, 0.3f)] private float bounceStretch = 0.08f;

    [SerializeField] private bool allowDragDuringFx = true;

    public string ItemType { get; private set; }
    public State CurrentState { get; private set; } = State.Spawning;
    public event Action<IconCollectionBehaviour> OnCompleted;

    private Canvas rootCanvas;
    private float spawnDelay;
    private Vector3 homeLocalPosition;
    private Vector3 homeScale;

    private Camera UICamera =>
        rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;

    private void Awake()
    {
        if (drag == null) drag = GetComponent<DragHandler2D>();
        if (mover == null) mover = GetComponent<IconCollectionMover>();
        if (anim == null) anim = GetComponentInChildren<IconCollectionAnimation>();
        if (breath == null) breath = GetComponentInChildren<BreathBehaviour>();
        if (image == null) image = GetComponentInChildren<Image>();

        homeScale = transform.localScale;
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
        transform.localScale = homeScale;
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

        mover.Kill();
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
        breath.StopBreathing();

        mover.JumpAndScaleTo(target.SnapPosition, jumpPower, jumpDuration, jumpEase,
            Vector3.one * endScale, scaleEase, () =>
            {
                target.NotifyAccepted();
                Complete();
            });
    }

    private void Reject(DropTarget target)
    {
        bool wrongTarget = target != null;
        if (wrongTarget) target.NotifyRejected();

        SetState(State.Returning);
        anim.Drop();

        if (wrongTarget)
        {
            mover.JumpToLocal(homeLocalPosition, wrongJumpPower, wrongJumpDuration, wrongJumpEase,
                () => PlayArrivalFx(done => mover.BounceAndWobble(
                    homeLocalPosition, homeScale, bounceHeight, bounceCount, bounceDuration,
                    wobbleAngle, bounceDecay, bounceStretch, done)));
        }
        else
        {
            mover.JumpToLocal(homeLocalPosition, returnJumpPower, returnJumpDuration, returnJumpEase,
                () => PlayArrivalFx(done => mover.ScaleBounceInPlace(
                    homeScale, scaleBounceAmount, scaleBounceCount, scaleBounceDuration,
                    scaleBounceDecay, done)));
        }
    }

    private void PlayArrivalFx(Action<Action> playFx)
    {
        if (allowDragDuringFx)
        {
            SetState(State.Idle);
            playFx(() => { if (CurrentState == State.Idle) breath.StartBreathing(); });
        }
        else
        {
            playFx(BackToIdle);
        }
    }

    private void BackToIdle()
    {
        SetState(State.Idle);
        breath.StartBreathing();
    }

    private void Complete()
    {
        SetState(State.Completed);
        OnCompleted?.Invoke(this);
    }
}