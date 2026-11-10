namespace RCKit.Ads.JNative
{
    public abstract class JNativeAdsUnit
    {
        protected string adUnitId;

        protected bool supportMedia = true;

        protected JNativeAdsUnit(string adUnitId)
        {
            this.adUnitId = adUnitId;
        }

        protected JNativeAdsUnit(string adUnitId, bool supportMedia)
        {
            this.adUnitId = adUnitId;
            this.supportMedia = supportMedia;
        }
    }
}
