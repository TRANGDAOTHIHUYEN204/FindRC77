namespace RCKit.Ads.Max
{
    public abstract class MaxAdsUnit
    {
        protected string adUnitId;

        protected MaxAdsUnit(string adUnitId)
        {
            this.adUnitId = adUnitId;
        }
    }
}