using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DifferentPoint : MonoBehaviour
{
    private Collider2D _collider;

    public bool IsFound { get; private set; }
    public Vector2 Center => _collider.bounds.center;   
    public Vector2 Size => _collider.bounds.size;      

    private void Awake() => _collider = GetComponent<Collider2D>();

    public void ResetPoint() => IsFound = false;

    public bool IsHit(Vector2 worldPoint)
        => !IsFound && _collider.OverlapPoint(worldPoint);

    public void MarkFound() => IsFound = true;
}