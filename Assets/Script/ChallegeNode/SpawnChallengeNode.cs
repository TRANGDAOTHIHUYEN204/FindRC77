
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnChallengeNode : MonoBehaviour
{
    private int _countSpawn;
    private int _changedCount;

    [SerializeField] private FactoryManager factoryManager;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private SpriteRenderer spriteBackGround;

    [SerializeField] private List<ChallengeNode> nodes = new List<ChallengeNode>();
    [SerializeField] private Sprite sprite;
    
    [SerializeField] private float slideDistance = 1f;
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private float bounceHeight = 0.2f;
    [SerializeField] private float bounceDuration = 0.2f;
    [SerializeField] private float spawnInterval = 0.08f;

    [SerializeField] private StarTrailEffect starTrailPrefab;
    private int _session;
    public void Init(int countSpawn)
    {
        _countSpawn = countSpawn;
        Spawn();
    }

    public void Spawn()
    {
        _session++; 
        StopAllCoroutines();
        Clear();
        _changedCount = 0;

        if (_countSpawn <= 0 || spriteBackGround == null || factoryManager == null || spawnPoint == null)
            return;

        Vector3 pos = spawnPoint.position;
        float startX = pos.x;
        float endX = 2f * spriteBackGround.bounds.center.x - startX;

        for (int i = 0; i < _countSpawn; i++)
        {
            ChallengeNode node = factoryManager.CreateChallengeNode();
            if (node == null) continue;

            float t = _countSpawn == 1 ? 0.5f : (float)i / (_countSpawn - 1);

            pos.x = Mathf.Lerp(startX, endX, t);

            Vector3 targetPosition = pos;
            node.transform.position =
                targetPosition + Vector3.down * slideDistance;

            node.Init(sprite);
            nodes.Add(node);
            StartCoroutine(SpawnAnimation(node.transform, targetPosition, i * spawnInterval));
        }
    }

    private IEnumerator SpawnAnimation(Transform target, Vector3 targetPosition, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (target == null) yield break;

        Vector3 startPosition =
            targetPosition + Vector3.down * slideDistance;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            if (target == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            
            t = 1f - Mathf.Pow(1f - t, 3f);

            target.position =
                Vector3.LerpUnclamped(startPosition, targetPosition, t);

            yield return null;
        }

        if (target == null) yield break;
        target.position = targetPosition;
        elapsed = 0f;

        while (elapsed < bounceDuration)
        {
            if (target == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / bounceDuration);
            float offsetY = Mathf.Sin(t * Mathf.PI) * bounceHeight;

            target.position =
                targetPosition + Vector3.up * offsetY;

            yield return null;
        }

        if (target != null)
            target.position = targetPosition;
    }
    public void ChangeNext(Sprite spriteChange)
    {
        ChangeNext(spriteChange, null);
    }

    public void ChangeNext(Sprite spriteChange, Vector3? fromWorldPos)
    {
        if (_changedCount >= nodes.Count) return;

        ChallengeNode node = nodes[_changedCount];
        _changedCount++; 
        int session = _session; 
        if (fromWorldPos == null || starTrailPrefab == null)
        {
            node.ChangeSprite(spriteChange);
            return;
        }

        StarTrailEffect fx = Instantiate(starTrailPrefab);
        fx.gameObject.SetActive(true);
        fx.Play(fromWorldPos.Value, node.transform.position, () =>
        {
            if (session != _session || node == null) return;
            node.ChangeSprite(spriteChange);
        });
    }

    public void Clear()
    {
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i] != null)
                factoryManager.Release(nodes[i].gameObject);
        }

        nodes.Clear();
    }
    public void PlayWinBounce(float stagger = 0.05f)
    {
        StartCoroutine(WinBounceRoutine(stagger));
    }

    private IEnumerator WinBounceRoutine(float stagger)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null)
                nodes[i].PlayWinBounce();
            if (stagger > 0f)
                yield return new WaitForSeconds(stagger);
        }
    }
}
