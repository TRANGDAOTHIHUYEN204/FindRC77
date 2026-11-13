using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonAdsController : MonoBehaviour
{
    private Button _button;
    private Button.ButtonClickedEvent _originalClick;
    private bool _processing;

    private void Awake()
    {
        _button = GetComponent<Button>();

        _originalClick = _button.onClick;

        _button.onClick = new Button.ButtonClickedEvent();
        _button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        _processing = false;
    }

    private void OnClick()
    {
        if (_processing) return;

        _processing = true;

        var ads = AdsController.Instance;

        if (ads == null)
        {
            ContinueAction();
            return;
        }

        ads.RequestInter(ContinueAction);
    }

    private void ContinueAction()
    {
        _processing = false;
        _originalClick?.Invoke();
    }
}