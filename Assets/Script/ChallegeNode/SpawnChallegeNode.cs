using UnityEngine;
using System.Collections.Generic;

public class SpawnChallegeNode : MonoBehaviour
{
    private int _countSpawn;
    [SerializeField] private FactoryManager factoryManager;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private SpriteRenderer spriteBackGround;

    [SerializeField] private List<ChallengeNode> nodes = new List<ChallengeNode>();
    [SerializeField] private ChallengeNode challengeNode;
    [SerializeField] private Sprite sprite;
    
    public void Init(int countSpawn)
    {
        _countSpawn = countSpawn;
        Spawn();
    }

    public void Spawn()
    {
        Clear();

        if (_countSpawn <= 0 || spriteBackGround == null || factoryManager == null || spawnPoint == null) return;

        Vector3 pos = spawnPoint.position;
        float startX = pos.x;
        float endX = 2f * spriteBackGround.bounds.center.x - startX;

        for (int i = 0; i < _countSpawn; i++)
        {
            ChallengeNode node = factoryManager.CreateChallengeNode();
            if (node == null) continue;

            float t = _countSpawn == 1 ? 0.5f : (float)i / (_countSpawn - 1);
            pos.x = Mathf.Lerp(startX, endX, t);

            node.transform.position = pos;
            node.Init(sprite);
            nodes.Add(node);
        }
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
}