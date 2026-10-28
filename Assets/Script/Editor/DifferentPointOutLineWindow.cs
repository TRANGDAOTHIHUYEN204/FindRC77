#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tool click vào ảnh để nạp Outline vào đúng từng Different Point của LevelData.
///
/// Cách dùng:
///  1. Chọn level ở panel trái.
///  2. Tạo point mới (nút "+ New Point" / Enter) hoặc chọn một Element có sẵn trong list bên phải
///     (hoặc Ctrl+Click vào vị trí point trên ảnh). Element đang chọn = ACTIVE POINT (màu đỏ).
///  3. Chế độ Outline: mỗi click trái trên ảnh = thêm 1 đỉnh vào Outline của ACTIVE POINT.
///     Position/Size của point tự tính theo bounding box của outline.
///  4. Chế độ Mark Center: click 1 điểm = tâm vẽ mark của ACTIVE POINT (chấm tím).
///  5. Xong point này -> Enter (hoặc "+ New Point") để sang point kế tiếp. Cuối cùng bấm Save.
///  Chế độ Center: mỗi click = 1 point mới (Position = chỗ click, Size = Point Size, Outline tuỳ chọn).
/// </summary>
public class DifferentPointOutLineWindow : EditorWindow
{
    private enum ClickMode { Outline, Center, MarkCenter }
    private enum PointShape { Ellipse, Rectangle }

    private class LevelState
    {
        public readonly List<DifferentPointInfo> shapes = new List<DifferentPointInfo>(); // toạ độ tuyệt đối
        public bool dirty;
    }

    // ---- danh sách level (serialize để không mất khi recompile) ----
    [SerializeField] private List<LevelData> levels = new List<LevelData>();
    [SerializeField] private int levelIndex = -1;

    // ---- cấu hình ----
    [SerializeField] private Vector2 designSize = new Vector2(1080f, 720f);
    [SerializeField] private Vector2 pivot = new Vector2(0.5f, 0.5f);
    [SerializeField] private bool outlineRelativeToPosition = false; // true: Outline tính theo gốc Position
    [SerializeField] private ClickMode clickMode = ClickMode.Outline;
    [SerializeField] private bool autoBounds = true;                 // tự tính Position/Size từ Outline
    [SerializeField] private Vector2 pointSize = new Vector2(80f, 80f);
    [SerializeField] private PointShape pointShape = PointShape.Ellipse;
    [SerializeField] private bool generateOutline = true;            // Center mode: có sinh Outline không
    [SerializeField] private int ellipseSegments = 16;

    // ---- trạng thái ----
    private readonly Dictionary<LevelData, LevelState> states = new Dictionary<LevelData, LevelState>();
    private int selected = -1;               // ACTIVE POINT
    private Vector2 listScroll, levelScroll;

    private bool hasHover;
    private Vector3 hoverDesign;
    private bool hasLastClick;
    private Vector3 lastClick;

    private int dragVertex = -1;             // đang kéo đỉnh outline của active point
    private Rect dragRect;

    private const float ToolbarHeight = 150f;
    private const float LevelPanelWidth = 210f;
    private const float SidebarWidth = 320f;
    private const float VertexHitRadius = 9f;

    private static readonly Color ShapeColor = new Color(0.2f, 1f, 0.3f, 1f);
    private static readonly Color SelectedColor = new Color(1f, 0.3f, 0.3f, 1f);
    private static readonly Color CrossColor = new Color(0f, 0.8f, 1f, 0.9f);
    private static readonly Color MarkCenterColor = Color.magenta;

    private LevelData Level => (levelIndex >= 0 && levelIndex < levels.Count) ? levels[levelIndex] : null;

    private DifferentPointInfo ActiveShape()
    {
        if (Level == null) return null;
        var shapes = GetState(Level).shapes;
        return (selected >= 0 && selected < shapes.Count) ? shapes[selected] : null;
    }

    [MenuItem("Tools/Different Point Editor")]
    public static void Open()
    {
        var w = GetWindow<DifferentPointOutLineWindow>("Different Point Editor");
        w.minSize = new Vector2(1200f, 620f);
        w.wantsMouseMove = true;
    }

    private void OnEnable() { wantsMouseMove = true; }

    private void OnDestroy()
    {
        bool anyDirty = false;
        foreach (var kv in states) if (kv.Value.dirty) { anyDirty = true; break; }
        if (!anyDirty) return;

        if (EditorUtility.DisplayDialog("Different Point Editor",
                "Còn level chưa save. Save tất cả trước khi đóng?", "Save", "Bỏ qua"))
            SaveAll();
    }

    // ------------------------------------------------------------------ GUI

    private void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.MouseMove) hasHover = false;
        if (e.rawType == EventType.MouseUp) dragVertex = -1;

        DrawToolbar();

        Rect body = new Rect(0f, ToolbarHeight, position.width, position.height - ToolbarHeight);
        Rect levelPanel = new Rect(body.x + 4f, body.y, LevelPanelWidth, body.height - 4f);
        Rect sidebar = new Rect(body.xMax - SidebarWidth - 4f, body.y, SidebarWidth, body.height - 4f);
        Rect imagesArea = new Rect(levelPanel.xMax + 8f, body.y,
            sidebar.x - levelPanel.xMax - 16f, body.height);

        DrawLevelPanel(levelPanel);

        if (Level == null)
        {
            GUI.Label(imagesArea, "Thêm LevelData vào danh sách bên trái (kéo thả hoặc nút Scan) rồi chọn 1 level.",
                EditorStyles.wordWrappedLabel);
            HandleDragDrop(levelPanel);
            return;
        }

        DrawImages(imagesArea);
        DrawSidebar(sidebar);
        HandleDragDrop(levelPanel);
        HandleKeyboard();

        if (e.type == EventType.MouseMove) Repaint();
    }

    private void DrawToolbar()
    {
        GUILayout.BeginArea(new Rect(4f, 4f, position.width - 8f, ToolbarHeight - 4f));

        clickMode = (ClickMode)GUILayout.Toolbar((int)clickMode,
            new[]
            {
                "Outline: click = thêm đỉnh",
                "Center: click = tạo point mới",
                "Mark Center: click = đặt tâm mark"
            },
            GUILayout.Height(24f));

        designSize = EditorGUILayout.Vector2Field("Design Size", designSize);
        pivot = EditorGUILayout.Vector2Field("Pivot (0.5,0.5 = tâm)", pivot);

        using (new EditorGUILayout.HorizontalScope())
        {
            pointSize = EditorGUILayout.Vector2Field("Point Size", pointSize);
            if (clickMode == ClickMode.Center)
            {
                pointShape = (PointShape)EditorGUILayout.EnumPopup(pointShape, GUILayout.Width(80f));
                generateOutline = EditorGUILayout.ToggleLeft("Outline", generateOutline, GUILayout.Width(65f));
            }
        }

        autoBounds = EditorGUILayout.ToggleLeft(
            "Tự tính Position/Size theo Outline (bounding box) mỗi khi thêm/kéo đỉnh", autoBounds);
        outlineRelativeToPosition = EditorGUILayout.ToggleLeft(
            "Outline lưu tương đối theo Position (mặc định: toạ độ tuyệt đối trong root)",
            outlineRelativeToPosition);

        GUILayout.EndArea();
    }

    // ---------------------------------------------------------- Level panel

    private void DrawLevelPanel(Rect r)
    {
        GUILayout.BeginArea(r, EditorStyles.helpBox);

        EditorGUILayout.LabelField("Levels (" + levels.Count + ")", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Selected")) AddFromSelection();
            if (GUILayout.Button("Scan Project")) ScanProject();
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Remove"))
            {
                if (Level != null)
                {
                    states.Remove(Level);
                    levels.RemoveAt(levelIndex);
                    levelIndex = Mathf.Min(levelIndex, levels.Count - 1);
                    ResetEditingState();
                }
            }
            if (GUILayout.Button("Clear List"))
            {
                levels.Clear(); states.Clear(); levelIndex = -1; ResetEditingState();
            }
        }

        levelScroll = EditorGUILayout.BeginScrollView(levelScroll);
        for (int i = 0; i < levels.Count; i++)
        {
            var lv = levels[i];
            if (lv == null) continue;
            var st = GetState(lv);

            Color old = GUI.backgroundColor;
            if (i == levelIndex) GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
            string label = (st.dirty ? "* " : "") + lv.name + "  (" + st.shapes.Count + ")";
            if (GUILayout.Button(label, EditorStyles.miniButton)) SelectLevel(i);
            GUI.backgroundColor = old;
        }
        EditorGUILayout.EndScrollView();

        GUILayout.EndArea();
    }

    private void HandleDragDrop(Rect dropArea)
    {
        Event e = Event.current;
        if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;
        if (!dropArea.Contains(e.mousePosition)) return;

        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (var o in DragAndDrop.objectReferences)
                if (o is LevelData lv) AddLevel(lv);
        }
        e.Use();
    }

    private void AddFromSelection()
    {
        foreach (var o in Selection.objects)
            if (o is LevelData lv) AddLevel(lv);
    }

    private void ScanProject()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:LevelData"))
        {
            var lv = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
            if (lv != null) AddLevel(lv);
        }
        levels.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
    }

    private void AddLevel(LevelData lv)
    {
        if (lv == null || levels.Contains(lv)) return;
        levels.Add(lv);
        if (levelIndex < 0) SelectLevel(levels.Count - 1);
        Repaint();
    }

    private void SelectLevel(int i)
    {
        levelIndex = i;
        ResetEditingState();
        Repaint();
    }

    private void ResetEditingState()
    {
        selected = -1;
        dragVertex = -1;
        hasLastClick = false;
    }

    // ---------------------------------------------------------------- Images

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

        DrawSprite(rawRect, Level.RawSprite);
        DrawSprite(errRect, Level.ErrorSprite);

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
        if (Event.current.type != EventType.Repaint) return;

        var shapes = GetState(Level).shapes;
        for (int i = 0; i < shapes.Count; i++)
        {
            var s = shapes[i];
            bool sel = i == selected;
            Color c = sel ? SelectedColor : ShapeColor;

            if (s.Outline != null && s.Outline.Length >= 2)
                DrawPolyline(r, s.Outline, true, c, sel ? 3f : 2f);
            else
            {
                // chưa có outline: vẽ khung theo Size
                Vector3 ps = s.Position;
                Vector2 hs = s.Size * 0.5f;
                DrawPolyline(r, new[]
                {
                    new Vector3(ps.x - hs.x, ps.y - hs.y, 0f), new Vector3(ps.x - hs.x, ps.y + hs.y, 0f),
                    new Vector3(ps.x + hs.x, ps.y + hs.y, 0f), new Vector3(ps.x + hs.x, ps.y - hs.y, 0f)
                }, true, c, sel ? 3f : 1.5f);
            }

            Vector3 gp = ToGui(s.Position, r);
            Handles.color = c;
            Handles.DrawSolidDisc(gp, Vector3.forward, 2.5f);
            Handles.Label(gp + new Vector3(4f, -16f, 0f), "Element " + i);

            // tâm mark (set tay)
            if (s.HasMarkCenter)
            {
                Vector3 mp = ToGui(s.MarkCenter, r);
                Handles.color = MarkCenterColor;
                Handles.DrawSolidDisc(mp, Vector3.forward, 4f);
                Handles.DrawLine(mp + new Vector3(-8f, 0f, 0f), mp + new Vector3(8f, 0f, 0f));
                Handles.DrawLine(mp + new Vector3(0f, -8f, 0f), mp + new Vector3(0f, 8f, 0f));
            }

            // active point: vẽ từng đỉnh + số thứ tự đỉnh
            if (sel && s.Outline != null)
            {
                for (int j = 0; j < s.Outline.Length; j++)
                {
                    Vector3 vp = ToGui(s.Outline[j], r);
                    Handles.color = Color.yellow;
                    Handles.DrawSolidDisc(vp, Vector3.forward, 4f);
                    Handles.Label(vp + new Vector3(5f, 0f, 0f), j.ToString());
                }
            }
        }

        // crosshair đồng bộ ở cả 2 ảnh
        if (hasHover)
        {
            Vector3 g = ToGui(hoverDesign, r);
            Handles.color = CrossColor;
            Handles.DrawLine(new Vector3(r.x, g.y), new Vector3(r.xMax, g.y));
            Handles.DrawLine(new Vector3(g.x, r.y), new Vector3(g.x, r.yMax));
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

    // --------------------------------------------------------------- Sidebar

    private void DrawSidebar(Rect r)
    {
        GUILayout.BeginArea(r, EditorStyles.helpBox);
        var st = GetState(Level);

        EditorGUILayout.LabelField(Level.name + (st.dirty ? "  (chưa save)" : ""), EditorStyles.boldLabel);

        // POINT ĐANG CHỌN
        var active = ActiveShape();
        Color old = GUI.color;
        GUI.color = active != null ? new Color(1f, 0.6f, 0.6f) : Color.yellow;
        EditorGUILayout.LabelField(active != null
            ? $"POINT ĐANG CHỌN: Element {selected}  ({active.Outline?.Length ?? 0} đỉnh)"
            : "Chưa chọn point nào - click ảnh sẽ tự tạo point mới", EditorStyles.boldLabel);
        GUI.color = old;

        EditorGUILayout.LabelField("Hover : " + (hasHover ? Fmt(hoverDesign) : "-") +
                                   "     Click : " + (hasLastClick ? Fmt(lastClick) : "-"));

        string help;
        switch (clickMode)
        {
            case ClickMode.Outline:
                help = "Click trái: thêm đỉnh vào point đang chọn (kéo đỉnh vàng để chỉnh)\nClick phải / Backspace: xóa đỉnh cuối\nEnter: point mới   |   Esc: bỏ chọn\nCtrl+Click: chọn point tại vị trí click\nDelete: xóa point đang chọn";
                break;
            case ClickMode.MarkCenter:
                help = "Click trái: đặt tâm mark (chấm tím) cho point đang chọn\n(chưa chọn point thì tự chọn point gần chỗ click)\nClick phải / Backspace: xóa tâm mark (dùng lại tâm tự động)\nCtrl+Click: chọn point   |   Delete: xóa point đang chọn";
                break;
            default:
                help = "Click trái: tạo point mới tại chỗ click\nClick phải / Backspace: xóa point vừa tạo\nCtrl+Click: chọn point   |   Delete: xóa point đang chọn";
                break;
        }
        EditorGUILayout.HelpBox(help, MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+ New Point", GUILayout.Height(24f))) { NewPoint(); }
            GUI.enabled = active != null;
            if (GUILayout.Button("Undo Đỉnh", GUILayout.Height(24f))) { UndoVertex(); }
            if (GUILayout.Button("Xóa Outline", GUILayout.Height(24f))) { ClearActiveOutline(); }
            if (GUILayout.Button("Xóa Point", GUILayout.Height(24f))) { DeleteSelected(); }
            GUI.enabled = true;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Different points: " + st.shapes.Count, EditorStyles.boldLabel);

        listScroll = EditorGUILayout.BeginScrollView(listScroll);
        DrawShapeList(st);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = st.shapes.Count > 0;
            if (GUILayout.Button("Xóa hết point level này")) ClearCurrentLevel();
            GUI.enabled = true;
            if (GUILayout.Button("Xóa hết (mọi level)")) ClearAllLevels();
        }

        if (GUILayout.Button("Reload From Level"))
        {
            if (!st.dirty || EditorUtility.DisplayDialog("Reload", "Bỏ thay đổi chưa save của level này?", "Reload", "Hủy"))
            {
                states[Level] = LoadState(Level);
                ResetEditingState();
            }
        }

        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
        if (GUILayout.Button("Save Level Này → LevelData", GUILayout.Height(28f))) SaveLevel(Level);
        if (GUILayout.Button("Save Tất Cả Level (đã sửa)", GUILayout.Height(24f))) SaveAll();
        GUI.backgroundColor = Color.white;

        GUILayout.EndArea();
    }

    // Danh sách từng điểm, giống Inspector: Element N -> Position / Size / Mark Center / Outline
    private void DrawShapeList(LevelState st)
    {
        float oldLabel = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 70f;

        for (int i = 0; i < st.shapes.Count; i++)
        {
            var s = st.shapes[i];
            bool sel = i == selected;

            using (new EditorGUILayout.HorizontalScope())
            {
                bool open = EditorGUILayout.Foldout(sel,
                    $"Element {i}   ({s.Position.x:0.#}, {s.Position.y:0.#})   outline: {(s.Outline?.Length ?? 0)}",
                    true);
                if (open != sel) selected = open ? i : -1;

                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    st.shapes.RemoveAt(i);
                    if (selected == i) selected = -1;
                    else if (selected > i) selected--;
                    st.dirty = true;
                    GUIUtility.ExitGUI();
                }
            }

            if (i != selected) continue;

            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            Vector2 pos = EditorGUILayout.Vector2Field("Position", s.Position);
            Vector2 size = EditorGUILayout.Vector2Field("Size", s.Size);
            if (EditorGUI.EndChangeCheck())
            {
                MoveResizeShape(s, pos, size); // outline dịch/scale theo
                st.dirty = true;
            }

            // Mark Center
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                bool has = EditorGUILayout.ToggleLeft("Mark Center", s.HasMarkCenter, GUILayout.Width(110f));
                Vector2 mc = s.MarkCenter;
                GUI.enabled = has;
                mc = EditorGUILayout.Vector2Field(GUIContent.none, mc);
                GUI.enabled = true;
                if (EditorGUI.EndChangeCheck())
                {
                    s.HasMarkCenter = has;
                    s.MarkCenter = new Vector3(mc.x, mc.y, 0f);
                    st.dirty = true;
                }
            }

            int n = s.Outline?.Length ?? 0;
            EditorGUILayout.LabelField("Outline", n == 0 ? "List is empty" : n + " đỉnh");

            for (int j = 0; j < n; j++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    Vector2 v = EditorGUILayout.Vector2Field("Element " + j, s.Outline[j]);
                    if (EditorGUI.EndChangeCheck())
                    {
                        s.Outline[j] = new Vector3(v.x, v.y, 0f);
                        RecalcBounds(s);
                        st.dirty = true;
                    }
                    if (GUILayout.Button("-", GUILayout.Width(20f)))
                    {
                        var list = new List<Vector3>(s.Outline);
                        list.RemoveAt(j);
                        s.Outline = list.ToArray();
                        RecalcBounds(s);
                        st.dirty = true;
                        GUIUtility.ExitGUI();
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sinh outline từ Size", EditorStyles.miniButton))
                {
                    s.Outline = BuildOutline(s.Position, s.Size);
                    st.dirty = true;
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        EditorGUIUtility.labelWidth = oldLabel;
    }

    // ----------------------------------------------------------------- Input

    private void HandleMouse(Rect imageRect)
    {
        Event e = Event.current;

        // đang kéo 1 đỉnh của active point
        if (e.type == EventType.MouseDrag && dragVertex >= 0)
        {
            if (imageRect == dragRect)
            {
                var s = ActiveShape();
                if (s != null && s.Outline != null && dragVertex < s.Outline.Length)
                {
                    Vector3 dp = ToDesign(e.mousePosition, imageRect);
                    s.Outline[dragVertex] = dp;
                    RecalcBounds(s);
                    GetState(Level).dirty = true;
                    hasHover = true; hoverDesign = dp;
                }
                e.Use();
                Repaint();
            }
            return;
        }

        if (!imageRect.Contains(e.mousePosition)) return;

        if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
        {
            hasHover = true;
            hoverDesign = ToDesign(e.mousePosition, imageRect);
        }

        if (e.type != EventType.MouseDown) return;

        GUI.FocusControl(null);
        GUIUtility.keyboardControl = 0;

        Vector3 p = ToDesign(e.mousePosition, imageRect);

        if (e.button == 0)
        {
            if (e.control || e.command)
            {
                selected = FindShapeAt(p);
            }
            else if (clickMode == ClickMode.Outline)
            {
                int hit = HitVertex(imageRect, e.mousePosition);
                if (hit >= 0)
                {
                    dragVertex = hit;
                    dragRect = imageRect;
                }
                else
                {
                    lastClick = p; hasLastClick = true;
                    AddVertexToActive(p);
                }
            }
            else if (clickMode == ClickMode.MarkCenter)
            {
                lastClick = p; hasLastClick = true;
                SetMarkCenter(p);
            }
            else
            {
                lastClick = p; hasLastClick = true;
                AddCenterPoint(p);
            }
            e.Use();
            Repaint();
        }
        else if (e.button == 1)
        {
            UndoStep();
            e.Use();
            Repaint();
        }
    }

    private int HitVertex(Rect r, Vector2 mouse)
    {
        var s = ActiveShape();
        if (s == null || s.Outline == null) return -1;

        int best = -1;
        float bestD = VertexHitRadius;
        for (int i = 0; i < s.Outline.Length; i++)
        {
            float d = Vector2.Distance(ToGui(s.Outline[i], r), mouse);
            if (d <= bestD) { bestD = d; best = i; }
        }
        return best;
    }

    private void HandleKeyboard()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown) return;
        if (GUIUtility.keyboardControl != 0) return; // đang gõ vào ô text/number

        switch (e.keyCode)
        {
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                if (clickMode == ClickMode.Outline)
                {
                    var a = ActiveShape();
                    if (a == null || (a.Outline != null && a.Outline.Length > 0)) NewPoint();
                }
                e.Use(); Repaint();
                break;
            case KeyCode.Backspace:
                UndoStep();
                e.Use(); Repaint();
                break;
            case KeyCode.Delete:
                DeleteSelected();
                e.Use(); Repaint();
                break;
            case KeyCode.Escape:
                selected = -1;
                e.Use(); Repaint();
                break;
        }
    }

    // ----------------------------------------------------------- Coordinates

    // GUI (pixel trong rect ảnh) -> toạ độ thiết kế (Y hướng lên)
    private Vector3 ToDesign(Vector2 mouse, Rect r)
    {
        float nx = (mouse.x - r.x) / r.width;
        float ny = 1f - (mouse.y - r.y) / r.height;
        return new Vector3(
            Mathf.Round((nx - pivot.x) * designSize.x * 10f) / 10f,
            Mathf.Round((ny - pivot.y) * designSize.y * 10f) / 10f, 0f);
    }

    private Vector3 ToGui(Vector3 p, Rect r)
    {
        float nx = p.x / designSize.x + pivot.x;
        float ny = p.y / designSize.y + pivot.y;
        return new Vector3(r.x + nx * r.width, r.y + (1f - ny) * r.height, 0f);
    }

    private static string Fmt(Vector3 v) => $"({v.x:0.#}, {v.y:0.#})";

    // ------------------------------------------------------------- Edit ops

    // Tạo 1 Different Point rỗng và đặt làm ACTIVE POINT
    private void NewPoint()
    {
        var st = GetState(Level);
        st.shapes.Add(new DifferentPointInfo
        {
            Position = Vector3.zero,
            Size = pointSize,
            Outline = new Vector3[0]
        });
        selected = st.shapes.Count - 1;
        st.dirty = true;
        Repaint();
    }

    private void AddVertexToActive(Vector3 p)
    {
        var st = GetState(Level);
        if (ActiveShape() == null) NewPoint();

        var s = st.shapes[selected];
        var list = new List<Vector3>(s.Outline ?? new Vector3[0]);
        list.Add(new Vector3(p.x, p.y, 0f));
        s.Outline = list.ToArray();

        if (list.Count == 1 && !autoBounds) { s.Position = list[0]; }
        RecalcBounds(s);
        st.dirty = true;
    }

    private void AddCenterPoint(Vector3 p)
    {
        var st = GetState(Level);
        st.shapes.Add(new DifferentPointInfo
        {
            Position = new Vector3(p.x, p.y, 0f),
            Size = pointSize,
            Outline = generateOutline ? BuildOutline(p, pointSize) : new Vector3[0]
        });
        selected = st.shapes.Count - 1;
        st.dirty = true;
    }

    // Click 1 điểm = tâm mark của ACTIVE POINT
    private void SetMarkCenter(Vector3 p)
    {
        if (ActiveShape() == null) selected = FindShapeAt(p); // chưa chọn thì chọn point gần chỗ click

        var s = ActiveShape();
        if (s == null) return;

        s.MarkCenter = new Vector3(p.x, p.y, 0f);
        s.HasMarkCenter = true;
        GetState(Level).dirty = true;
    }

    // Position/Size theo bounding box của Outline
    private void RecalcBounds(DifferentPointInfo s)
    {
        if (!autoBounds || s.Outline == null || s.Outline.Length == 0) return;

        if (s.Outline.Length == 1)
        {
            s.Position = new Vector3(s.Outline[0].x, s.Outline[0].y, 0f);
            s.Size = pointSize;
            return;
        }

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (var p in s.Outline)
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        Vector2 c = (min + max) * 0.5f;
        s.Position = new Vector3(c.x, c.y, 0f);
        s.Size = max - min;
    }

    private Vector3[] BuildOutline(Vector3 c, Vector2 size)
    {
        Vector2 half = size * 0.5f;
        if (pointShape == PointShape.Rectangle)
        {
            return new[]
            {
                new Vector3(c.x - half.x, c.y - half.y, 0f),
                new Vector3(c.x - half.x, c.y + half.y, 0f),
                new Vector3(c.x + half.x, c.y + half.y, 0f),
                new Vector3(c.x + half.x, c.y - half.y, 0f),
            };
        }

        int n = Mathf.Max(6, ellipseSegments);
        var pts = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float a = Mathf.PI * 2f * i / n;
            pts[i] = new Vector3(c.x + Mathf.Cos(a) * half.x, c.y + Mathf.Sin(a) * half.y, 0f);
        }
        return pts;
    }

    // Right-click / Backspace
    private void UndoStep()
    {
        if (clickMode == ClickMode.Outline) UndoVertex();
        else if (clickMode == ClickMode.MarkCenter) ClearMarkCenter();
        else UndoLastPoint();
    }

    private void ClearMarkCenter()
    {
        var s = ActiveShape();
        if (s == null || !s.HasMarkCenter) return;
        s.HasMarkCenter = false;
        s.MarkCenter = Vector3.zero;
        GetState(Level).dirty = true;
        Repaint();
    }

    private void UndoVertex()
    {
        var s = ActiveShape();
        if (s == null || s.Outline == null || s.Outline.Length == 0) return;
        var list = new List<Vector3>(s.Outline);
        list.RemoveAt(list.Count - 1);
        s.Outline = list.ToArray();
        RecalcBounds(s);
        GetState(Level).dirty = true;
        Repaint();
    }

    private void ClearActiveOutline()
    {
        var s = ActiveShape();
        if (s == null) return;
        s.Outline = new Vector3[0];
        GetState(Level).dirty = true;
        Repaint();
    }

    private void UndoLastPoint()
    {
        var st = GetState(Level);
        if (st.shapes.Count == 0) return;
        st.shapes.RemoveAt(st.shapes.Count - 1);
        if (selected >= st.shapes.Count) selected = -1;
        st.dirty = true;
        Repaint();
    }

    private void DeleteSelected()
    {
        var st = GetState(Level);
        if (selected < 0 || selected >= st.shapes.Count) return;
        st.shapes.RemoveAt(selected);
        selected = -1;
        st.dirty = true;
    }

    private void ClearCurrentLevel()
    {
        var st = GetState(Level);
        if (st.shapes.Count == 0) return;
        if (!EditorUtility.DisplayDialog("Clear", $"Xóa hết {st.shapes.Count} point của level \"{Level.name}\" (chưa save)?", "Xóa", "Hủy"))
            return;
        st.shapes.Clear();
        st.dirty = true;
        ResetEditingState();
    }

    private void ClearAllLevels()
    {
        if (!EditorUtility.DisplayDialog("Clear All", "Xóa point của TẤT CẢ level trong danh sách (chưa save)?", "Xóa hết", "Hủy"))
            return;
        foreach (var lv in levels)
        {
            if (lv == null) continue;
            var st = GetState(lv);
            st.shapes.Clear();
            st.dirty = true;
        }
        ResetEditingState();
    }

    private int FindShapeAt(Vector3 p)
    {
        var shapes = GetState(Level).shapes;
        int best = -1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < shapes.Count; i++)
        {
            var s = shapes[i];
            Vector2 half = s.Size * 0.5f + Vector2.one * 6f;
            if (Mathf.Abs(p.x - s.Position.x) > half.x || Mathf.Abs(p.y - s.Position.y) > half.y) continue;
            float d = Vector2.Distance(p, s.Position);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    private static void MoveResizeShape(DifferentPointInfo s, Vector2 newPos, Vector2 newSize)
    {
        Vector2 oldPos = s.Position;
        Vector2 oldSize = s.Size;
        float sx = oldSize.x > 0.0001f ? newSize.x / oldSize.x : 1f;
        float sy = oldSize.y > 0.0001f ? newSize.y / oldSize.y : 1f;

        if (s.Outline != null)
        {
            for (int i = 0; i < s.Outline.Length; i++)
            {
                Vector3 o = s.Outline[i];
                s.Outline[i] = new Vector3(
                    newPos.x + (o.x - oldPos.x) * sx,
                    newPos.y + (o.y - oldPos.y) * sy, 0f);
            }
        }
        s.Position = new Vector3(newPos.x, newPos.y, 0f);
        s.Size = newSize;
        // MarkCenter không đổi theo Position/Size: nó độc lập với outline
    }

    // ------------------------------------------------------------------ Data

    private LevelState GetState(LevelData lv)
    {
        if (!states.TryGetValue(lv, out var st))
        {
            st = LoadState(lv);
            states[lv] = st;
        }
        return st;
    }

    private LevelState LoadState(LevelData lv)
    {
        var st = new LevelState();
        if (lv == null || lv.DifferentPoints == null) return st;

        foreach (var d in lv.DifferentPoints)
        {
            if (d == null) continue;
            Vector3[] outline = d.Outline != null ? (Vector3[])d.Outline.Clone() : new Vector3[0];

            if (outlineRelativeToPosition)
                for (int i = 0; i < outline.Length; i++)
                    outline[i] += new Vector3(d.Position.x, d.Position.y, 0f);

            st.shapes.Add(new DifferentPointInfo
            {
                Position = d.Position,
                Size = d.Size,
                Outline = outline,
                HasMarkCenter = d.HasMarkCenter,
                MarkCenter = d.MarkCenter
            });
        }
        return st;
    }

    private void SaveLevel(LevelData lv)
    {
        if (lv == null) return;
        var st = GetState(lv);

        var result = new List<DifferentPointInfo>(st.shapes.Count);
        for (int idx = 0; idx < st.shapes.Count; idx++)
        {
            var s = st.shapes[idx];
            Vector3[] outline = s.Outline != null ? (Vector3[])s.Outline.Clone() : new Vector3[0];

            if (outlineRelativeToPosition)
                for (int i = 0; i < outline.Length; i++)
                    outline[i] -= new Vector3(s.Position.x, s.Position.y, 0f);

            if (outline.Length == 0 && s.Position == Vector3.zero)
                Debug.LogWarning($"[DifferentPointEditor] {lv.name}: Element {idx} đang rỗng (chưa có outline, Position = 0).", lv);

            result.Add(new DifferentPointInfo
            {
                Position = s.Position,
                Size = s.Size,
                Outline = outline,
                HasMarkCenter = s.HasMarkCenter,
                MarkCenter = s.MarkCenter
            });
        }

        Undo.RecordObject(lv, "Save Different Points");
        lv.SetDifferentPoints(result); // tự cập nhật countDifferent + SetDirty
        EditorUtility.SetDirty(lv);
        AssetDatabase.SaveAssets();
        st.dirty = false;
        Debug.Log($"[DifferentPointEditor] Saved {result.Count} points to {lv.name}", lv);
    }

    private void SaveAll()
    {
        int count = 0;
        foreach (var lv in levels)
        {
            if (lv == null) continue;
            if (states.TryGetValue(lv, out var st) && st.dirty)
            {
                SaveLevel(lv);
                count++;
            }
        }
        Debug.Log($"[DifferentPointEditor] Saved {count} level(s).");
        Repaint();
    }
}
#endif