using UnityEngine;
using UnityEngine.UI;

public class HintOverlay : MonoBehaviour
{
    [SerializeField] private RectTransform top, bottom, left, right;
    [SerializeField] private RectTransform hole; 
    [SerializeField] private Color overlayColor = new Color(0, 0, 0, 0.7f);
    [SerializeField, Range(0.01f, 0.5f)] private float softEdge = 0.05f;

    private RectTransform _rect;
    private static Sprite _holeSprite;

    private void Awake()
    {
        _rect = (RectTransform)transform;

        if (_holeSprite == null) _holeSprite = CreateHoleSprite(256, softEdge);
        hole.GetComponent<Image>().sprite = _holeSprite;

        foreach (var t in new[] { top, bottom, left, right, hole })
        {
            var img = t.GetComponent<Image>();
            img.color = overlayColor;
            img.raycastTarget = false;
        }
    }

    public void Show(Vector3 worldCenter, Vector2 worldSize, float padding = 10f)
    {
        if (_rect == null) _rect = (RectTransform)transform;
        gameObject.SetActive(true);

        Rect r = _rect.rect;
        Vector2 c = _rect.InverseTransformPoint(worldCenter);
        Vector3 s3 = _rect.InverseTransformVector(new Vector3(worldSize.x, worldSize.y, 0f));
        
        float radius = Mathf.Max(Mathf.Abs(s3.x), Mathf.Abs(s3.y)) * 0.5f + padding;

        float x0 = c.x - radius, x1 = c.x + radius;
        float y0 = c.y - radius, y1 = c.y + radius;
        
        SetRect(hole, x0, y0, x1, y1);

        float cx0 = Mathf.Clamp(x0, r.xMin, r.xMax);
        float cx1 = Mathf.Clamp(x1, r.xMin, r.xMax);
        float cy0 = Mathf.Clamp(y0, r.yMin, r.yMax);
        float cy1 = Mathf.Clamp(y1, r.yMin, r.yMax);

        SetRect(bottom, r.xMin, r.yMin, r.xMax, cy0);
        SetRect(top,    r.xMin, cy1,    r.xMax, r.yMax);
        SetRect(left,   r.xMin, cy0,    cx0,    cy1);
        SetRect(right,  cx1,    cy0,    r.xMax, cy1);
    }

    public void Hide() => gameObject.SetActive(false);

    private void SetRect(RectTransform t, float x0, float y0, float x1, float y1)
    {
        Rect r = _rect.rect;
        t.anchorMin = t.anchorMax = Vector2.zero;
        t.pivot = Vector2.zero;
        t.anchoredPosition = new Vector2(x0 - r.xMin, y0 - r.yMin);
        t.sizeDelta = new Vector2(Mathf.Max(0f, x1 - x0), Mathf.Max(0f, y1 - y0));
    }
    private static Sprite CreateHoleSprite(int size, float soft)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        float half = size * 0.5f;
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half;
            float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f - soft, 1f, d));
            px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}