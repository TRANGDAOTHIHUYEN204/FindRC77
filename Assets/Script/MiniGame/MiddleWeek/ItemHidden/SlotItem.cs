using Coffee.UIEffects;  
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private RectTransform flyTarget;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private UIEffectTweener shinyTweener;  
    
    [SerializeField] private float iconScale = 1.4f;
    [SerializeField] private float iconUpTime = 0.12f;
    [SerializeField] private float iconDownTime = 0.2f;
    
    [SerializeField] private float textScale = 1.5f;
    [SerializeField] private float textUpTime = 0.1f;
    [SerializeField] private float textDownTime = 0.15f;

    public int Id { get; private set; }
    public RectTransform FlyTarget => flyTarget;

    private bool _initialized;
    private string _pendingText;
    private Sequence _seq;
    private Vector3 _iconBaseScale;
    private Vector3 _textBaseScale;

    private void Awake()
    {
        _iconBaseScale = icon.rectTransform.localScale;
        _textBaseScale = countText.rectTransform.localScale;
    }

    public void Setup(int id, Sprite sprite)
    {
        Id = id;
        icon.sprite = sprite;
        icon.preserveAspect = true;
    }

    public void SetProgress(int found, int total)
    {
        string newText = $"{found}/{total}";

        if (!_initialized)
        {
            _initialized = true;
            countText.text = newText;
            return;
        }

        string current = _pendingText ?? countText.text;
        if (current == newText) return;

        PlayFeedback(newText);
    }

    private void PlayFeedback(string newText)
    {
        KillAndFinish();
        _pendingText = newText;

        var iconRt = icon.rectTransform;
        var textRt = countText.rectTransform;

        _seq = DOTween.Sequence().SetLink(gameObject).SetUpdate(true);
        
        _seq.Append(iconRt.DOScale(_iconBaseScale * iconScale, iconUpTime).SetEase(Ease.OutQuad));
        _seq.Append(iconRt.DOScale(_iconBaseScale, iconDownTime).SetEase(Ease.OutBack));
        _seq.AppendCallback(() =>
        {
            countText.text = _pendingText;
            _pendingText = null;
        });
        _seq.Append(textRt.DOScale(_textBaseScale * textScale, textUpTime).SetEase(Ease.OutQuad));
        _seq.Append(textRt.DOScale(_textBaseScale, textDownTime).SetEase(Ease.OutBack));
        _seq.AppendCallback(() => { if (shinyTweener != null) shinyTweener.Play(); });

        _seq.OnComplete(() => _seq = null);
    }

    private void KillAndFinish()
    {
        if (_seq != null) { _seq.Kill(); _seq = null; }
        if (_pendingText != null) { countText.text = _pendingText; _pendingText = null; }
        icon.rectTransform.localScale = _iconBaseScale;
        countText.rectTransform.localScale = _textBaseScale;
    }

    private void OnDisable()
    {
        if (icon != null && countText != null) KillAndFinish();
    }
}