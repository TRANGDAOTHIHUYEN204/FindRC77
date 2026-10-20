using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Level Editor: chỉnh LevelData và vẽ trực tiếp các vùng khác biệt lên ảnh A/B.
// Tọa độ DifferentPointInfo là local của RectTransform RawImage (pivot giữa, trục y hướng lên),
// nên kích thước vùng ảnh ở đây phải khớp với RawImage trong scene GamePlay (mặc định 1040x650).
public class LevelEditorWindow : EditorWindow
{
    private const string PrefImageW = "LevelEditor.ImageW";
    private const string PrefImageH = "LevelEditor.ImageH";
    private const string LevelFolder = "Assets/SOData";
    private const string GamePlayScene = "Assets/Scenes/GamePlay.unity";
    private const float LeftWidth = 210f;
    private const float RightWidth = 330f;
    private const float Handle = 8f;
    private const float PanelGap = 12f;
    private const float MinPointSize = 6f;

    private enum DragMode { None, Create, Move, Resize }

    private readonly List<LevelData> _levels = new List<LevelData>();
    private LevelData _level;
    private SerializedObject _so;
    private int _selected = -1;

    private Vector2 _imageSize;
    private Vector2 _listScroll, _rightScroll;
    private Rect _panelA, _panelB;
    private float _scale;
    private bool _showLabels = true;
    private float _overlayAlpha;

    private DragMode _drag;
    private int _undoGroup;
    private Vector2 _dragStartLocal, _dragStartPos, _dragStartSize, _resizeAnchor;

    private float _diffThreshold = 0.12f;
    private int _diffGrid = 256;
    private int _diffMinCells = 3;
    private float _diffPadding = 12f;

    [MenuItem("Tools/Level Editor")]
    public static void Open() => GetWindow<LevelEditorWindow>("Level Editor").minSize = new Vector2(900, 500);

    private void OnEnable()
    {
        _imageSize = new Vector2(EditorPrefs.GetFloat(PrefImageW, 1040f), EditorPrefs.GetFloat(PrefImageH, 650f));
        Undo.undoRedoPerformed += OnUndoRedo;
        RefreshLevels();
    }

    private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

    private void OnUndoRedo()
    {
        _so?.Update();
        if (_selected >= PointCount) _selected = PointCount - 1;
        Repaint();
    }

    private void OnProjectChange()
    {
        RefreshLevels();
        Repaint();
    }

    #region Data

    private SerializedProperty Points => _so.FindProperty("differentPoints");
    private int PointCount => _so == null ? 0 : Points.arraySize;

    private void RefreshLevels()
    {
        _levels.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:LevelData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data != null) _levels.Add(data);
        }
        _levels.Sort((a, b) => a.NumberLevel.CompareTo(b.NumberLevel));

        if (_level == null && _levels.Count > 0) Select(_levels[0]);
        else if (_level != null && !_levels.Contains(_level)) Select(_levels.FirstOrDefault());
    }

    private void Select(LevelData level)
    {
        _level = level;
        _so = level != null ? new SerializedObject(level) : null;
        _selected = -1;
        _drag = DragMode.None;
        GUI.FocusControl(null);
    }

    private void GetPoint(int i, out Vector2 pos, out Vector2 size)
    {
        SerializedProperty p = Points.GetArrayElementAtIndex(i);
        pos = p.FindPropertyRelative("Position").vector3Value;
        size = p.FindPropertyRelative("Size").vector2Value;
    }

    private void SetPoint(int i, Vector2 pos, Vector2 size)
    {
        SerializedProperty p = Points.GetArrayElementAtIndex(i);
        p.FindPropertyRelative("Position").vector3Value = new Vector3(Round(pos.x), Round(pos.y), 0f);
        p.FindPropertyRelative("Size").vector2Value = new Vector2(Round(Mathf.Abs(size.x)), Round(Mathf.Abs(size.y)));
    }

    private int AddPoint(Vector2 pos, Vector2 size)
    {
        int i = Points.arraySize;
        Points.InsertArrayElementAtIndex(i);
        SetPoint(i, pos, size);
        return i;
    }

    private void RemovePoint(int i)
    {
        Points.DeleteArrayElementAtIndex(i);
        if (_selected >= Points.arraySize) _selected = Points.arraySize - 1;
    }

    private void Apply()
    {
        _so.FindProperty("countDifferent").intValue = Points.arraySize;
        _so.ApplyModifiedProperties();
    }

    private static float Round(float v) => Mathf.Round(v * 10f) / 10f;

    #endregion

    private void OnGUI()
    {
        if (_so != null && _so.targetObject == null) Select(null);
        _so?.Update();

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawLevelList();
            using (new EditorGUILayout.VerticalScope())
            {
                if (_so == null)
                {
                    EditorGUILayout.HelpBox("Chưa có LevelData nào. Bấm \"+ Level mới\" để tạo.", MessageType.Info);
                }
                else
                {
                    DrawToolbar();
                    Rect canvas = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    DrawCanvas(canvas);
                }
            }
            if (_so != null) DrawRightPanel();
        }

        if (_so != null && _so.hasModifiedProperties) Apply();
    }

    #region Level list

    private void DrawLevelList()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(LeftWidth)))
        {
            EditorGUILayout.LabelField("Levels", EditorStyles.boldLabel);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            foreach (LevelData level in _levels)
            {
                if (level == null) continue;
                string label = $"Level {level.NumberLevel}  ({level.DifferentPoints?.Count ?? 0} điểm)";
                bool isCurrent = level == _level;
                var style = new GUIStyle(EditorStyles.label) { fontStyle = isCurrent ? FontStyle.Bold : FontStyle.Normal };
                Rect r = EditorGUILayout.GetControlRect();
                if (isCurrent) EditorGUI.DrawRect(r, new Color(0.24f, 0.48f, 0.9f, 0.35f));
                if (!HasProblems(level, out _)) label = "  " + label;
                else label = "⚠ " + label;
                if (GUI.Button(r, label, style)) Select(level);
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("+ Level mới")) CreateLevel(null);
            using (new EditorGUI.DisabledScope(_level == null))
            {
                if (GUILayout.Button("Nhân bản level này")) CreateLevel(_level);
                if (GUILayout.Button("Chọn asset trong Project"))
                {
                    Selection.activeObject = _level;
                    EditorGUIUtility.PingObject(_level);
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("Đồng bộ LevelManager", "Gán toàn bộ level (theo thứ tự số level) vào LevelManager trong scene GamePlay")))
                SyncLevelManager();
            using (new EditorGUI.DisabledScope(_level == null || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("▶ Chơi thử level này")) PlayTest();
            }
            if (GUILayout.Button("Làm mới danh sách")) RefreshLevels();
        }
    }

    private void CreateLevel(LevelData source)
    {
        int number = _levels.Count == 0 ? 1 : _levels.Max(l => l.NumberLevel) + 1;
        if (!AssetDatabase.IsValidFolder(LevelFolder)) AssetDatabase.CreateFolder("Assets", "SOData");
        string path = AssetDatabase.GenerateUniqueAssetPath($"{LevelFolder}/Level_{number:00}_Data.asset");

        LevelData level = source != null ? Instantiate(source) : CreateInstance<LevelData>();
        var so = new SerializedObject(level);
        so.FindProperty("numberLevel").intValue = number;
        if (source == null)
        {
            LevelData template = _level != null ? _level : _levels.LastOrDefault();
            if (template != null)
            {
                var tso = new SerializedObject(template);
                so.FindProperty("timeLimit").floatValue = tso.FindProperty("timeLimit").floatValue;
                so.CopyFromSerializedProperty(tso.FindProperty("markData"));
            }
            else so.FindProperty("timeLimit").floatValue = 240f;
            AssignSpritesByConvention(so, number);
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(level, path);
        AssetDatabase.SaveAssets();
        RefreshLevels();
        Select(level);
    }

    // Quy ước sẵn có: Assets/Resources/Level N/N_A.png và N_B.png
    private static bool AssignSpritesByConvention(SerializedObject so, int number)
    {
        Sprite a = FindSprite($"Assets/Resources/Level {number}", $"{number}_A");
        Sprite b = FindSprite($"Assets/Resources/Level {number}", $"{number}_B");
        if (a != null) so.FindProperty("rawSprite").objectReferenceValue = a;
        if (b != null) so.FindProperty("errorSprite").objectReferenceValue = b;
        return a != null || b != null;
    }

    private static Sprite FindSprite(string folder, string fileName)
    {
        if (!AssetDatabase.IsValidFolder(folder)) return null;
        foreach (string guid in AssetDatabase.FindAssets(fileName, new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != fileName) continue;
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (sprite != null) return sprite;
        }
        return null;
    }

    private LevelManager FindLevelManager(bool openSceneIfMissing)
    {
        LevelManager manager = FindObjectsByType<LevelManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (manager != null || !openSceneIfMissing) return manager;

        if (!EditorUtility.DisplayDialog("Level Editor", "Scene GamePlay chưa mở. Mở scene GamePlay?", "Mở", "Hủy")) return null;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
        EditorSceneManager.OpenScene(GamePlayScene);
        return FindObjectsByType<LevelManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
    }

    private void SyncLevelManager()
    {
        LevelManager manager = FindLevelManager(true);
        if (manager == null) return;

        var so = new SerializedObject(manager);
        SerializedProperty arr = so.FindProperty("levels");
        arr.arraySize = _levels.Count;
        for (int i = 0; i < _levels.Count; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = _levels[i];
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        ShowNotification(new GUIContent($"Đã gán {_levels.Count} level vào LevelManager (nhớ lưu scene)"));
    }

    private void PlayTest()
    {
        LevelManager manager = FindLevelManager(true);
        if (manager == null) return;

        SerializedProperty arr = new SerializedObject(manager).FindProperty("levels");
        int index = -1;
        for (int i = 0; i < arr.arraySize; i++)
            if (arr.GetArrayElementAtIndex(i).objectReferenceValue == _level) { index = i; break; }

        if (index < 0)
        {
            if (!EditorUtility.DisplayDialog("Level Editor", "Level này chưa có trong LevelManager. Đồng bộ danh sách rồi chơi?", "Đồng bộ", "Hủy")) return;
            SyncLevelManager();
            index = _levels.IndexOf(_level);
        }

        SaveGame.CurrentLevel = index;
        EditorApplication.EnterPlaymode();
    }

    #endregion

    #region Toolbar & properties

    private void DrawToolbar()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_so.FindProperty("numberLevel"), new GUIContent("Số level"));
                EditorGUILayout.PropertyField(_so.FindProperty("timeLimit"), new GUIContent("Thời gian (s)"));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(_so.FindProperty("rawSprite"), new GUIContent("Ảnh A (gốc)"));
                EditorGUILayout.PropertyField(_so.FindProperty("errorSprite"), new GUIContent("Ảnh B (lỗi)"));
                if (GUILayout.Button(new GUIContent("Tự gán", "Tìm Resources/Level N/N_A, N_B"), GUILayout.Width(60)))
                {
                    if (!AssignSpritesByConvention(_so, _so.FindProperty("numberLevel").intValue))
                        ShowNotification(new GUIContent("Không tìm thấy ảnh theo quy ước"));
                }
            }
            EditorGUILayout.PropertyField(_so.FindProperty("markData"), new GUIContent("Mark Data"), true);
            EditorGUILayout.PropertyField(_so.FindProperty("textWin"), new GUIContent("Text thắng"));

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                _imageSize = EditorGUILayout.Vector2Field(new GUIContent("Kích thước RawImage", "Phải khớp RectTransform RawImage trong scene GamePlay"), _imageSize);
                if (EditorGUI.EndChangeCheck())
                {
                    _imageSize = Vector2.Max(_imageSize, Vector2.one * 10f);
                    EditorPrefs.SetFloat(PrefImageW, _imageSize.x);
                    EditorPrefs.SetFloat(PrefImageH, _imageSize.y);
                }
                _showLabels = GUILayout.Toggle(_showLabels, "Số thứ tự", EditorStyles.miniButton, GUILayout.Width(70));
                GUILayout.Label(new GUIContent("Chồng A lên B", "Kéo để so sánh ảnh A đè lên ảnh B"), GUILayout.Width(85));
                _overlayAlpha = GUILayout.HorizontalSlider(_overlayAlpha, 0f, 1f, GUILayout.Width(80));
            }

            if (HasProblems(_level, out string problems))
                EditorGUILayout.HelpBox(problems, MessageType.Warning);
        }
    }

    private bool HasProblems(LevelData level, out string message)
    {
        var issues = new List<string>();
        if (level.RawSprite == null) issues.Add("Thiếu ảnh A.");
        if (level.ErrorSprite == null) issues.Add("Thiếu ảnh B.");
        if (level.RawSprite != null && level.ErrorSprite != null && level.RawSprite.rect.size != level.ErrorSprite.rect.size)
            issues.Add($"Ảnh A ({level.RawSprite.rect.size}) và B ({level.ErrorSprite.rect.size}) khác kích thước.");

        List<DifferentPointInfo> points = level.DifferentPoints ?? new List<DifferentPointInfo>();
        if (points.Count == 0) issues.Add("Chưa có điểm khác biệt nào.");
        if (level.CountDifferent != points.Count) issues.Add($"countDifferent ({level.CountDifferent}) khác số điểm ({points.Count}).");
        if (level.TimeLimit <= 0) issues.Add("Thời gian phải > 0.");

        Rect bounds = new Rect(-_imageSize * 0.5f, _imageSize);
        for (int i = 0; i < points.Count; i++)
        {
            Rect r = ToRect(points[i].Position, points[i].Size);
            if (r.xMin < bounds.xMin - 0.5f || r.yMin < bounds.yMin - 0.5f || r.xMax > bounds.xMax + 0.5f || r.yMax > bounds.yMax + 0.5f) issues.Add($"Điểm #{i + 1} tràn ra ngoài ảnh.");
            for (int j = i + 1; j < points.Count; j++)
                if (r.Overlaps(ToRect(points[j].Position, points[j].Size))) issues.Add($"Điểm #{i + 1} và #{j + 1} chồng nhau.");
        }

        message = string.Join("\n", issues);
        return issues.Count > 0;
    }

    private static Rect ToRect(Vector2 center, Vector2 size) => new Rect(center - size * 0.5f, size);

    #endregion

    #region Canvas

    private void DrawCanvas(Rect area)
    {
        EditorGUI.DrawRect(area, new Color(0.13f, 0.13f, 0.13f));
        area = new RectOffset(8, 8, 8, 8).Remove(area);

        float sideScale = Mathf.Min((area.width - PanelGap) / (2f * _imageSize.x), area.height / _imageSize.y);
        float stackScale = Mathf.Min(area.width / _imageSize.x, (area.height - PanelGap) / (2f * _imageSize.y));
        bool sideBySide = sideScale >= stackScale;
        _scale = Mathf.Max(0.01f, sideBySide ? sideScale : stackScale);

        Vector2 size = _imageSize * _scale;
        Vector2 total = sideBySide ? new Vector2(size.x * 2f + PanelGap, size.y) : new Vector2(size.x, size.y * 2f + PanelGap);
        Vector2 origin = area.center - total * 0.5f;
        _panelA = new Rect(origin, size);
        _panelB = sideBySide
            ? new Rect(origin + new Vector2(size.x + PanelGap, 0f), size)
            : new Rect(origin + new Vector2(0f, size.y + PanelGap), size);

        DrawSprite(_panelA, _level.RawSprite, 1f);
        DrawSprite(_panelB, _level.ErrorSprite, 1f);
        if (_overlayAlpha > 0f) DrawSprite(_panelB, _level.RawSprite, _overlayAlpha);
        GUI.Label(new Rect(_panelA.x + 4, _panelA.y + 2, 60, 18), "A", EditorStyles.whiteBoldLabel);
        GUI.Label(new Rect(_panelB.x + 4, _panelB.y + 2, 60, 18), "B", EditorStyles.whiteBoldLabel);

        for (int i = 0; i < PointCount; i++)
        {
            GetPoint(i, out Vector2 pos, out Vector2 sz);
            DrawPoint(_panelA, i, pos, sz);
            DrawPoint(_panelB, i, pos, sz);
        }

        HandleInput();
    }

    private static void DrawSprite(Rect r, Sprite sprite, float alpha)
    {
        if (sprite == null)
        {
            EditorGUI.DrawRect(r, new Color(0.25f, 0.25f, 0.25f));
            GUI.Label(r, "Chưa gán ảnh", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
            return;
        }
        Texture2D tex = sprite.texture;
        Rect tr = sprite.textureRect;
        var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTextureWithTexCoords(r, tex, uv, true);
        GUI.color = old;
    }

    private void DrawPoint(Rect panel, int index, Vector2 pos, Vector2 size)
    {
        Rect r = LocalToGui(panel, pos, size);
        bool selected = index == _selected;
        Color line = selected ? new Color(1f, 0.85f, 0.1f) : new Color(0.1f, 1f, 0.4f);
        EditorGUI.DrawRect(r, new Color(line.r, line.g, line.b, selected ? 0.22f : 0.12f));
        float t = selected ? 2f : 1.5f;
        EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), line);
        EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), line);
        EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), line);
        EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), line);

        if (selected)
            foreach (Vector2 c in Corners(r))
                EditorGUI.DrawRect(new Rect(c - Vector2.one * Handle * 0.5f, Vector2.one * Handle), Color.white);

        if (_showLabels)
        {
            var labelRect = new Rect(r.x + 2, r.y + 1, 30, 16);
            EditorGUI.DrawRect(new Rect(labelRect.x, labelRect.y, index >= 9 ? 18 : 11, 14), new Color(0, 0, 0, 0.6f));
            GUI.Label(labelRect, (index + 1).ToString(), EditorStyles.whiteMiniLabel);
        }
    }

    private static Vector2[] Corners(Rect r) =>
        new[] { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.yMax) };

    private Rect LocalToGui(Rect panel, Vector2 pos, Vector2 size)
    {
        Vector2 center = new Vector2(panel.center.x + pos.x * _scale, panel.center.y - pos.y * _scale);
        Vector2 s = size * _scale;
        return new Rect(center - s * 0.5f, s);
    }

    private Vector2 GuiToLocal(Rect panel, Vector2 gui) =>
        new Vector2((gui.x - panel.center.x) / _scale, -(gui.y - panel.center.y) / _scale);

    private bool _dragOnB;

    private Rect DragPanel => _dragOnB ? _panelB : _panelA;

    private void HandleInput()
    {
        Event e = Event.current;
        int id = GUIUtility.GetControlID(FocusType.Keyboard);

        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown when e.button == 0 && (_panelA.Contains(e.mousePosition) || _panelB.Contains(e.mousePosition)):
                _dragOnB = _panelB.Contains(e.mousePosition);
                OnMouseDown(DragPanel, e.mousePosition);
                GUIUtility.hotControl = id;
                GUIUtility.keyboardControl = id;
                e.Use();
                break;

            case EventType.MouseDrag when GUIUtility.hotControl == id:
                OnMouseDrag(GuiToLocal(DragPanel, e.mousePosition));
                e.Use();
                break;

            case EventType.MouseUp when GUIUtility.hotControl == id:
                OnMouseUp();
                GUIUtility.hotControl = 0;
                e.Use();
                break;

            case EventType.KeyDown when GUIUtility.keyboardControl == id && _selected >= 0:
                OnKeyDown(e);
                break;
        }
    }

    private void OnMouseDown(Rect panel, Vector2 mouse)
    {
        Undo.IncrementCurrentGroup();
        _undoGroup = Undo.GetCurrentGroup();
        Vector2 local = GuiToLocal(panel, mouse);
        _dragStartLocal = local;

        if (_selected >= 0)
        {
            GetPoint(_selected, out Vector2 pos, out Vector2 size);
            Rect r = LocalToGui(panel, pos, size);
            foreach (Vector2 c in Corners(r))
            {
                if (Vector2.Distance(c, mouse) > Handle) continue;
                Vector2 cornerLocal = GuiToLocal(panel, c);
                _resizeAnchor = pos * 2f - cornerLocal;
                _drag = DragMode.Resize;
                return;
            }
        }

        for (int i = PointCount - 1; i >= 0; i--)
        {
            GetPoint(i, out Vector2 pos, out Vector2 size);
            if (!ToRect(pos, size).Contains(local)) continue;
            _selected = i;
            _dragStartPos = pos;
            _dragStartSize = size;
            _drag = DragMode.Move;
            return;
        }

        _selected = AddPoint(local, Vector2.zero);
        _drag = DragMode.Create;
        Apply();
    }

    private void OnMouseDrag(Vector2 local)
    {
        if (_selected < 0 || _selected >= PointCount) return;
        switch (_drag)
        {
            case DragMode.Create:
                SetPoint(_selected, (_dragStartLocal + local) * 0.5f, local - _dragStartLocal);
                break;
            case DragMode.Resize:
                SetPoint(_selected, (_resizeAnchor + local) * 0.5f, local - _resizeAnchor);
                break;
            case DragMode.Move:
                SetPoint(_selected, _dragStartPos + (local - _dragStartLocal), _dragStartSize);
                break;
        }
        Apply();
        Repaint();
    }

    private void OnMouseUp()
    {
        if (_drag == DragMode.Create && _selected >= 0 && _selected < PointCount)
        {
            GetPoint(_selected, out _, out Vector2 size);
            if (size.x < MinPointSize || size.y < MinPointSize)
            {
                RemovePoint(_selected);
                _selected = -1;
                Apply();
            }
        }
        _drag = DragMode.None;
        Undo.CollapseUndoOperations(_undoGroup);
        Repaint();
    }

    private void OnKeyDown(Event e)
    {
        if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
        {
            RemovePoint(_selected);
            Apply();
            e.Use();
            return;
        }

        Vector2 delta = e.keyCode switch
        {
            KeyCode.LeftArrow => Vector2.left,
            KeyCode.RightArrow => Vector2.right,
            KeyCode.UpArrow => Vector2.up,
            KeyCode.DownArrow => Vector2.down,
            _ => Vector2.zero
        };
        if (delta == Vector2.zero) return;

        GetPoint(_selected, out Vector2 pos, out Vector2 size);
        if (e.alt) SetPoint(_selected, pos, size + delta * (e.shift ? 10f : 1f) * 2f);
        else SetPoint(_selected, pos + delta * (e.shift ? 10f : 1f), size);
        Apply();
        e.Use();
    }

    #endregion

    #region Right panel

    private void DrawRightPanel()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(RightWidth)))
        {
            EditorGUILayout.LabelField($"Điểm khác biệt ({PointCount})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Kéo trên ảnh: tạo vùng mới\nKéo vùng: di chuyển · Kéo góc: đổi cỡ\nMũi tên: dịch (Shift x10) · Alt+mũi tên: đổi cỡ\nDelete: xóa vùng đang chọn", EditorStyles.wordWrappedMiniLabel);

            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
            for (int i = 0; i < PointCount; i++)
            {
                SerializedProperty p = Points.GetArrayElementAtIndex(i);
                bool isSel = i == _selected;
                using (new EditorGUILayout.VerticalScope(isSel ? EditorStyles.helpBox : GUIStyle.none))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Toggle(isSel, $"#{i + 1}", EditorStyles.miniButton, GUILayout.Width(40)) && !isSel)
                        {
                            _selected = i;
                            GUI.FocusControl(null);
                        }
                        EditorGUILayout.LabelField($"{(Vector2)p.FindPropertyRelative("Position").vector3Value}  {p.FindPropertyRelative("Size").vector2Value}", EditorStyles.miniLabel, GUILayout.MinWidth(0));
                        if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                        {
                            RemovePoint(i);
                            Apply();
                            GUIUtility.ExitGUI();
                        }
                    }
                    if (isSel)
                    {
                        EditorGUIUtility.labelWidth = 60;
                        SerializedProperty posProp = p.FindPropertyRelative("Position");
                        EditorGUI.BeginChangeCheck();
                        Vector2 newPos = EditorGUILayout.Vector2Field("Vị trí", posProp.vector3Value);
                        if (EditorGUI.EndChangeCheck()) posProp.vector3Value = newPos;
                        EditorGUILayout.PropertyField(p.FindPropertyRelative("Size"), new GUIContent("Kích cỡ"));
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUI.enabled = i > 0;
                            if (GUILayout.Button("▲ Lên", EditorStyles.miniButtonLeft)) { Points.MoveArrayElement(i, i - 1); _selected = i - 1; }
                            GUI.enabled = i < PointCount - 1;
                            if (GUILayout.Button("▼ Xuống", EditorStyles.miniButtonRight)) { Points.MoveArrayElement(i, i + 1); _selected = i + 1; }
                            GUI.enabled = true;
                        }
                        EditorGUIUtility.labelWidth = 0;
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(PointCount == 0))
            {
                if (GUILayout.Button("Xóa tất cả điểm") &&
                    EditorUtility.DisplayDialog("Level Editor", $"Xóa {PointCount} điểm của level {_level.NumberLevel}?", "Xóa", "Hủy"))
                {
                    Points.ClearArray();
                    _selected = -1;
                    Apply();
                }
            }

            EditorGUILayout.Space();
            DrawAutoDetect();
        }
    }

    #endregion

    #region Auto detect

    private void DrawAutoDetect()
    {
        EditorGUILayout.LabelField("Tự dò khác biệt A/B", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Chỉ hiệu quả khi B được sửa từ A (cùng bố cục, chỉ khác vài chỗ). Ảnh vẽ lại toàn bộ sẽ ra vùng rất lớn.", EditorStyles.wordWrappedMiniLabel);
        EditorGUIUtility.labelWidth = 110;
        _diffThreshold = EditorGUILayout.Slider(new GUIContent("Ngưỡng màu", "Độ lệch màu tối thiểu để tính là khác"), _diffThreshold, 0.02f, 0.5f);
        _diffGrid = EditorGUILayout.IntSlider(new GUIContent("Độ phân giải", "Số ô theo chiều ngang khi so sánh"), _diffGrid, 64, 512);
        _diffMinCells = EditorGUILayout.IntSlider(new GUIContent("Bỏ vùng < (ô)", "Lọc nhiễu: bỏ vùng nhỏ hơn số ô này"), _diffMinCells, 1, 50);
        _diffPadding = EditorGUILayout.Slider(new GUIContent("Nới rộng (px)", "Cộng thêm vào mỗi cạnh của vùng dò được"), _diffPadding, 0f, 60f);
        EditorGUIUtility.labelWidth = 0;

        using (new EditorGUI.DisabledScope(_level.RawSprite == null || _level.ErrorSprite == null))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Dò & thay thế")) RunAutoDetect(true);
                if (GUILayout.Button("Dò & thêm vào")) RunAutoDetect(false);
            }
        }
    }

    private void RunAutoDetect(bool replace)
    {
        if (replace && PointCount > 0 &&
            !EditorUtility.DisplayDialog("Level Editor", $"Thay {PointCount} điểm hiện tại bằng kết quả dò?", "Thay", "Hủy"))
            return;

        Sprite a = _level.RawSprite;
        int gw = _diffGrid;
        int gh = Mathf.Max(1, Mathf.RoundToInt(gw * a.textureRect.height / a.textureRect.width));
        Color[] pa = ReadSprite(a, gw, gh);
        Color[] pb = ReadSprite(_level.ErrorSprite, gw, gh);

        var mask = new bool[gw * gh];
        for (int i = 0; i < mask.Length; i++)
        {
            Color d = pa[i] - pb[i];
            mask[i] = Mathf.Max(Mathf.Abs(d.r), Mathf.Abs(d.g), Mathf.Abs(d.b)) > _diffThreshold;
        }

        List<RectInt> boxes = FindRegions(Dilate(mask, gw, gh, 2), mask, gw, gh);
        Vector2 cell = new Vector2(_imageSize.x / gw, _imageSize.y / gh);
        var found = boxes.Select(b =>
        {
            Vector2 min = new Vector2(b.xMin * cell.x, b.yMin * cell.y) - _imageSize * 0.5f - Vector2.one * _diffPadding;
            Vector2 max = new Vector2(b.xMax * cell.x, b.yMax * cell.y) - _imageSize * 0.5f + Vector2.one * _diffPadding;
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }).ToList();
        found = MergeOverlapping(found);

        if (replace) Points.ClearArray();
        foreach (Rect r in found.OrderByDescending(r => r.center.y).ThenBy(r => r.center.x))
            AddPoint(r.center, r.size);
        _selected = -1;
        Apply();
        ShowNotification(new GUIContent($"Dò được {found.Count} vùng"));
    }

    // Đọc pixel qua RenderTexture nên không cần bật Read/Write cho texture.
    private static Color[] ReadSprite(Sprite sprite, int w, int h)
    {
        Texture2D tex = sprite.texture;
        Rect tr = sprite.textureRect;
        var scale = new Vector2(tr.width / tex.width, tr.height / tex.height);
        var offset = new Vector2(tr.x / tex.width, tr.y / tex.height);

        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture prev = RenderTexture.active;
        Graphics.Blit(tex, rt, scale, offset);
        RenderTexture.active = rt;
        var read = new Texture2D(w, h, TextureFormat.RGBA32, false);
        read.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        read.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        Color[] pixels = read.GetPixels();
        DestroyImmediate(read);
        return pixels;
    }

    private static bool[] Dilate(bool[] mask, int w, int h, int radius)
    {
        var result = new bool[mask.Length];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (!mask[y * w + x]) continue;
            for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx >= 0 && ny >= 0 && nx < w && ny < h) result[ny * w + nx] = true;
            }
        }
        return result;
    }

    // Gom vùng liên thông trên mask đã nới; bounding box tính theo các ô khác biệt thật (raw).
    private List<RectInt> FindRegions(bool[] grown, bool[] raw, int w, int h)
    {
        var boxes = new List<RectInt>();
        var visited = new bool[grown.Length];
        var stack = new Stack<int>();

        for (int start = 0; start < grown.Length; start++)
        {
            if (!grown[start] || visited[start]) continue;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1, count = 0;
            visited[start] = true;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                int x = i % w, y = i / w;
                if (raw[i])
                {
                    count++;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
                if (x > 0) Visit(i - 1);
                if (x < w - 1) Visit(i + 1);
                if (y > 0) Visit(i - w);
                if (y < h - 1) Visit(i + w);
            }
            if (count >= _diffMinCells) boxes.Add(new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1));
        }
        return boxes;

        void Visit(int n)
        {
            if (!grown[n] || visited[n]) return;
            visited[n] = true;
            stack.Push(n);
        }
    }

    private static List<Rect> MergeOverlapping(List<Rect> rects)
    {
        bool merged = true;
        while (merged)
        {
            merged = false;
            for (int i = 0; i < rects.Count && !merged; i++)
            for (int j = i + 1; j < rects.Count; j++)
            {
                if (!rects[i].Overlaps(rects[j])) continue;
                rects[i] = Rect.MinMaxRect(Mathf.Min(rects[i].xMin, rects[j].xMin), Mathf.Min(rects[i].yMin, rects[j].yMin),
                                           Mathf.Max(rects[i].xMax, rects[j].xMax), Mathf.Max(rects[i].yMax, rects[j].yMax));
                rects.RemoveAt(j);
                merged = true;
                break;
            }
        }
        return rects;
    }

    #endregion
}
