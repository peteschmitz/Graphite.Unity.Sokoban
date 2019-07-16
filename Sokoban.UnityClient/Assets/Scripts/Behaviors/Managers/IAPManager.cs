using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Events;
using UnityEngine.Purchasing;
using UnityEngine.SceneManagement;

public class IAPManager : IAPListener
{
    private static CodelessIAPStoreListener StoreListener => CodelessIAPStoreListener.Instance;
    private static IStoreController m_StoreController => StoreListener.StoreController;          // The Unity Purchasing system.
    private static IExtensionProvider m_StoreExtensionProvider => StoreListener.ExtensionProvider; // The store-specific Purchasing subsystems.

    public class GrantEvent : UnityEvent<string, bool> { }
    public class GrantFailureEvent : UnityEvent<string, string> { }
    public class RestorationEvent : UnityEvent<bool> { }

    public const string IAPRemoveAds = "remove_ads";
    private static bool hasRecovered = false;

    public static string[] IAPSingles = new List<string>
        {
            IAPRemoveAds
        }
        .Concat(IAPLevelPak.ActiveLevelPaks)
        .ToArray();

    public GrantEvent OnGrantEvent = new GrantEvent();
    public GrantFailureEvent OnGrantFailedEvent = new GrantFailureEvent();
    public RestorationEvent OnRestoredEvent = new RestorationEvent();

    private HashSet<string> localGrants = new HashSet<string>();
    private List<(string, Action)> grantCallbacks = new List<(string, Action)>();
    private bool initialized = false;

    void Start()
    {
        // DANGER : Nuke locally saved IAP in debug-mode
        if (GameContext.IsDebugMode)
        {
            // PlayerPrefs.DeleteKey(IAPLevelPak.LevelPak1);
            // PlayerPrefs.DeleteKey(IAPManager.IAPRemoveAds);
            // PlayerPrefs.Save();
        }

        this.OnGrantEvent.AddListener((product, fromLocal) => Debug.Log("IAP Grant: " + product + $"(from local: {fromLocal})"));
        this.OnGrantFailedEvent.AddListener((product, reason) => Debug.Log("IAP Failed: " + product + " reason: " + reason));

        this.AttemptLocalGrants();
        if (CodelessIAPStoreListener.initializationComplete)
        {
            this.RestoreGrants();
        }
        else
        {
            StoreListener.OnInitialiedEvent.AddListener((store) =>
            {
                this.RestoreGrants();
            });
        }

        this.onPurchaseComplete.AddListener((product) =>
        {
            this.Grant(product);
        });
        this.onPurchaseFailed.AddListener((product, reason) =>
        {
            this.OnGrantFailedEvent.Invoke(product.definition.id, reason.ToString());
        });
        this.initialized = true;
    }

    public Product GetProduct(string apiKey)
    {
        if (!this.IsInitialized())
        {
            Debug.LogError($"IAPManager->Attempted to get {apiKey} before initialization");
            return null;
        }
        var product = m_StoreController.products.WithID(apiKey);
        if (product == null)
        {
            Debug.LogError($"IAPManager->Requested {apiKey} but no product found.");
        }
        return product;
    }

    public void WithGrant(string apiKey, Action action)
    {
        if (!this.initialized || !CodelessIAPStoreListener.initializationComplete)
        {
            this.grantCallbacks.Add((apiKey, action));
        }
        else
        {
            if (this.HasGrant(apiKey))
            {
                action.Invoke();
            }
        }
    }

    private void AttemptLocalGrants()
    {
        IAPSingles.ForEach(x =>
        {
            if (!this.localGrants.Contains(x))
            {
                var iapReceipt = PlayerPrefs.GetString(x, "");
                if (!String.IsNullOrEmpty(iapReceipt))
                {
                    this.localGrants.Add(x);
                    this.Grant(x, true);
                }
            }
        });
    }

    public void RestoreTransactions()
    {
        m_StoreExtensionProvider.GetExtension<IAppleExtensions>()?.RestoreTransactions(result =>
        {
            this.OnRestoredEvent.Invoke(result);
            Debug.Log($"IAPManager-> Restoration result: {result}");
        });
    }

    private void RestoreGrants()
    {
        if (!hasRecovered)
        {
            hasRecovered = true;
            this.RestoreTransactions();
        }
        IAPSingles.ForEach(x =>
        {
            if (!this.localGrants.Contains(x) && this.HasGrant(x))
            {
                this.Grant(x);
            }
        });
    }

    private void Grant(Product product)
    {
        if (String.IsNullOrEmpty(product?.definition?.id))
        {
            throw new ArgumentException("Definition was null during grant.");
        }
        if (String.IsNullOrEmpty(product.receipt))
        {
            throw new ArgumentException("Receipt was null during grant, definition: " + product.definition.id);
        }

        try
        {
            var productId = product.definition.id;
            var price = product.metadata?.localizedPrice ?? 0.0m;
            var currency = product.metadata?.isoCurrencyCode ?? "";
            var receipt = product.receipt;
            AnalyticsManager.Event(() => Analytics.Transaction(productId, price, currency, receipt, null));
            AnalyticsManager.Event(() => AnalyticsEvent.ItemAcquired(
                AcquisitionType.Premium,
                SceneManager.GetActiveScene().name,
                1.0f,
                productId,
                eventData: new Dictionary<string, object>{
                    { "platform", Application.platform.ToString() },
                    { "isGamepad", GameContext.IsNavigationEnabled }
                }));

            Debug.Log($"IAP Purchase: {productId}, receipt: {receipt}, tx: {product.transactionID})");
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        try
        {
            PlayerPrefs.SetString(product.definition.id, product.receipt);
            PlayerPrefs.Save();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        this.Grant(product.definition.id);
    }

    private void Grant(string iapKey, bool fromLocal = false)
    {
        var executedCallbacks = new List<(string, Action)>();
        foreach (var callback in this.grantCallbacks)
        {
            if (iapKey.Equals(callback.Item1))
            {
                callback.Item2();
                executedCallbacks.Add(callback);
            }
        }
        this.grantCallbacks = this.grantCallbacks.Except(executedCallbacks).ToList();

        this.OnGrantEvent.Invoke(iapKey, fromLocal);
    }

    public bool HasGrant(string iapKey)
    {
        if (iapKey.IsValid() && this.localGrants.Contains(iapKey))
        {
            return true;
        }

        if (!this.IsInitialized())
        {
            return false;
        }
        // var products = m_StoreController.products;
        // var product = products.WithID(iapKey);

        if (iapKey.IsValid() && this.localGrants.Contains(iapKey))
        {
            return true;
        }

        return this.IsInitialized() &&
            m_StoreController.products.WithID(iapKey) != null &&
            StoreListener.GetProduct(iapKey) != null &&
            StoreListener.GetProduct(iapKey).hasReceipt;
    }

    private bool IsInitialized()
    {
        return m_StoreController != null && m_StoreExtensionProvider != null && CodelessIAPStoreListener.initializationComplete;
    }
}

public static class IAPLevelPak
{
    public static readonly string LevelPak1 = "level_pak_1";

    public static readonly string[] ActiveLevelPaks = new string[]
    {
        LevelPak1
    };

    public static readonly Dictionary<string, HashSet<string>> IAPLevelLookup = new Dictionary<string, HashSet<string>>
    {
        {
            LevelPak1,
            new HashSet<string>
            {
                "Hard-05_level_2019-06-28-12-33-01-614_ground-32_moves-66_items-7_cols-6_rows-9",
                "Hard-06_level_2019-06-28-16-24-03-174_ground-35_moves-74_items-8_cols-7_rows-8",
                "Hard-07_level_2019-06-28-13-37-42-021_ground-41_moves-74_items-10_cols-7_rows-9",
                "Hard-08_level_2019-06-27-22-37-11-943_ground-31_moves-76_items-9_cols-7_rows-7",
                "Hard-09_level_2019-06-28-09-19-36-527_ground-36_moves-79_items-9_cols-7_rows-8",
                "Hard-10_level_2019-06-27-22-35-32-578_ground-48_moves-80_items-13_cols-7_rows-9",
                "Hard-11_level_2019-06-28-21-47-59-630_ground-43_moves-81_items-10_cols-9_rows-8",
                "Hard-12_level_2019-06-28-12-28-32-380_ground-45_moves-89_items-12_cols-9_rows-8",
                "Hard-13_level_2019-06-28-12-17-33-940_ground-37_moves-92_items-10_cols-9_rows-7",
                "Hard-14_level_2019-06-28-21-26-28-949_ground-41_moves-93_items-11_cols-9_rows-7",
                "Hard-15_level_2019-06-28-21-57-55-461_ground-35_moves-95_items-11_cols-9_rows-5",
                "Hard-16_level_2019-06-28-16-07-51-767_ground-33_moves-98_items-10_cols-6_rows-8",
                "Hard-17_level_2019-06-27-22-31-04-104_ground-59_moves-102_items-9_cols-9_rows-9",
                "Hard-18_level_2019-06-28-13-43-53-028_ground-51_moves-154_items-14_cols-9_rows-7",
                "Med-05_level_2019-06-27-23-46-15-828_ground-35_moves-36_items-6_cols-9_rows-5",
                "Med-06_level_2019-06-28-13-29-50-219_ground-33_moves-38_items-7_cols-7_rows-8",
                "Med-07_level_2019-06-28-09-11-25-206_ground-29_moves-44_items-7_cols-7_rows-7",
                "Med-08_level_2019-06-27-19-44-30-427_ground-28_moves-46_items-9_cols-8_rows-6",
                "Med-09_level_2019-06-28-09-02-30-140_ground-28_moves-46_items-9_cols-7_rows-6",
                "Med-10_level_2019-06-28-09-16-05-176_ground-37_moves-46_items-10_cols-7_rows-8",
                "Med-11_level_2019-06-27-23-50-13-425_ground-38_moves-46_items-6_cols-7_rows-9",
                "Med-12_level_2019-06-27-22-32-50-821_ground-26_moves-47_items-9_cols-5_rows-7",
                "Med-13_level_2019-06-28-12-40-04-429_ground-37_moves-47_items-8_cols-8_rows-7",
                "Med-14_level_2019-06-28-09-38-28-492_ground-40_moves-47_items-7_cols-10_rows-6",
                "Med-15_level_2019-06-28-16-10-14-351_ground-26_moves-48_items-7_cols-5_rows-8",
                "Med-16_level_2019-06-27-23-38-35-959_ground-30_moves-53_items-8_cols-6_rows-6",
                "Med-17_level_2019-06-28-12-24-24-526_ground-34_moves-55_items-11_cols-8_rows-7",
                "Med-18_level_2019-06-27-22-37-57-683_ground-34_moves-55_items-8_cols-7_rows-7"
            }
        }
    };

    public static string GetIAPRequirement(string levelName)
    {
        foreach (var activePak in ActiveLevelPaks)
        {
            if (IAPLevelLookup.ContainsKey(activePak))
            {
                if (IAPLevelLookup[activePak].Contains(levelName))
                {
                    return activePak;
                }
            }
        }

        return string.Empty;
    }
}