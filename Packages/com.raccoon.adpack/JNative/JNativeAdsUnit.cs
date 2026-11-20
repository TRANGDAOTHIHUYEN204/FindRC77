using GoogleMobileAds.Api;
using JKit.Monetize.Ads;
using RCKit.Ads.AdMob;

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

        // JNATIVE_HAS_ADSOURCE: do chính com.raccoon.jnativeads khai báo (JNativeFeatureDefines, Editor) khi bản
        // plugin có NativeOverlay.OnAdPaidInfo + NativeAdSourceInfo. Không gate theo version vì các branch plugin
        // dùng chung dãy số nhưng code khác nhau. Không có define → dùng OnAdPaid, không có ad source.
        protected void SubscribePaid(NativeOverlay overlay)
        {
#if JNATIVE_HAS_ADSOURCE
            overlay.OnAdPaidInfo += (adValue, source) =>
                GoogleAdsMediation.RaisePaid(adValue, adUnitId, AdPaidInfo.FormatNative, ToAdSource(source));
#else
            overlay.OnAdPaid += adValue =>
                GoogleAdsMediation.RaisePaid(adValue, adUnitId, AdPaidInfo.FormatNative, AdSource.FromResponseInfo(null));
#endif
        }

#if JNATIVE_HAS_ADSOURCE
        private static AdSource ToAdSource(NativeAdSourceInfo s)
        {
            if (s == null) return AdSource.FromResponseInfo(null);
            return new AdSource(s.ad_source_name, s.ad_source_id, s.ad_source_instance_name, s.ad_source_instance_id,
                s.mediation_group_name, s.mediation_ab_test_name, s.mediation_ab_test_variant);
        }
#endif
    }
}
