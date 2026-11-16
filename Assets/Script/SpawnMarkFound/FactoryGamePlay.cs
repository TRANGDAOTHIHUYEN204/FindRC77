using UnityEngine;

public class FactoryGameplay : MonoBehaviour
{
    [SerializeField] private MarkFound prefabMarkFound;
    [SerializeField] private ChallengeNode challengeNodePrefab;
    [SerializeField] private DifferentPair differentPairPrefab;
    [SerializeField] private ErrorMark errorMark;
    [SerializeField] private HeartLives heartPrefab;
    [SerializeField] private HintHand handPrefab;
    [SerializeField] private StarTrailEffect starTrailPrefab;

    private readonly FactoryManager _factory = new FactoryManager();

    public StarTrailEffect CreateStarTrail() => _factory.Create(starTrailPrefab);
    public HintHand CreateHand() => _factory.Create(handPrefab);
    public MarkFound Create() => _factory.Create(prefabMarkFound);
    public ChallengeNode CreateChallengeNode() => _factory.Create(challengeNodePrefab);
    public DifferentPair CreateDifferentPair() => _factory.Create(differentPairPrefab);
    public ErrorMark CreateErrorManager() => _factory.Create(errorMark);
    public HeartLives CreateHeart() => _factory.Create(heartPrefab);

    public void Release(GameObject obj) => _factory.Release(obj);
    public void Detach(GameObject obj) => _factory.Detach(obj);
    public void ReleaseAll() => _factory.ReleaseAll();

    private void OnDestroy() => _factory.ReleaseAll();
}