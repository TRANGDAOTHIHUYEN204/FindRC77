using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Nút "No Ads Offer" ở home: mở màn ưu đãi, tự bật màn này một lần ở lượt vào home thứ 3,
/// và ẩn đi khi người chơi đã mua gói.
public class NoAdsHomeButton : MonoBehaviour
{
    [SerializeField] private NoAdsOfferPopup popup;
    [SerializeField] private Button button;
    [SerializeField] private float autoShowDelay = 0.5f;

    private void OnEnable()
    {
        if (button != null) button.onClick.AddListener(Open);
        NoAdsOffer.OnOwned += Hide;
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(Open);
        NoAdsOffer.OnOwned -= Hide;
    }

    private void Start()
    {
        bool autoShow = NoAdsOffer.RegisterHomeVisit();

        if (NoAdsOffer.IsOwned)
        {
            gameObject.SetActive(false);
            return;
        }

        // Bắt đầu đếm vòng ưu đãi 24h từ lần đầu thấy nút.
        _ = NoAdsOffer.Remaining;

        if (autoShow) StartCoroutine(AutoShow());
    }

    private IEnumerator AutoShow()
    {
        yield return new WaitForSecondsRealtime(autoShowDelay);
        Open();
    }

    private void Open()
    {
        if (popup != null) popup.Show();
    }

    private void Hide()
    {
        if (this != null) gameObject.SetActive(false);
    }
}
