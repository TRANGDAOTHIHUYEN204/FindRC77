using System;
using DG.Tweening;
using UnityEngine;
using Char.Haptic;
using TMPro;

public class ShopCoinManager : MonoBehaviour
{
    [Serializable]
    private class Pack
    {
        public int hint = 1;
        public int price = 100;
    }

    [SerializeField] private GameObject shopPanel;

    [Header("Toast không đủ coin")]
    [SerializeField] private RectTransform notEnoughToast; 
    [SerializeField] private TextMeshProUGUI coinText;    
    [SerializeField] private CanvasGroup toastGroup;      
    [SerializeField] private float toastDuration = 0.5f;
    [SerializeField] private float toastRiseY = 150f;
    [SerializeField] private float toastStartScale = 0.5f;

    [Header("Gói")]
    [SerializeField] private Pack pack1 = new Pack { hint = 1, price = 100 };
    [SerializeField] private Pack pack5 = new Pack { hint = 5, price = 450 };
    [SerializeField] private Pack pack10 = new Pack { hint = 10, price = 900 };

    [Header("SFX")]
    [SerializeField] private AudioClip buySuccessSfx;
    [SerializeField] private AudioClip notEnoughSfx;

    private Sequence _toastSeq;

    public void OpenShop() => OpenPanel(shopPanel);
    public void CloseShop() => ClosePanel(shopPanel);

    public void BuyPack1() => Buy(pack1);
    public void BuyPack5() => Buy(pack5);
    public void BuyPack10() => Buy(pack10);

    private void Buy(Pack pack)
    {
        if (pack == null || pack.hint <= 0 || pack.price <= 0) return;

        if (!Wallets.Coin.TrySpend(pack.price))
        {
            NotEnough();
            return;
        }

        SaveGame.HintBalance += pack.hint;
        PlaySfx(buySuccessSfx);
        Haptics.Light();
    }

    private void NotEnough()
    {
        ShowNotEnoughToast();
        PlaySfx(notEnoughSfx);
        Haptics.Heavy();
    }

    private void ShowNotEnoughToast()
    {
        if (notEnoughToast == null) return;

        _toastSeq?.Kill();

        if (coinText != null)
            coinText.text = Wallets.Coin.Amount.ToString();

        notEnoughToast.gameObject.SetActive(true);
        notEnoughToast.anchoredPosition = Vector2.zero;
        notEnoughToast.localScale = Vector3.one * toastStartScale;
        if (toastGroup != null) toastGroup.alpha = 1f;

        _toastSeq = DOTween.Sequence().SetUpdate(true);
        _toastSeq.Join(notEnoughToast.DOScale(1f, toastDuration).SetEase(Ease.OutBack));
        _toastSeq.Join(notEnoughToast.DOAnchorPosY(toastRiseY, toastDuration).SetEase(Ease.OutQuad));
        if (toastGroup != null)
            _toastSeq.Join(toastGroup.DOFade(0f, toastDuration * 0.4f).SetDelay(toastDuration * 0.6f));
        _toastSeq.OnComplete(() => notEnoughToast.gameObject.SetActive(false));
    }

    private void OnDisable()
    {
        _toastSeq?.Kill();
        if (notEnoughToast != null) notEnoughToast.gameObject.SetActive(false);
    }

    private static void OpenPanel(GameObject panel)
    {
        if (panel == null) return;

        panel.SetActive(true);

        PanelAnimation anim = panel.GetComponentInChildren<PanelAnimation>(true);
        if (anim != null) anim.Show();
    }

    private static void ClosePanel(GameObject panel)
    {
        if (panel == null) return;

        PanelAnimation anim = panel.GetComponentInChildren<PanelAnimation>(true);
        if (anim != null)
            anim.Close(() => panel.SetActive(false));
        else
            panel.SetActive(false);
    }

    private static void PlaySfx(AudioClip clip)
    {
        if (clip != null && SoundManagerHome.Instance != null)
            SoundManagerHome.Instance.PlaySfx(clip);
    }
}