using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPManager : MonoBehaviour
{

    StoreController m_StoreController; // The Unity Purchasing system.

    //Your products IDs. They should match the ids of your products in your store.
    public string goldProductId_1;
    public string goldProductId_2;

    [Space]
    [Header("Remove Ads IAP")]
    public string removeAdsProductId;
    public GameObject removeAdsButton;
    private const string removeAdsKey = "RemoveAdsPurchased";
    /////////////////////////////

    private void Awake() {
        InitializeIAP();
    }

    private void Start() { 
        if (PlayerPrefs.HasKey(removeAdsKey))
       {
            AdManager.Instance.ADSEnabled = false;
            removeAdsButton.SetActive(false);
        }
    }

    async void InitializeIAP()
    {
        m_StoreController = UnityIAPServices.StoreController();

        m_StoreController.OnPurchasePending += OnPurchasePending;
        m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        m_StoreController.OnPurchaseFailed += OnPurchaseFailed;

        m_StoreController.OnStoreDisconnected += OnStoreDisconnected;
        Debug.Log("Connecting to store.");
        await m_StoreController.Connect();

        m_StoreController.OnProductsFetchFailed += OnProductsFetchedFailed;
        m_StoreController.OnProductsFetched += OnProductsFetched;
        FetchProducts();
    }

    private void OnDestroy()
    {
        if (m_StoreController != null)
        {
            m_StoreController.OnPurchasePending -= OnPurchasePending;
            m_StoreController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            m_StoreController.OnPurchaseFailed -= OnPurchaseFailed;
            m_StoreController.OnStoreDisconnected -= OnStoreDisconnected;
            m_StoreController.OnProductsFetchFailed -= OnProductsFetchedFailed;
            m_StoreController.OnProductsFetched -= OnProductsFetched;
        }
    }

    private void FetchProducts()
    {
        var initialProductsToFetch = new List<ProductDefinition>
        {
            new(goldProductId_1, ProductType.Consumable),
            new(goldProductId_2, ProductType.Consumable),
            new(removeAdsProductId, ProductType.NonConsumable)
        };

        m_StoreController.FetchProducts(initialProductsToFetch);
    }

    #region Local Buying Methods

    public void BuyRemoveAds()
    {
        m_StoreController.PurchaseProduct(removeAdsProductId);
    }

    public void BuyGoldPack(string productID)
    {
        m_StoreController.PurchaseProduct(productID);
    }

    private void AddGold(int value)
    {
       // GameManager.Instance.AddCoin(value);
    }

    #endregion

    #region IAP CALLBACKS

    private void OnPurchaseFailed(FailedOrder order)
    {
        var product = GetFirstProductInOrder(order);
        if (product == null)
        {
            Debug.Log("Could not find product in failed order.");
        }

        Debug.Log($"Purchase failed - Product: '{product?.definition.id}'," +
                    $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                    + $"Purchase Failure Details: {order.Details}");
    }

    private void OnPurchasePending(PendingOrder order)
    {
        var product = GetFirstProductInOrder(order);
        if (product is null)
        {
            Debug.Log("Could not find product in order.");
            return;
        }
        Debug.Log($"Purchase complete - Product: {product.definition.id}");
        m_StoreController.ConfirmPurchase(order);
    }

    private void OnPurchaseConfirmed(Order order)
    {
        switch (order)
        {
            case ConfirmedOrder confirmedOrder:
                OnPurchaseConfirmed(confirmedOrder);
                break;
            case FailedOrder failedOrder:
                OnPurchaseConfirmationFailed(failedOrder);
                break;
            default:
                Debug.Log("Unknown OnPurchaseConfirmed result.");
                break;
        }
    }

    private void OnPurchaseConfirmed(ConfirmedOrder order)
    {
        var product = GetFirstProductInOrder(order);
        if (product == null)
        {
            Debug.Log("Could not find product in purchase confirmation.");
        }
        else if (product.definition.id == goldProductId_1)
        {
           // AddGold(500);
            Debug.Log("Player has purchased the 500 gold pack.");
        }
        else if (product.definition.id == goldProductId_2)
        {
           // AddGold(1500);
            Debug.Log("Player has purchased the 1500 gold pack.");
        }
        else if (product.definition.id == removeAdsProductId)
        {
            Debug.Log("Player has purchased the Remove Ads product.");
            PlayerPrefs.SetInt(removeAdsKey, 1);
            AdManager.Instance.ADSEnabled = false;
            removeAdsButton.SetActive(false);
        }

        Debug.Log($"Purchase confirmed- Product: {product?.definition.id}");
    }

    private void OnPurchaseConfirmationFailed(FailedOrder order)
    {
        var product = GetFirstProductInOrder(order);
        if (product == null)
        {
            Debug.Log("Could not find product in failed confirmation.");
        }

        Debug.Log($"Confirmation failed - Product: '{product?.definition.id}'," +
                    $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                    + $"Confirmation Failure Details: {order.Details}");
    }

    // Calling StoreController.Connect without listeners on StoreController.OnProductsFetched and StoreController.OnProductsFetchedFailed will result in warnings.
    private void OnProductsFetched(List<Product> products)
    {
        Debug.Log($"Products fetched successfully for {products.Count} products.");
    }

    private void OnProductsFetchedFailed(ProductFetchFailed failure)
    {
        Debug.Log($"Products fetch failed for {failure.FailedFetchProducts.Count} products: {failure.FailureReason}");
    }

    // Calling StoreController.Connect without a listener on the StoreController.OnStoreDisconnected event will result in warnings.
    private void OnStoreDisconnected(StoreConnectionFailureDescription description)
    {
        Debug.Log($"Store disconnected details: {description.message}");
    }

    #endregion

    Product GetFirstProductInOrder(Order order)
    {
        return order.CartOrdered.Items().First()?.Product;
    }

    public void RestorePurchases()
    {
        UnityIAPServices.StoreController().RestoreTransactions((success, error) =>
        {
            if (success) {
                // This does not mean anything was restored,
                // merely that the restoration process succeeded.
                Debug.Log("Restoration process succeeded.");
            } else {
                // Restoration failed. `error` contains the failure reason.
                Debug.LogError($"Restoration process failed: {error}");
            }
        });
    }
}