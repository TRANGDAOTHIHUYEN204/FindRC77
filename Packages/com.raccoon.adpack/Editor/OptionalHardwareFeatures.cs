using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace RaccoonAds.Editor
{
    /// <summary>
    /// Đánh dấu các tính năng phần cứng là không bắt buộc trong manifest của app (launcher).
    ///
    /// SDK quảng cáo / AppsFlyer thêm permission như ACCESS_WIFI_STATE. Google Play suy ra từ đó
    /// tính năng bắt buộc android.hardware.wifi, nên app bị ẩn trên máy không có wifi, Google Play Games
    /// trên PC, Android XR... và Play Console báo "bị hạn chế phạm vi cung cấp do tính năng phần cứng bắt buộc".
    /// Khai &lt;uses-feature required="false"&gt; ở manifest app (độ ưu tiên cao nhất khi merge, kèm tools:replace)
    /// thì Play không suy ra tính năng bắt buộc nữa. Permission vẫn giữ nguyên.
    ///
    /// Tự chạy mỗi lần build Android (sửa manifest launcher trong project Gradle sinh ra).
    /// Menu Raccoon > Fix Android Manifest ghi thêm vào các manifest trong Assets/Plugins/Android để thấy trong git.
    /// </summary>
    public class OptionalHardwareFeatures : IPostGenerateGradleAndroidProject
    {
        private const string AndroidNs = "http://schemas.android.com/apk/res/android";
        private const string ToolsNs = "http://schemas.android.com/tools";
        private const string Tag = "[RaccoonAds][Manifest]";

        // Tính năng mà permission của SDK hay kéo theo, cộng hướng màn hình (activity khóa dọc/ngang).
        private static readonly string[] Features =
        {
            "android.hardware.wifi",
            "android.hardware.telephony",
            "android.hardware.location",
            "android.hardware.location.network",
            "android.hardware.location.gps",
            "android.hardware.bluetooth",
            "android.hardware.screen.portrait",
            "android.hardware.screen.landscape",
        };

        // Chạy sau các plugin khác để không bị ghi đè.
        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string unityLibraryPath)
        {
            var manifest = Path.Combine(unityLibraryPath, "..", "launcher", "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifest))
            {
                Debug.LogWarning($"{Tag} Không thấy manifest launcher: {manifest}");
                return;
            }
            int changed = Patch(manifest);
            if (changed > 0)
                Debug.Log($"{Tag} Build: đánh dấu {changed} tính năng phần cứng là không bắt buộc (android:required=false).");
        }

        [MenuItem("Raccoon/Fix Android Manifest")]
        private static void FixFromMenu()
        {
            var files = new[]
            {
                "Assets/Plugins/Android/AndroidManifest.xml",
                "Assets/Plugins/Android/LauncherManifest.xml",
            };
            var lines = new List<string>();
            foreach (var f in files)
            {
                if (!File.Exists(f)) continue;
                int changed = Patch(f);
                lines.Add(changed > 0 ? $"{f}: thêm/sửa {changed} mục" : $"{f}: đã đủ");
            }
            if (lines.Count > 0) AssetDatabase.Refresh();

            var msg = (lines.Count > 0 ? string.Join("\n", lines) : "Không có manifest tùy chỉnh trong Assets/Plugins/Android.") +
                      "\n\nCác tính năng sau được đánh dấu android:required=\"false\":\n" + string.Join("\n", Features) +
                      "\n\nNgoài ra mỗi lần build Android kit tự sửa manifest của app (launcher), kể cả project không có manifest tùy chỉnh.";
            Debug.Log($"{Tag} {msg}");
            EditorUtility.DisplayDialog("Fix Android Manifest", msg, "OK");
        }

        /// <summary>Thêm uses-feature required=false cho mọi tính năng trong danh sách. Trả về số mục đã đổi.</summary>
        private static int Patch(string manifestPath)
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(manifestPath);
            var root = doc.DocumentElement;
            if (root == null || root.Name != "manifest") return 0;
            if (!root.HasAttribute("xmlns:tools")) root.SetAttribute("xmlns:tools", ToolsNs);

            int changed = 0;
            foreach (var name in Features)
            {
                XmlElement node = null;
                foreach (XmlNode n in root.SelectNodes("uses-feature"))
                {
                    if (n is XmlElement e && e.GetAttribute("name", AndroidNs) == name) { node = e; break; }
                }
                if (node == null)
                {
                    node = doc.CreateElement("uses-feature");
                    node.SetAttribute("name", AndroidNs, name);
                    root.InsertBefore(node, root.FirstChild);
                    root.InsertBefore(doc.CreateTextNode("\n    "), node);
                }
                else if (node.GetAttribute("required", AndroidNs) == "false" &&
                         node.GetAttribute("replace", ToolsNs).Contains("android:required"))
                {
                    continue;
                }
                node.SetAttribute("required", AndroidNs, "false");
                node.SetAttribute("replace", ToolsNs, "android:required");
                changed++;
            }

            if (changed > 0)
            {
                var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false) };
                using (var w = XmlWriter.Create(manifestPath, settings)) doc.Save(w);
            }
            return changed;
        }
    }
}
