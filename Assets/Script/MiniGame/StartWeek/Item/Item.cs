using UnityEngine;
using UnityEngine.UI;

public class Item : MonoBehaviour
{
    [SerializeField] private Image spriteItem;
    [SerializeField] private ItemMoveMent mover;
    [SerializeField] private float halfWidth = 40f;
    [SerializeField] private float halfHeight = 40f;

    private FactoryItem factory;
    private HandlePlayerCollisionItem checker;
    private bool released;

    public ItemType Type { get; private set; }
    public int Score { get; private set; }
    public int ActiveIndex { get; set; } = -1;

    public float X => mover.X;
    public float Y => mover.Y;
    public float HalfWidth => halfWidth;
    public float HalfHeight => halfHeight;

    private void Reset() => mover = GetComponent<ItemMoveMent>();

    public void Setup(ItemConfig config, FactoryItem factory, HandlePlayerCollisionItem checker)
    {
        this.factory = factory;
        this.checker = checker;
        released = false;
        Type = config.type;
        Score = config.score;
        spriteItem.sprite = config.sprite;
        checker.Register(this);
    }

    public void StartMove(float killY, float speedOverride = -1f)
        => mover.Init(killY, OnFellOut, speedOverride);

    public void StopMove() => mover.Stop();
    
    private void OnFellOut()
    {
        if (released) return;
        checker.NotifyMissed(this);
        ReturnToPool();
    }
    public void ReturnToPool()
    {
        if (released) return;
        released = true;

        checker.Unregister(this);
        mover.Stop();
        factory.Release(gameObject);
    }
}