#if ENABLE_RACCOON_ADMOB
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RCKit.Ads.Raccoon;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCKit.Ads.Main
{
    [RequireComponent(typeof(AdLoadingGate))]
    public partial class RaccoonAdsManager : MonoBehaviour
    {
        private readonly Dictionary<AD_TYPE, object> _adUnits = new Dictionary<AD_TYPE, object>();

        [SerializeField] private AdsScriptableObj adsData;

        private AdsManager manager;
        
        private Action onInitialized;

        public Action<bool> OnClosedOpenAd;

#if UNITY_EDITOR
        private const string ResourcesFolderPath = "Assets/RaccoonAds/Resources";
        private const string AdsDataAssetPath = ResourcesFolderPath + "/RaccoonAdUnit.asset";

        private void Reset()
        {
            adsData = LoadOrCreateAdsData();
        }

        private static AdsScriptableObj LoadOrCreateAdsData()
        {
            var data = AssetDatabase.LoadAssetAtPath<AdsScriptableObj>(AdsDataAssetPath);
            if (data != null)
                return data;

            EnsureFolderExists(ResourcesFolderPath);

            data = ScriptableObject.CreateInstance<AdsScriptableObj>();
            AssetDatabase.CreateAsset(data, AdsDataAssetPath);
            AssetDatabase.SaveAssets();
            return data;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            var parts = folderPath.Split('/');
            var currentPath = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                currentPath = nextPath;
            }
        }
#endif

        public void Initialize(Action onInitialized)
        {
            if(_initializing || _initialized) return;
            _initializing = true;
            this.onInitialized = onInitialized;
            OnInitialize();
        }

        #region AD

        // minShowDelayOverride: dùng cho ShowMultiAd - các ad sau ad đầu tiên trong chuỗi chỉ cần
        // hiện loading panel tối thiểu 1s (thay vì mặc định MinShowDelaySeconds = 5s) vì lúc đó
        // ad trước đã show/đóng xong nên không còn race Show/Hide của native overlay.
        public void ShowAd(AD_TYPE key, Action<bool> onComplete = null, float? minShowDelayOverride = null)
        {
            if (_creativeMode)
            {
                onComplete?.Invoke(true);
                return;
            }

            if (!_adUnits.TryGetValue(key, out var unit))
            {
                Debug.LogWarning($"[GameAds] Không tìm thấy ad unit key: {key}");
                onComplete?.Invoke(false);
                return;
            }

            if (manager.AdsRemoved && manager.removables.Contains(unit))
            {
                onComplete?.Invoke(true);
                return;
            }

            if (!IsReadyToRequestAds(key))
            {
                onComplete?.Invoke(false);
                return;
            }

            //Hide banner
            HideBannerNoneSetEnable();

            if (IsAdBusy)
            {
                if (_pendingAdRequests.Any(r => r.type == key))
                {
                    Debug.Log($"[GameAds] Đã có request {key} trong queue, bỏ qua request trùng.");
                    return;
                }

                Debug.Log($"[GameAds] Ad khác đang loading/show, queue lại request: {key}");
                _pendingAdRequests.Enqueue((key, () => ShowAd(key, onComplete, minShowDelayOverride)));
                return;
            }

            IsAdBusy = true;
            var isNative = (int)key / 10 % 2 == 1;

            if (isNative)
            {
                var ads = unit as RaccoonAdsInterstitial;
                AdLoadingGate.Instance?.RequestShowAd(
                    canPossiblyLoad: CanPossiblyShowAds,
                    isReady: () => ads.IsReady(),
                    requestLoad: ads.Load,
                    showAd: () =>
                    {
                        UnityMainThreadDispatcher.Enqueue(() =>
                        {
                            Debug.Log($"[GameAds] Ad showing: {key}");
                            ads.Show(onComplete);
                        });
                    },
                    onResult: success =>
                    {
                        if (!success)
                        {
                            ShowBannerCheckEnable();
                            onComplete?.Invoke(false);
                            OnProcessNextQueue();
                        }
                    },
                    minShowDelayOverride: minShowDelayOverride);
            }
            else
            {
                //cast unit to RaccoonAdsInterstitial or  RaccoonAdsRewarded or GoogleAdsAppOpen
                switch (unit)
                {
                    case IAdsRewarded rewarded:
                        AdLoadingGate.Instance?.RequestShowAd(
                            canPossiblyLoad: CanPossiblyShowAds,
                            isReady: rewarded.IsReady,
                            requestLoad: rewarded.Load,
                            showAd: () =>
                            {
                                UnityMainThreadDispatcher.Enqueue(() =>
                                {
                                    Debug.Log($"[GameAds] Ad showing: {key}");
                                    rewarded.Show(
                                        onRewarded: () => onComplete?.Invoke(true),
                                        onFailed: () => onComplete?.Invoke(false));
                                });
                            },
                            onResult: success =>
                            {
                                if (!success)
                                {
                                    ShowBannerCheckEnable();
                                    onComplete?.Invoke(false);
                                    OnProcessNextQueue();
                                }
                            },
                            minShowDelayOverride: minShowDelayOverride);
                        break;

                    case IAppOpenAds appOpen:
                        AdLoadingGate.Instance?.RequestShowAd(
                            canPossiblyLoad: CanPossiblyShowAds,
                            isReady: appOpen.IsReady,
                            requestLoad: appOpen.Load,
                            showAd: () =>
                            {
                                UnityMainThreadDispatcher.Enqueue(() =>
                                {
                                    Debug.Log($"[GameAds] Ad showing: {key}");
                                    appOpen.Show(() => onComplete?.Invoke(true));
                                });
                            },
                            onResult: success =>
                            {
                                if (!success)
                                {
                                    ShowBannerCheckEnable();
                                    onComplete?.Invoke(false);
                                    OnProcessNextQueue();
                                }
                            },
                            minShowDelayOverride: minShowDelayOverride);
                        break;

                    case IAdsInterstitial interstitial:
                        AdLoadingGate.Instance?.RequestShowAd(
                            canPossiblyLoad: CanPossiblyShowAds,
                            isReady: interstitial.IsReady,
                            requestLoad: interstitial.Load,
                            showAd: () =>
                            {
                                UnityMainThreadDispatcher.Enqueue(() =>
                                {
                                    Debug.Log($"[GameAds] Ad showing: {key}");
                                    interstitial.Show(onComplete);
                                });
                            },
                            onResult: success =>
                            {
                                if (!success)
                                {
                                    ShowBannerCheckEnable();
                                    onComplete?.Invoke(false);
                                    OnProcessNextQueue();
                                }
                            },
                            minShowDelayOverride: minShowDelayOverride);
                        break;

                    default:
                        Debug.LogWarning($"[GameAds] Ad unit key {key} không hỗ trợ show: {unit?.GetType().Name}");
                        ShowBannerCheckEnable();
                        onComplete?.Invoke(false);
                        OnProcessNextQueue();
                        break;
                }
            }
            
        }

        public void ShowMultiAd(Action<bool> onComplete = null, bool onlyLastAdCallback = true, params AD_TYPE[] keys)
        {
            if (_creativeMode)
            {
                onComplete?.Invoke(true);
                return;
            }

            if (keys == null || keys.Length == 0)
            {
                onComplete?.Invoke(false);
                return;
            }

            // Preload các ad kế tiếp (bỏ qua ad đầu tiên) song song ngay từ đầu, để chúng đã
            // sẵn sàng (cache) khi tới lượt show, tránh việc loading gate phải ẩn/hiện lại giữa 2 ad.
            // Ad đầu tiên KHÔNG preload ở đây vì ShowAd() bên dưới sẽ tự request load nó ngay lập tức
            // (qua AdLoadingGate) - preload thêm ở đây chỉ khiến Load() bị gọi trùng một cách vô ích.
            for (var i = 1; i < keys.Length; i++)
            {
                if (_adUnits.TryGetValue(keys[i], out var unit))
                    PreloadUnit(unit);
            }

            // Show tuần tự đúng thứ tự keys: ad đầu tiên show luôn, các ad sau tự vào
            // _pendingAdRequests (do _isAdBusy) và được show kế tiếp khi ad trước đó đóng
            // (ProcessNextPendingAdRequest) - nếu đã preload xong thì show ngay, không cần loading gate.
            // onlyLastAdCallback = true: chỉ ad cuối cùng nhận onComplete, các ad trước đó không có
            // callback (null) để tránh onComplete bị gọi lặp lại nhiều lần cho cùng 1 chuỗi multi-ad.
            // Ad đầu tiên (i == 0) giữ nguyên MinShowDelaySeconds mặc định (chưa ad nào show trước
            // đó, cần đủ 5s tránh race Show/Hide của native overlay). Các ad sau chỉ cần tối thiểu
            // 1s hiện loading panel vì lúc đó ad trước đã show/đóng xong.
            var lastIndex = keys.Length - 1;
            for (var i = 0; i < keys.Length; i++)
            {
                var callback = !onlyLastAdCallback || i == lastIndex ? onComplete : null;
                var minShowDelayOverride = i == 0 ? (float?)null : _multiAdMinShowDelaySeconds;
                ShowAd(keys[i], callback, minShowDelayOverride);
            }
        }

        private void PreloadUnit(object unit, Action<bool> onComplete = null)
        {
            // Chưa init (chưa có consent) thì không Load: GMA tự init khi load và gửi request thiếu TC string.
            if (!_initialized)
            {
                onComplete?.Invoke(false);
                return;
            }

            if (_useAllNative)
            {
                if (unit is RaccoonAdsInterstitial interstitial)
                {
                    HookLoadResult(interstitial, onComplete);
                    interstitial.Load();
                    return;
                }
            }

            switch (unit)
            {
                case IAdsRewarded rewarded:
                    HookLoadResult(rewarded, onComplete);
                    rewarded.Load();
                    break;
                case IAppOpenAds appOpen:
                    HookLoadResult(appOpen, onComplete);
                    appOpen.Load();
                    break;
                case IAdsInterstitial interstitial:
                    HookLoadResult(interstitial, onComplete);
                    interstitial.Load();
                    break;
                default:
                    onComplete?.Invoke(false);
                    break;
            }
        }

        // Gắn 1 lần vào OnLoadSucceed/OnLoadFailed của lượt Load() sắp gọi, tự gỡ ngay sau khi
        // 1 trong 2 event bắn để không rò rỉ subscription cho các lượt load tiếp theo.
        private void HookLoadResult(IAdsInterstitial ad, Action<bool> onComplete)
        {
            if (onComplete == null) return;
            Action onSucceed = null;
            Action onFailed = null;
            onSucceed = () => { ad.OnLoadSucceed -= onSucceed; ad.OnLoadFailed -= onFailed; onComplete(true); };
            onFailed = () => { ad.OnLoadSucceed -= onSucceed; ad.OnLoadFailed -= onFailed; onComplete(false); };
            ad.OnLoadSucceed += onSucceed;
            ad.OnLoadFailed += onFailed;
        }

        private void HookLoadResult(IAdsRewarded ad, Action<bool> onComplete)
        {
            if (onComplete == null) return;
            Action onSucceed = null;
            Action onFailed = null;
            onSucceed = () => { ad.OnLoadSucceed -= onSucceed; ad.OnLoadFailed -= onFailed; onComplete(true); };
            onFailed = () => { ad.OnLoadSucceed -= onSucceed; ad.OnLoadFailed -= onFailed; onComplete(false); };
            ad.OnLoadSucceed += onSucceed;
            ad.OnLoadFailed += onFailed;
        }

        private void HookLoadResult(IAppOpenAds ad, Action<bool> onComplete)
        {
            if (onComplete == null) return;
            Action onSucceed = null;
            Action onFailed = null;
            onSucceed = () => { ad.OnLoadSucceed -= onSucceed; ad.OnLoadFailed -= onFailed; onComplete(true); };
            onFailed = () => { ad.OnLoadSucceed -= onSucceed; ad.OnLoadFailed -= onFailed; onComplete(false); };
            ad.OnLoadSucceed += onSucceed;
            ad.OnLoadFailed += onFailed;
        }
        
        // ShowAdUntilLoaded bypass AdLoadingGate nên không có cushion trước show -> dính race
        // Show->Close ngay lập tức (xem MinShowDelaySeconds trong AdLoadingGate). Dùng chung
        // delay đó ở đây để mọi đường show (qua gate hay không) đều an toàn như nhau.
        private IEnumerator CoShowAfterDelay(Action showAction)
        {
            yield return new WaitForSecondsRealtime(AdLoadingGate.MinShowDelaySeconds);
            UnityMainThreadDispatcher.Enqueue(showAction);
        }

        /// <summary>
        /// show ad without using gate
        /// </summary>
        /// <param name="key"></param>
        /// <param name="hideBanner"></param>
        /// <param name="onComplete"></param>
        public void ShowAdUntilLoaded(AD_TYPE key, bool hideBanner, Action<bool> onComplete = null)
        {
            Debug.Log("ShowAdUntilLoaded called: " + key);
            if (_creativeMode)
            {
                onComplete?.Invoke(true);
                return;
            }

            if (!_adUnits.TryGetValue(key, out var unit))
            {
                Debug.LogWarning($"[RaccoonAdsManager] Không tìm thấy ad unit key: {key}");
                onComplete?.Invoke(false);
                return;
            }

            if (!CanPossiblyShowAds())
            {
                onComplete?.Invoke(false);
                return;
            }
            
            if (manager.AdsRemoved && manager.removables.Contains(unit))
            {
                onComplete?.Invoke(true);
                return;
            }

            if (!IsReadyToRequestAds(key))
            {
                onComplete?.Invoke(false);
                return;
            }

            if (IsAdBusy)
            {
                if (_pendingAdRequests.Any(r => r.type == key))
                {
                    Debug.Log($"[GameAds] Đã có request {key} trong queue, bỏ qua request trùng.");
                    return;
                }

                Debug.Log($"[GameAds] Ad khác đang loading/show, queue lại request: {key}");
                _pendingAdRequests.Enqueue((key, () => ShowAdUntilLoaded(key, hideBanner, onComplete)));
                return;
            }
            
            var isNative = (int)key / 10 % 2 == 1;
            
            if (key is AD_TYPE.BANNER_COLLAPSE or AD_TYPE.BANNER_COLLAPSE_NATIVE)
            {
                var ads = unit as RaccoonAdsBanner;
                if (ads != null && ads.IsReady())
                {
                    IsAdBusy = true;
                    //show luon
                    if(hideBanner)
                        HideBannerNoneSetEnable();
                    StartCoroutine(CoShowAfterDelay(() =>
                    {
                        ads.Show();
                        ShowBannerCheckEnable();
                        onComplete?.Invoke(true);
                        OnProcessNextQueue();
                    }));
                    return;
                }

                Action onLoaded = null;
                onLoaded = () =>
                {
                    if (ads != null) ads.OnLoadSucceed -= onLoaded;
                    ShowAdUntilLoaded(key, hideBanner, onComplete);
                };
                if (ads != null) ads.OnLoadSucceed += onLoaded;
                ads?.Load();
            }
            else if (isNative)
            {
                var ads = unit as RaccoonAdsInterstitial;
                if (ads != null && ads.IsReady())
                {
                    IsAdBusy = true;
                    //show luon
                    if(hideBanner)
                        HideBannerNoneSetEnable();
                    StartCoroutine(CoShowAfterDelay(() =>
                    {
                        ads.Show(onComplete);
                    }));
                    return;
                }

                Action onLoaded = null;
                onLoaded = () =>
                {
                    if (ads != null) ads.OnLoadSucceed -= onLoaded;
                    ShowAdUntilLoaded(key, hideBanner, onComplete);
                };
                if (ads != null) ads.OnLoadSucceed += onLoaded;
                ads?.Load();
            }
            else
            {
                //cast unit to RaccoonAdsInterstitial or  RaccoonAdsRewarded or GoogleAdsAppOpen
                Action onLoaded = null;
                switch (unit)
                {
                    case IAdsRewarded rewarded:
                        if (rewarded.IsReady())
                        {
                            IsAdBusy = true;
                            //show luon
                            if(hideBanner)
                                HideBannerNoneSetEnable();
                            StartCoroutine(CoShowAfterDelay(() =>
                            {
                                rewarded.Show(
                                    onRewarded: () => onComplete?.Invoke(true),
                                    onFailed: () => onComplete?.Invoke(false));
                            }));
                            return;
                        }

                        onLoaded = () =>
                        {
                            rewarded.OnLoadSucceed -= onLoaded;
                            ShowAdUntilLoaded(key, hideBanner, onComplete);
                        };
                        rewarded.OnLoadSucceed += onLoaded;
                        rewarded?.Load();
                        break;

                    case IAppOpenAds appOpen:
                        if (appOpen.IsReady())
                        {
                            IsAdBusy = true;
                            //show luon
                            if(hideBanner)
                                HideBannerNoneSetEnable();
                            StartCoroutine(CoShowAfterDelay(() =>
                            {
                                appOpen.Show(() => onComplete?.Invoke(true));
                            }));
                            return;
                        }

                        onLoaded = () =>
                        {
                            appOpen.OnLoadSucceed -= onLoaded;
                            ShowAdUntilLoaded(key, hideBanner, onComplete);
                        };
                        appOpen.OnLoadSucceed += onLoaded;
                        appOpen.Load();
                        break;

                    case IAdsInterstitial interstitial:
                        if (interstitial.IsReady())
                        {
                            IsAdBusy = true;
                            //show luon
                            if(hideBanner)
                                HideBannerNoneSetEnable();
                            StartCoroutine(CoShowAfterDelay(() =>
                            {
                                interstitial.Show(onComplete);
                            }));
                            return;
                        }

                        onLoaded = () =>
                        {
                            interstitial.OnLoadSucceed -= onLoaded;
                            ShowAdUntilLoaded(key, hideBanner, onComplete);
                        };
                        interstitial.OnLoadSucceed += onLoaded;
                        interstitial.Load();
                        break;

                    default:
                        Debug.LogWarning($"[GameAds] Ad unit key {key} không hỗ trợ show: {unit?.GetType().Name}");
                        ShowBannerCheckEnable();
                        onComplete?.Invoke(false);
                        OnProcessNextQueue();
                        break;
                }
            }
        }

        /// <summary>
        /// Hủy các request show/load đang chờ (queue pendingAd) của 1 loại ad cụ thể.
        /// Nếu ad này đang load dở (in-flight): để nó chạy tiếp, chỉ chặn reload-on-fail cho lượt
        /// fail kế tiếp. Nếu nó load thành công thì không đụng tới, cứ để cache sẵn sàng.
        /// </summary>
        public void StopLoadingAd(AD_TYPE adType)
        {
            var count = _pendingAdRequests.Count;
            for (var i = 0; i < count; i++)
            {
                var request = _pendingAdRequests.Dequeue();
                if (request.type != adType)
                    _pendingAdRequests.Enqueue(request);
            }

            if (!_adUnits.TryGetValue(adType, out var unit)) return;
            switch (unit)
            {
                case RaccoonAdsInterstitial interstitial:
                    interstitial.CancelReloadOnFail();
                    break;
                case RaccoonAdsRewarded rewarded:
                    rewarded.CancelReloadOnFail();
                    break;
            }
        }

        #endregion

        #region HELPERS

        public void SetStopTrackingPaused()
        {
            _stopCheckingPausedApp = true;
            if (stopCheckingAdOpenCoroutine != null)
                StopCoroutine(stopCheckingAdOpenCoroutine);
            stopCheckingAdOpenCoroutine = StartCoroutine(StartDelayedOnCheckingPauseApp());
        }

        public void PreloadAdsWaitShow(AD_TYPE type, Action<bool> onComplete = null)
        {
            if (_adUnits.TryGetValue(type, out var unit))
                PreloadUnit(unit, onComplete);
            else
                onComplete?.Invoke(false);
        }

        // Unit có preload_ad = true đã được LoadAllAds() load sau khi init xong.
        public bool IsPreloadAd(AD_TYPE type)
        {
            return adsData.GetAdUnitConfigs.Any(c => c.ad_type == type && c.preload_ad);
        }

        public bool AdsIsReady(AD_TYPE type)
        {
            if (!_adUnits.TryGetValue(type, out var unit))
                return false;

            if (_useAllNative)
                return (unit as RaccoonAdsInterstitial)?.IsReady() ?? false;

            return unit switch
            {
                IAdsRewarded rewarded => rewarded.IsReady(),
                IAppOpenAds appOpen => appOpen.IsReady(),
                IAdsInterstitial interstitial => interstitial.IsReady(),
                _ => false
            };
        }
        
        public void OnLoadingSceneLoaded()
        {
            _startCheckShowAppOpenAds = true;
        }

        public bool IsAdsRemoved => manager.AdsRemoved;
        
        public void SetAdsRemoved()
        {
            manager.AdsRemoved = true;
        }
        
        // Cùng convention với AdsScriptableObj: mỗi family AD_TYPE chiếm 1 dải 10-30 giá trị liên tục.
        private static bool IsBannerFamily(AD_TYPE type)
        {
            var value = (int)type;
            return value is >= 80 and < 100;
        }

        private static bool IsBannerCollapseFamily(AD_TYPE type)
        {
            var value = (int)type;
            return value is >= 120 and < 140;
        }

        public void HideAllBanner()
        {
            _enableBanner = false;
            _pendingBannerShow = null;
            foreach (var kv in _adUnits)
            {
                if (!IsBannerFamily(kv.Key) && !IsBannerCollapseFamily(kv.Key)) continue;
                var ad = kv.Value as RaccoonAdsBanner;
                ad?.Hide();
            }
        }
        
        public void HideBanner(AD_TYPE bannerType)
        {
            _enableBanner = false;
            _pendingBannerShow = null;
            if (!_adUnits.TryGetValue(bannerType, out var unit))
                return;
            var banner = unit as RaccoonAdsBanner;
            banner?.Hide();
        }
        
        public void ShowBanner(AD_TYPE bannerType)
        {
            if (!IsBannerFamily(bannerType))
            {
                Debug.LogWarning($"[RaccoonAdsManager] ShowBanner chỉ dùng cho BANNER/BANNER_2/BANNER_NATIVE/BANNER_NATIVE_2, không hỗ trợ {bannerType}");
                return;
            }
            _enableBanner = true;
            if (!_initialized)
            {
                // Nhớ lại, InitializeDone sẽ show khi SDK init xong (sau khi có consent).
                _pendingBannerShow = () => ShowBanner(bannerType);
                return;
            }
            if (!_adUnits.TryGetValue(bannerType, out var unit))
                return;
            if (unit is not RaccoonAdsBanner banner) return;
            if (banner.IsReady())
            {
                banner.Show();
                return;
            }
            banner.SetRefreshTime(ad_refesh_time);
            banner.OnLoadSucceed -= OnBannerLoadSucceedTryShow;
            banner.OnLoadSucceed += OnBannerLoadSucceedTryShow;
            banner.Load();
        }
        
        public void ShowBannerCollapse(AD_TYPE bannerType, Action onComplete = null)
        {
            if (!IsBannerCollapseFamily(bannerType))
            {
                Debug.LogWarning($"[RaccoonAdsManager] ShowBannerCollapse chỉ dùng cho BANNER_COLLAPSE/BANNER_COLLAPSE_NATIVE*, không hỗ trợ {bannerType}");
                return;
            }
            _enableBanner = true;
            if (!_initialized)
            {
                _pendingBannerShow = () => ShowBannerCollapse(bannerType, onComplete);
                return;
            }
            if (!_adUnits.TryGetValue(bannerType, out var unit))
                return;
            if (unit is not RaccoonAdsBanner banner) return;
            if (banner.IsReady())
            {
                banner.Show();
                return;
            }
            banner.SetRefreshTime(ad_refesh_time);
            banner.OnLoadSucceed -= OnBannerLoadSucceedTryShow;
            banner.OnLoadSucceed += OnBannerLoadSucceedTryShow;
            banner.ClearOnHiddenEvent();
            banner.OnHidden += ()=>
            {
                _enableBanner = false;
                onComplete?.Invoke();
            };
            banner.Load();
        }

        // Banner load xong: nếu đang có ad khác (inter/rewarded/appOpen) load hoặc hiện dở
        // (_isAdBusy), KHÔNG show ngay để tránh đè lên ad đó - đợi ad đó đóng, OnClosed sẽ tự
        // gọi ForceHidePanelShowAd() -> ShowBannerCheckEnable() để show banner lúc đó.
        // Không có ad nào bận thì show luôn.
        private void OnBannerLoadSucceedTryShow()
        {
            if (IsAdBusy) return;
            ShowBannerCheckEnable();
        }

        ///use for change banner native theme
        public void SetBannerNativeTheme(AD_TYPE bannerType, string theme)
        {
            if (!_adUnits.TryGetValue(bannerType, out var unit)) return;
            var banner = unit as RaccoonAdsBanner;
            banner?.SetBannerTheme(theme);
        }

        public void AddRemovableAd(AD_TYPE type)
        {
            if (!_adUnits.TryGetValue(type, out var unit) || !(unit is IAdsRemovable adUnit))
                return;

            var removables = manager.removables;
            var index = removables?.Length ?? 0;
            Array.Resize(ref removables, index + 1);
            removables[index] = adUnit;
            manager.removables = removables;
        }
        
        public void AddRemovableAd(params AD_TYPE[] type)
        {
            foreach (var t in type)
            {
                AddRemovableAd(t);
            }
        }

        #endregion
    }
}
#endif