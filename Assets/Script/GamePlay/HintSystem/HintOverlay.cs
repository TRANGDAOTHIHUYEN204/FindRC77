
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HintOverlay : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private RectTransform top;
    [SerializeField] private RectTransform bottom;
    [SerializeField] private RectTransform left;
    [SerializeField] private RectTransform right;
    [SerializeField] private RectTransform hole;

    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.7f);

    [SerializeField, Range(0.01f, 0.5f)]
    private float softEdge = 0.05f;

    [Header("Animated Hint")]
    [SerializeField, Range(0f, 0.3f)]
    private float pulseAmplitude = 0.12f;

    [SerializeField, Min(0.1f)]
    private float pulseSpeed = 2f;

    private RectTransform _rect;
    private Image _holeImage;

    private Coroutine _hintRoutine;

    private static Sprite _holeSprite;

    private Vector2 _hintCenter;
    private float _baseRadius;

    private void Awake()
    {
        _rect = (RectTransform)transform;

        if (_holeSprite == null)
            _holeSprite = CreateHoleSprite(256, softEdge);

        _holeImage = hole.GetComponent<Image>();
        _holeImage.sprite = _holeSprite;
        _holeImage.raycastTarget = false;

        RectTransform[] parts =
        {
            top, bottom, left, right
        };

        foreach (RectTransform part in parts)
        {
            Image image = part.GetComponent<Image>();
            image.color = overlayColor;
            image.raycastTarget = false;
        }
    }
    
    public void Show(Vector3 worldCenter, Vector2 worldSize, float padding = 10f)
    {
        StopHintAnimation();

        if (_rect == null)
            _rect = (RectTransform)transform;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        ApplyCircleHole();

        Vector2 center = _rect.InverseTransformPoint(worldCenter);
        Vector3 size = _rect.InverseTransformVector(new Vector3(worldSize.x, worldSize.y, 0f));
        float radius = Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.y)) * 0.5f + padding;

        UpdateOverlay(center, radius);
    }

    public void ShowAnimatedHint(Vector3 worldCenter, Vector2 worldSize, float padding = 10f)
    {
        StopHintAnimation();

        if (_rect == null)
            _rect = (RectTransform)transform;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        // Same circular soft-hole as normal hint (no ring sprite / square cutout).
        ApplyCircleHole();

        Vector2 center = _rect.InverseTransformPoint(worldCenter);
        Vector3 size = _rect.InverseTransformVector(new Vector3(worldSize.x, worldSize.y, 0f));

        _baseRadius = Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.y)) * 0.5f + padding;
        _hintCenter = center;

        UpdateOverlay(_hintCenter, _baseRadius);
        _hintRoutine = StartCoroutine(AnimateHint());
    }

    private void ApplyCircleHole()
    {
        _holeImage.sprite = _holeSprite;
        _holeImage.color = overlayColor;
        hole.localScale = Vector3.one;
    }

    private void UpdateOverlay(Vector2 center, float radius)
    {
        Rect rect = _rect.rect;

        float x0 = center.x - radius;
        float x1 = center.x + radius;
        float y0 = center.y - radius;
        float y1 = center.y + radius;

        SetRect(hole, x0, y0, x1, y1);

        float cx0 = Mathf.Clamp(x0, rect.xMin, rect.xMax);
        float cx1 = Mathf.Clamp(x1, rect.xMin, rect.xMax);
        float cy0 = Mathf.Clamp(y0, rect.yMin, rect.yMax);
        float cy1 = Mathf.Clamp(y1, rect.yMin, rect.yMax);

        SetRect(bottom, rect.xMin, rect.yMin, rect.xMax, cy0);
        SetRect(top, rect.xMin, cy1, rect.xMax, rect.yMax);
        SetRect(left, rect.xMin, cy0, cx0, cy1);
        SetRect(right, cx1, cy0, rect.xMax, cy1);
    }

    public void Hide()
    {
        StopHintAnimation();
        gameObject.SetActive(false);
    }

    private void StopHintAnimation()
    {
        if (_hintRoutine != null)
        {
            StopCoroutine(_hintRoutine);
            _hintRoutine = null;
        }
    }

    private void SetRect(RectTransform target, float x0, float y0, float x1, float y1)
    {
        Rect rect = _rect.rect;

        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.zero;
        target.pivot = Vector2.zero;

        target.anchoredPosition = new Vector2(x0 - rect.xMin, y0 - rect.yMin);

        target.sizeDelta = new Vector2(Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
    }

    private static Sprite CreateHoleSprite(int size, float soft)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float half = size * 0.5f;
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(
                    new Vector2(x + 0.5f, y + 0.5f),
                    new Vector2(half, half)) / half;

                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f - soft, 1f, distance));

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private IEnumerator AnimateHint()
    {
        while (true)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) * pulseAmplitude;
            UpdateOverlay(_hintCenter, _baseRadius * pulse);
            yield return null;
        }
    }
}
