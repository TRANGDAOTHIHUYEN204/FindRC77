#if UNITY_IOS
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using Debug = UnityEngine.Debug; // Fix conflict namespace

public class AutoEmbedPostProcessor
{
    [PostProcessBuild(2000)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
    {
        if (buildTarget != BuildTarget.iOS) return;

        Debug.Log(">>> [AutoEmbed v11] Bắt đầu xử lý project Xcode...");

        var projPath = PBXProject.GetPBXProjectPath(buildPath);
        var proj = new PBXProject();
        proj.ReadFromString(File.ReadAllText(projPath));

        var mainTargetGuid = proj.GetUnityMainTargetGuid();
        var unityFrameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();
        
        // --- Cấu hình Build Settings ---
        // Tắt Bitcode (không còn là yêu cầu của Apple và nhiều SDK không hỗ trợ)
        proj.SetBuildProperty(mainTargetGuid, "ENABLE_BITCODE", "NO");
        proj.SetBuildProperty(unityFrameworkTargetGuid, "ENABLE_BITCODE", "NO");
        
        // Luôn Embed Swift Standard Libraries
        proj.SetBuildProperty(mainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
        proj.SetBuildProperty(unityFrameworkTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
        

        // --- Lấy danh sách đen từ Podfile ---
        // Đọc trực tiếp Podfile để lấy danh sách các pod được định nghĩa trong target 'Unity-iPhone'.
        // Đây là những framework mà CocoaPods sẽ tự động embed, ta cần bỏ qua chúng.
        var ignoredFrameworks = GetIgnoredPodsFromPodfile(buildPath);
        Debug.Log($"[AutoEmbed] Các framework sẽ được bỏ qua (do Pods quản lý trong target Unity-iPhone): {string.Join(", ", ignoredFrameworks)}");

        // Bắt đầu quét và nhúng các framework cần thiết
        ScanAndForceEmbed(buildPath, proj, mainTargetGuid, ignoredFrameworks);

        File.WriteAllText(projPath, proj.WriteToString());
        Debug.Log(">>> [AutoEmbed v11] HOÀN TẤT!");
    }

    /// <summary>
    /// Đọc file Podfile để tìm các pod được khai báo trong target 'Unity-iPhone'.
    /// </summary>
    private static HashSet<string> GetIgnoredPodsFromPodfile(string buildPath)
    {
        var ignoredPods = new HashSet<string>();
        var podfilePath = Path.Combine(buildPath, "Podfile");

        if (!File.Exists(podfilePath))
        {
            Debug.LogWarning("[AutoEmbed] Không tìm thấy file Podfile. Sẽ tiến hành embed tất cả dynamic framework tìm được.");
            return ignoredPods;
        }

        var lines = File.ReadAllLines(podfilePath);
        bool inUnityIPhoneTarget = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Phát hiện bắt đầu khối target 'Unity-iPhone'
            if (Regex.IsMatch(trimmedLine, @"target\s+'Unity-iPhone'\s+do"))
            {
                inUnityIPhoneTarget = true;
                continue;
            }

            // Phát hiện kết thúc khối
            if (inUnityIPhoneTarget && trimmedLine == "end")
            {
                inUnityIPhoneTarget = false;
                break; // Đã xử lý xong target 'Unity-iPhone', không cần đọc thêm
            }

            // Nếu đang ở trong khối target, tìm các dòng khai báo pod
            if (inUnityIPhoneTarget)
            {
                // Regex để bắt `pod 'PodName'` hoặc `pod "PodName"`
                var match = Regex.Match(trimmedLine, @"^pod\s+['""]([^'""/]+)");
                if (match.Success)
                {
                    var podName = match.Groups[1].Value;
                    // Thêm vào danh sách dưới dạng tên framework để so sánh sau này
                    ignoredPods.Add(podName + ".framework");
                }
            }
        }
        return ignoredPods;
    }

    private static void ScanAndForceEmbed(string buildPath, PBXProject proj, string mainTargetGuid, HashSet<string> ignoredList)
    {
        // Quét tất cả framework trong folder build
        var frameworkPaths = Directory.GetDirectories(buildPath, "*.framework", SearchOption.AllDirectories);

        foreach (var fullPath in frameworkPaths)
        {
            var fileName = Path.GetFileName(fullPath);
            
            Debug.Log($"process: {fileName}");
            
            // 2. Bỏ qua những framework nằm trong danh sách đen (đã được định nghĩa trong target 'Unity-iPhone' của Podfile)
            if (ignoredList.Contains(fileName))
            {
                Debug.Log($"[AutoEmbed] Bỏ qua {fileName} vì đã được Podfile quản lý.");
                continue;
            }
            
            // 3. Chỉ xử lý Dynamic Framework (loại gây crash nếu không được embed)
            if (IsDynamicFramework(fullPath))
            {
                EmbedFrameworkSafe(proj, mainTargetGuid, fullPath, fileName, buildPath);
            }
            else
            {
                Debug.Log($"[AutoEmbed] Bỏ qua {fileName} vì không phải là Dynamic Framework.");
            }
        }
    }

    private static void EmbedFrameworkSafe(PBXProject proj, string targetGuid, string fullPath, string fileName, string buildPath)
    {
        var relativePath = MakeRelativePath(buildPath, fullPath);
        
        // --- BƯỚC 1: Tìm xem file đã có trong Project chưa ---
        var fileGuid = proj.FindFileGuidByProjectPath(relativePath);
        
        // Tìm dự phòng bằng tên file (nếu path bị lệch)
        if (string.IsNullOrEmpty(fileGuid)) fileGuid = proj.FindFileGuidByProjectPath("Frameworks/" + fileName);
        if (string.IsNullOrEmpty(fileGuid)) fileGuid = proj.FindFileGuidByProjectPath("Pods/" + fileName);

        // --- BƯỚC 2: Nếu chưa có -> THÊM MỚI ---
        if (string.IsNullOrEmpty(fileGuid))
        {
            // Thêm file tham chiếu vào project, đặt nó vào group "Frameworks" trên cây thư mục Xcode
            fileGuid = proj.AddFile(relativePath, "Frameworks/" + fileName, PBXSourceTree.Source);
            Debug.Log($"[AutoEmbed] Đã thêm Reference mới vào Project: {fileName}");
        }

        // --- BƯỚC 3: Embed & Sign ---
        if (!string.IsNullOrEmpty(fileGuid))
        {
            // Hàm này sẽ thêm framework vào mục "Frameworks, Libraries, and Embedded Content" với trạng thái "Embed & Sign"
            PBXProjectExtensions.AddFileToEmbedFrameworks(proj, targetGuid, fileGuid);
            
            // Đảm bảo framework cũng được link (Link Binary With Libraries)
            proj.AddFileToBuild(targetGuid, fileGuid);
            
            Debug.Log($"[AutoEmbed] Đã FORCE EMBED & SIGN: {fileName}");
        }
    }

    // --- CÁC HÀM HELPER ---

    private static string MakeRelativePath(string basePath, string fullPath)
    {
        // Đảm bảo sử dụng dấu / cho đường dẫn
        basePath = basePath.Replace("\\", "/");
        fullPath = fullPath.Replace("\\", "/");
        if (fullPath.StartsWith(basePath))
        {
            return fullPath.Substring(basePath.Length + (basePath.EndsWith("/") ? 0 : 1));
        }
        return fullPath;
    }

    // Kiểm tra file là Dynamic hay Static bằng lệnh 'file' của macOS
    private static bool IsDynamicFramework(string frameworkPath)
    {
        try
        {
            var binaryName = Path.GetFileNameWithoutExtension(frameworkPath);
            var binaryPath = Path.Combine(frameworkPath, binaryName);
            if (!File.Exists(binaryPath)) return false;

            var info = new ProcessStartInfo
            {
                FileName = "/usr/bin/file",
                Arguments = $"\"{binaryPath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(info))
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                // Chuỗi này xác định một thư viện liên kết động
                return output.Contains("dynamically linked shared library");
            }
        }
        catch { return false; }
    }
}
#endif