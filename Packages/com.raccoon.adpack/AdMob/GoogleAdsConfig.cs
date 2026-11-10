using GoogleMobileAds.Common;

namespace RCKit.Ads.AdMob
{
    public class GoogleAdsConfig : IConfigProvider
    {
        public void RunConfig()
        {
            MobileAdsEventExecutor.Initialize();
        }
    }
}