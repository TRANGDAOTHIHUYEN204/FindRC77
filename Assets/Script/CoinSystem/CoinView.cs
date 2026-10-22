using TMPro;
using UnityEngine;

public class CoinView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private void OnEnable()
    {
        Wallets.Coin.OnChanged += Refresh;
        Refresh(Wallets.Coin.Amount);
    }

    private void OnDisable()
    {
        Wallets.Coin.OnChanged -= Refresh;
    }

    private void Refresh(int amount)
    {
        label.text = amount.ToString();
    }
}