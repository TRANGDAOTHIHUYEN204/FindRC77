using System;
using UnityEngine;

namespace JKit.Monetize.Ads
{
    public class AdsLifecycle : MonoBehaviour
    {
        private static AdsLifecycle _instance;
        private Action<bool> _onApplicationPause;
        
        static AdsLifecycle()
        {
            var gameObject = new GameObject("JKitAdsLifecycle");
            DontDestroyOnLoad(gameObject);
            _instance = gameObject.AddComponent<AdsLifecycle>();
        }
        
        public static event Action<bool> AdsOnApplicationPause
        {
            add => _instance._onApplicationPause += value;
            remove => _instance._onApplicationPause -= value;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            _onApplicationPause?.Invoke(pauseStatus);
        }
    }
}