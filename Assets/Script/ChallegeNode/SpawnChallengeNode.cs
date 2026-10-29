using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SpawnChallengeNode : MonoBehaviour
{
    private int _countSpawn;
    private int _changedCount;
    private int _session;

    [SerializeField] private FactoryManager factoryManager;

    [Header("UI")]
    [SerializeField] private RectTransform container;
    [SerializeField] private Canvas canvas;

    [SerializeField] private List<ChallengeNode> nodes = new List<ChallengeNode>();
    [SerializeField] private Sprite sprite;

    [Header("Animation")]
    [SerializeField] private float slideDistance = 100f;
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private float bounceHeight = 20f;
    [SerializeField] private float bounceDuration = 0.2f;
    [SerializeField] private float spawnInterval = 0.08f;

    [SerializeField] private SpawnStarTrail spawnStarTrail;

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

        if (_countSpawn <= 0 || container == null || factoryManager == null)
        {
            Debug.LogWarning($"[SpawnChallengeNode] Bỏ qua spawn: count={_countSpawn}, container={container}, factory={factoryManager}", this);
            return;
        }

        for (int i = 0; i < _countSpawn; i++)
        {
            ChallengeNode node = factoryManager.CreateChallengeNode();
            if (node == null)
            {
                Debug.LogError("[SpawnChallengeNode] CreateChallengeNode() trả về null", this);
                continue;
            }

            node.transform.SetParent(container, false);
            node.transform.localScale = Vector3.one;
            node.transform.SetAsLastSibling();

            node.Init(sprite);
            nodes.Add(node);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(container);

        for (int i = 0; i < nodes.Count; i++)
            StartCoroutine(SpawnAnimation(nodes[i].Visual, i * spawnInterval));
    }

    private IEnumerator SpawnAnimation(RectTransform visual, float delay)
    {
        if (visual == null) yield break;
        visual.anchoredPosition = Vector2.down * slideDistance;

        yield return new WaitForSeconds(delay);
        if (visual == null) yield break;

        Vector2 start = Vector2.down * slideDistance;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            if (visual == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            visual.anchoredPosition = Vector2.LerpUnclamped(start, Vector2.zero, t);
            yield return null;
        }

        if (visual == null) yield break;
        visual.anchoredPosition = Vector2.zero;
        elapsed = 0f;

        while (elapsed < bounceDuration)
        {
            if (visual == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / bounceDuration);
            visual.anchoredPosition = Vector2.up * (Mathf.Sin(t * Mathf.PI) * bounceHeight);
            yield return null;
        }

        if (visual != null)
            visual.anchoredPosition = Vector2.zero;
    }
    public void ResetNodes()
    {
        _session++;                 
        _changedCount = 0;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == null) continue;
            nodes[i].ChangeSprite(sprite);   
        }
    }
    
    public void ChangeNext(Sprite spriteChange)
    {
        ChangeNext(spriteChange, null);
    }

    public void ChangeNext(
        Sprite spriteChange,
        RectTransform fromPoint)
    {
        if (_changedCount >= nodes.Count)
            return;

        ChallengeNode node = nodes[_changedCount];
        _changedCount++;

        if (node == null)
            return;
        
        if (fromPoint == null ||
            node.Visual == null ||
            spawnStarTrail == null)
        {
            node.ChangeSprite(spriteChange);
            return;
        }

        int session = _session;

        bool played = spawnStarTrail.Play(
            fromPoint,
            node.Visual,
            () =>
            {
                if (session != _session || node == null)
                    return;

                node.ChangeSprite(spriteChange);
            });
        if (!played)
            node.ChangeSprite(spriteChange);
    }


    public void Clear()
    {
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i] == null) continue;
            if (nodes[i].Visual != null)
                nodes[i].Visual.anchoredPosition = Vector2.zero;
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