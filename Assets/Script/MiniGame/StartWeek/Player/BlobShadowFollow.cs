using UnityEngine;

/// Bóng đổ (blob shadow) của nhân vật UI: nằm cùng cha, đứng trước Player trong hierarchy
/// để vẽ phía sau nhân vật, mỗi frame bám theo vị trí ngang của Player.
[RequireComponent(typeof(RectTransform))]
public class BlobShadowFollow : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Vector2 offset;

    private RectTransform _rt;

    private void Awake() => _rt = (RectTransform)transform;

    private void LateUpdate()
    {
        if (target == null) return;
        _rt.anchoredPosition = target.anchoredPosition + offset;
    }
}
