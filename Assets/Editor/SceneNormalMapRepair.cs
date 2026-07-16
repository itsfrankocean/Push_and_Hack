using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class SceneNormalMapRepair
{
    private const string ScenesRoot = "Assets/Scenes";

    [MenuItem("Tools/Lighting/Enable Normal Maps In All Level Scenes")]
    public static void ApplyAll()
    {
        SecondaryNormalTextureRepair.RepairAll(showDialog: false, forceReimportAll: true);

        var scenePaths = FindLevelScenePaths();
        var totalLights = 0;
        var enabledLights = 0;
        var failures = new List<string>();

        foreach (var scenePath in scenePaths)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var lights = FindLights(scene);
            var sceneChanged = false;
            var sceneEnabled = 0;

            foreach (var light in lights)
            {
                totalLights++;
                var serializedLight = new SerializedObject(light);
                var useNormalMap = serializedLight.FindProperty("m_UseNormalMap");

                if (useNormalMap == null)
                {
                    failures.Add($"{scenePath}: {GetHierarchyPath(light.transform)} has no m_UseNormalMap property.");
                    continue;
                }

                if (!useNormalMap.boolValue)
                {
                    useNormalMap.boolValue = true;
                    serializedLight.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(light);
                    sceneChanged = true;
                    enabledLights++;
                    sceneEnabled++;
                }
            }

            if (sceneChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    failures.Add($"{scenePath}: failed to save scene.");
                }
            }

            Debug.Log($"SceneNormalMapRepair: {scenePath} checked {lights.Count} lights and enabled normal maps on {sceneEnabled}.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        failures.AddRange(ValidateAllLights(scenePaths));
        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Scene normal-map repair validation failed:\n" + string.Join("\n", failures));
        }

        Debug.Log(
            $"SceneNormalMapRepair completed. Scenes: {scenePaths.Count}, lights checked: {totalLights}, lights enabled: {enabledLights}. All level lights now use normal maps.");
    }

    private static List<string> FindLevelScenePaths()
    {
        return AssetDatabase.FindAssets("t:Scene", new[] { ScenesRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => Path.GetFileNameWithoutExtension(path).StartsWith("Level", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<Light2D> FindLights(Scene scene)
    {
        var lights = new List<Light2D>();
        foreach (var root in scene.GetRootGameObjects())
        {
            lights.AddRange(root.GetComponentsInChildren<Light2D>(true));
        }

        return lights;
    }

    private static List<string> ValidateAllLights(IEnumerable<string> scenePaths)
    {
        var failures = new List<string>();

        foreach (var scenePath in scenePaths)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (var light in FindLights(scene))
            {
                var serializedLight = new SerializedObject(light);
                var useNormalMap = serializedLight.FindProperty("m_UseNormalMap");
                if (useNormalMap == null || !useNormalMap.boolValue)
                {
                    failures.Add($"{scenePath}: {GetHierarchyPath(light.transform)} still has normal maps disabled.");
                }
            }
        }

        return failures;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        var names = new Stack<string>();
        for (var current = transform; current != null; current = current.parent)
        {
            names.Push(current.name);
        }

        return string.Join("/", names);
    }
}
