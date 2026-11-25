using UnityEngine;

/// Một layer sprite đặt đè lên ảnh level (cả ảnh A và B).
/// Toạ độ theo không gian thiết kế 1080x720, gốc ở tâm ảnh, trục y hướng lên
/// (giống DifferentPointInfo).
[System.Serializable]
public class InteractableLayer
{
    public Sprite Sprite;
    public Vector2 Position;
    public Vector2 Size;
}

/// Đồ vật bấm vào được (vd: tủ quần áo). Lúc đóng hiện ClosedLayers,
/// bấm vào HotspotPosition/HotspotSize thì chuyển sang OpenLayers và mở khoá
/// các điểm khác trong HiddenPointIndices (index trong LevelData.DifferentPoints).
[System.Serializable]
public class InteractableInfo
{
    public string Name;
    public Vector2 HotspotPosition;
    public Vector2 HotspotSize;
    public InteractableLayer[] ClosedLayers;
    public InteractableLayer[] OpenLayers;
    public int[] HiddenPointIndices;
}
