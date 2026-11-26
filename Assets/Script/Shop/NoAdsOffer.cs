using System;
using System.Text;
using UnityEngine;
using RCKit.Ads.Main;

/// Gói "No Ads Offer": bỏ quảng cáo xen kẽ / mở app / banner vĩnh viễn + tặng hint.
/// Quảng cáo có thưởng (xem video lấy hint) vẫn giữ để người chơi tự chọn.
public static class NoAdsOffer
{
    /// Product ID non-consumable — phải tạo đúng ID này trên Google Play Console / App Store Connect.
    public const string ProductId = "remove_ads";
    public const int BonusHints = 5;

    /// Tự bật màn ưu đãi đúng một lần, khi người chơi vào home lần thứ mấy.
    public const int AutoShowHomeVisit = 3;

    /// Đồng hồ ưu đãi chạy theo chu kỳ, hết giờ thì bắt đầu vòng mới.
    public static readonly TimeSpan Cycle = TimeSpan.FromHours(24);

    /// Giá hiển thị khi chưa lấy được giá từ store (Editor, mất mạng...).
    public const string FallbackPrice = "$4.99";
    public const string FallbackOldPrice = "$9.99";

    private const string KeyOwned = "save_no_ads_owned";
    private const string KeyTimerStart = "save_no_ads_timer_start";
    private const string KeyHomeVisits = "save_home_visits";
    private const string KeyAutoShown = "save_no_ads_auto_shown";

    public static event Action OnOwned;

    private static int _ownedCache = -1;

    public static bool IsOwned
    {
        get
        {
            if (_ownedCache < 0) _ownedCache = PlayerPrefs.GetInt(KeyOwned, 0);
            return _ownedCache == 1;
        }
    }

    /// Cấp quyền lợi gói. Gọi lại nhiều lần (restore) cũng không cộng hint thêm.
    public static void Grant()
    {
        if (IsOwned)
        {
            ApplyToAds();
            return;
        }

        PlayerPrefs.SetInt(KeyOwned, 1);
        _ownedCache = 1;
        SaveGame.HintBalance += BonusHints;
        PlayerPrefs.Save();
        Debug.Log($"[NoAds] Granted: no ads + {BonusHints} hints.");

        ApplyToAds();
        OnOwned?.Invoke();
    }

    /// Báo cho SDK quảng cáo tắt các loại quảng cáo không có thưởng.
    /// Trả về true khi SDK đã nhận (hoặc không cần làm gì nữa); false nếu SDK chưa init xong.
    /// IapService gọi liên tục tới khi true, nên áp được ngay khi SDK vừa init.
    public static bool ApplyToAds()
    {
#if ENABLE_RACCOON_ADMOB
        if (!IsOwned) return true;

        var mgr = RaccoonAdsManager.Instance;
        if (mgr == null || !mgr.Initialized) return false;
        if (mgr.IsAdsRemoved) return true;

        try
        {
            foreach (AD_TYPE type in Enum.GetValues(typeof(AD_TYPE)))
                if (!AdsController.IsRewardType(type))
                    mgr.AddRemovableAd(type);

            mgr.SetAdsRemoved();
            mgr.HideAllBanner();
            Debug.Log("[NoAds] Ads removed in SDK.");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
#endif
        return true;
    }

    /// Thời gian còn lại của vòng ưu đãi hiện tại. Lần đầu gọi sẽ bắt đầu đếm.
    public static TimeSpan Remaining
    {
        get
        {
            long now = DateTime.UtcNow.Ticks;
            long start = 0;
            long.TryParse(PlayerPrefs.GetString(KeyTimerStart, "0"), out start);

            // Chưa bắt đầu, hoặc giờ máy bị chỉnh lùi → bắt đầu vòng mới từ bây giờ.
            if (start <= 0 || start > now)
            {
                start = now;
                PlayerPrefs.SetString(KeyTimerStart, start.ToString());
                PlayerPrefs.Save();
            }

            long cycle = Cycle.Ticks;
            long left = cycle - (now - start) % cycle;
            return new TimeSpan(left);
        }
    }

    public static string FormatRemaining()
    {
        TimeSpan t = Remaining;
        return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }

    /// Gọi mỗi lần vào màn home. Trả về true nếu lần này cần tự bật màn ưu đãi.
    public static bool RegisterHomeVisit()
    {
        int visits = PlayerPrefs.GetInt(KeyHomeVisits, 0);
        if (visits < AutoShowHomeVisit) visits++;
        PlayerPrefs.SetInt(KeyHomeVisits, visits);

        bool show = !IsOwned && visits == AutoShowHomeVisit && PlayerPrefs.GetInt(KeyAutoShown, 0) == 0;
        if (show) PlayerPrefs.SetInt(KeyAutoShown, 1);

        PlayerPrefs.Save();
        return show;
    }

    /// Giá gốc (gạch ngang) = gấp đôi giá bán, viết theo đúng định dạng tiền của store.
    /// Giá có phần lẻ thì làm tròn kiểu x.99 (4.99 → 9.99).
    public static string FormatOldPrice(string localizedPrice, decimal price)
    {
        if (string.IsNullOrEmpty(localizedPrice) || price <= 0) return FallbackOldPrice;

        int first = -1, last = -1;
        for (int i = 0; i < localizedPrice.Length; i++)
        {
            if (!char.IsDigit(localizedPrice[i])) continue;
            if (first < 0) first = i;
            last = i;
        }
        if (first < 0) return FallbackOldPrice;

        string number = localizedPrice.Substring(first, last - first + 1);

        // Dấu thập phân = dấu . hoặc , cuối cùng nếu theo sau là 1-2 chữ số; còn lại là dấu nhóm nghìn.
        int sepIndex = Math.Max(number.LastIndexOf('.'), number.LastIndexOf(','));
        int decimals = 0;
        char decSep = '.';
        char groupSep = '\0';
        if (sepIndex >= 0)
        {
            int after = number.Length - sepIndex - 1;
            if (after >= 1 && after <= 2)
            {
                decimals = after;
                decSep = number[sepIndex];
                foreach (char c in number.Substring(0, sepIndex))
                    if (c == '.' || c == ',' || c == ' ' || c == ' ') { groupSep = c; break; }
            }
            else
            {
                groupSep = number[sepIndex];
            }
        }

        decimal old = price * 2m;
        if (decimals > 0 && decimal.Truncate(old) != old)
            old = decimal.Ceiling(old) - (decimals == 2 ? 0.01m : 0.1m);
        else if (decimals == 0)
            old = decimal.Round(old);

        long whole = (long)decimal.Truncate(old);
        var sb = new StringBuilder();
        string digits = whole.ToString();
        for (int i = 0; i < digits.Length; i++)
        {
            if (groupSep != '\0' && i > 0 && (digits.Length - i) % 3 == 0) sb.Append(groupSep);
            sb.Append(digits[i]);
        }
        if (decimals > 0)
        {
            decimal frac = old - whole;
            long fracDigits = (long)decimal.Round(frac * (decimals == 2 ? 100m : 10m));
            sb.Append(decSep).Append(fracDigits.ToString(decimals == 2 ? "00" : "0"));
        }

        return localizedPrice.Substring(0, first) + sb + localizedPrice.Substring(last + 1);
    }
}
