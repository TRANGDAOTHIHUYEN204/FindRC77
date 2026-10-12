using UnityEngine;

public class CircleDifferent : MonoBehaviour
{
    [SerializeField] private CircleDifferentData circleDifferentData;
    private CircleDifferentBehaviour _circleDifferentBehaviour;
    [SerializeField] private LineRenderer lineRenderer;
    public void Init()
    {
        _circleDifferentBehaviour = new CircleDifferentBehaviour(lineRenderer);
        
    }
}
