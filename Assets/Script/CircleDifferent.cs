using UnityEngine;

public class CircleDifferent : MonoBehaviour
{
    [SerializeField] private CircleDifferentData circleDifferentData;
    private CircleDifferentBehaviour _circleDifferentBehaviour;
    [SerializeField] private LineRenderer lineRenderer;
    private Vector2 _pointCircle;
    private float _radius;
    
    public void Init(float radius, Vector2 pointCircle)
    {
        _circleDifferentBehaviour = new CircleDifferentBehaviour(lineRenderer);
        _radius = radius;
        _pointCircle = pointCircle;
    }

    public void DrawCircle()
    {
        _circleDifferentBehaviour.DrawCircle(_pointCircle, _radius, circleDifferentData.LineWidth, circleDifferentData.ColorLine, circleDifferentData.Segments);
    }
}
