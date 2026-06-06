using UnityEngine;
using UnityEditor;
using System.IO;

namespace GUIPack2DFree
{
    public class PopupGeneratorWindow : EditorWindow
    {
        private string folderName = "";
        private readonly string parentPath = "Assets/GuiPack2DFree/Popups"; // Change this to specify a different parent path
        private readonly string prefabPath = "Assets/GuiPack2DFree/Prefabs/Popup/PopupBase.prefab"; // Path to the prefab to duplicate

        string pathScripts = "";
        string pathPrefabs = "";
        string pathSprites = "";
        string newPrefabPath = "";
        string newScriptPath = "";

        [MenuItem("Tools/Popup Generator")]
        public static void ShowWindow()
        {
            GetWindow<PopupGeneratorWindow>("Popup Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("Popup Generator", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Popup name:");
            folderName = EditorGUILayout.TextField(folderName);

            if (GUILayout.Button("Generate Popup"))
            {
                if (string.IsNullOrEmpty(folderName))
                {
                    Debug.LogWarning("Name cannot be empty!");
                    return;
                }
                GenerateFoldersAndAssets();
            }

            if (GUILayout.Button("Attach Script"))
            {
                if (string.IsNullOrEmpty(folderName))
                {
                    Debug.LogWarning("Name cannot be empty!");
                    return;
                }

                AttachScriptToPrefab(newPrefabPath, newScriptPath);
            }
        }

        private void GenerateFoldersAndAssets()
        {
            string subName = "Popup" + folderName;

            pathScripts = Path.Combine(parentPath, "Scripts", subName);
            pathPrefabs = Path.Combine(parentPath, "Prefabs", subName);
            pathSprites = Path.Combine(parentPath, "Sprites", subName);

            CreateFolder(pathScripts);
            CreateFolder(pathPrefabs);
            CreateFolder(pathSprites);

            CreateScript(pathScripts, subName);
            DuplicatePrefab(pathPrefabs, subName);

            AssetDatabase.Refresh();

            newPrefabPath = Path.Combine(pathPrefabs, subName + ".prefab");
            newScriptPath = Path.Combine(subName);

        }

        private void CreateFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path);
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
                Debug.Log($"Created folder: {path}");
            }
            else
            {
                Debug.LogWarning($"Folder already exists: {path}");
            }
        }

        private void CreateScript(string folderPath, string scriptName)
        {
            string scriptPath = Path.Combine(folderPath, scriptName + ".cs");

            if (!File.Exists(scriptPath))
            {
                string scriptContent = "using UnityEngine;\n\n" +
                                       "namespace GUIPack2DFree\n" +
                                       "{\n" +
                                       "    public class " + scriptName + " : Popup\n" +
                                       "    {\n" +
                                       "    }\n" +
                                       "}\n";

                File.WriteAllText(scriptPath, scriptContent);
                AssetDatabase.Refresh();
                Debug.Log("Script Created: " + scriptPath);
            }
            else
            {
                Debug.LogWarning("Script already exists: " + scriptPath);
            }
        }

        private void DuplicatePrefab(string folderPath, string newPrefabName)
        {
            if (!File.Exists(prefabPath))
            {
                Debug.LogError("Prefab not found at: " + prefabPath);
            }

            string newPrefabPath = Path.Combine(folderPath, newPrefabName + ".prefab");

            if (!File.Exists(newPrefabPath))
            {
                AssetDatabase.CopyAsset(prefabPath, newPrefabPath);
                AssetDatabase.Refresh();
                Debug.Log("Prefab Duplicated: " + newPrefabPath);
            }
            else
            {
                Debug.LogWarning("Prefab already exists: " + newPrefabPath);
            }
        }

        private void AttachScriptToPrefab(string pathPrefab, string scriptName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pathPrefab);

            if (prefab == null)
            {
                Debug.LogError("Failed to load prefab at: " + pathPrefab);
                return;
            }

            // Create an instance to modify
            GameObject prefabInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (prefabInstance == null)
            {
                Debug.LogError("Failed to instantiate prefab.");
                return;
            }

            // Find the script in the project by searching for all assets with the same name
            string[] guids = AssetDatabase.FindAssets(scriptName);
            if (guids.Length == 0)
            {
                Debug.LogError("Script not found: " + scriptName);
                Object.DestroyImmediate(prefabInstance);
                return;
            }

            // Loop through the found results to match the correct namespace and class name
            MonoScript monoScript = null;
            foreach (var guid in guids)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);

                if (monoScript != null && monoScript.GetClass() != null)
                {
                    // We found a valid class
                    break;
                }
            }

            if (monoScript == null || monoScript.GetClass() == null)
            {
                Debug.LogError("Failed to get class from script: " + scriptName);
                Object.DestroyImmediate(prefabInstance);
                return;
            }

            System.Type scriptType = monoScript.GetClass();
            if (scriptType == null)
            {
                Debug.LogError("Failed to get class from script: " + scriptName);
                Object.DestroyImmediate(prefabInstance);
                return;
            }

            // Attach the script to the prefab instance if not already added
            if (prefabInstance.GetComponent(scriptType) == null)
            {
                prefabInstance.AddComponent(scriptType);
                Debug.Log("Attached script " + scriptName + " to prefab.");
            }
            else
            {
                Debug.LogWarning("Prefab already has the script: " + scriptName);
            }

            // Apply changes to the prefab
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, pathPrefab);
            Object.DestroyImmediate(prefabInstance);  // Cleanup instance

            AssetDatabase.Refresh();
        }
    }
}