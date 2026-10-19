using UnityEngine;

public static class FrameMaskUtil
{
    private static Sprite _sprite;

    private static Sprite GetSprite()
    {
        if (_sprite != null) return _sprite;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _sprite;
    }

    public static void ApplyMask(RectTransform frame, MarkFound mark, int order)
    {
        var lines = mark.GetComponentsInChildren<LineRenderer>(true);
        if (lines.Length == 0) return;
        int layerId = lines[0].sortingLayerID;

        Transform t = frame.Find("MarkSpriteMask");
        SpriteMask mask;
        if (t == null)
        {
            var go = new GameObject("MarkSpriteMask");
            go.transform.SetParent(frame, false);
            mask = go.AddComponent<SpriteMask>();
            mask.sprite = GetSprite();
        }
        else mask = t.GetComponent<SpriteMask>();

        Rect r = frame.rect;
        mask.transform.localPosition = new Vector3(r.center.x, r.center.y, 0f);
        mask.transform.localRotation = Quaternion.identity;
        mask.transform.localScale = new Vector3(r.width, r.height, 1f);

        mask.isCustomRangeActive = true;
        mask.frontSortingLayerID = layerId;
        mask.backSortingLayerID  = layerId;
        mask.frontSortingOrder   = order + 1;
        mask.backSortingOrder    = order - 2;

        foreach (var lr in lines)
        {
            lr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            lr.sortingOrder = lr.name == "Outline" ? order - 1 : order;
        }
    }
}