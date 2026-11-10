using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

public class BuildConfigEditor : EditorWindow
{
    private static string DefaultDefinesPrefsKey => new DirectoryInfo(Application.dataPath).Parent.Name;
    private const  string DefaultAutoKeystorePassword = "123456";

    private string definesPrefsKey;
    private string autoKeystorePassword  = DefaultAutoKeystorePassword;
    private string editingKeystorePass   = "";

    private string newVersion     = "";
    private int    newVersionCode = 1;

    private List<string> customDefines = new List<string>();
    private Vector2      scrollPos;

    // Scanner fields
    private List<string> scanDirectories = new List<string>();
    private Vector2      directoriesScrollPos;
    private List<string> scannedSymbols = new List<string>();


    private bool   buildAppBundleChecked = false; // To store the state of the "Build App Bundle" toggle
    private bool   autoPasswordChecked   = true;
    private string keystorePassword      = "";
    
    
    [MenuItem("Tools/Chars Kit/Build Config Editor %t", priority = 1)]
    public static void OpenWindow()
    {
        var window = GetWindow<BuildConfigEditor>("Build Config Editor");
        window.minSize = new Vector2(window.minSize.x, 400); // Set onl
        
        EditorApplication.ExecuteMenuItem("File/Build Profiles");

    }

    private void OnEnable()
    {
        newVersion = PlayerSettings.bundleVersion;
#if UNITY_ANDROID
        newVersionCode = PlayerSettings.Android.bundleVersionCode;
#endif
        definesPrefsKey = DefaultDefinesPrefsKey;
        LoadCustomDefines();
        LoadAutoKeystorePassword();
        LoadScanDirectories();
    }

    private void OnDisable()
    {
        SaveCustomDefines();
        SaveScanDirectories();
    }

    private void OnGUI()
    {
        // General infos------------------------------------------------------------------------------------------------
        EditorGUILayout.LabelField("First time settings:", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Prefs Key:", GUILayout.Width(110));
        EditorGUILayout.LabelField(definesPrefsKey, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();


        // Auto Keystore Password Editor
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Auto Password:", GUILayout.Width(110));
        editingKeystorePass = EditorGUILayout.PasswordField(editingKeystorePass);
        if (GUILayout.Button("Apply", GUILayout.Width(60)))
        {
            ApplyAutoKeystorePasswordChange();
        }
        EditorGUILayout.EndHorizontal();
        
        // Scanner Section--------------------------------------------------
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scan Directories for Symbols", EditorStyles.label);

        // Directory list buttons
        EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+Add Directory", GUILayout.Width(100)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Directory to Scan", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path) && !scanDirectories.Contains(path))
                {
                    scanDirectories.Add(path);
                    SaveScanDirectories();
                }
            }
            if (GUILayout.Button("Scan All", GUILayout.Width(80)))
            {
                ScanAllDirectoriesForSymbols();
                if (scannedSymbols.Count > 0)
                {
                    ScannedSymbolsPopup.Show(scannedSymbols, customDefines, OnSymbolsSelected);
                }
            }
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                scanDirectories.Clear();
                scannedSymbols.Clear();
                SaveScanDirectories();
            }
            EditorGUILayout.EndHorizontal();

            // Directory list
            if (scanDirectories.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                directoriesScrollPos = EditorGUILayout.BeginScrollView(directoriesScrollPos, GUILayout.Height(60));
                for (int i = 0; i < scanDirectories.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(scanDirectories[i], EditorStyles.miniLabel);
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        scanDirectories.RemoveAt(i);
                        SaveScanDirectories();
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }


        EditorGUILayout.Space();
        EditorGUILayout.LabelField("General infos:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Project Version:", EditorStyles.label, GUILayout.Width(210));
        newVersion = EditorGUILayout.TextField(newVersion);
        EditorGUILayout.EndHorizontal();
        
        // Auto Password
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Use Auto Password:", GUILayout.Width(210));
        autoPasswordChecked = EditorGUILayout.Toggle(autoPasswordChecked);
        EditorGUILayout.EndHorizontal();

        if (autoPasswordChecked)
        {
            keystorePassword = autoKeystorePassword;
        }
        else
        {
            keystorePassword = EditorGUILayout.PasswordField("Keystore Password", keystorePassword);
        }

#if UNITY_ANDROID
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Android Version Code (bundleVersionCode):", EditorStyles.label);
        newVersionCode = EditorGUILayout.IntField(newVersionCode);
#endif

        // Define Symbols-----------------------------------------------------------------------------------------------
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Tracked Custom Scripting Define Symbols:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply All", GUILayout.Width(100)))
        {
            ApplyAllSymbols();
        }
        if (GUILayout.Button("Remove All", GUILayout.Width(100)))
        {
            RemoveAllSymbols();
        }
        if (GUILayout.Button("+", GUILayout.Width(100)))
        {
            customDefines.Add("");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginVertical("box");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
        for (int i = 0; i < customDefines.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            customDefines[i] = EditorGUILayout.TextField(customDefines[i]);

            if (GUILayout.Button(SymbolExists(customDefines[i]) ? "Remove" : "Add", GUILayout.Width(80)))
            {
                ToggleSymbol(customDefines[i]);
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                customDefines.RemoveAt(i);
                i--; // Adjust index because list shrinks
                SaveCustomDefines();
                ApplyAllSymbols();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        // Build Settings--------------------------------------------------
        EditorGUILayout.Space();
        // "Build App Bundle (Google Play)" checkbox
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Build App Bundle (Google Play):", GUILayout.Width(210));
        buildAppBundleChecked = EditorGUILayout.Toggle(buildAppBundleChecked);
        EditorGUILayout.EndHorizontal();
       
        if (GUILayout.Button("Apply Config"))
        {
            ApplyConfig();
        }
        EditorGUILayout.Space();
    }

    private void ApplyConfig()
    {
        if (string.IsNullOrEmpty(newVersion))
        {
            LogWarn("Version cannot be empty!");
        }
        else
        {
            // Update the main bundle version
            LogNormal($"Version updated: {PlayerSettings.bundleVersion} --> {newVersion}");
            PlayerSettings.bundleVersion = newVersion;
        }
        
        
#if UNITY_ANDROID
        // For Android, set bundleVersionCode
        if (newVersionCode < 1)
        {
            LogWarn($"Android Version Code ({newVersionCode}) must be greater than current!");
        }
        else
        {
            LogNormal($"Bundle Version Code updated: {PlayerSettings.Android.bundleVersionCode} --> {newVersionCode}");
            PlayerSettings.Android.bundleVersionCode = newVersionCode;
        }
        
        // Apply "Build App Bundle (Google Play)" setting
        if (buildAppBundleChecked)
        {
            EditorUserBuildSettings.buildAppBundle       = true;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
            LogNormal("AAB Build");
        }
        else
        {
            EditorUserBuildSettings.buildAppBundle       = false;
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
            LogNormal("APK Build");
        }

        ApplyKeystorePassword();
#endif
    }
    
    
    /*----symbols-------------------------------------------------------------------------------------------------------*/
    private void ToggleSymbol(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
            return;

        var currentDefines = GetCurrentDefines();
        var symbols = new HashSet<string>(currentDefines.Split(';'));

        if (symbols.Contains(symbol))
        {
            symbols.Remove(symbol);
            LogNormal($"Removed Symbol: {symbol}");
        }
        else
        {
            symbols.Add(symbol);
            LogNormal($"Added Symbol: {symbol}");
        }

        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, string.Join(";", symbols));
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, string.Join(";", symbols));
#if UNITY_IOS
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, string.Join(";", symbols));
#endif
    }

    private bool SymbolExists(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
            return false;

        var currentDefines = GetCurrentDefines();
        return new HashSet<string>(currentDefines.Split(';')).Contains(symbol);
    }
    
    private void ApplyAllSymbols()
    {
        foreach (var symbol in customDefines)
        {
            if (!string.IsNullOrEmpty(symbol) && !SymbolExists(symbol))
            {
                ToggleSymbol(symbol);
            }
        }
    }
    
    private void RemoveAllSymbols()
    {
        foreach (var symbol in customDefines)
        {
            if (!string.IsNullOrEmpty(symbol) && SymbolExists(symbol))
            {
                ToggleSymbol(symbol);
            }
        }
    }

    private void OnSymbolsSelected(List<string> symbols)
    {
        int addedCount = 0;
        foreach (var symbol in symbols)
        {
            if (!customDefines.Contains(symbol))
            {
                customDefines.Add(symbol);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            SaveCustomDefines();
            LogNormal($"Added {addedCount} symbols to the list.");
            Repaint();
        }
    }

    /*----scanner-------------------------------------------------------------------------------------------------------*/
    private static readonly HashSet<string> BuiltInSymbols = new HashSet<string>
    {
        "UNITY_EDITOR", "UNITY_EDITOR_WIN", "UNITY_EDITOR_OSX", "UNITY_EDITOR_LINUX",
        "UNITY_STANDALONE", "UNITY_STANDALONE_WIN", "UNITY_STANDALONE_OSX", "UNITY_STANDALONE_LINUX",
        "UNITY_ANDROID", "UNITY_IOS", "UNITY_WEBGL", "UNITY_WSA", "UNITY_TVOS",
        "UNITY_2019", "UNITY_2020", "UNITY_2021", "UNITY_2022", "UNITY_2023", "UNITY_6",
        "UNITY_2019_1_OR_NEWER", "UNITY_2020_1_OR_NEWER", "UNITY_2021_1_OR_NEWER", "UNITY_2022_1_OR_NEWER",
        "DEBUG", "TRACE", "DEVELOPMENT_BUILD", "UNITY_ASSERTIONS",
        "ENABLE_MONO", "ENABLE_IL2CPP", "NET_4_6", "NET_STANDARD_2_0", "NET_STANDARD_2_1",
        "CSHARP_7_OR_LATER", "CSHARP_7_3_OR_NEWER",
        "true", "false"
    };

    private void ScanAllDirectoriesForSymbols()
    {
        HashSet<string> foundSymbols = new HashSet<string>();

        foreach (var directory in scanDirectories)
        {
            if (!Directory.Exists(directory))
            {
                LogWarn($"Directory not found: {directory}");
                continue;
            }

            try
            {
                string[] files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    ExtractSymbolsFromFile(file, foundSymbols);
                }
            }
            catch (System.Exception ex)
            {
                LogErr($"Error scanning {directory}: {ex.Message}");
            }
        }

        scannedSymbols = new List<string>(foundSymbols);
        scannedSymbols.Sort();

        LogNormal($"Scan complete. Found {scannedSymbols.Count} unique symbols.");
    }

    private void ExtractSymbolsFromFile(string filePath, HashSet<string> symbols)
    {
        try
        {
            string content = File.ReadAllText(filePath);

            // Match #if, #elif, #ifdef, #ifndef directives
            Regex directivePattern = new Regex(@"#(?:if|elif|ifdef|ifndef)\s+(.+)$", RegexOptions.Multiline);
            // Extract individual symbol names (uppercase with underscores)
            Regex symbolPattern = new Regex(@"\b([A-Z_][A-Za-z0-9_]*)\b");

            MatchCollection directives = directivePattern.Matches(content);
            foreach (Match directive in directives)
            {
                string expression = directive.Groups[1].Value;
                MatchCollection symbolMatches = symbolPattern.Matches(expression);
                foreach (Match symbolMatch in symbolMatches)
                {
                    string symbol = symbolMatch.Groups[1].Value;

                    // Filter out built-in symbols and version patterns
                    if (!BuiltInSymbols.Contains(symbol) &&
                        !symbol.StartsWith("UNITY_") &&
                        symbol.Length > 1)
                    {
                        symbols.Add(symbol);
                    }
                }
            }
        }
        catch (System.Exception)
        {
            // Silently skip files that can't be read
        }
    }

    private string GetCurrentDefines()
    {
#if UNITY_ANDROID
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
#elif UNITY_IOS
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS);
#else
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
#endif
    }

    private void SaveCustomDefines()
    {
        string key = definesPrefsKey + "_Defines";
        string json = JsonUtility.ToJson(new DefinesWrapper { defines = customDefines });
        EditorPrefs.SetString(key, json);
    }

    [System.Serializable]
    private class DefinesWrapper
    {
        public List<string> defines = new List<string>();
    }

    private void LoadCustomDefines()
    {
        string key = definesPrefsKey + "_Defines";
        if (EditorPrefs.HasKey(key))
        {
            string json = EditorPrefs.GetString(key);
            DefinesWrapper wrapper = JsonUtility.FromJson<DefinesWrapper>(json);
            if (wrapper != null && wrapper.defines != null)
            {
                customDefines = wrapper.defines;
            }
        }
        else
        {
            customDefines = new List<string>();
        }
    }

    private void SaveScanDirectories()
    {
        string key = definesPrefsKey + "_ScanDirs";
        string json = JsonUtility.ToJson(new DefinesWrapper { defines = scanDirectories });
        EditorPrefs.SetString(key, json);
    }

    private void LoadScanDirectories()
    {
        string key = definesPrefsKey + "_ScanDirs";
        if (EditorPrefs.HasKey(key))
        {
            string json = EditorPrefs.GetString(key);
            DefinesWrapper wrapper = JsonUtility.FromJson<DefinesWrapper>(json);
            if (wrapper != null && wrapper.defines != null)
            {
                scanDirectories = wrapper.defines;
            }
        }
        else
        {
            scanDirectories = new List<string>();
        }
    }

    /*----Keystores------------------------------------------------------------------------------------------------------*/
    private void LoadAutoKeystorePassword()
    {
        string key = definesPrefsKey + "_KeystorePass";
        if (EditorPrefs.HasKey(key))
        {
            autoKeystorePassword = EditorPrefs.GetString(key);
        }
        else
        {
            autoKeystorePassword = DefaultAutoKeystorePassword;
        }
        editingKeystorePass = autoKeystorePassword;
    }

    private void ApplyAutoKeystorePasswordChange()
    {
        if (string.IsNullOrEmpty(editingKeystorePass))
        {
            LogWarn("Auto Password cannot be empty!");
            return;
        }

        if (editingKeystorePass == autoKeystorePassword)
        {
            LogNormal("Auto Password unchanged.");
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Confirm Auto Password Change",
            "Are you sure you want to change the Auto Keystore Password?\n\nThis will update the stored auto password used for builds.",
            "Confirm",
            "Cancel"
        );

        if (confirmed)
        {
            autoKeystorePassword = editingKeystorePass;
            EditorPrefs.SetString(definesPrefsKey + "_KeystorePass", autoKeystorePassword);
            LogNormal("Auto Keystore Password updated.");
        }
        else
        {
            editingKeystorePass = autoKeystorePassword;
        }
    }

    private void ApplyKeystorePassword()
    {
#if UNITY_ANDROID
        if (string.IsNullOrEmpty(keystorePassword))
        {
            LogWarn("Keystore Password empty! Ignore if already set!");
            return;
        }

        // Set the Keystore password and Key password in Player Settings
        PlayerSettings.Android.keystorePass = keystorePassword;
        PlayerSettings.Android.keyaliasPass = keystorePassword;  // Use the same password for the key alias (if needed)

        LogNormal($"Keystore Password applied!");
#endif
    }
    /*----log-----------------------------------------------------------------------------------------------------------*/
    private void LogNormal(string content)
    {
        Debug.Log($"[Build Config Editor] <color=#3fb4fc>---{content}</color>");
    }

    private void LogWarn(string content)
    {
        Debug.Log($"[Build Config Editor] <color=#fcba3f>!!!{content}</color>");
    }

    private void LogErr(string content)
    {
        Debug.Log($"[Build Config Editor] <color=#f7580f>???{content}</color>");
    }
}

public class ScannedSymbolsPopup : EditorWindow
{
    private List<string> symbols;
    private List<string> trackedDefines;
    private Dictionary<string, bool> selected = new Dictionary<string, bool>();
    private Vector2 scrollPos;
    private System.Action<List<string>> onApply;

    public static void Show(List<string> symbols, List<string> trackedDefines, System.Action<List<string>> onApply)
    {
        var window = GetWindow<ScannedSymbolsPopup>(true, "Scanned Symbols");
        window.symbols = symbols;
        window.trackedDefines = trackedDefines;
        window.onApply = onApply;
        window.selected.Clear();
        window.minSize = new Vector2(300, 150);
        window.maxSize = new Vector2(500, 300);
        window.ShowUtility();
    }

    private void OnGUI()
    {
        if (symbols == null || symbols.Count == 0)
        {
            EditorGUILayout.LabelField("No symbols found.");
            return;
        }

        EditorGUILayout.LabelField($"Found Symbols ({symbols.Count}):", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginVertical("box");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var symbol in symbols)
        {
            if (!selected.ContainsKey(symbol))
                selected[symbol] = false;

            EditorGUILayout.BeginHorizontal();
            selected[symbol] = GUILayout.Toggle(selected[symbol], GUIContent.none, GUILayout.Width(20));
            EditorGUILayout.LabelField(symbol);

            if (trackedDefines != null && trackedDefines.Contains(symbol))
            {
                EditorGUILayout.LabelField("[tracked]", EditorStyles.miniLabel, GUILayout.Width(60));
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Add Selected to List"))
        {
            var result = new List<string>();
            foreach (var kvp in selected)
            {
                if (kvp.Value)
                    result.Add(kvp.Key);
            }

            onApply?.Invoke(result);
            Close();
        }
    }
}

