using UnityEngine;
using UnityEngine.UI;

public class LevelImageView : MonoBehaviour
{
    [SerializeField] private Image rawImage;
    [SerializeField] private Image errorImage;
    
    [SerializeField] private RectTransform frameA;
    [SerializeField] private RectTransform frameB;

    public RectTransform RootA => rawImage.rectTransform;
    public RectTransform RootB => errorImage.rectTransform;

    public void ShowLevel(LevelData level)
    {
        if (level == null) return;
        rawImage.sprite = level.RawSprite;
        errorImage.sprite = level.ErrorSprite;
    }

    public bool IsInsideImages(Vector2 screenPoint)
    {
        if (frameA == null || frameB == null) return false;
        return IsInsideFrame(frameA, screenPoint) || IsInsideFrame(frameB, screenPoint);
    }

    private bool IsInsideFrame(RectTransform frame, Vector2 screenPoint)
    {
        Canvas canvas = frame.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(frame, screenPoint, cam);
    }
}