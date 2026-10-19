using UnityEngine;

public class HintSpotlight : MonoBehaviour
{
    [SerializeField] private ZoomManager zoom;
    [SerializeField] private HintOverlay overlayA;  
    [SerializeField] private HintOverlay overlayB;   

    private RectTransform _a, _b;
    private DifferentPair _pair;
    private void OnEnable()  => zoom.OnViewChanged += Refresh;
    private void OnDisable() => zoom.OnViewChanged -= Refresh;

    public void Show(DifferentPair pair)
    {
        _pair = pair;
        pair.GetWorldRect(pair.RootA, out Vector3 c, out Vector2 _);
        zoom.FocusOn(pair.RootA, c);
        Refresh();
    }
    
    public void Hide()
    {
        _pair = null;
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

        overlayA.Show(ca, sa);
        overlayB.Show(cb, sb);
    }
}