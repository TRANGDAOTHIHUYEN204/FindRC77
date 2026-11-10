using GoogleMobileAds.Api;

namespace RCKit.Ads.AdMob
{
    public abstract class GoogleAdsUnit
    {
        protected string adUnitId;

        protected GoogleAdsUnit(string adUnitId)
        {
            this.adUnitId = adUnitId;
        }

    }
}