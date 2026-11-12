using TMPro;
using UnityEngine;

/// <summary>
/// Màn loading của AdLoadingGate (Figma "LoadingAdsGate-Coffee Time"): "Tea break" giữa
/// 2 ly cà phê, dấu chấm đếm 1, 2, 3 rồi lặp lại. Gate chỉ bật / tắt panel.
///
/// <para>Các dấu chấm chưa hiện được vẽ trong suốt nên chữ căn giữa không bị xô lệch.
/// Chạy theo unscaled time vì gate giữ timeScale = 0 trong lúc chờ ads.</para>
/// </summary>
public sealed class AdsGateView : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private string _text = "Tea break";
    [Tooltip("Số giây mỗi trạng thái dấu chấm.")]
    [SerializeField, Min(0.05f)] private float _dotSeconds = 0.4f;

    private const int MaxDots = 3;
    private int _dots = -1;
    private float _startTime;

    private void OnEnable()
    {
        _startTime = Time.unscaledTime;
        _dots = -1;
        SetDots(1);
    }

    private void Update()
    {
        if (_label == null) return;
        SetDots(1 + (int)((Time.unscaledTime - _startTime) / _dotSeconds) % MaxDots);
    }

    private void SetDots(int dots)
    {
        if (_label == null || dots == _dots) return;
        _dots = dots;
        _label.text = _text + new string('.', dots)
                      + (dots < MaxDots ? "<alpha=#00>" + new string('.', MaxDots - dots) : "");
    }
}
