#if UNITY_IOS

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GoogleMobileAds. Api;
using UnityEngine;

namespace JKit. Monetize.Ads
{
    /// <summary>
    /// iOS implementation of INativeOverlay using native Objective-C plugin
    /// </summary>
    internal class IOSNativeOverlay : INativeOverlay
    {
        #region Native Plugin Imports

        [DllImport("__Internal")]
        private static extern void JNativeAds_Initialize();

        [DllImport("__Internal")]
        private static extern string JNativeAds_Create(
            string instanceId,
            string gameObjectName,
            string unitID,
            long refresh,
            float x, float y, float width, float height,
            string templateName,
            string quitType,
            float paddingStart, float paddingEnd,
            float paddingTop, float paddingBottom
        );

        [DllImport("__Internal")]
        private static extern void JNativeAds_Load(string instanceId);

        [DllImport("__Internal")]
        private static extern void JNativeAds_Show(string instanceId);

        [DllImport("__Internal")]
        private static extern void JNativeAds_Hide(string instanceId);

        [DllImport("__Internal")]
        private static extern void JNativeAds_Destroy(string instanceId);

        [DllImport("__Internal")]
        private static extern void JNativeAds_OnApplicationPause(string instanceId, bool pause);

        [DllImport("__Internal")]
        private static extern void JNativeAds_SetBannerTheme(string instanceId, string theme);

        [DllImport("__Internal")]
        private static extern void JNativeAds_SetRefreshTime(string instanceId, long seconds);
        
        [DllImport("__Internal")]
        private static extern void JNativeAds_SetQuitButtonScale(string instanceId, float scale);

        #endregion

        #region Static Management

        private static bool _initialized = false;
        private static Dictionary<string, IOSNativeOverlay> _instances = new Dictionary<string, IOSNativeOverlay>();
        private static IOSNativeOverlayCallbackReceiver _callbackReceiver;

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            
            JNativeAds_Initialize();
            
            // Tạo GameObject để nhận callbacks từ native
            var go = new GameObject("IOSNativeOverlayCallbackReceiver");
            UnityEngine.Object. DontDestroyOnLoad(go);
            _callbackReceiver = go.AddComponent<IOSNativeOverlayCallbackReceiver>();
            
            _initialized = true;
        }

        internal static IOSNativeOverlay GetInstance(string instanceId)
        {
            if (_instances.TryGetValue(instanceId, out var instance))
            {
                return instance;
            }
            return null;
        }

        #endregion

        #region Instance Fields

        private readonly string _instanceId;
        private bool _isDestroyed = false;

        #endregion

        #region Events

        public event Action OnImpression;
        public event Action OnClicked;
        public event Action OnClosed;
        public event Action OnLoadSucceed;
        public event Action<AdValue> OnAdPaid;
        public event Action<LoadAdError> OnLoadFailed;

        #endregion

        #region Constructor

        public IOSNativeOverlay(
            string unitID,
            long refresh,
            float x, float y, float width, float height,
            string template, string quitType,
            float paddingStart, float paddingEnd, float paddingTop, float paddingBottom
        )
        {
            EnsureInitialized();

            // Tạo unique instance ID
            _instanceId = Guid.NewGuid().ToString();
            _instances[_instanceId] = this;

            // Gọi native để tạo overlay
            JNativeAds_Create(
                _instanceId,
                _callbackReceiver.gameObject.name,
                unitID,
                refresh,
                x, y, width, height,
                template, quitType,
                paddingStart, paddingEnd, paddingTop, paddingBottom
            );

            Debug.Log($"[IOSNativeOverlay] Created instance: {_instanceId}");
        }

        #endregion

        #region INativeOverlay Implementation

        public void Load()
        {
            if (_isDestroyed) return;
            JNativeAds_Load(_instanceId);
            Debug.Log($"[IOSNativeOverlay] Load called for instance: {_instanceId}");
        }

        public void Show()
        {
            if (_isDestroyed) return;
            JNativeAds_Show(_instanceId);
            Debug.Log($"[IOSNativeOverlay] Show called for instance:  {_instanceId}");
        }

        public void Hide()
        {
            if (_isDestroyed) return;
            JNativeAds_Hide(_instanceId);
            Debug.Log($"[IOSNativeOverlay] Hide called for instance: {_instanceId}");
        }

        public void Destroy()
        {
            if (_isDestroyed) return;
            
            _isDestroyed = true;
            JNativeAds_Destroy(_instanceId);
            _instances.Remove(_instanceId);
            
            Debug.Log($"[IOSNativeOverlay] Destroy called for instance: {_instanceId}");
        }

        public void setQuitButtonScale(float scale) 
        {
            if (_isDestroyed) return;
            JNativeAds_SetQuitButtonScale(_instanceId, scale);
        }

        public void setQuitButtonCoverScale(float scale) 
        {
            if (_isDestroyed) return;
            // JNativeAds_SetQuitButtonScale(_instanceId, scale);
        }
        
        public void setBannerTheme(string theme) 
        {
            if (_isDestroyed) return;
            JNativeAds_SetBannerTheme(_instanceId, theme);
        }
        
        public void setRefreshTime(long seconds) 
        {
            if (_isDestroyed) return;
            JNativeAds_SetRefreshTime(_instanceId, seconds);
        }

        void INativeOverlay. OnApplicationPause(bool pauseStatus)
        {
            if (_isDestroyed) return;
            JNativeAds_OnApplicationPause(_instanceId, pauseStatus);
        }

        #endregion

        #region Internal Callback Handlers (called from IOSNativeOverlayCallbackReceiver)

        internal void HandleLoadSucceed()
        {
            Debug. Log($"[IOSNativeOverlay] HandleLoadSucceed for instance: {_instanceId}");
            OnLoadSucceed?. Invoke();
        }

        internal void HandleLoadFailed(string errorMessage)
        {
            Debug.Log($"[IOSNativeOverlay] HandleLoadFailed for instance: {_instanceId}, error: {errorMessage}");
            OnLoadFailed?.Invoke(new LoadAdError(errorMessage));
        }

        internal void HandleClosed()
        {
            Debug.Log($"[IOSNativeOverlay] HandleClosed for instance: {_instanceId}");
            OnClosed?.Invoke();
        }

        internal void HandleClicked()
        {
            Debug.Log($"[IOSNativeOverlay] HandleClicked for instance: {_instanceId}");
            OnClicked?.Invoke();
        }

        internal void HandleImpression()
        {
            Debug. Log($"[IOSNativeOverlay] HandleImpression for instance: {_instanceId}");
            OnImpression?.Invoke();
        }

        internal void HandleAdPaid(string jsonData)
        {
            Debug.Log($"[IOSNativeOverlay] HandleAdPaid for instance: {_instanceId}, data: {jsonData}");
            
            try
            {
                var adValueData = JsonUtility.FromJson<AdValueJsonData>(jsonData);
                long micros = (long)adValueData.valueMicros;
                var adValue = new AdValue
                {
                    CurrencyCode = adValueData.currencyCode,
                    Value = micros,
                    Precision = (AdValue.PrecisionType)adValueData.precision
                };
                OnAdPaid?. Invoke(adValue);
            }
            catch (Exception e)
            {
                Debug.LogError($"[IOSNativeOverlay] Failed to parse AdValue JSON: {e.Message}");
            }
        }

        #endregion

        #region Helper Classes

        [Serializable]
        private class AdValueJsonData
        {
            public string currencyCode;
            public double valueMicros;
            public int precision;
        }

        #endregion
    }

    /// <summary>
    /// MonoBehaviour to receive UnitySendMessage callbacks from native iOS code
    /// </summary>
    internal class IOSNativeOverlayCallbackReceiver : MonoBehaviour
    {
        #region Message Format
        // Native gửi message với format: "instanceId|data"
        // Ví dụ: "abc-123|" hoặc "abc-123|error message" hoặc "abc-123|{json}"
        #endregion

        private (string instanceId, string data) ParseMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return (null, null);
            }

            int separatorIndex = message. IndexOf('|');
            if (separatorIndex < 0)
            {
                // Không có separator, toàn bộ là instanceId
                return (message, string.Empty);
            }

            string instanceId = message. Substring(0, separatorIndex);
            string data = message. Substring(separatorIndex + 1);
            return (instanceId, data);
        }

        private IOSNativeOverlay GetOverlay(string instanceId)
        {
            return IOSNativeOverlay.GetInstance(instanceId);
        }

        #region Callback Methods (called by UnitySendMessage from native)

        /// <summary>
        /// Called when native ad loads successfully
        /// Message format: "instanceId|"
        /// </summary>
        public async void OnLoadSucceed(string message)
        {
            await Awaitable.MainThreadAsync();
            
            var (instanceId, _) = ParseMessage(message);
            var overlay = GetOverlay(instanceId);
            overlay?.HandleLoadSucceed();
        }

        /// <summary>
        /// Called when native ad fails to load
        /// Message format: "instanceId|errorMessage"
        /// </summary>
        public async void OnLoadFailed(string message)
        {
            await Awaitable.MainThreadAsync();
            
            var (instanceId, errorMessage) = ParseMessage(message);
            var overlay = GetOverlay(instanceId);
            overlay?.HandleLoadFailed(errorMessage ??  "Unknown error");
        }

        /// <summary>
        /// Called when native ad is closed
        /// Message format: "instanceId|"
        /// </summary>
        public async void OnClosed(string message)
        {
            await Awaitable.MainThreadAsync();
            
            var (instanceId, _) = ParseMessage(message);
            var overlay = GetOverlay(instanceId);
            overlay?.HandleClosed();
        }

        /// <summary>
        /// Called when native ad is clicked
        /// Message format: "instanceId|"
        /// </summary>
        public async void OnClicked(string message)
        {
            await Awaitable.MainThreadAsync();
            
            var (instanceId, _) = ParseMessage(message);
            var overlay = GetOverlay(instanceId);
            overlay?.HandleClicked();
        }

        /// <summary>
        /// Called when native ad records an impression
        /// Message format: "instanceId|"
        /// </summary>
        public async void OnImpression(string message)
        {
            await Awaitable.MainThreadAsync();
            
            var (instanceId, _) = ParseMessage(message);
            var overlay = GetOverlay(instanceId);
            overlay?.HandleImpression();
        }

        /// <summary>
        /// Called when ad generates revenue
        /// Message format: "instanceId|{jsonData}"
        /// </summary>
        public async void OnAdPaid(string message)
        {
            await Awaitable.MainThreadAsync();
            
            var (instanceId, jsonData) = ParseMessage(message);
            var overlay = GetOverlay(instanceId);
            overlay?.HandleAdPaid(jsonData ??  "{}");
        }

        #endregion
    }
}

#endif