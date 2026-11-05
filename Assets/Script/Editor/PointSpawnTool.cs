using UnityEditor;
using UnityEngine;

public class PointSpawnTool : EditorWindow
{
    private enum OriginMode { Center, BottomLeft }

    private FindHiddenLevelData level;
    private int entryIndex;

    private bool useBackgroundSize = false;
    private Vector2 manualSize = new Vector2(3500, 3500);
    private OriginMode origin = OriginMode.Center;
    private float pointRadius = 7f;
    private bool showItemSprite = true;
    private bool showOtherEntries = true;
    private bool lockAspect = true;
    private bool autoSyncCount = true;

    private Vector2 listScroll;
    private bool showList;

    [MenuItem("Tools/Point Spawn Tool")]
    public static void Open()
    {
        var w = GetWindow<PointSpawnTool>("Point Spawn Tool");
        w.minSize = new Vector2(450, 750);
    }

    private void OnEnable() => wantsMouseMove = true;

    // ---------- Helpers ----------

    private HiddenEntry CurrentEntry
    {
        get
        {
            if (level == null || level.Entries == null || level.Entries.Count == 0) return null;
            entryIndex = Mathf.Clamp(entryIndex, 0, level.Entries.Count - 1);
            return level.Entries[entryIndex];
        }
    }

    private PointSpawnItem CurrentTarget => CurrentEntry?.points;

    private Vector2 GetRefSize()
    {
        if (useBackgroundSize && level != null && level.Background != null)
            return level.Background.rect.size;
        if (manualSize.x > 0 && manualSize.y > 0) return manualSize;
        return new Vector2(1080, 1850);
    }

    private Vector2 OriginOffset(Vector2 size) =>
        origin == OriginMode.Center ? size * 0.5f : Vector2.zero;

    private static Color EntryColor(int i) => Color.HSVToRGB((i * 0.17f) % 1f, 0.8f, 1f);

    // ---------- GUI ----------

    private void OnGUI()
    {
        DrawSettings();

        Rect area = GUILayoutUtility.GetRect(0, 10000, 0, 10000, GUILayout.ExpandHeight(true));
        GUI.Box(area, GUIContent.none);

        if (level == null || level.Background == null)
        {
            EditorGUI.HelpBox(new Rect(area.x + 10, area.y + 10, area.width - 20, 40),
                "Kéo FindHiddenLevelData (có Background) vào ô phía trên.", MessageType.Info);
            return;
        }

        Vector2 size = GetRefSize();
        Rect imgRect = FitRect(area, size.x / size.y);
        DrawSprite(imgRect, level.Background);

        DrawAllPoints(imgRect, size);
        DrawHoverGhost(imgRect, size);
        HandleInput(imgRect, size);
    }

    private void DrawSettings()
    {
        EditorGUILayout.Space(4);
        level = (FindHiddenLevelData)EditorGUILayout.ObjectField("Level", level, typeof(FindHiddenLevelData), false);

        var entry = CurrentEntry;
        if (level != null && level.Entries != null && level.Entries.Count > 0)
        {
            string[] names = new string[level.Entries.Count];
            for (int i = 0; i < names.Length; i++)
            {
                var e = level.Entries[i];
                string dn = e.data != null ? e.data.name : "(null data)";
                string pn = e.points != null ? $"{e.points.name} [{e.points.Points.Count}]" : "(chưa có PointSpawnItem)";
                names[i] = $"{i}: {dn}  →  {pn}";
            }
            entryIndex = EditorGUILayout.Popup("Entry đang ghi", entryIndex, names);
            entry = CurrentEntry;
        }
        else if (level != null)
        {
            EditorGUILayout.HelpBox("Level chưa có entry nào.", MessageType.Warning);
        }

        if (entry != null && entry.points == null)
            EditorGUILayout.HelpBox("Entry này chưa gán PointSpawnItem. Hãy gán trong Inspector của Level.", MessageType.Warning);

        DrawItemData(entry);

        EditorGUILayout.Space(4);
        useBackgroundSize = EditorGUILayout.Toggle("Lấy size từ Background", useBackgroundSize);
        using (new EditorGUI.DisabledScope(useBackgroundSize))
            manualSize = EditorGUILayout.Vector2Field("Size ảnh (thủ công)", manualSize);

        origin = (OriginMode)EditorGUILayout.EnumPopup("Gốc toạ độ", origin);
        pointRadius = EditorGUILayout.Slider("Kích thước điểm", pointRadius, 3, 20);

        using (new EditorGUILayout.HorizontalScope())
        {
            showItemSprite = EditorGUILayout.ToggleLeft("Hiện ảnh item", showItemSprite, GUILayout.Width(110));
            showOtherEntries = EditorGUILayout.ToggleLeft("Hiện điểm entry khác", showOtherEntries);
        }

        int count = GetPointCount();
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"Số điểm: {count}  |  Size: {GetRefSize().x} x {GetRefSize().y}");
            if (GUILayout.Button("Danh sách", GUILayout.Width(75))) showList = !showList;
            using (new EditorGUI.DisabledScope(CurrentTarget == null || count == 0))
            {
                if (GUILayout.Button("Undo", GUILayout.Width(50))) RemovePoint(count - 1);
                if (GUILayout.Button("Xoá hết", GUILayout.Width(65)) &&
                    EditorUtility.DisplayDialog("Xoá hết?", "Xoá toàn bộ điểm của entry này?", "Xoá", "Huỷ"))
                    ClearPoints();
            }
        }

        if (entry != null) DrawEntryCount(count);

        if (entry != null && entry.count > 0 && count < entry.count)
            EditorGUILayout.HelpBox($"Entry cần {entry.count} điểm, hiện mới có {count}.", MessageType.Warning);

        EditorGUILayout.HelpBox("Chuột trái: thêm điểm   |   Chuột phải: xoá điểm gần nhất", MessageType.None);

        using (new EditorGUI.DisabledScope(level == null))
        {
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("💾 LƯU TẤT CẢ", GUILayout.Height(28)))
                SaveAll();
            GUI.backgroundColor = prevBg;
        }

        if (showList && CurrentTarget != null) DrawList();
    }

    // Sửa HiddenEntry.count của level (0 = spawn tất cả điểm)
    private void DrawEntryCount(int pointCount)
    {
        var so = new SerializedObject(level);
        var entries = so.FindProperty("entries");
        if (entries == null || entryIndex < 0 || entryIndex >= entries.arraySize) return;
        var countProp = entries.GetArrayElementAtIndex(entryIndex).FindPropertyRelative("count");

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            int c = EditorGUILayout.IntField("Count (0 = tất cả)", countProp.intValue);
            if (EditorGUI.EndChangeCheck()) countProp.intValue = Mathf.Max(0, c);

            if (GUILayout.Button("Count = số điểm", GUILayout.Width(110)))
                countProp.intValue = pointCount;
        }

        autoSyncCount = EditorGUILayout.ToggleLeft("Tự động đồng bộ Count khi thêm/xoá điểm", autoSyncCount);

        if (so.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            Repaint();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }

    // Gán HiddenEntry.count của entry đang chọn = số điểm hiện có
    private void SyncCount()
    {
        if (level == null) return;
        var so = new SerializedObject(level);
        var entries = so.FindProperty("entries");
        if (entries == null || entryIndex < 0 || entryIndex >= entries.arraySize) return;
        var countProp = entries.GetArrayElementAtIndex(entryIndex).FindPropertyRelative("count");
        int n = GetPointCount();
        if (countProp.intValue == n) return;
        countProp.intValue = n;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(level);
        AssetDatabase.SaveAssets();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    private void SaveAll()
    {
        if (level == null) return;
        EditorUtility.SetDirty(level);
        if (level.Entries != null)
        {
            foreach (var e in level.Entries)
            {
                if (e.data != null) EditorUtility.SetDirty(e.data);
                if (e.points != null) EditorUtility.SetDirty(e.points);
            }
        }
        AssetDatabase.SaveAssets();
        ShowNotification(new GUIContent("Đã lưu!"));
        Debug.Log($"[PointSpawnTool] Đã lưu level '{level.name}' và các asset liên quan.");
    }

    // ---------- Item data (size + targetId) ----------

    private void DrawItemData(HiddenEntry entry)
    {
        if (entry == null) return;
        if (entry.data == null)
        {
            EditorGUILayout.HelpBox("Entry này chưa gán FindHiddenData.", MessageType.Warning);
            return;
        }

        var data = entry.data;
        var so = new SerializedObject(data);
        var sizeProp = so.FindProperty("size");
        var idProp = so.FindProperty("targetId");
        so.Update();

        EditorGUILayout.LabelField($"Item: {data.name}", EditorStyles.boldLabel);

        // Size (có khoá tỉ lệ)
        Vector3 oldSize = sizeProp.vector3Value;
        EditorGUI.BeginChangeCheck();
        Vector3 newSize = EditorGUILayout.Vector3Field("Size item", oldSize);
        if (EditorGUI.EndChangeCheck())
        {
            if (lockAspect)
            {
                if (!Mathf.Approximately(newSize.x, oldSize.x) && oldSize.x > 0f)
                    newSize.y = Mathf.Round(newSize.x * oldSize.y / oldSize.x);
                else if (!Mathf.Approximately(newSize.y, oldSize.y) && oldSize.y > 0f)
                    newSize.x = Mathf.Round(newSize.y * oldSize.x / oldSize.y);
            }
            sizeProp.vector3Value = newSize;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            lockAspect = EditorGUILayout.ToggleLeft("Khoá tỉ lệ", lockAspect, GUILayout.Width(90));

            using (new EditorGUI.DisabledScope(data.SpriteItem == null))
            {
                if (GUILayout.Button("Size gốc Sprite"))
                {
                    Vector2 s = data.SpriteItem.rect.size;
                    sizeProp.vector3Value = new Vector3(s.x, s.y, sizeProp.vector3Value.z);
                }
            }
            if (GUILayout.Button("x0.5", GUILayout.Width(45)))
            {
                var v = sizeProp.vector3Value;
                sizeProp.vector3Value = new Vector3(Mathf.Round(v.x * 0.5f), Mathf.Round(v.y * 0.5f), v.z);
            }
            if (GUILayout.Button("x2", GUILayout.Width(35)))
            {
                var v = sizeProp.vector3Value;
                sizeProp.vector3Value = new Vector3(v.x * 2f, v.y * 2f, v.z);
            }
        }

        // Target Id
        EditorGUILayout.PropertyField(idProp, new GUIContent("Target Id"));

        if (so.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Repaint();
        }
    }

    private void DrawList()
    {
        var arr = new SerializedObject(CurrentTarget).FindProperty("points");
        listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.Height(110));
        for (int i = 0; i < arr.arraySize; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                Vector3 v = arr.GetArrayElementAtIndex(i).vector3Value;
                EditorGUILayout.LabelField($"{i}: ({v.x:0.#}, {v.y:0.#}, {v.z:0.#})");
                if (GUILayout.Button("X", GUILayout.Width(24))) { RemovePoint(i); break; }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    // ---------- Vẽ ----------

    private static Rect FitRect(Rect area, float aspect)
    {
        float w = area.width - 10, h = area.height - 10;
        if (w / h > aspect) w = h * aspect; else h = w / aspect;
        return new Rect(area.x + (area.width - w) * 0.5f, area.y + (area.height - h) * 0.5f, w, h);
    }

    private static void DrawSprite(Rect rect, Sprite sprite)
    {
        Texture2D tex = sprite.texture;
        Rect tr = sprite.textureRect;
        Rect uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        GUI.DrawTextureWithTexCoords(rect, tex, uv);
    }

    private Vector2 LocalToGui(Rect imgRect, Vector2 size, Vector3 local)
    {
        Vector2 o = OriginOffset(size);
        float u = (local.x + o.x) / size.x;
        float v = (local.y + o.y) / size.y;
        return new Vector2(imgRect.x + u * imgRect.width, imgRect.yMax - v * imgRect.height);
    }

    private Vector3 GuiToLocal(Rect imgRect, Vector2 size, Vector2 gui)
    {
        float u = (gui.x - imgRect.x) / imgRect.width;
        float v = (imgRect.yMax - gui.y) / imgRect.height;
        Vector2 o = OriginOffset(size);
        return new Vector3(Mathf.Round(u * size.x - o.x), Mathf.Round(v * size.y - o.y), 0f);
    }

    private void DrawItemSprite(Vector2 guiPos, Rect imgRect, Vector2 size, FindHiddenData data, float alpha)
    {
        if (data == null || data.SpriteItem == null) return;
        float w = data.Size.x / size.x * imgRect.width;
        float h = data.Size.y / size.y * imgRect.height;
        if (w <= 1f || h <= 1f) return;
        var prev = GUI.color;
        GUI.color = new Color(1, 1, 1, alpha);
        DrawSprite(new Rect(guiPos.x - w * 0.5f, guiPos.y - h * 0.5f, w, h), data.SpriteItem);
        GUI.color = prev;
    }

    private void DrawAllPoints(Rect imgRect, Vector2 size)
    {
        if (level.Entries == null) return;

        for (int ei = 0; ei < level.Entries.Count; ei++)
        {
            var e = level.Entries[ei];
            if (e.points == null) continue;
            bool selected = ei == entryIndex;
            if (!selected && !showOtherEntries) continue;

            var arr = new SerializedObject(e.points).FindProperty("points");
            Color c = EntryColor(ei);
            c.a = selected ? 0.95f : 0.45f;

            for (int i = 0; i < arr.arraySize; i++)
            {
                Vector2 p = LocalToGui(imgRect, size, arr.GetArrayElementAtIndex(i).vector3Value);

                if (selected && showItemSprite) DrawItemSprite(p, imgRect, size, e.data, 0.85f);

                Handles.color = c;
                Handles.DrawSolidDisc(p, Vector3.forward, selected ? pointRadius : pointRadius * 0.7f);
                Handles.color = selected ? Color.white : new Color(1, 1, 1, 0.4f);
                Handles.DrawWireDisc(p, Vector3.forward, selected ? pointRadius : pointRadius * 0.7f);

                if (selected)
                    GUI.Label(new Rect(p.x + pointRadius + 2, p.y - 9, 40, 18), i.ToString(), EditorStyles.whiteBoldLabel);
            }
        }
    }

    private void DrawHoverGhost(Rect imgRect, Vector2 size)
    {
        Event e = Event.current;
        if (!imgRect.Contains(e.mousePosition)) return;

        var entry = CurrentEntry;
        if (entry != null && showItemSprite)
            DrawItemSprite(e.mousePosition, imgRect, size, entry.data, 0.5f);

        Vector3 l = GuiToLocal(imgRect, size, e.mousePosition);
        GUI.Label(new Rect(e.mousePosition.x + 14, e.mousePosition.y + 14, 140, 20),
            $"({l.x}, {l.y})", EditorStyles.whiteBoldLabel);
        Repaint();
    }

    // ---------- Input ----------

    private void HandleInput(Rect imgRect, Vector2 size)
    {
        Event e = Event.current;
        if (e.type != EventType.MouseDown || !imgRect.Contains(e.mousePosition)) return;
        if (CurrentTarget == null) return;

        if (e.button == 0) { AddPoint(GuiToLocal(imgRect, size, e.mousePosition)); e.Use(); }
        else if (e.button == 1) { RemoveNearest(imgRect, size, e.mousePosition); e.Use(); }
    }

    // ---------- Ghi dữ liệu vào SO ----------

    private int GetPointCount()
    {
        var t = CurrentTarget;
        return t == null ? 0 : new SerializedObject(t).FindProperty("points").arraySize;
    }

    private void AddPoint(Vector3 local)
    {
        var t = CurrentTarget;
        Undo.RecordObject(t, "Add Spawn Point");
        var so = new SerializedObject(t);
        var arr = so.FindProperty("points");
        arr.arraySize++;
        arr.GetArrayElementAtIndex(arr.arraySize - 1).vector3Value = local;
        so.ApplyModifiedProperties();
        Save(t);
    }

    private void RemovePoint(int index)
    {
        var t = CurrentTarget;
        if (t == null || index < 0) return;
        Undo.RecordObject(t, "Remove Spawn Point");
        var so = new SerializedObject(t);
        var arr = so.FindProperty("points");
        if (index >= arr.arraySize) return;
        arr.DeleteArrayElementAtIndex(index);
        so.ApplyModifiedProperties();
        Save(t);
    }

    private void RemoveNearest(Rect imgRect, Vector2 size, Vector2 mouse)
    {
        var arr = new SerializedObject(CurrentTarget).FindProperty("points");
        int best = -1;
        float bestDist = 20f;
        for (int i = 0; i < arr.arraySize; i++)
        {
            float d = Vector2.Distance(mouse, LocalToGui(imgRect, size, arr.GetArrayElementAtIndex(i).vector3Value));
            if (d < bestDist) { bestDist = d; best = i; }
        }
        if (best >= 0) RemovePoint(best);
    }

    private void ClearPoints()
    {
        var t = CurrentTarget;
        Undo.RecordObject(t, "Clear Spawn Points");
        var so = new SerializedObject(t);
        so.FindProperty("points").arraySize = 0;
        so.ApplyModifiedProperties();
        Save(t);
    }

    private void Save(PointSpawnItem t)
    {
        EditorUtility.SetDirty(t);
        AssetDatabase.SaveAssets();
        if (autoSyncCount) SyncCount();
        Repaint();
    }
}