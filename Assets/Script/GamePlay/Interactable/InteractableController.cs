using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// Dựng và điều khiển các đồ vật tương tác (vd: tủ quần áo mở ra được) của level.
/// Layer sprite được đặt làm con của ảnh A và B nên tự theo zoom/pan.
/// Được DifferentPairManagerHolder tạo và gọi Build/Clear → reset level là tự đóng lại.
public class InteractableController : MonoBehaviour
{
    private static readonly Vector2 DesignSize = new Vector2(1080f, 720f);
    private const float OpenDuration = 0.3f;

    private class Runtime
    {
        public InteractableInfo Info;
        public bool IsOpen;
        public readonly List<Image> Closed = new List<Image>();
        public readonly List<Image> Open = new List<Image>();
        public readonly List<DifferentPair> HiddenPairs = new List<DifferentPair>();
    }

    private readonly List<Runtime> _items = new List<Runtime>();
    private readonly List<GameObject> _containers = new List<GameObject>();
    private RectTransform _rootA, _rootB;

    public bool HasAny => _items.Count > 0;

    public void Build(LevelData level, RectTransform rootA, RectTransform rootB, IReadOnlyList<DifferentPair> pairs)
    {
        Clear();
        if (level == null || level.Interactables == null || level.Interactables.Length == 0) return;

        _rootA = rootA;
        _rootB = rootB;
        Transform containerA = CreateContainer(rootA);
        Transform containerB = CreateContainer(rootB);

        foreach (var info in level.Interactables)
        {
            if (info == null) continue;
            var rt = new Runtime { Info = info };

            CreateLayers(info.ClosedLayers, containerA, rootA, rt.Closed);
            CreateLayers(info.ClosedLayers, containerB, rootB, rt.Closed);
            CreateLayers(info.OpenLayers, containerA, rootA, rt.Open);
            CreateLayers(info.OpenLayers, containerB, rootB, rt.Open);

            if (info.HiddenPointIndices != null && pairs != null)
            {
                foreach (int idx in info.HiddenPointIndices)
                {
                    if (idx < 0 || idx >= pairs.Count || pairs[idx] == null) continue;
                    pairs[idx].IsLocked = true;
                    rt.HiddenPairs.Add(pairs[idx]);
                }
            }

            SetState(rt, false, false);
            _items.Add(rt);
        }
    }

    public void Clear()
    {
        foreach (var rt in _items)
        {
            KillTweens(rt.Closed);
            KillTweens(rt.Open);
        }
        _items.Clear();

        foreach (var go in _containers)
            if (go != null) Destroy(go);
        _containers.Clear();
    }

    /// Bấm vào vùng hotspot của đồ vật đang đóng → mở ra. Trả về true nếu đã xử lý click.
    public bool TryInteract(Vector2 screenPoint, Camera cam)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            var rt = _items[i];
            if (rt.IsOpen) continue;
            if (!IsInHotspot(rt.Info, _rootA, screenPoint, cam) &&
                !IsInHotspot(rt.Info, _rootB, screenPoint, cam)) continue;

            SetState(rt, true, true);
            return true;
        }
        return false;
    }

    private void SetState(Runtime rt, bool open, bool animate)
    {
        rt.IsOpen = open;
        foreach (var pair in rt.HiddenPairs)
            if (pair != null) pair.IsLocked = !open;

        KillTweens(rt.Closed);
        KillTweens(rt.Open);

        foreach (var img in rt.Closed) Show(img, !open, animate);
        foreach (var img in rt.Open) Show(img, open, animate);
    }

    private static void Show(Image img, bool visible, bool animate)
    {
        if (img == null) return;
        img.transform.localScale = Vector3.one;
        img.color = Color.white;

        if (!visible)
        {
            if (!animate || !img.gameObject.activeSelf)
            {
                img.gameObject.SetActive(false);
                return;
            }
            // Layer trạng thái đóng (nắp, cửa, dáng cũ của nhân vật) mờ dần rồi tắt.
            img.DOFade(0f, OpenDuration * 0.8f).SetUpdate(true)
               .OnComplete(() => { if (img != null) img.gameObject.SetActive(false); });
            img.transform.DOScale(1.04f, OpenDuration * 0.8f).SetUpdate(true);
            return;
        }

        img.gameObject.SetActive(true);
        if (!animate) return;

        img.color = new Color(1f, 1f, 1f, 0f);
        img.DOFade(1f, OpenDuration).SetUpdate(true);
        img.transform.localScale = new Vector3(0.85f, 1f, 1f);
        img.transform.DOScale(Vector3.one, OpenDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private static void KillTweens(List<Image> images)
    {
        foreach (var img in images)
        {
            if (img == null) continue;
            img.DOKill();
            img.transform.DOKill();
        }
    }

    private Transform CreateContainer(RectTransform root)
    {
        var go = new GameObject("Interactables", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(root, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        // Đặt dưới outline/mark/hint (các con khác của root).
        rt.SetAsFirstSibling();
        _containers.Add(go);
        return rt;
    }

    private static void CreateLayers(InteractableLayer[] layers, Transform container, RectTransform root, List<Image> output)
    {
        if (layers == null) return;
        Vector2 scale = GetScale(root);

        foreach (var layer in layers)
        {
            if (layer == null || layer.Sprite == null) continue;

            var go = new GameObject(layer.Sprite.name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(container, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.Scale(layer.Position, scale);
            rt.sizeDelta = Vector2.Scale(layer.Size, scale);

            var img = go.GetComponent<Image>();
            img.sprite = layer.Sprite;
            img.raycastTarget = false;
            output.Add(img);
        }
    }

    private static bool IsInHotspot(InteractableInfo info, RectTransform root, Vector2 screenPoint, Camera cam)
    {
        if (root == null) return false;

        // Chỉ nhận click trong khung ảnh đang hiển thị (khi zoom có thể tràn ra ngoài).
        if (root.parent is RectTransform frame &&
            !RectTransformUtility.RectangleContainsScreenPoint(frame, screenPoint, cam))
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPoint, cam, out Vector2 local))
            return false;

        Vector2 scale = GetScale(root);
        Vector2 d = local - Vector2.Scale(info.HotspotPosition, scale);
        Vector2 half = Vector2.Scale(info.HotspotSize, scale) * 0.5f;
        return Mathf.Abs(d.x) <= half.x && Mathf.Abs(d.y) <= half.y;
    }

    private static Vector2 GetScale(RectTransform root)
    {
        Vector2 rect = root != null ? root.rect.size : DesignSize;
        if (rect.x < 1f || rect.y < 1f) rect = DesignSize;
        return new Vector2(rect.x / DesignSize.x, rect.y / DesignSize.y);
    }
}
