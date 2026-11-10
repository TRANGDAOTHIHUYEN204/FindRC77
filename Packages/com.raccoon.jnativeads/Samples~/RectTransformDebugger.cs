using UnityEngine;

namespace JKit.Monetize.Ads
{
    /// <summary>
    /// Component để debug vị trí RectTransform và kiểm tra ads placement
    /// Attach vào GameObject có RectTransform mà bạn dùng cho ads
    /// </summary>
    public class RectTransformDebugger : MonoBehaviour
    {
        public RectTransform targetRectTransform;
        public bool logOnStart = true;
        public bool drawGizmos = true;
        
        void Start()
        {
            if (targetRectTransform == null)
            {
                targetRectTransform = GetComponent<RectTransform>();
            }
            
            if (logOnStart)
            {
                LogRectTransformInfo();
            }
        }
        
        [ContextMenu("Log RectTransform Info")]
        public void LogRectTransformInfo()
        {
            if (targetRectTransform == null)
            {
                Debug.LogError("[RectTransformDebugger] No RectTransform assigned!");
                return;
            }
            
            Debug.Log("===== RECT TRANSFORM DEBUG INFO =====");
            
            // Screen info
            Debug.Log($"Screen Size: {Screen.width} x {Screen.height}");
            Debug.Log($"Screen Safe Area: {Screen.safeArea}");
            
            // RectTransform basic info
            Debug.Log($"GameObject: {targetRectTransform.gameObject.name}");
            Debug.Log($"AnchoredPosition: {targetRectTransform.anchoredPosition}");
            Debug.Log($"SizeDelta: {targetRectTransform.sizeDelta}");
            Debug.Log($"Pivot: {targetRectTransform.pivot}");
            Debug.Log($"AnchorMin: {targetRectTransform.anchorMin}");
            Debug.Log($"AnchorMax: {targetRectTransform.anchorMax}");
            
            // World corners (screen space pixels)
            var corners = new Vector3[4];
            targetRectTransform.GetWorldCorners(corners);
            
            Debug.Log($"--- World Corners (Screen Space) ---");
            Debug.Log($"  Bottom-Left  [0]: {corners[0]}");
            Debug.Log($"  Top-Left     [1]: {corners[1]}");
            Debug.Log($"  Top-Right    [2]: {corners[2]}");
            Debug.Log($"  Bottom-Right [3]: {corners[3]}");
            
            // Calculated position and size (as used in NativeOverlay)
            var x = corners[0].x;
            var y = corners[0].y;
            var width = corners[2].x - corners[0].x;
            var height = corners[2].y - corners[0].y;
            
            Debug.Log($"--- Calculated for Native (Unity coords) ---");
            Debug.Log($"  X: {x}");
            Debug.Log($"  Y: {y}");
            Debug.Log($"  Width: {width}");
            Debug.Log($"  Height: {height}");
            
            // After conversion to iOS coordinates (what gets sent to native)
            var iosY = Screen.height - y - height;
            
            Debug.Log($"--- After iOS Conversion ---");
            Debug.Log($"  X: {x}");
            Debug.Log($"  Y: {iosY} (converted from Unity {y})");
            Debug.Log($"  Width: {width}");
            Debug.Log($"  Height: {height}");
            
            // Check if out of bounds
            bool outOfBounds = false;
            if (x < 0 || x > Screen.width)
            {
                Debug.LogWarning($"⚠️ X ({x}) is out of screen bounds!");
                outOfBounds = true;
            }
            if (iosY < 0 || iosY > Screen.height)
            {
                Debug.LogWarning($"⚠️ iOS Y ({iosY}) is out of screen bounds!");
                outOfBounds = true;
            }
            if (x + width > Screen.width)
            {
                Debug.LogWarning($"⚠️ Rect extends beyond right edge ({x + width} > {Screen.width})!");
                outOfBounds = true;
            }
            if (iosY + height > Screen.height)
            {
                Debug.LogWarning($"⚠️ Rect extends beyond bottom edge (iOS: {iosY + height} > {Screen.height})!");
                outOfBounds = true;
            }
            
            if (!outOfBounds)
            {
                Debug.Log("✅ Rect is within screen bounds");
            }
            
            // Check if too small
            if (width < 50 || height < 50)
            {
                Debug.LogWarning($"⚠️ Rect is very small ({width}x{height}) - ads might not render properly!");
            }
            
            // Check visibility
            if (!targetRectTransform.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("⚠️ GameObject is not active in hierarchy!");
            }
            
            var canvas = targetRectTransform.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"Canvas: {canvas.name}, RenderMode: {canvas.renderMode}");
            }
            else
            {
                Debug.LogWarning("⚠️ No Canvas found in parent!");
            }
            
            Debug.Log("====================================");
        }
        
        void OnDrawGizmos()
        {
            if (!drawGizmos || targetRectTransform == null) return;
            
            var corners = new Vector3[4];
            targetRectTransform.GetWorldCorners(corners);
            
            // Draw rect in scene view
            Gizmos.color = Color.yellow;
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }
            
            // Draw X at center
            var center = (corners[0] + corners[2]) / 2f;
            Gizmos.DrawLine(center + Vector3.left * 10, center + Vector3.right * 10);
            Gizmos.DrawLine(center + Vector3.down * 10, center + Vector3.up * 10);
        }
    }
}

