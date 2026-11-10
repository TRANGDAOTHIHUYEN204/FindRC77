using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace RCKit.Ads.Main
{
    public class RaccoonAdsFirst : MonoBehaviour
    {
        [SerializeField] private Slider sliderLoading;
        [SerializeField] private Text txtLoadingPercent;
        [SerializeField] private float timeLoading = 15f;
        
        [SerializeField] private AD_TYPE adTypeFirst;
        [SerializeField] private bool stopRetryLoadAfterLoading;
        
        [SerializeField] private UnityEvent onBeginLoading, onEndLoading;
        private float multi_speed = 1f;
        
        private void Start()
        {
            multi_speed = 1f;
            StartCoroutine(WaitFirstAd());
            StartCoroutine(StartLoading());
        }
        
        private IEnumerator StartLoading()
        {
#if UNITY_EDITOR
            timeLoading = 2f;
            Application.runInBackground = true;
#endif
            multi_speed = 1f;
            var wait = new WaitForEndOfFrame();
            yield return wait;
            var timeLoad = 0f;
            yield return wait;
            onBeginLoading?.Invoke();
            while (timeLoad < timeLoading)
            {
                timeLoad += Time.deltaTime * multi_speed;
                var progress = timeLoad / timeLoading;
                UpdateProcess(progress);
                yield return wait;
            }
            yield return new WaitForSeconds(0.5f);
#if ENABLE_RACCOON_ADMOB
            RaccoonAdsManager.Instance?.OnLoadingSceneLoaded();
#endif
            onEndLoading?.Invoke();
        }

        private void UpdateProcess(float progress)
        {
            if (sliderLoading)
                sliderLoading.value = progress;
            var progressPercent = Mathf.RoundToInt(MathF.Min(progress, 1) * 100);
            if (txtLoadingPercent)
                txtLoadingPercent.text = progressPercent + "%";
        }
        
        private IEnumerator WaitFirstAd()
        {
#if !ENABLE_RACCOON_ADMOB
            yield break;
#else
            var loadAd = false;
            var wait = new WaitForSecondsRealtime(0.2f);
            yield return wait;
            
            RaccoonAdsManager.Instance?.Initialize(null);
            
            while (true)
            {
                yield return wait;

                if (RaccoonAdsManager.Instance == null || !RaccoonAdsManager.Instance.Initialized)
                    continue;
                if (!loadAd)
                {
                    Debug.Log("WaitFirstAd Load");
                    loadAd = true;
                    if (RaccoonAdsManager.Instance.IsPreloadAd(adTypeFirst)) continue;
                    
                    RaccoonAdsManager.Instance.PreloadAdsWaitShow(adTypeFirst);
                    Debug.Log("WaitFirstAd PreloadAdsWaitShow");
                    continue;
                }
                if (!RaccoonAdsManager.Instance.AdsIsReady(adTypeFirst)) continue;
                yield return wait;
                Debug.Log($"Show First Ad {adTypeFirst.ToString()}");
                RaccoonAdsManager.Instance.ShowAdUntilLoaded(adTypeFirst, false);
                multi_speed = 10f;
                break;
            }
#endif
        }

        private void OnDestroy()
        {
#if ENABLE_RACCOON_ADMOB
            RaccoonAdsManager.Instance?.OnLoadingSceneLoaded();
            if (stopRetryLoadAfterLoading)
                RaccoonAdsManager.Instance?.StopLoadingAd(adTypeFirst);
#endif
        }
    }
}
