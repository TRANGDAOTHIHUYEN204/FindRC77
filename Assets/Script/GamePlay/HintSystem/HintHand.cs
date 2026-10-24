using UnityEngine;

public class HintHand : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(40f, -80f, 0f);
    [SerializeField] private float moveDistance = 20f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int sortingOrder = 50;

    private SpriteRenderer _renderer;
    private Vector3 _localPoint;
    private bool _hasTarget;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null)
            _renderer.sortingOrder = sortingOrder;
    }
    
    public void PointAt(RectTransform root, Vector3 worldPoint)
    {
        if (root == null)
        {
            Hide();
            return;
        }

        transform.SetParent(root, false);
        Vector3 lossy = root.lossyScale;
        transform.localScale = new Vector3(SafeInv(lossy.x), SafeInv(lossy.y), 1f);
        transform.localRotation = Quaternion.identity;

        _localPoint = root.InverseTransformPoint(worldPoint);
        _hasTarget = true;
        gameObject.SetActive(true);

        if (_renderer != null)
            _renderer.sortingOrder = sortingOrder;
    }

    public void Hide()
    {
        _hasTarget = false;
        gameObject.SetActive(false);
    }

    public void Detach()
    {
        _hasTarget = false;
        transform.SetParent(null, false);
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!_hasTarget) return;

        float bob = Mathf.Sin(Time.unscaledTime * moveSpeed * Mathf.PI * 2f) * moveDistance;
        transform.localPosition = _localPoint + offset + Vector3.up * bob;
    }

    private static float SafeInv(float v) => Mathf.Abs(v) < 0.0001f ? 1f : 1f / v;
}
