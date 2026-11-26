using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Màn "No Ads Offer": đồng hồ ưu đãi, giá theo store, nút mua và "No, thanks".
public class NoAdsOfferPopup : MonoBehaviour
{
    [SerializeField] private PanelAnimation panelAnimation;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text oldPriceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button noThanksButton;
    [Tooltip("Hào quang tia sau nhân vật, xoay chậm cho sinh động.")]
    [SerializeField] private RectTransform rays;
    [SerializeField] private float raysTurnSeconds = 24f;

    private float _tick;
    private bool _buying;

    private void Awake()
    {
        if (buyButton != null) buyButton.onClick.AddListener(Buy);
        if (noThanksButton != null) noThanksButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        IapService.Instance.OnProductsReady += RefreshPrice;
        RefreshPrice();
        RefreshTimer();

        if (rays != null)
        {
            rays.DOKill();
            rays.localRotation = Quaternion.identity;
            rays.DOLocalRotate(new Vector3(0f, 0f, -360f), raysTurnSeconds, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true);
        }
    }

    private void OnDisable()
    {
        if (IapService.HasInstance) IapService.Instance.OnProductsReady -= RefreshPrice;
        if (rays != null) rays.DOKill();
    }

    private void Update()
    {
        _tick -= Time.unscaledDeltaTime;
        if (_tick > 0f) return;
        _tick = 1f;
        RefreshTimer();
    }

    public void Show()
    {
        if (NoAdsOffer.IsOwned) return;

        gameObject.SetActive(true);
        if (panelAnimation != null) panelAnimation.Show();
    }

    public void Close()
    {
        if (_buying) return;

        if (panelAnimation != null && panelAnimation.gameObject.activeInHierarchy)
            panelAnimation.Close(() => gameObject.SetActive(false));
        else
            gameObject.SetActive(false);
    }

    private void RefreshTimer()
    {
        if (timerText != null) timerText.text = NoAdsOffer.FormatRemaining();
    }

    private void RefreshPrice()
    {
        string price = IapService.Instance.GetLocalizedPrice(NoAdsOffer.ProductId, out decimal value);
        if (priceText != null) priceText.text = price ?? NoAdsOffer.FallbackPrice;
        if (oldPriceText != null)
            oldPriceText.text = price != null ? NoAdsOffer.FormatOldPrice(price, value) : NoAdsOffer.FallbackOldPrice;
    }

    private void Buy()
    {
        if (_buying) return;
        _buying = true;
        if (buyButton != null) buyButton.interactable = false;

        IapService.Instance.Buy(NoAdsOffer.ProductId, ok =>
        {
            if (this == null) return;
            _buying = false;
            if (buyButton != null) buyButton.interactable = true;
            if (ok) Close();
        });
    }
}
