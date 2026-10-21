using System.Collections;
using UnityEngine;

public class MarkFound : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;

    private IDrawShape _drawShape;
    private Coroutine _drawRoutine;
    [SerializeField] private BounceEffect bounceEffect;
    private Coroutine _bounceRoutine;
    
    public void Init(ShapeMark shapeMark, Vector2 center, Vector2 size, float lineWidth, Color lineColor, int segments, float duration = 0.7f)
    {
        _drawShape = CreateBehaviour(shapeMark);

        if (_drawRoutine != null) StopCoroutine(_drawRoutine);
        _drawRoutine = StartCoroutine(_drawShape.Draw(center, size, lineWidth, lineColor, segments, duration));
    }

    private IDrawShape CreateBehaviour(ShapeMark shapeMark)
    {
        switch (shapeMark)
        {
            case ShapeMark.Rectangle:
                return new MarkRectangleBehaviour(lineRenderer);
            default:
                return new MarkCircleBehaviour(lineRenderer);
        }
    }
    public void PlayWinBounce()
    {
        if (!gameObject.activeInHierarchy) return;
        if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
        _bounceRoutine = StartCoroutine(WidthBounce());
    }

    private IEnumerator WidthBounce()
    {
        const float duration = 0.35f;
        const float amount = 0.8f; 
        float baseMul = 1f;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(e / duration) * Mathf.PI);
            lineRenderer.widthMultiplier = baseMul * (1f + k * amount);
            yield return null;
        }
        lineRenderer.widthMultiplier = baseMul;
        _bounceRoutine = null;
    }
}