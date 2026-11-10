using System;
using RCKit.Ads.Main;
using UnityEngine;

namespace RCKit.Ads
{
    public class AdsExampleNew : MonoBehaviour
    {
        public float timeShowInter = 60f;
        private float timeCountAdsInter;
        private bool initialized = false;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            RaccoonAdsManager.Instance.Initialize(()=>
            {
                initialized = true;
                OnInitialized();
                // RaccoonAdsManager.Instance.PreloadAdsWaitShow(AD_TYPE.OPEN_AD_FIRST_NATIVE);
            });
        }

        private void OnInitialized()
        {
            // initialized = true;
            Debug.Log("OnInitialized RaccoonAdsManager DONE ");
            RaccoonAdsManager.Instance.OnLoadingSceneLoaded();
            Invoke(nameof(ShowBanner), 10f);
            // RaccoonAdsManager.Instance.AddRemovableAd(AD_TYPE.OPEN_AD_NATIVE, AD_TYPE.INTER_FORCE_NATIVE, AD_TYPE.BANNER_NATIVE);
            
			RaccoonAdsManager.Instance.ShowAd(AD_TYPE.OPEN_AD_FIRST_NATIVE);
        }
        
        private void Update()
        {
            if (!initialized) return;
            
            CheckInterAds();
            // if (RaccoonAdsManager.Instance.AdsIsReady(AD_TYPE.OPEN_AD_FIRST_NATIVE))
            // {
            //     initialized = false;
            //     RaccoonAdsManager.Instance.ShowAd(AD_TYPE.OPEN_AD_FIRST_NATIVE, (p) => OnInitialized());
            // }
        }

        private void CheckInterAds()
        {
            timeCountAdsInter += Time.deltaTime;
            if(timeCountAdsInter >= timeShowInter)
            {
                timeCountAdsInter = -3;
                //show popup
                CheckShowInterstitialAd();
            }
        }
        
        private void CheckShowInterstitialAd()
        {
            RaccoonAdsManager.Instance.ShowAd(AD_TYPE.INTER_FORCE_NATIVE, null);
        }

        private void ShowRewardAds(Action<bool> callback)
        {
            RaccoonAdsManager.Instance.ShowAd(AD_TYPE.REWARD_ITEM_NATIVE, (s)=>
            {
                Debug.Log("Show Rewarded Ads -  " + s);
                callback?.Invoke(s);
            });
        }

        public void OnClickShowMultiAds()
        {
            // RaccoonAdsManager.Instance.ShowMultiAd(null, AD_TYPE.INTER_FORCE_NATIVE, AD_TYPE.INTER_FORCE_NATIVE_2);
        }

        public void OnClickRewardedAds()
        {
            ShowRewardAds(b =>
            {

            });
        }

        public void ShowBanner()
        {
            RaccoonAdsManager.Instance.ShowBanner(AD_TYPE.BANNER_NATIVE);
            
            
        }

        public void HideBanner()
        {
            RaccoonAdsManager.Instance.HideBanner(AD_TYPE.BANNER_NATIVE);
        }

        public void OnClickRemovableAds()
        {
            RaccoonAdsManager.Instance.SetAdsRemoved();
        }
    }
}
