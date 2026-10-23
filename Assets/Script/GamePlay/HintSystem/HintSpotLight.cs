using UnityEngine;

public class HintSpotlight : MonoBehaviour
{
    [SerializeField] private ZoomManager zoom;
    [SerializeField] private HintOverlay overlayA;  
    [SerializeField] private HintOverlay overlayB;   

    private RectTransform _a, _b;
    private DifferentPair _pair;
    private bool _animated;
    
    
    private void OnEnable()  => zoom.OnViewChanged += Refresh;
    private void OnDisable() => zoom.OnViewChanged -= Refresh;

    public void Show(DifferentPair pair) => ShowInternal(pair, false);
    public void ShowAnimated(DifferentPair pair) => ShowInternal(pair, true);

    private void ShowInternal(DifferentPair pair, bool animated)
    {
        _pair = pair;
        _animated = animated;
        pair.GetWorldRect(pair.RootA, out Vector3 c, out Vector2 _);
        zoom.FocusOn(pair.RootA, c);
        Refresh();
    }

    public void Hide()
    {
        _pair = null;
        _animated = false;
        overlayA.Hide();
        overlayB.Hide();
        zoom.Unlock();
        zoom.ResetZoomSmooth();
    }

    private void Refresh()
    {
        if (_pair == null) return;

        _pair.GetWorldRect(_pair.RootA, out Vector3 ca, out Vector2 sa);
        _pair.GetWorldRect(_pair.RootB, out Vector3 cb, out Vector2 sb);

        if (_animated)
        {
            overlayA.ShowAnimatedHint(ca, sa);
            overlayB.ShowAnimatedHint(cb, sb);
        }
        else
        {
            overlayA.Show(ca, sa);
            overlayB.Show(cb, sb);
        }
    }
}