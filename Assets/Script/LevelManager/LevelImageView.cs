using UnityEngine;
using UnityEngine.UI;

public class LevelImageView : MonoBehaviour
{
    [SerializeField] private Image rawImage;
    [SerializeField] private Image errorImage;

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
        if (rawImage == null || errorImage == null) return false;
        return IsInside(rawImage, screenPoint) || IsInside(errorImage, screenPoint);
    }

    private bool IsInside(Image image, Vector2 screenPoint)
    {
        Canvas canvas = image.canvas;
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform, screenPoint, cam);
    }
}