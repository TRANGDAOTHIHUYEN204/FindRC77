using UnityEngine;
using UnityEngine.UI;

public class LevelImageView : MonoBehaviour
{
    [System.Serializable]
    public class LevelRoots
    {
        public GameObject rootA;
        public GameObject rootB;
    }
    
    [SerializeField] private Image rawImage;
    [SerializeField] private Image errorImage;
    
    [SerializeField] private LevelRoots[] levelRoots;

    public void ShowLevel(LevelData level, int currentIndex)
    {
        if (level == null) return;

        rawImage.sprite = level.RawSprite;
        errorImage.sprite = level.ErrorSprite;

        ShowRoots(currentIndex);
    }

    private void ShowRoots(int currentIndex)
    {
        if (levelRoots == null)
            return;

        for (int i = 0; i < levelRoots.Length; i++)
        {
            bool isCurrentLevel = i == currentIndex;

            if (levelRoots[i].rootA != null)
                levelRoots[i].rootA.SetActive(isCurrentLevel);

            if (levelRoots[i].rootB != null)
                levelRoots[i].rootB.SetActive(isCurrentLevel);
        }
    }

    public void GetCurrentRoots(int currentIndex, out GameObject rootA, out GameObject rootB)
    {
        rootA = null;
        rootB = null;

        if (levelRoots == null || currentIndex < 0 || currentIndex >= levelRoots.Length)
        {
            return;
        }

        rootA = levelRoots[currentIndex].rootA;
        rootB = levelRoots[currentIndex].rootB;
    }

    public bool IsInsideImages(Vector2 screenPoint)
    {
        if (rawImage == null || errorImage == null)
            return false;

        return IsInsideImage(rawImage, screenPoint) || IsInsideImage(errorImage, screenPoint);
    }

    private bool IsInsideImage(Image image, Vector2 screenPoint)
    {
        Canvas canvas = image.canvas;

        Camera uiCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform, screenPoint, uiCamera);
    }

    public Vector2 ScreenToWorldPoint(Vector2 screenPoint, Camera worldCamera)
    {
        float distance = Mathf.Abs(worldCamera.transform.position.z - rawImage.canvas.transform.position.z);

        Vector3 worldPoint = worldCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, distance));

        return worldPoint;
    }
}
