using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

internal static class Round4CharacterLightingRepair
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string Level2ScenePath = "Assets/Scenes/Level2.unity";
    private const string SpriteLitMaterialPath = "Assets/Materials/Sprite-Lit-Default.mat";
    private const string ReportPath = "Assets/Editor/Round4CharacterLightingReport.txt";
    private const string NormalName = "_NormalMap";

    [MenuItem("Tools/Lighting/Apply Round 4 Character Lighting Repair")]
    public static void Apply()
    {
        SecondaryNormalTextureRepair.RepairAll(showDialog: false, forceReimportAll: true);

        var report = new StringBuilder();
        report.AppendLine("Round 4 Character Lighting Report");
        report.AppendLine("=================================");
        report.AppendLine();

        RepairPlayerPrefab(report);
        InspectLevel2Scene(report);

        File.WriteAllText(ReportPath, report.ToString());
        AssetDatabase.ImportAsset(ReportPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Round4CharacterLightingRepair completed. Report written to {ReportPath}.");
    }

    private static void RepairPlayerPrefab(StringBuilder report)
    {
        var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            var mc = FindChildByName(root.transform, "MC");
            var renderer = mc != null ? mc.GetComponent<SpriteRenderer>() : null;
            var litMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteLitMaterialPath);

            report.AppendLine("[Prefab]");
            report.AppendLine($"Prefab: {PlayerPrefabPath}");
            report.AppendLine($"Hierarchy path: {(mc != null ? GetPath(mc) : "MC not found")}");
            report.AppendLine($"SpriteRenderer GameObject: {(renderer != null ? renderer.gameObject.name : "not found")}");

            var allCasters = root.GetComponentsInChildren<ShadowCaster2D>(true).ToList();
            report.AppendLine($"ShadowCaster2D before repair: {FormatObjectNames(allCasters.Select(c => c.gameObject))}");

            var changed = false;
            foreach (var caster in allCasters.ToArray())
            {
                if (mc != null && caster.gameObject == mc.gameObject)
                {
                    continue;
                }

                Object.DestroyImmediate(caster, true);
                changed = true;
            }

            ShadowCaster2D mcCaster = null;
            if (mc != null)
            {
                mcCaster = mc.GetComponent<ShadowCaster2D>();
                if (mcCaster == null)
                {
                    mcCaster = mc.gameObject.AddComponent<ShadowCaster2D>();
                    changed = true;
                }
            }

            if (renderer != null && litMaterial != null && renderer.sharedMaterial != litMaterial)
            {
                renderer.sharedMaterial = litMaterial;
                changed = true;
            }

            if (mc != null && mc.GetComponent<PlayerSpriteLightingRefresh>() == null)
            {
                mc.gameObject.AddComponent<PlayerSpriteLightingRefresh>();
                changed = true;
            }

            if (mcCaster != null)
            {
                var so = new SerializedObject(mcCaster);
                changed |= SetBool(so, "m_HasRenderer", renderer != null);
                changed |= SetBool(so, "m_UseRendererSilhouette", true);
                changed |= SetBool(so, "m_Enabled", false);
                changed |= SetBool(so, "m_CastsShadows", false);
                changed |= SetBool(so, "m_SelfShadows", false);
                changed |= SetFloat(so, "m_AlphaCutoff", 0.1f);
                changed |= SetInt(so, "m_CastingOption", 3);
                changed |= SetInt(so, "m_ShadowCastingSource", 2);
                changed |= SetObject(so, "m_ShadowShape2DComponent", renderer);
                changed |= SetFloat(so, "m_ShadowMesh.m_TrimEdge", 0f);
                changed |= SetInt(so, "m_PreviousShadowCastingSource", -1);
                changed |= SetInt(so, "m_PreviousEdgeProcessing", -1);
                changed |= SetObject(so, "m_PreviousShadowShape2DSource", null);
                changed |= SetObject(so, "m_ShadowMesh.m_Mesh", null);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var spritePaths = GetPlayerSpritePaths(root);
            report.AppendLine($"ShadowCaster2D after repair: {(mcCaster != null ? mcCaster.gameObject.name : "not found")}");
            report.AppendLine($"MC material: {(renderer != null && renderer.sharedMaterial != null ? AssetDatabase.GetAssetPath(renderer.sharedMaterial) : "none")}");
            report.AppendLine($"MC current sprite: {(renderer != null && renderer.sprite != null ? AssetDatabase.GetAssetPath(renderer.sprite) : "none")}");
            report.AppendLine("MC animation/combat sprite sheets:");
            foreach (var path in spritePaths.OrderBy(p => p))
            {
                report.AppendLine($"- {path}: {DescribeSecondaryNormal(path)}");
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }

            report.AppendLine($"Prefab changed: {changed}");
            report.AppendLine();
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void InspectLevel2Scene(StringBuilder report)
    {
        var scene = EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Single);
        var player = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "Player");
        var mc = player != null ? FindChildByName(player.transform, "MC") : null;
        var renderer = mc != null ? mc.GetComponent<SpriteRenderer>() : null;
        var casters = player != null
            ? player.GetComponentsInChildren<ShadowCaster2D>(true)
            : new ShadowCaster2D[0];

        report.AppendLine("[Level2 Scene]");
        report.AppendLine($"Scene: {Level2ScenePath}");
        report.AppendLine($"Hierarchy path: {(mc != null ? GetPath(mc) : "Player/MC not found")}");
        report.AppendLine($"SpriteRenderer GameObject: {(renderer != null ? renderer.gameObject.name : "not found")}");
        report.AppendLine($"ShadowCaster2D GameObjects: {FormatObjectNames(casters.Select(c => c.gameObject))}");
        report.AppendLine($"MC has PlayerSpriteLightingRefresh: {(mc != null && mc.GetComponent<PlayerSpriteLightingRefresh>() != null)}");
        report.AppendLine($"MC current sprite secondary normal: {(renderer != null && renderer.sprite != null ? DescribeSecondaryNormal(AssetDatabase.GetAssetPath(renderer.sprite)) : "none")}");
        report.AppendLine();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static HashSet<string> GetPlayerSpritePaths(GameObject root)
    {
        var paths = new HashSet<string>();

        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.sprite != null)
            {
                paths.Add(AssetDatabase.GetAssetPath(renderer.sprite));
            }
        }

        foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null)
            {
                continue;
            }

            var serializedObject = new SerializedObject(behaviour);
            var iterator = serializedObject.GetIterator();
            while (iterator.NextVisible(true))
            {
                if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                    iterator.objectReferenceValue is Sprite sprite)
                {
                    paths.Add(AssetDatabase.GetAssetPath(sprite));
                }
            }
        }

        foreach (var animator in root.GetComponentsInChildren<Animator>(true))
        {
            var controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                continue;
            }

            foreach (var clip in controller.animationClips)
            {
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    if (binding.propertyName != "m_Sprite")
                    {
                        continue;
                    }

                    foreach (var keyframe in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                    {
                        if (keyframe.value is Sprite sprite)
                        {
                            paths.Add(AssetDatabase.GetAssetPath(sprite));
                        }
                    }
                }
            }
        }

        return paths;
    }

    private static string DescribeSecondaryNormal(string spritePath)
    {
        var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer == null)
        {
            return "TextureImporter not found";
        }

        var normal = importer.secondarySpriteTextures
            .FirstOrDefault(texture => texture.name == NormalName);
        if (normal.texture == null)
        {
            return "MISSING _NormalMap";
        }

        var normalPath = AssetDatabase.GetAssetPath(normal.texture);
        var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        return $"{NormalName} -> {normalPath}, normal sRGB={(normalImporter != null && normalImporter.sRGBTexture)}";
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            var found = FindChildByName(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static string GetPath(Component component)
    {
        return GetPath(component.transform);
    }

    private static string GetPath(Transform transform)
    {
        var names = new Stack<string>();
        var current = transform;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join(" > ", names);
    }

    private static string FormatObjectNames(IEnumerable<GameObject> objects)
    {
        var names = objects.Select(gameObject => GetPath(gameObject.transform)).ToArray();
        return names.Length == 0 ? "none" : string.Join(", ", names);
    }

    private static bool SetBool(SerializedObject so, string propertyName, bool value)
    {
        var property = so.FindProperty(propertyName);
        if (property == null || property.boolValue == value)
        {
            return false;
        }

        property.boolValue = value;
        return true;
    }

    private static bool SetInt(SerializedObject so, string propertyName, int value)
    {
        var property = so.FindProperty(propertyName);
        if (property == null || property.intValue == value)
        {
            return false;
        }

        property.intValue = value;
        return true;
    }

    private static bool SetFloat(SerializedObject so, string propertyName, float value)
    {
        var property = so.FindProperty(propertyName);
        if (property == null || Mathf.Approximately(property.floatValue, value))
        {
            return false;
        }

        property.floatValue = value;
        return true;
    }

    private static bool SetObject(SerializedObject so, string propertyName, Object value)
    {
        var property = so.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == value)
        {
            return false;
        }

        property.objectReferenceValue = value;
        return true;
    }
}
