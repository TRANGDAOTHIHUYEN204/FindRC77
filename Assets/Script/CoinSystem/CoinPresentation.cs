using System.Collections;
using UnityEngine;

public class CoinPresentation : MonoBehaviour
{
    [SerializeField] private Coin coinPrefab;
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private RectTransform target;
    [SerializeField] private RectTransform container;

    [Header("Burst")]
    [SerializeField] private int maxVisualCoins = 10;
    [SerializeField] private float scatterRadius = 60f;  
    [SerializeField] private float holdTime = 0.3f;       
    [SerializeField] private float stagger = 0.08f;       

    [Header("SFX")]
    [SerializeField] private AudioClip coinSpawnSfx;     
    [SerializeField] private AudioClip coinFlySfx;        

    private int _unpaid;
    private int _session;

    private void Awake()
    {
        if (coinPrefab.gameObject.scene.IsValid())
            coinPrefab.gameObject.SetActive(false);
    }

    private void OnEnable() => TryClaim();

    private void OnDisable()
    {
        _session++;
        StopAllCoroutines();

        if (_unpaid > 0)
        {
            Wallets.Coin.Add(_unpaid);
            _unpaid = 0;
        }

        foreach (Transform child in container) Destroy(child.gameObject);
    }

    public void TryClaim()
    {
        int reward = PendingReward.Amount;
        if (reward <= 0) return;

        PendingReward.Clear();
        _unpaid += reward;

        StartCoroutine(PlayBurst(reward, _session));
    }

    private IEnumerator PlayBurst(int reward, int session)
    {
        int count = Mathf.Min(maxVisualCoins, reward); 
        int perCoin = reward / count;
        int remainder = reward % count;
        
        var coins = new Coin[count];
        for (int i = 0; i < count; i++)
        {
            var coin = Instantiate(coinPrefab, container);
            coin.gameObject.SetActive(true);

            var rt = (RectTransform)coin.transform;
            rt.position = spawnPoint.position;
            rt.anchoredPosition += Random.insideUnitCircle * scatterRadius;

            coins[i] = coin;
        }

        PlaySfx(coinSpawnSfx);

        yield return new WaitForSeconds(holdTime);
        
        for (int i = 0; i < count; i++)
        {
            if (session != _session) yield break;

            var coin = coins[i];
            if (coin == null) continue;

            int value = perCoin + (i < remainder ? 1 : 0);

            PlaySfx(coinFlySfx);
            
            coin.Fly(coin.transform.position, target.position, 0f, c =>
            {
                if (c != null) Destroy(c.gameObject);
                if (session != _session) return;

                if (value > 0)
                {
                    _unpaid -= value;
                    Wallets.Coin.Add(value);
                }
            });

            yield return new WaitForSeconds(stagger);
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null && SoundManagerHome.Instance != null)
            SoundManagerHome.Instance.PlaySfx(clip);
    }
}