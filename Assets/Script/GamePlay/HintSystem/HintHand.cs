using UnityEngine;

public class HintHand : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0f, -0.3f, 0f);
    [SerializeField] private float moveDistance = 0.15f;
    [SerializeField] private float moveSpeed = 2f;

    private Transform _target;
    private Vector3 _fixedPoint;
    private bool _useTarget;
    
    public void Follow(Transform target)
    {
        _target = target;
        _useTarget = true;
        gameObject.SetActive(true);
    }
    
    public void PointAt(Vector3 worldPoint)
    {
        _fixedPoint = worldPoint;
        _useTarget = false;
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);

    private void LateUpdate()
    {
        Vector3 basePos = _useTarget
            ? (_target != null ? _target.position : transform.position)
            : _fixedPoint;

        float bob = Mathf.Sin(Time.unscaledTime * moveSpeed * Mathf.PI * 2f) * moveDistance;
        transform.position = basePos + offset + Vector3.up * bob;
    }
}