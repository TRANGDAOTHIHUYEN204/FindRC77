using System;

namespace RCKit.Ads
{
    public interface IAdsBanner
    {
        event Action OnShowed;
        event Action OnHidden;
        event Action OnLoadFailed;
        event Action OnLoadSucceed;

        bool IsShowed();
        void Show();
        void Hide();
        void Load();
        void Destroy();
        
        bool IsReady();

        void SetRefreshTime(long refreshTime);
        
        void SetBannerTheme(string theme);
    }
}