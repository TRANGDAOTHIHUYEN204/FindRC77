using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DifferentPoint : MonoBehaviour
{
    [SerializeField] private BoxCollider2D boxCollider;

    public Vector2 Center => boxCollider.bounds.center;
    public Vector2 Size   => boxCollider.bounds.size;

    public void Init(DifferentPointInfo data, Transform root)
    {
        transform.position = root.TransformPoint(data.Position); 

        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        Vector3 rootScale = root.lossyScale;
        transform.localScale = new Vector3(
            rootScale.x / parentScale.x,
            rootScale.y / parentScale.y,
            1f);

        boxCollider.offset = Vector2.zero;
        boxCollider.size = data.Size;
    }

    public bool IsHit(Vector2 worldPoint) => boxCollider.OverlapPoint(worldPoint);
}