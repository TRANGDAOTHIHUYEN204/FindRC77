#if ENABLE_RACCOON_ADMOB
using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using Raccoon;
using Raccoon.iOS;
using RCKit.Ads.AdMob;
using RCKit.Ads.JNative;
using RCKit.Ads.Raccoon;
#if ENABLE_UNITY_AD_SDK
using RCKit.Ads.UnityAds;
#endif
#if ENABLE_TIKTOK_SDK
using RCKit.Ads.TikTok;
#endif
using UnityEngine;

namespace RCKit.Ads.Main
{
    public partial class RaccoonAdsManager : MonoBehaviour
    {
        public static RaccoonAdsManager Instance;

        [Tooltip("Enable if use all ad = native")] [SerializeField]
        private bool _useAllNative = true;

        [Tooltip("Creative Mode build for MKT only donot enable when build aab")] [SerializeField]
        private bool _creativeMode = false;

        [Tooltip("Add device to test ads")] [SerializeField]
        List<string> testDeviceIds = new List<string>();

        [Header("USE for Native banner")]
        [Tooltip("Rect transform to show banner native")] [SerializeField]
        private RectTransform _rectBannerNative;
        
        [Header("USE for Native banner collapse")]
        [Tooltip("Rect transform to show banner native collapse")] [SerializeField]
        private RectTransform _rectBannerCollapseNative;

        [Header("USE for Admob Banner(not native)")]
        [Tooltip("Banner position for admob banner")] [SerializeField]
        private AdPosition _bannerPosition;

        [Tooltip("Banner type for admob banner")] [SerializeField]
        private BANNER_TYPE _bannerType;
        
        [Tooltip("Ad type to show app open ad")] [SerializeField]
        public AD_TYPE _adTypeAppOpen = AD_TYPE.OPEN_AD_NATIVE;
#if RACCOON_MULTI_APPOPEN_AD
        public AD_TYPE _adTypeAppOpen2 = AD_TYPE.OPEN_AD_NATIVE_2;
#endif

        private float ad_scale_btn_cover = 1.8f;

        // ShowMultiAd: min show delay riêng cho các ad sau ad đầu tiên trong chuỗi (ad đầu vẫn
        // dùng AdLoadingGate.MinShowDelaySeconds mặc định).
        private const float _multiAdMinShowDelaySeconds = 1f;
        
        //REMOTE
        // Bật/tắt hiển thị 4 field dưới đây trong Inspector qua Raccoon Ads Setup Window
        // (Field Visibility section) - set/xoá scripting define symbol tương ứng. Khi tắt,
        // field biến thành const mang giá trị mặc định thay vì bị ẩn nhưng vẫn giữ giá trị cũ.
#if RACCOON_SHOW_AD_SCALE_BTN
        [Header("REMOTE CONFIG variable for native ad ")]
        [SerializeField] public float ad_scale_btn = 0.38f;
#else
        private const float ad_scale_btn = 0.38f;
#endif
        
#if RACCOON_SHOW_AD_REFRESH_TIME
        [SerializeField] public long ad_refesh_time = -1;
#else
        private const long ad_refesh_time = -1;
#endif
        
#if RACCOON_SHOW_RELOAD_ON_CLICK
        [SerializeField] private bool reload_on_click = true;
#else
        private const bool reload_on_click = true;
#endif

#if RACCOON_SHOW_CLOSE_AD_ON_CLICK
        [SerializeField] private bool close_ad_on_click = false;
#else
        private const bool close_ad_on_click = false;
#endif

        private bool _startCheckShowAppOpenAds;
        private bool _enableBanner = false;
        private bool _initializing = false;
        private bool _initialized = false;

        public bool Initialized => _initialized;

        // ShowBanner/ShowBannerCollapse gọi trước khi init xong (chưa có consent) => show sau ở InitializeDone.
        private Action _pendingBannerShow;

        // Không được Load trước khi SDK init: GMA tự init khi load và gửi request thiếu TC string
        // (user EEA/UK/CH chưa consent) => AdMob báo "Consent requirement: Low coverage".
        private bool IsReadyToRequestAds(AD_TYPE key)
        {
            if (_initialized) return true;
            Debug.LogWarning($"[RaccoonAdsManager] Chưa init xong (đang chờ consent), bỏ qua request {key}.");
            return false;
        }

        //QUEUE
        private bool _isAdBusy;

        private bool IsAdBusy
        {
            get => _isAdBusy;
            set
            {
                _isAdBusy = value;
                if (showAdOpenCoroutine != null) StopCoroutine(showAdOpenCoroutine);
                showAdOpenCoroutine = null;
                if (_isAdBusy)
                    pendingShowAdOpen = true;
                else
                    showAdOpenCoroutine = StartCoroutine(StartOffPendingShowAdOpen());
            }
        }

        private readonly Queue<(AD_TYPE type, Action action)> _pendingAdRequests = new Queue<(AD_TYPE type, Action action)>();
        
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnInitialize()
        {
#if UNITY_IPHONE
            MobileAds.SetiOSAppPauseOnBackground(true);
#endif
            RequestConfiguration requestConfiguration = new RequestConfiguration
            {
                TestDeviceIds = testDeviceIds
            };
            MobileAds.SetRequestConfiguration(requestConfiguration);
            var config = new GoogleAdsConfig();
            var consent = new GoogleUMPConsent
            {
                debugGeography = DebugGeography.EEA,
            };
            var mediation = new GoogleAdsMediation();
            manager = new AdsManager(consent)
            {
                configs = new IConfigProvider[] { config },
                mediations = new IMediationProvider[] { mediation },
            };

            RegisterAdUnits();
            GoogleAdsMediation.OnPaid += OnAdPaid;
            mediation.OnMediationInitializeCompleted += InitializeDone;
            manager.onRequestShowATT = RequestShowATT;
            manager.Initialize();
        }

        private void RequestShowATT()
        {
            // if (onRequestATT != null)
            //     UnityMainThreadDispatcher.Enqueue(onRequestATT);
            //show attracking
            if (AdsManager.IsPlatformIOS)
            {
                ATTrackingManager.ShowRequest();
            }
        }

        private void InitializeDone()
        {
            _initializing = false;
            _initialized = true;
            LoadAllAds();
            var pendingBanner = _pendingBannerShow;
            _pendingBannerShow = null;
            if (_enableBanner && pendingBanner != null)
                UnityMainThreadDispatcher.Enqueue(pendingBanner);
            UnityMainThreadDispatcher.Enqueue(onInitialized);
#if ENABLE_UNITY_AD_SDK
            // Init Unity Ads sau khi UMP gather consent xong để truyền consent sang Unity Ads.
            UnityMainThreadDispatcher.Enqueue(UnityAdsInitializer.Initialize);
#endif
#if ENABLE_TIKTOK_SDK
            // Init TikTok (disableAutoStart) rồi tự chờ UMP xong mới startTrack nếu có consent.
            UnityMainThreadDispatcher.Enqueue(TikTokInitializer.Initialize);
#endif
        }

        private void LoadAllAds()
        {
            if (!_initialized)
            {
                Debug.LogWarning("RaccoonAdsManager do not initialize");
                return;
            }

            foreach (var unitConfig in adsData.GetAdUnitConfigs)
            {
                if (!unitConfig.preload_ad) continue;
                if (_adUnits.TryGetValue(unitConfig.ad_type, out var unit))
                    PreloadUnit(unit);
            }
        }

        private void RegisterAdUnits()
        {
            foreach (var unitConfig in adsData.GetAdUnitConfigs)
            {
                var type = unitConfig.ad_type;
                var adUnitId = unitConfig.ad_id;

                // AD_TYPE is laid out in decade-wide bands: family = value/20 for inter-like types
                // (0-39), or explicit bands for reward (40-59), app-open (60-79) and banner (80-99);
                // within each family the "native" variant starts 10 above the base value.
                var isNative = (int)type / 10 % 2 == 1;
                var isBanner = (int)type is >= 80 and < 100;
                var isReward = (int)type is >= 40 and < 60;
                var isAppOpen = (int)type is >= 60 and < 80;
                var isBannerCollapsed = (int)type is >= 120 and < 140;

                object unit;
                if (isBanner)
                {
                    var bannerRect = GetRectFromTransform(_rectBannerNative);
                    if (isNative)
                    {
                        var time = ad_refesh_time <= 0 ? 15 : ad_refesh_time;
                        IAdsBanner banner = new JNativeAdsBanner(adUnitId, bannerRect, time, false)
                            .SetCollapse(false, false);
                        unit = new RaccoonAdsBanner(banner);
                    }
                    else
                    {
                        IAdsBanner banner = new GoogleAdsBanner(adUnitId, GetAdSize(), _bannerPosition, false);
                        unit = new RaccoonAdsBanner(banner);
                    }
                }
                else if (isBannerCollapsed)
                {
                    var bannerCollapseRect = GetRectFromTransform(_rectBannerCollapseNative);
                    if (isNative)
                    {
                        var time = ad_refesh_time <= 0 ? 15 : ad_refesh_time;
                        IAdsBanner bannerCollapse = new JNativeAdsBanner(adUnitId, bannerCollapseRect, time, false)
                            .SetCollapse(true, unitConfig.has_close)
                            .SetQuitButtonScale(ad_scale_btn)
                            .SetQuitButtonCoverScale(ad_scale_btn_cover);
                        unit = new RaccoonAdsBanner(bannerCollapse);
                    }
                    else
                    {
                        IAdsBanner banner = new GoogleAdsBanner(adUnitId, GetAdSize(), _bannerPosition, false);
                        unit = new RaccoonAdsBanner(banner);
                    }
                }
                else if (isNative)
                {
                    var isMultiAd = unitConfig.is_multi_ad && !string.IsNullOrEmpty(unitConfig.ad_id_2);
                    var layoutQuit = GetQuitLayoutNative(unitConfig.layout_quit_native, isMultiAd);
                    var layoutTemp = GetLayoutTemplateNative(unitConfig.layout_template_native, unitConfig.use_only_portrait);
#if RACCOON_DEBUG_LAYOUT_NATIVE
                    var native = new JNativeAdsInterstitial(adUnitId, supportMedia: unitConfig.layout_support_media,
                        quitLayout: layoutQuit, template: layoutTemp, reloadOnClick: reload_on_click);
#else
                    var native = new JNativeAdsInterstitial(adUnitId, supportMedia: true,
                        quitLayout: layoutQuit, template: layoutTemp, reloadOnClick: reload_on_click);
#endif
                    if (unitConfig.use_only_portrait)
                        native.SetUseOnlyPortrait(unitConfig.use_only_portrait);
                    if (close_ad_on_click)
                        native.SetCloseAdAfterClickAd();
                    if (isMultiAd)
                        native.SetMultiAd(unitConfig.ad_id_2);
                    var temp = new RaccoonAdsInterstitial(native);
                    if(unitConfig.preload_ad)
                        temp.SetReloadAfterShow(unitConfig.preload_ad);
                    temp.SetReloadOnFail(unitConfig.reload_on_fail);
                    unit = temp;
                }
                else if (isReward)
                {
                    IAdsRewarded rewarded = new GoogleAdsRewardedVideo(adUnitId);
                    var rewardedUnit = new RaccoonAdsRewarded(rewarded);
                    rewardedUnit.SetReloadOnFail(unitConfig.reload_on_fail);
                    unit = rewardedUnit;
                }
                else if (isAppOpen)
                {
                    unit = new GoogleAdsAppOpen(adUnitId);
                }
                else
                {
                    IAdsInterstitial interstitial = new GoogleAdsInterstitial(adUnitId);
                    var interstitialUnit = new RaccoonAdsInterstitial(interstitial);
                    interstitialUnit.SetReloadOnFail(unitConfig.reload_on_fail);
                    unit = interstitialUnit;
                }

                _adUnits[type] = unit;
                if (unitConfig.removable)
                    AddRemovableAd(unitConfig.ad_type);
            }

            HandleAdUnits();
        }

        private void HandleAdUnits()
        {
            foreach (var unit in _adUnits.Values)
            {
                switch (unit)
                {
                    case IAdsInterstitial interstitial:
                        interstitial.OnLoadSucceed += () =>
                        {
                            interstitial.SetRefreshTime(ad_refesh_time);
                            interstitial.SetQuitButtonScale(ad_scale_btn);
                            interstitial.SetQuitButtonCoverScale(ad_scale_btn_cover);
                        };
                        interstitial.OnClosed += ()=>
                        {
                            ForceHidePanelShowAd();
                            OnProcessNextQueue();
                        };
                        break;
                    case IAdsRewarded rewarded:
                        rewarded.OnLoadSucceed += () =>
                        {
                            rewarded.SetRefreshTime(ad_refesh_time);
                            rewarded.SetQuitButtonScale(ad_scale_btn);
                        };
                        rewarded.OnClosed += ()=>
                        {
                            ForceHidePanelShowAd();
                            OnProcessNextQueue();
                        };
                        break;
                    case IAppOpenAds appOpen:
                        appOpen.OnLoadSucceed += () =>
                        {
                            appOpen.SetRefreshTime(ad_refesh_time);
                            appOpen.SetQuitButtonScale(ad_scale_btn);
                        };
                        appOpen.OnClosed += ()=>
                        {
                            ForceHidePanelShowAd();
                            OnProcessNextQueue();
                        };
                        break;
                }
            }
        }

        private async void OnProcessNextQueue()
        {
            await Awaitable.MainThreadAsync();
            // Set qua property IsAdBusy (không set field _isAdBusy trực tiếp) để nó tự start
            // coroutine đếm 3s cho pendingShowAdOpen - không tự start thêm ở đây, tránh trường hợp
            // ProcessNextPendingAdRequest() show ngay 1 ad khác (set IsAdBusy = true lại) mà vẫn còn
            // 1 timer 3s cũ chạy tiếp và tự tắt pendingShowAdOpen dù ad đó vẫn đang hiện.
            IsAdBusy = false;
            ProcessNextPendingAdRequest();
        }
            
        
        private void ForceHidePanelShowAd()
        {
            AdLoadingGate.Instance?.ResumeGame(true);
            ShowBannerCheckEnable();
        }

        private void OnAdPaid(AdValue adValue)
        {
            GameAppsFlyer.SendAdRevenue(adValue.Value, adValue.CurrencyCode);
        }

        private bool CanPossiblyShowAds()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        // Bật/tắt qua scripting define symbol RACCOON_DEBUG_LAYOUT_NATIVE trong Raccoon Ads Setup Window
        // (Field Visibility section). Bật: luôn ép layout/template Native về giá trị mặc định
        // (bỏ qua enum truyền vào). Tắt: dùng đúng mapping theo từng enum bên dưới.
        private string GetQuitLayoutNative(NATIVE_LAYOUT_QUIT_INTER nativeLayoutQuitInter, bool isMultiAd = false)
        {
            // Multi ad bắt buộc dùng layout inter2, bất kể layout_quit_native cấu hình gì và
            // bất kể symbol debug bật/tắt.
            if (isMultiAd) return "inter2";

#if RACCOON_DEBUG_LAYOUT_NATIVE
            return nativeLayoutQuitInter switch
            {
                NATIVE_LAYOUT_QUIT_INTER.INTER_1 => "inter",
                NATIVE_LAYOUT_QUIT_INTER.INTER_2 => "inter2",
                _ => "inter3"
            };
#else
            // chuyển thành mặc đinh là inter 3
            return "inter3";
#endif
        }

        private string GetLayoutTemplateNative(NATIVE_TEMPLATE_TYPE nativeLayoutTemplate, bool useOnlyPortrait)
        {
            if (useOnlyPortrait)
            {
                return "portrait";
            }
#if RACCOON_DEBUG_LAYOUT_NATIVE
            return nativeLayoutTemplate switch
            {
                NATIVE_TEMPLATE_TYPE.FULLSCREEN_ORIGIN => "fullscreen",
                NATIVE_TEMPLATE_TYPE.FULLSCREEN_MEDIA_2 => "landscape_2",
                NATIVE_TEMPLATE_TYPE.FULLSCREEN_FAN => "landscape_3",
                NATIVE_TEMPLATE_TYPE.FULLSCREEN_BANNER => "landscape_4",
                _ => ""
            };
#else
            //tự detect theo man hình khi empty
            return "";
#endif
        }

        #region QUEUE

        private void ProcessNextPendingAdRequest()
        {
            if (IsAdBusy) return;
            if (_pendingAdRequests.Count == 0) return;
            var next = _pendingAdRequests.Dequeue();
            next.action.Invoke();
        }

        #endregion

        #region FOR BANNER

        public void ShowBannerCheckEnable()
        {
            if (_creativeMode) return;
            if (!_enableBanner) return;
            foreach (var kv in _adUnits)
            {
                if (!IsBannerFamily(kv.Key) && !IsBannerCollapseFamily(kv.Key)) continue;
                if (kv.Value is not RaccoonAdsBanner ad) continue;
                if (ad.HideByOtherAd)
                    ad?.Show();
            }

            Debug.Log("ShowBannerCheckEnable Done");
        }

        public void HideBannerNoneSetEnable()
        {
            foreach (var kv in _adUnits)
            {
                if (!IsBannerFamily(kv.Key) && !IsBannerCollapseFamily(kv.Key)) continue;
                if (kv.Value is not RaccoonAdsBanner ad) continue;
                if (ad.IsShowed())
                    ad.HideByOtherAd = true;
                ad?.Hide();
            }
        }


        private static Rect GetRectFromTransform(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return new Rect(0, Screen.height - 100, Screen.width, 100);

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            var x = corners[0].x;
            var y = corners[0].y;
            var width = corners[2].x - corners[0].x;
            var height = corners[2].y - corners[0].y;
            return new Rect(x, y, width, height);
        }

        private AdSize GetAdSize()
        {
            return _bannerType switch
            {
                BANNER_TYPE.BANNER => AdSize.Banner,
                BANNER_TYPE.IABBANNER => AdSize.IABBanner,
                BANNER_TYPE.MEDIUM_RECTANGLE => AdSize.MediumRectangle,
                BANNER_TYPE.LEADERBOARD => AdSize.Leaderboard,
                _ => AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth)
            };
        }

        #endregion
        
        #region Application Pause

        private bool _stopCheckingPausedApp = false;
        private bool pendingShowAdOpen;
        private Coroutine showAdOpenCoroutine, stopCheckingAdOpenCoroutine;

        private IEnumerator StartDelayedOnCheckingPauseApp()
        {
            yield return new WaitForSeconds(3f);
            _stopCheckingPausedApp = false;
        }

        private IEnumerator StartOffPendingShowAdOpen()
        {
            // Cố ý dùng scaled WaitForSeconds (không phải Realtime): trong lúc app đang bị
            // pause hoặc AdLoadingGate đang show/load 1 ad khác, Time.timeScale = 0 nên đếm
            // này sẽ treo vô thời hạn, giữ pendingShowAdOpen = true suốt thời gian đó -> chặn
            // App Open show đè lên. Countdown chỉ thực sự chạy (và hết sau 3s) khi timeScale
            // được trả lại bình thường, tức ad/pause đã kết thúc.
            yield return new WaitForSeconds(3f);
            pendingShowAdOpen = false;
            showAdOpenCoroutine = null;
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                
            }
            else if (_initialized)
            {
                TryShowAppOpenOnForeground();
                // Debug.Log("[ShowAppOpen] OnApplicationPause Done");
            }
        }

        // Chặn việc 2 lượt resume liên tiếp (VD OnApplicationPause(false) bắn đúp) cùng start
        // 2 coroutine ShowAppOpenAd song song trong lúc chưa cái nào kịp gọi ShowAd() - nếu không
        // sẽ có 1 request App Open bị dồn vào _pendingAdRequests và show lại ngay sau cái đầu.
        private bool _isWaitingToShowAppOpen;

        private void TryShowAppOpenOnForeground()
        {
            if (!_startCheckShowAppOpenAds || _stopCheckingPausedApp) return;
        
            if (AdLoadingGate.Instance != null
                && AdLoadingGate.Instance.IsAdShowing)
                return;

            if (pendingShowAdOpen) return;
            if (_isWaitingToShowAppOpen) return;

            Debug.Log("[ShowAppOpen] TryShowAppOpenOnForeground Done");
            _isWaitingToShowAppOpen = true;
            StartCoroutine(ShowAppOpenAd());
            // ShowAd(_adTypeAppOpen, null);
        }

        private IEnumerator ShowAppOpenAd()
        {
            // Debug.Log("ShowAppOpenAd 1");
            // Cố ý dùng scaled WaitForSeconds: nếu Time.timeScale về 0 (ad khác chen vào loading)
            // trong lúc đang chờ, delay này treo lại theo, giữ _isWaitingToShowAppOpen = true
            // suốt thời gian đó -> không bắn ShowAd App Open đè lên ad đang show/load.
            yield return new WaitForSeconds(0.2f);
            // Debug.Log("ShowAppOpenAd 2");
            _isWaitingToShowAppOpen = false;
#if RACCOON_MULTI_APPOPEN_AD
            ShowMultiAd(OnClosedOpenAd, true, _adTypeAppOpen, _adTypeAppOpen2);
#else 
            ShowAd(_adTypeAppOpen, OnClosedOpenAd);
#endif
        }
        #endregion

    }
}
#endif