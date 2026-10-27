using UnityEditor;
using UnityEngine;

public class OutlineClickTool : EditorWindow
{
    static readonly Vector2 Design = new Vector2(1040f, 650f);   // cùng DesignSize với DifferentPair

    LevelData level;
    RectTransform root;      // RectTransform ảnh A (cùng cái truyền vào rootA)
    int index;
    bool capturing;

    [MenuItem("Tools/Outline Click Tool")]
    static void Open() => GetWindow<OutlineClickTool>("Outline Click");

    void OnEnable()  => SceneView.duringSceneGui += OnScene;
    void OnDisable() => SceneView.duringSceneGui -= OnScene;

    // ---------- helpers ----------
    Vector2 Scale()
    {
        Vector2 r = root.rect.size;
        return new Vector2(r.x / Design.x, r.y / Design.y);
    }

    SerializedProperty GetPoint(SerializedObject so, int i)
        => so.FindProperty("differentPoints").GetArrayElementAtIndex(i);

    bool Valid(out SerializedObject so)
    {
        so = null;
        if (level == null || root == null) return false;
        so = new SerializedObject(level);
        var list = so.FindProperty("differentPoints");
        return list != null && index >= 0 && index < list.arraySize;
    }

    // ---------- window ----------
    void OnGUI()
    {
        level = (LevelData)EditorGUILayout.ObjectField("Level Data", level, typeof(LevelData), false);
        root  = (RectTransform)EditorGUILayout.ObjectField("Root ảnh A (scene)", root, typeof(RectTransform), true);

        if (level == null || root == null)
        {
            EditorGUILayout.HelpBox("Kéo LevelData và RectTransform ảnh A vào.", MessageType.Info);
            return;
        }

        var so = new SerializedObject(level);
        int count = so.FindProperty("differentPoints").arraySize;
        index = Mathf.Clamp(EditorGUILayout.IntSlider("Điểm khác biệt (index)", index, 0, Mathf.Max(0, count - 1)), 0, Mathf.Max(0, count - 1));

        if (Valid(out so))
        {
            var p = GetPoint(so, index);
            int n = p.FindPropertyRelative("Outline").arraySize;
            EditorGUILayout.LabelField($"Position {p.FindPropertyRelative("Position").vector3Value}   Size {p.FindPropertyRelative("Size").vector2Value}   Số đỉnh: {n}");
        }

        EditorGUILayout.Space();
        GUI.backgroundColor = capturing ? Color.green : Color.white;
        if (GUILayout.Button(capturing ? "ĐANG VẼ - bấm để dừng (hoặc Esc)" : "Bắt đầu click vẽ", GUILayout.Height(32)))
            capturing = !capturing;
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox("Click trái trong Scene view = thêm đỉnh.\nCtrl+Z = hoàn tác đỉnh vừa thêm.\nGiữ Alt để xoay/kéo camera.", MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Xóa đỉnh cuối")) RemoveLast();
            if (GUILayout.Button("Xóa hết điểm này")) ClearOutline();
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Fit Position/Size theo viền")) FitBounds();
            if (GUILayout.Button("Điểm tiếp theo ▶")) { index = Mathf.Min(index + 1, count - 1); SceneView.RepaintAll(); }
        }
    }

    void RemoveLast()
    {
        if (!Valid(out var so)) return;
        Undo.RecordObject(level, "Remove outline vertex");
        var o = GetPoint(so, index).FindPropertyRelative("Outline");
        if (o.arraySize > 0) o.arraySize--;
        so.ApplyModifiedProperties();
        SceneView.RepaintAll();
    }

    void ClearOutline()
    {
        if (!Valid(out var so)) return;
        Undo.RecordObject(level, "Clear outline");
        GetPoint(so, index).FindPropertyRelative("Outline").arraySize = 0;
        so.ApplyModifiedProperties();
        SceneView.RepaintAll();
    }

    // Đặt lại Position = tâm viền, Size = khung bao viền (Outline vẫn giữ đúng vị trí thực)
    void FitBounds()
    {
        if (!Valid(out var so)) return;
        var p = GetPoint(so, index);
        var o = p.FindPropertyRelative("Outline");
        if (o.arraySize < 2) return;

        Vector3 pos = p.FindPropertyRelative("Position").vector3Value;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = -min;
        for (int i = 0; i < o.arraySize; i++)
        {
            Vector3 a = pos + o.GetArrayElementAtIndex(i).vector3Value;
            min = Vector2.Min(min, a); max = Vector2.Max(max, a);
        }
        Vector3 newPos = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, pos.z);

        Undo.RecordObject(level, "Fit outline bounds");
        for (int i = 0; i < o.arraySize; i++)
        {
            var e = o.GetArrayElementAtIndex(i);
            Vector3 abs = pos + e.vector3Value;
            e.vector3Value = new Vector3(abs.x - newPos.x, abs.y - newPos.y, 0f);
        }
        p.FindPropertyRelative("Position").vector3Value = newPos;
        p.FindPropertyRelative("Size").vector2Value = max - min;
        so.ApplyModifiedProperties();
        SceneView.RepaintAll();
    }

    // ---------- scene view ----------
    void OnScene(SceneView sv)
    {
        if (!Valid(out var so)) return;

        Vector2 s = Scale();
        Vector3 ToWorld(Vector2 d) => root.TransformPoint(new Vector3(d.x * s.x, d.y * s.y, 0f));

        // vẽ tất cả outline đã có (điểm đang chọn màu đỏ, còn lại màu vàng mờ)
        var list = so.FindProperty("differentPoints");
        for (int k = 0; k < list.arraySize; k++)
        {
            var el = list.GetArrayElementAtIndex(k);
            Vector3 pos = el.FindPropertyRelative("Position").vector3Value;
            var o = el.FindPropertyRelative("Outline");
            int n = o.arraySize;
            if (n == 0) continue;

            var pts = new Vector3[n + 1];
            for (int i = 0; i < n; i++)
                pts[i] = ToWorld((Vector2)pos + (Vector2)o.GetArrayElementAtIndex(i).vector3Value);
            pts[n] = pts[0];

            bool sel = k == index;
            Handles.color = sel ? Color.red : new Color(1f, 0.9f, 0.2f, 0.5f);
            Handles.DrawAAPolyLine(sel ? 3f : 1.5f, pts);

            if (sel)
                for (int i = 0; i < n; i++)
                {
                    Handles.DrawSolidDisc(pts[i], root.forward, HandleUtility.GetHandleSize(pts[i]) * 0.04f);
                    Handles.Label(pts[i], i.ToString());
                }
        }

        // tâm + khung Size của điểm đang chọn, để dễ biết đang click vào chỗ nào
        {
            var el = GetPoint(so, index);
            Vector2 pos = el.FindPropertyRelative("Position").vector3Value;
            Vector2 h = el.FindPropertyRelative("Size").vector2Value * 0.5f;
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(ToWorld(pos), root.forward, HandleUtility.GetHandleSize(ToWorld(pos)) * 0.05f);
            Handles.DrawPolyLine(ToWorld(pos + new Vector2(-h.x, h.y)), ToWorld(pos + new Vector2(h.x, h.y)),
                                 ToWorld(pos + new Vector2(h.x, -h.y)), ToWorld(pos + new Vector2(-h.x, -h.y)),
                                 ToWorld(pos + new Vector2(-h.x, h.y)));
        }

        if (!capturing) return;

        Event e = Event.current;
        int id = GUIUtility.GetControlID(FocusType.Passive);

        if (e.type == EventType.Layout)
            HandleUtility.AddDefaultControl(id);   // chặn việc click chọn object khác

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            capturing = false; e.Use(); Repaint(); return;
        }

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            var plane = new Plane(root.forward, root.position);
            if (plane.Raycast(ray, out float t))
            {
                Vector3 local = root.InverseTransformPoint(ray.GetPoint(t));
                Vector2 design = new Vector2(local.x / s.x, local.y / s.y);   // đổi về hệ DesignSize
                Vector2 pos = GetPoint(so, index).FindPropertyRelative("Position").vector3Value;

                Undo.RecordObject(level, "Add outline vertex");
                var o = GetPoint(so, index).FindPropertyRelative("Outline");
                int n = o.arraySize;
                o.arraySize = n + 1;
                o.GetArrayElementAtIndex(n).vector3Value = (Vector3)(design - pos);   // tọa độ so với Position
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(level);

                Repaint();
            }
            e.Use();
        }

        sv.Repaint();
    }
}