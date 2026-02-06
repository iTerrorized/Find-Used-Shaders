using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Terrorized.Tools
{
    public class FindUsedShadersWindow : EditorWindow
    {
        private GameObject selectedGameObject;
        private Dictionary<Shader, List<MaterialGameObjectPair>> shaderToMaterials = new Dictionary<Shader, List<MaterialGameObjectPair>>();
        private Dictionary<Shader, bool> shaderFoldouts = new Dictionary<Shader, bool>();
        private Vector2 scrollPosition;

        [System.Serializable]
        public class MaterialGameObjectPair
        {
            public Material material;
            public GameObject gameObject;
            public string rendererName;

            public MaterialGameObjectPair(Material mat, GameObject go, string name)
            {
                material = mat;
                gameObject = go;
                rendererName = name;
            }
        }

        [MenuItem("Terrorized/Tools/Find Used Shaders")]
        public static void ShowWindow()
        {
            GetWindow<FindUsedShadersWindow>("Find Used Shaders");
        }

        private void OnGUI()
        {
            GUILayout.Label("Find Used Shaders", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Game Object selection field
            EditorGUI.BeginChangeCheck();
            selectedGameObject = EditorGUILayout.ObjectField("Game Object", selectedGameObject, typeof(GameObject), true) as GameObject;

            if (EditorGUI.EndChangeCheck())
            {
                if (selectedGameObject != null)
                {
                    AnalyzeGameObject();
                }
                else
                {
                    ClearData();
                }
            }

            GUILayout.Space(10);

            if (selectedGameObject == null)
            {
                EditorGUILayout.HelpBox("Please select a Game Object to analyze its shaders.", MessageType.Info);
                return;
            }

            if (shaderToMaterials.Count == 0)
            {
                EditorGUILayout.HelpBox("No renderers with materials found on the selected Game Object.", MessageType.Warning);
                return;
            }

            // Display shader information
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var kvp in shaderToMaterials)
            {
                Shader shader = kvp.Key;
                List<MaterialGameObjectPair> materialGameObjectPairs = kvp.Value;

                if (shader == null) continue;

                // Ensure foldout state exists
                if (!shaderFoldouts.ContainsKey(shader))
                    shaderFoldouts[shader] = false;

                // Shader foldout
                EditorGUILayout.BeginVertical("box");
                shaderFoldouts[shader] = EditorGUILayout.Foldout(shaderFoldouts[shader],
                    $"{shader.name} ({materialGameObjectPairs.Count} materials)", true);

                if (shaderFoldouts[shader])
                {
                    EditorGUI.indentLevel++;

                    foreach (var pair in materialGameObjectPairs)
                    {
                        EditorGUILayout.BeginHorizontal();

                        // Material field
                        EditorGUILayout.ObjectField("Material", pair.material, typeof(Material), false);

                        // Game Object field
                        EditorGUILayout.ObjectField("Game Object", pair.gameObject, typeof(GameObject), true);

                        EditorGUILayout.EndHorizontal();

                        // Show renderer name for context
                        if (!string.IsNullOrEmpty(pair.rendererName))
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.LabelField("Renderer: " + pair.rendererName, EditorStyles.miniLabel);
                            EditorGUI.indentLevel--;
                        }

                        GUILayout.Space(5);
                    }

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();

            // Summary
            GUILayout.Space(10);
            EditorGUILayout.LabelField($"Total Shaders Found: {shaderToMaterials.Count}", EditorStyles.boldLabel);
        }

        private void AnalyzeGameObject()
        {
            ClearData();

            if (selectedGameObject == null) return;

            // Get all renderers in the game object and its children, including inactive ones
            Renderer[] renderers = selectedGameObject.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;

                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null || material.shader == null) continue;

                    Shader shader = material.shader;

                    if (!shaderToMaterials.ContainsKey(shader))
                    {
                        shaderToMaterials[shader] = new List<MaterialGameObjectPair>();
                    }

                    // Check if this material-gameObject combination already exists
                    bool exists = shaderToMaterials[shader].Any(pair =>
                        pair.material == material && pair.gameObject == renderer.gameObject);

                    if (!exists)
                    {
                        shaderToMaterials[shader].Add(new MaterialGameObjectPair(
                            material,
                            renderer.gameObject,
                            renderer.gameObject.name
                        ));
                    }
                }
            }

            // Sort shaders by name for consistent display
            var sortedShaders = shaderToMaterials.OrderBy(kvp => kvp.Key.name).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            shaderToMaterials = sortedShaders;
        }

        private void ClearData()
        {
            shaderToMaterials.Clear();
            shaderFoldouts.Clear();
        }

        private void OnSelectionChange()
        {
            // Auto-select the currently selected game object in the hierarchy
            if (Selection.activeGameObject != null && Selection.activeGameObject != selectedGameObject)
            {
                selectedGameObject = Selection.activeGameObject;
                AnalyzeGameObject();
                Repaint();
            }
        }
    }
}