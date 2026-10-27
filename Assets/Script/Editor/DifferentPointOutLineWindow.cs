#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class DifferentPointOutLineWindow : EditorWindow
{
    private LevelData level;
    private Vector2 designSize = new Vector2(1040f, 650f);
    private Vector2 pivot = new Vector2(0.5f, 0.5f);
    private bool outlineRelativeToPosition = false; // true: Outline tính theo gốc Position

    private readonly List<DifferentPointInfo> shapes = new List<DifferentPointInfo>(); // lưu toạ độ tuyệt đối
    private readonly List<Vector3> current = new List<Vector3>();
    private int selected = -1;
    private Vector2 listScroll;

    private const float SidebarWidth = 280f;
    private static readonly Color CurrentColor = Color.yellow;
    private static readonly Color ShapeColor = new Color(0.2f, 1f, 0.3f, 1f);
    private static readonly Color SelectedColor = new Color(1f, 0.3f, 0.3f, 1f);

    [MenuItem("Tools/Different Point Editor")]
    public static void Open()
    {
        var w = GetWindow<DifferentPointOutLineWindow>("Different Point Editor");
        w.minSize = new Vector2(900f, 500f);
    }

    // ------------------------------------------------------------------ GUI

    private void OnGUI()
    {
        DrawToolbar();

        if (level == null)
        {
            EditorGUILayout.HelpBox("Kéo một LevelData vào ô phía trên.", MessageType.Info);
            return;
        }

        Rect body = new Rect(0f, GetToolbarHeight(), position.width, position.height - GetToolbarHeight());
        Rect imagesArea = new Rect(body.x + 4f, body.y, body.width - SidebarWidth - 12f, body.height);
        Rect sidebar = new Rect(body.xMax - SidebarWidth - 4f, body.y, SidebarWidth, body.height);

        DrawImages(imagesArea);
        DrawSidebar(sidebar);

        HandleKeyboard();
    }

    private float GetToolbarHeight() => 110f;

    private void DrawToolbar()
    {
        GUILayout.BeginArea(new Rect(4f, 4f, position.width - 8f, GetToolbarHeight() - 4f));

        EditorGUI.BeginChangeCheck();
        var newLevel = (LevelData)EditorGUILayout.ObjectField("Level Data", level, typeof(LevelData), false);
        if (EditorGUI.EndChangeCheck() && newLevel != level)
        {
            level = newLevel;
            LoadFromLevel();
        }

        designSize = EditorGUILayout.Vector2Field("Design Size", designSize);
        pivot = EditorGUILayout.Vector2Field("Pivot (0.5,0.5 = tâm)", pivot);
        outlineRelativeToPosition = EditorGUILayout.ToggleLeft(
            "Outline tính tương đối theo Position (mặc định: toạ độ trong root, giống Position)",
            outlineRelativeToPosition);

        GUILayout.EndArea();
    }

    private void DrawImages(Rect area)
    {
        float cellW = (area.width - 8f) * 0.5f;
        const float labelH = 20f;
        float cellH = area.height - labelH - 4f;

        float aspect = designSize.x / Mathf.Max(1f, designSize.y);
        float w = cellW, h = cellW / aspect;
        if (h > cellH) { h = cellH; w = h * aspect; }

        Rect rawRect = new Rect(area.x + (cellW - w) * 0.5f, area.y + labelH, w, h);
        Rect errRect = new Rect(area.x + cellW + 8f + (cellW - w) * 0.5f, area.y + labelH, w, h);

        GUI.Label(new Rect(rawRect.x, area.y, w, labelH), "RAW (ảnh gốc)", EditorStyles.boldLabel);
        GUI.Label(new Rect(errRect.x, area.y, w, labelH), "ERROR (ảnh lỗi)", EditorStyles.boldLabel);

        DrawSprite(rawRect, level.RawSprite);
        DrawSprite(errRect, level.ErrorSprite);

        DrawOverlay(rawRect);
        DrawOverlay(errRect);

        HandleMouse(rawRect);
        HandleMouse(errRect);
    }

    private void DrawSprite(Rect r, Sprite s)
    {
        EditorGUI.DrawRect(r, new Color(0.15f, 0.15f, 0.15f));
        if (s == null || s.texture == null) return;

        Texture2D t = s.texture;
        Rect tr = s.textureRect;
        Rect uv = new Rect(tr.x / t.width, tr.y / t.height, tr.width / t.width, tr.height / t.height);
        GUI.DrawTextureWithTexCoords(r, t, uv);
    }

    private void DrawOverlay(Rect r)
    {
        for (int i = 0; i < shapes.Count; i++)
        {
            DrawPolyline(r, shapes[i].Outline, true, i == selected ? SelectedColor : ShapeColor, i == selected ? 3f : 2f);
        }

        if (current.Count > 0)
        {
            DrawPolyline(r, current.ToArray(), false, CurrentColor, 2f);
            Handles.color = CurrentColor;
            foreach (var p in current)
                Handles.DrawSolidDisc(ToGui(p, r), Vector3.forward, 3.5f);
        }
    }

    private void DrawPolyline(Rect r, Vector3[] pts, bool loop, Color c, float width)
    {
        if (pts == null || pts.Length < 2) return;
        int n = pts.Length + (loop ? 1 : 0);
        var gui = new Vector3[n];
        for (int i = 0; i < n; i++)
            gui[i] = ToGui(pts[i % pts.Length], r);
        Handles.color = c;
        Handles.DrawAAPolyLine(width, gui);
    }

    private void DrawSidebar(Rect r)
    {
        GUILayout.BeginArea(r, EditorStyles.helpBox);

        EditorGUILayout.LabelField("Đang vẽ: " + current.Count + " điểm", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Click trái: thêm điểm\nClick phải / Backspace: xóa điểm cuối\nEnter: hoàn tất shape\nEsc: hủy shape đang vẽ",
            MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = current.Count >= 3;
            if (GUILayout.Button("Finish Shape")) FinishShape();
            GUI.enabled = current.Count > 0;
            if (GUILayout.Button("Cancel")) { current.Clear(); Repaint(); }
            GUI.enabled = true;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Different points: " + shapes.Count, EditorStyles.boldLabel);

        listScroll = EditorGUILayout.BeginScrollView(listScroll);
        for (int i = 0; i < shapes.Count; i++)
        {
            var s = shapes[i];
            using (new EditorGUILayout.HorizontalScope())
            {
                bool sel = i == selected;
                if (GUILayout.Toggle(sel, $"#{i + 1}  pts:{(s.Outline?.Length ?? 0)}", EditorStyles.miniButton))
                    selected = i;
                else if (sel) selected = -1;

                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    shapes.RemoveAt(i);
                    if (selected == i) selected = -1;
                    GUIUtility.ExitGUI();
                }
            }

            if (i == selected)
            {
                EditorGUI.BeginChangeCheck();
                Vector2 pos = EditorGUILayout.Vector2Field("Position", s.Position);
                Vector2 size = EditorGUILayout.Vector2Field("Size", s.Size);
                if (EditorGUI.EndChangeCheck())
                {
                    s.Position = new Vector3(pos.x, pos.y, 0f);
                    s.Size = size;
                }
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reload From Level")) LoadFromLevel();
            if (GUILayout.Button("Clear All"))
            {
                if (EditorUtility.DisplayDialog("Clear", "Xóa hết các different point (chưa save)?", "Xóa", "Hủy"))
                {
                    shapes.Clear(); current.Clear(); selected = -1;
                }
            }
        }

        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("Save To LevelData", GUILayout.Height(30f))) SaveToLevel();
        GUI.backgroundColor = Color.white;

        GUILayout.EndArea();
    }

    // -------------------------------------------------------------- Input

    private void HandleMouse(Rect imageRect)
    {
        Event e = Event.current;
        if (e.type != EventType.MouseDown || !imageRect.Contains(e.mousePosition)) return;

        if (e.button == 0)
        {
            current.Add(ToDesign(e.mousePosition, imageRect));
            e.Use();
            Repaint();
        }
        else if (e.button == 1)
        {
            if (current.Count > 0) current.RemoveAt(current.Count - 1);
            e.Use();
            Repaint();
        }
    }

    private void HandleKeyboard()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown) return;

        switch (e.keyCode)
        {
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                if (current.Count >= 3) FinishShape();
                e.Use();
                break;
            case KeyCode.Backspace:
                if (current.Count > 0) current.RemoveAt(current.Count - 1);
                e.Use(); Repaint();
                break;
            case KeyCode.Escape:
                current.Clear();
                e.Use(); Repaint();
                break;
        }
    }

    // -------------------------------------------------------- Coordinates

    // GUI (pixel trong rect ảnh) -> toạ độ thiết kế (Y hướng lên)
    private Vector3 ToDesign(Vector2 mouse, Rect r)
    {
        float nx = (mouse.x - r.x) / r.width;
        float ny = 1f - (mouse.y - r.y) / r.height;
        return new Vector3((nx - pivot.x) * designSize.x, (ny - pivot.y) * designSize.y, 0f);
    }

    private Vector3 ToGui(Vector3 p, Rect r)
    {
        float nx = p.x / designSize.x + pivot.x;
        float ny = p.y / designSize.y + pivot.y;
        return new Vector3(r.x + nx * r.width, r.y + (1f - ny) * r.height, 0f);
    }

    // ---------------------------------------------------------- Data

    private void FinishShape()
    {
        if (current.Count < 3) return;

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (var p in current)
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        Vector2 center = (min + max) * 0.5f;
        shapes.Add(new DifferentPointInfo
        {
            Position = new Vector3(center.x, center.y, 0f),
            Size = max - min,
            Outline = current.ToArray()
        });

        selected = shapes.Count - 1;
        current.Clear();
        Repaint();
    }

    private void LoadFromLevel()
    {
        shapes.Clear();
        current.Clear();
        selected = -1;
        if (level == null || level.DifferentPoints == null) return;

        foreach (var d in level.DifferentPoints)
        {
            if (d == null) continue;
            Vector3[] outline = d.Outline != null ? (Vector3[])d.Outline.Clone() : new Vector3[0];

            if (outlineRelativeToPosition)
                for (int i = 0; i < outline.Length; i++)
                    outline[i] += new Vector3(d.Position.x, d.Position.y, 0f);

            shapes.Add(new DifferentPointInfo { Position = d.Position, Size = d.Size, Outline = outline });
        }
    }

    private void SaveToLevel()
    {
        if (level == null) return;

        var result = new List<DifferentPointInfo>(shapes.Count);
        foreach (var s in shapes)
        {
            Vector3[] outline = s.Outline != null ? (Vector3[])s.Outline.Clone() : new Vector3[0];

            if (outlineRelativeToPosition)
                for (int i = 0; i < outline.Length; i++)
                    outline[i] -= new Vector3(s.Position.x, s.Position.y, 0f);

            result.Add(new DifferentPointInfo { Position = s.Position, Size = s.Size, Outline = outline });
        }

        Undo.RecordObject(level, "Save Different Points");
        level.SetDifferentPoints(result); // tự cập nhật countDifferent + SetDirty
        AssetDatabase.SaveAssets();
        Debug.Log($"[DifferentPointEditor] Saved {result.Count} points to {level.name}", level);
    }
}
#endif