using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace JKit.Monetize.Ads
{
    public class NativeOverlay
    {
        private INativeOverlay _client;
        public event Action OnImpression;
        public event Action OnClicked;
        public event Action OnClosed;
        public event Action<AdValue> OnAdPaid;
        
        private bool isPauseOnShow = false;
        private float _timeScale = 1;

        private static float _cachedPaddingX = -1;
        private static float _cachedPaddingY = -1;

        private NativeOverlay(INativeOverlay client)
        {
            _client = client;

            _client.OnImpression += async () =>
            {
                await Awaitable.MainThreadAsync();
                OnImpression?.Invoke();
            };

            _client.OnClicked += async () =>
            {
                await Awaitable.MainThreadAsync();
                OnClicked?.Invoke();
            };

            _client.OnClosed += async () =>
            {
                if (isPauseOnShow)
                {
                    Time.timeScale = _timeScale;
                    AudioListener.pause = false;
                }
                await Awaitable.MainThreadAsync();
                OnClosed?.Invoke();
            };

            _client.OnAdPaid += async (adValue) =>
            {
                await Awaitable.MainThreadAsync();
                OnAdPaid?.Invoke(adValue);
            };

            AdsLifecycle.AdsOnApplicationPause += _client.OnApplicationPause;
        }
        
        public NativeOverlay SetPauseOnShow(bool isPauseOnShow)
        {
            this.isPauseOnShow = isPauseOnShow;
            return this;
        }

        /// <summary>
        /// use for native full
        /// </summary>
        /// <param name="adUnitId"></param>
        /// <param name="refresh"></param>
        /// <param name="callback"></param>
        public static void Load(string adUnitId, long refresh, Action<NativeOverlay, LoadAdError> callback)
        {
            var padding = GetPadding();
            var builder = new NativeOptions.Builder()
                .SetForceAdPortrait(true)
                .SetOptionFullScreen()
                .Quit(NativeOptions.INTER_QUIT_3, padding.y, padding.x)
                .Refresh(refresh)
                .SetPauseOnShow(true)
                .Build(adUnitId);
            Load(builder, callback);
        }

        /// <summary>
        /// use for native full
        /// </summary>
        /// <param name="adUnitId"></param>
        /// <param name="refresh"></param>
        /// <param name="timeClose"></param>
        /// <param name="supportMedia"></param>
        /// <param name="layoutQuit"></param>
        /// <param name="callback"></param>
        public static void Load(string adUnitId, long refresh, bool supportMedia, string layoutQuit, string template,
            Action<NativeOverlay, LoadAdError> callback, int timeClose = 3, bool reloadOnClick = true)
        {
            var padding = GetPadding();
            var builder = new NativeOptions.Builder()
                .SetOptionFullScreen(template)
                .Quit(layoutQuit, padding.y, padding.x)
                .Refresh(refresh)
                .SetPauseOnShow(true)
                .SetTimeClose(timeClose)
                .SetReloadOnClick(reloadOnClick)
                .Build(adUnitId);
            Load(builder, callback);
        }

        public static void Load(NativeOptions options, Action<NativeOverlay, LoadAdError> callback)
        {
            var client = options.GetClient();
            if (client == null) return;
            var isPauseOnShow = options.IsPauseOnShow;
            client.OnLoadSucceed += () =>
            {
                var adNative = new NativeOverlay(client);
                adNative.SetPauseOnShow(isPauseOnShow);
                callback(adNative, null);
            };
            client.OnLoadFailed += error =>
            {
                // Không có NativeOverlay nào được trả về cho caller khi fail → phải tự destroy
                // client ở đây, nếu không native instance (views, handler retry/reload) bị rò rỉ.
                client.Destroy();
                callback(null, error);
            };
            client.Load();
            Debug.Log("Unity LoadAd");
        }

        public void Show()
        {
            if (isPauseOnShow)
            { 
                _timeScale = Time.timeScale;
                Time.timeScale = 0;
                AudioListener.pause = true;
            }

            _client?.Show();
        }
        public void Hide() => _client?.Hide();

        public void Destroy()
        {
            if (_client != null)
            {
                AdsLifecycle.AdsOnApplicationPause -= _client.OnApplicationPause;
                _client.Destroy();
                _client = null;
            }
        }

        public void SetScaleButtonQuit(float scale)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (_client != null)
            {
                scale = Mathf.Clamp(scale, 0.1f, 1f);
                _client.setQuitButtonScale(scale);
            }
#endif
        }
        
        public void SetScaleButtonQuitCover(float scale)
        {
#if UNITY_ANDROID
            if (_client != null)
            {
                scale = Mathf.Clamp(scale, 0.5f, 2f);
                _client.setQuitButtonCoverScale(scale);
            }
#endif
        }
        
        /// use for banner only, default is dark theme if not set
        /// NativeOptions.BANNER_THEME_LIGHT / BANNER_THEME_DARK
        public void SetBannerTheme(string theme)
        {
#if UNITY_ANDROID
            if (_client != null)
            {
                _client.setBannerTheme(theme);
            }
#endif
        }
        
        public void SetRefreshTime(long seconds)
        {
#if UNITY_ANDROID
            if (_client != null)
            {
                _client.setRefreshTime(seconds);
            }
#endif
        }
        
        public void OnResetEvent()
        {
            OnImpression = null;
            OnClicked = null;
            OnClosed = null;
            OnAdPaid = null;
        }

        public static Vector2 GetPadding()
        {
#if UNITY_IOS
            if (_cachedPaddingX < 0 || _cachedPaddingY < 0)
            {
                _cachedPaddingX = Screen.width  - Screen.safeArea.xMax;
                _cachedPaddingY = Screen.height - Screen.safeArea.yMax;
            }
#else
            _cachedPaddingX = Screen.width  - Screen.safeArea.xMax;
            _cachedPaddingY = Screen.height - Screen.safeArea.yMax;
#endif
            var padding_x = _cachedPaddingX;
            var padding_y = _cachedPaddingY;
            
            return new Vector2(padding_x, padding_y);
        }
    }

    public class NativeOptions
    {
        public const string BANNER_THEME_LIGHT = "light";
        public const string BANNER_THEME_DARK = "dark";
        
        public const string TEMPLATE_BANNER = "banner";
        public const string TEMPLATE_PORTRAIT = "portrait";
        public const string TEMPLATE_LANDSCAPE = "landscape";
        public const string TEMPLATE_FULLSCREEN = "fullscreen";
        public const string TEMPLATE_FULLSCREEN2 = "fullscreen2";
        public const string TEMPLATE_FULLSCREEN_MEDIA_2 = "landscape_2";
        public const string TEMPLATE_LANDSCAPE_FAN = "landscape_3";
        public const string TEMPLATE_LANDSCAPE_BANNER = "landscape_4";

        /// <summary>
        /// quit có hiển thị đếm giây
        /// </summary>
        public const string INTER_QUIT = "inter";
        public const string INTER_QUIT_2 = "inter2";
        public const string INTER_QUIT_3 = "inter3";
        

        /// <summary>
        /// quit không có hiển thị đếm giây
        /// </summary>
        public const string NATIVE_QUIT = "native";

        /// <summary>
        /// Không có nút quit
        /// </summary>
        public const string NONE_QUIT = "none";

        public const string ORIENTATION_PORTRAIT = "portrait";
        public const string ORIENTATION_LANDSCAPE = "landscape";
        public const float PADDING_20 = 20;
        public const float PADDING_40 = 40;

        private long _refresh;
        private string _adUnitId;
        private float _x, _y, _width, _height;
        private string _template;
        private string _orientation;
        private float _paddingStart, _paddingTop, _paddingEnd, _paddingBottom;
        private string _quitType;
        private bool _pauseApplication;
        private bool _muted;
        private bool _reloadOnClick;
        private bool _isPauseOnShow;
        private int _timeClose = 3;
        private bool _forceAdPortrait;
        
        public bool IsPauseOnShow { get => _isPauseOnShow; }

        private NativeOptions() { }

        public INativeOverlay GetClient()
        {
            
#if UNITY_EDITOR
            return null;
#elif UNITY_ANDROID
            return new AndroidNativeOverlay(
                        _adUnitId,
                        _refresh,
                        _x, _y,
                        _width, _height,
                        _template, _quitType,
                        _paddingStart, _paddingEnd,
                        _paddingTop, _paddingBottom, 
                        _forceAdPortrait, _timeClose, _reloadOnClick
            );
#elif UNITY_IOS
            return new IOSNativeOverlay(
                _adUnitId,
                _refresh,
                _x, _y,
                _width, _height,
                _template, _quitType,
                _paddingStart, _paddingEnd,
                _paddingTop, _paddingBottom
            );
#else
            return null;
#endif
        }

        public static string AutoOrientation()
        {
            return Screen.orientation switch
            {
                ScreenOrientation.LandscapeLeft or ScreenOrientation.LandscapeRight => "landscape",
                _                                                                   => "portrait"
            };
        }

        public static string AutoTemplate(string template = "")
        {
            if (!string.IsNullOrEmpty(template))
            {
                return template;
            }
            return Screen.orientation switch
            {
                ScreenOrientation.LandscapeLeft or ScreenOrientation.LandscapeRight => "landscape",
                _                                                                   => "portrait"
            };
        }

        public class Builder
        {
            private NativeOptions _options;

            public Builder()
            {
                _options = new NativeOptions();
                _options._refresh = 15;
                _options._reloadOnClick = true;
                _options._muted = true;
            }

            public Builder Template(string template)
            {
                _options._template = template;
                return this;
            }

            public Builder Orientation(string orientation)
            {
                _options._orientation = orientation;
                return this;
            }

            public Builder SetForceAdPortrait(bool force)
            {
                _options._forceAdPortrait = force;
                return this;
            }

            public Builder PositionAndSize(Rect rect,  bool minSizeMedia = true)
            {
                _options._x = rect.x;
                _options._y = Screen.height - rect.y - rect.height;  // ← Convert từ bottom-left sang top-left
                _options._width = rect.width;
                _options._height = rect.height;
                return this;
            }

            public Builder PositionAndSize(float x, float y, float width, float height)
            {
                _options._x = x;
                _options._y = y;
                _options._width = width;
                _options._height = height;
                return this;
            }

            public Builder PositionAndSize(RectTransform rectTransform, bool minSizeMedia = true)
            {
                var corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                var x = corners[0].x;
                var y = corners[0].y;
                var width = corners[2].x  - corners[0].x;
                var height = corners[2].y - corners[0].y;
                PositionAndSize(new Rect(x, y, width, height), minSizeMedia);
                return this;
            }

            public Builder Refresh(long seconds)
            {
                _options._refresh = seconds;
                return this;
            }

            public Builder Quit(string type, float paddingTop = 20, float paddingEnd = 20)
            {
                _options._quitType = type;
                _options._paddingTop = paddingTop;
                _options._paddingEnd = paddingEnd;
                return this;
            }

            public Builder SetPauseOnShow(bool isPauseOnShow)
            {
                _options._isPauseOnShow = isPauseOnShow;
                return this;
            }

            public Builder SetReloadOnClick(bool reloadOnClick)
            {
                _options._reloadOnClick = reloadOnClick;
                return this;
            }

            /// <summary>
            /// set time count native to close
            /// </summary>
            /// <param name="timeClose"></param>
            /// <returns></returns>
            public Builder SetTimeClose(int timeClose)
            {
                _options._timeClose = timeClose;
                return this;
            }

            public Builder SetOptionFullScreen(string template = "")
            {
                Template(AutoTemplate(template));
                Orientation(AutoOrientation());
                PositionAndSize(0, 0, Screen.width, Screen.height);
                return this;
            }

            public NativeOptions Build(string adUnitId)
            {
                if (string.IsNullOrEmpty(adUnitId))
                    throw new ArgumentException("AdUnitID must be set.");
                if (string.IsNullOrEmpty(_options._template))
                    throw new ArgumentException("Template must be set.");
                _options._adUnitId = adUnitId;
                return _options;
            }
        }
    }

    public interface INativeOverlay
    {
        event Action OnImpression;
        event Action OnClicked;
        event Action OnClosed;
        event Action OnLoadSucceed;
        event Action<AdValue> OnAdPaid;
        event Action<LoadAdError> OnLoadFailed;

        void Load();
        void Show();
        void Hide();
        void Destroy();
        internal void OnApplicationPause(bool pauseStatus);
        
#if UNITY_ANDROID || UNITY_IOS
        void setQuitButtonScale(float scale);
#endif

#if UNITY_ANDROID
        void setBannerTheme(string theme);

        void setRefreshTime(long seconds);
        
        void setQuitButtonCoverScale(float scale);
        
        void setEnforceMinViewTimeBeforeHide(bool enable);
#endif
    }
}