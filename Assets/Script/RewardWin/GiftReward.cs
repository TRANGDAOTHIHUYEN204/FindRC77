using TMPro;
using UnityEngine;

public class GiftReward : MonoBehaviour
{
    [SerializeField] private GameObject coinObject;
    [SerializeField] private GameObject hintObject;

    [SerializeField] private TextMeshProUGUI textCoin;
    [SerializeField] private TextMeshProUGUI textHint;

    public void SetReward(int coin, int hint)
    {
        bool hasCoin = coin > 0;
        bool hasHint = hint > 0;

        textCoin.text = $"+{coin}";
        textHint.text = $"+{hint}";

        coinObject.SetActive(hasCoin);
        hintObject.SetActive(hasHint);
        textCoin.gameObject.SetActive(hasCoin);
        textHint.gameObject.SetActive(hasHint);
    }
}