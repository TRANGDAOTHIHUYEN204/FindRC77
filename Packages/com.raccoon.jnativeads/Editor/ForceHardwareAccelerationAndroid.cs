#if UNITY_EDITOR && UNITY_ANDROID

using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Xml;
using UnityEngine;

/// <summary>
/// Ensures android:hardwareAccelerated="true" is applied to the application and all activities.
/// </summary>
public class ForceHardwareAccelerationAndroid : IPostGenerateGradleAndroidProject
{
    public int callbackOrder { get { return 999; } }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        ForceHardwareAcceleration(path);
    }

    private static bool ForceHardwareAcceleration(string path)
    {
        Debug.Log("--- ForceHardwareAcceleration: Starting execution ---");

        var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");

        if (!File.Exists(manifestPath))
        {
            Debug.LogError("ForceHardwareAcceleration: AndroidManifest.xml not found at: " + manifestPath);
            return false;
        }

        Debug.Log("ForceHardwareAcceleration: Found AndroidManifest.xml. Starting modification...");

        var manifest = new XmlDocument();
        manifest.Load(manifestPath);

        var nsManager = new XmlNamespaceManager(manifest.NameTable);
        nsManager.AddNamespace("android", "http://schemas.android.com/apk/res/android");
        var androidNs = "http://schemas.android.com/apk/res/android";

        // Ensure application node exists
        var applicationNode = manifest.SelectSingleNode("/manifest/application", nsManager) as XmlElement;
        if (applicationNode == null)
        {
            Debug.LogWarning("ForceHardwareAcceleration: <application> tag not found in manifest.");
            return false;
        }

        // Set on application
        SetHardwareAccelerated(applicationNode, androidNs, "application");

        // Set on all activities under application
        var activityNodes = manifest.SelectNodes("/manifest/application//activity", nsManager);
        if (activityNodes != null && activityNodes.Count > 0)
        {
            foreach (XmlNode node in activityNodes)
            {
                if (node is XmlElement element)
                {
                    SetHardwareAccelerated(element, androidNs, "activity");
                }
            }
        }
        else
        {
            Debug.Log("ForceHardwareAcceleration: No <activity> tags found under <application>.");
        }

        manifest.Save(manifestPath);
        Debug.Log("--- ForceHardwareAcceleration: Modification successful! ---");
        return true;
    }

    private static void SetHardwareAccelerated(XmlElement element, string androidNamespace, string elementType)
    {
        var current = element.GetAttribute("hardwareAccelerated", androidNamespace);
        if (!string.IsNullOrEmpty(current))
        {
            Debug.Log($"ForceHardwareAcceleration: {elementType} '{element.GetAttribute("name", androidNamespace) ?? element.GetAttribute("android:name")}' had hardwareAccelerated='{current}'. Overwriting to 'true'.");
        }
        else
        {
            Debug.Log($"ForceHardwareAcceleration: {elementType} '{element.GetAttribute("name", androidNamespace) ?? element.GetAttribute("android:name")}' did not have hardwareAccelerated. Setting to 'true'.");
        }

        element.SetAttribute("hardwareAccelerated", androidNamespace, "true");
    }
}
#endif