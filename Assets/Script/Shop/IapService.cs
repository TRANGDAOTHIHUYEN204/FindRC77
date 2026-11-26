using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

/// Bọc Unity IAP v5 (PurchasingController) cho cả game.
/// Tự khởi tạo khi app chạy, sống suốt vòng đời app.
public class IapService : MonoBehaviour
{
    private static IapService _instance;

    public static IapService Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<IapService>();
            if (_instance == null)
            {
                var go = new GameObject("IapService");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<IapService>();
            }
            return _instance;
        }
    }

    public static bool HasInstance => _instance != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() => Instance.Initialize();

    private static readonly List<ProductDefinition> ProductDefinitions = new List<ProductDefinition>
    {
        new ProductDefinition(NoAdsOffer.ProductId, ProductType.NonConsumable),
    };

    private PurchasingController _controller;
    private string _store;
    private string _buyingProductId;
    private Action<bool> _buyCallback;

    public bool IsReady { get; private set; }

    /// Bắn khi đã lấy được thông tin sản phẩm (giá theo vùng) từ store.
    public event Action OnProductsReady;

    private bool _adsApplied;

    private void Update()
    {
        // Người đã mua No Ads: chờ SDK quảng cáo init xong là tắt quảng cáo ngay,
        // kịp trước quảng cáo mở app đầu tiên.
        if (!_adsApplied && NoAdsOffer.IsOwned)
            _adsApplied = NoAdsOffer.ApplyToAds();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public async void Initialize()
    {
        if (_controller != null) return;

        try
        {
            _store = UnityIAPServices.GetDefaultStore();
            _controller = new PurchasingController(_store);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[IAP] Cannot create store controller: " + e.Message);
            _controller = null;
            return;
        }

        _controller.OnStoreConnected += store => _controller.FetchProducts(ProductDefinitions, store);
        _controller.OnStoreDisconnected += (store, failure) =>
        {
            IsReady = false;
            Debug.LogWarning($"[IAP] Store {store} disconnected: {failure.Message}");
        };
        _controller.OnProductsFetched += (store, products) =>
        {
            IsReady = true;
            Debug.Log($"[IAP] {products.Count} products ready on {store}.");
            OnProductsReady?.Invoke();
            // Lấy lại các giao dịch cũ (cài lại app vẫn nhận lại gói No Ads).
            _controller.FetchPurchases(store);
        };
        _controller.OnProductsFetchFailed += (store, failure) =>
            Debug.LogWarning($"[IAP] Fetch products failed: {failure.FailureReason}");
        _controller.OnPurchasePending += OnPurchasePending;
        _controller.OnPurchaseConfirmed += OnPurchaseConfirmed;
        _controller.OnPurchaseFailed += (store, failed) =>
        {
            Debug.LogWarning($"[IAP] Purchase failed: {failed.FailureReason} - {failed.Details}");
            FinishBuy(false);
        };
        _controller.OnPurchaseDeferred += (store, deferred) =>
        {
            Debug.Log("[IAP] Purchase deferred (waiting for approval).");
            FinishBuy(false);
        };
        _controller.OnPurchasesFetched += OnPurchasesFetched;
        _controller.OnPurchasesFetchFailed += (store, failure) =>
            Debug.LogWarning($"[IAP] Fetch purchases failed: {failure.FailureReason} - {failure.Message}");

        try
        {
            await _controller.Connect();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[IAP] Connect failed: " + e.Message);
        }
    }

    public Product GetProduct(string productId)
    {
        if (_controller == null || string.IsNullOrEmpty(_store)) return null;
        return _controller.GetProductById(productId, _store);
    }

    /// Giá theo vùng của store, null nếu chưa có.
    public string GetLocalizedPrice(string productId, out decimal price)
    {
        price = 0m;
        var product = GetProduct(productId);
        if (product == null || product.metadata == null) return null;

        price = product.metadata.localizedPrice;
        string text = product.metadata.localizedPriceString;
        return string.IsNullOrEmpty(text) ? null : text;
    }

    public void Buy(string productId, Action<bool> onDone)
    {
        if (_buyCallback != null)
        {
            onDone?.Invoke(false);
            return;
        }

        var product = IsReady ? GetProduct(productId) : null;
        if (product == null)
        {
            Debug.LogWarning($"[IAP] Product {productId} not available (store ready = {IsReady}).");
            if (_controller == null) Initialize();
            onDone?.Invoke(false);
            return;
        }

        _buyingProductId = productId;
        _buyCallback = onDone;
        _controller.PurchaseProduct(product);
    }

    public void Restore(Action<bool> onDone)
    {
        if (_controller == null)
        {
            onDone?.Invoke(false);
            return;
        }
        _controller.RestoreTransactions((store, ok, error) =>
        {
            if (!ok) Debug.LogWarning($"[IAP] Restore failed on {store}: {error}");
            onDone?.Invoke(ok);
        });
    }

    private void OnPurchasePending(string store, PendingOrder order)
    {
        bool hasBuying = false;
        foreach (var item in order.CartOrdered.Items())
        {
            string id = item.Product?.definition?.id;
            Grant(id);
            if (id == _buyingProductId) hasBuying = true;
        }

        // Lưu quyền lợi xong mới xác nhận với store.
        _controller.ConfirmPurchase(order);
        if (hasBuying) FinishBuy(true);
    }

    private void OnPurchaseConfirmed(string store, Order order)
    {
        if (order is FailedOrder failed)
            Debug.LogWarning($"[IAP] Confirm failed: {failed.FailureReason} - {failed.Details}");
    }

    private void OnPurchasesFetched(string store, Orders orders)
    {
        foreach (var confirmed in orders.ConfirmedOrders)
            foreach (var item in confirmed.CartOrdered.Items())
                Grant(item.Product?.definition?.id);
    }

    private static void Grant(string productId)
    {
        if (productId == NoAdsOffer.ProductId)
            NoAdsOffer.Grant();
    }

    private void FinishBuy(bool ok)
    {
        var cb = _buyCallback;
        _buyCallback = null;
        _buyingProductId = null;
        cb?.Invoke(ok);
    }
}
