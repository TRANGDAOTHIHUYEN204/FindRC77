using UnityEngine;
using DG.Tweening;

public class PopInOnStart : MonoBehaviour
{
    [SerializeField] private RectTransform target;     
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float pivotY = 0.2f;    

    private void Awake()
    {
        if (target == null) target = GetComponent<RectTransform>();
    }

    private void Start()
    {
        SetPivotKeepPosition(target, new Vector2(target.pivot.x, pivotY));

        target.localScale = Vector3.zero;
        target.DOScale(Vector3.one, duration)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject);   
    }
    
    private static void SetPivotKeepPosition(RectTransform rt, Vector2 newPivot)
    {
        Vector2 deltaPivot = newPivot - rt.pivot;
        Vector2 size = rt.rect.size;
        Vector3 deltaPos = new Vector3(deltaPivot.x * size.x * rt.localScale.x,
            deltaPivot.y * size.y * rt.localScale.y, 0f);
        rt.pivot = newPivot;
        rt.localPosition += deltaPos;
    }
}