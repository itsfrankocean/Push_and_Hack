using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

internal static class Round2LightingRepair
{
    private const string NormalName = "_NormalMap";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string Level2ScenePath = "Assets/Scenes/Level2.unity";
    private const string SpriteLitMaterialPath = "Assets/Materials/Sprite-Lit-Default.mat";

    [MenuItem("Tools/Lighting/Apply Round 2 Lighting Repair")]
    public static void ApplyAll()
    {
        var normalCount = RepairSecondaryNormals();
        var playerChanged = RepairPlayerPrefab();
        var washCount = RepairLevel2WashLights();

        Debug.Log($"Round2LightingRepair completed. Secondary normals touched: {normalCount}, player changed: {playerChanged}, wash lights: {washCount}.");
    }

    private static int RepairSecondaryNormals()
    {
        var changedCount = 0;
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites" });

        foreach (var guid in guids)
        {
            var spritePath = AssetDatabase.GUIDToAssetPath(guid);
            if (!spritePath.EndsWith(".png"))
            {
                continue;
            }

            var name = Path.GetFileNameWithoutExtension(spritePath);
            if (name.EndsWith("_Normal"))
            {
                continue;
            }

            var directory = Path.GetDirectoryName(spritePath)?.Replace('\\', '/');
            var normalPath = $"{directory}/{name}_Normal.png";
            if (!File.Exists(normalPath))
            {
                continue;
            }

            var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null)
            {
                var normalChanged = false;
                if (normalImporter.textureType != TextureImporterType.Default)
                {
                    normalImporter.textureType = TextureImporterType.Default;
                    normalChanged = true;
                }

                if (normalImporter.sRGBTexture)
                {
                    normalImporter.sRGBTexture = false;
                    normalChanged = true;
                }

                if (normalChanged)
                {
                    normalImporter.SaveAndReimport();
                    changedCount++;
                }
            }

            var normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (normalTexture == null || importer == null)
            {
                continue;
            }

            var textures = new List<SecondarySpriteTexture>(importer.secondarySpriteTextures ?? new SecondarySpriteTexture[0]);
            var found = false;
            var changed = false;

            for (var i = 0; i < textures.Count; i++)
            {
                if (textures[i].name != NormalName)
                {
                    continue;
                }

                found = true;
                if (textures[i].texture != normalTexture)
                {
                    var entry = textures[i];
                    entry.texture = normalTexture;
                    textures[i] = entry;
                    changed = true;
                }
            }

            if (!found)
            {
                textures.Add(new SecondarySpriteTexture
                {
                    name = NormalName,
                    texture = normalTexture
                });
                changed = true;
            }

            if (!changed)
            {
                continue;
            }

            importer.secondarySpriteTextures = textures.ToArray();
            importer.SaveAndReimport();
            changedCount++;
        }

        AssetDatabase.SaveAssets();
        return changedCount;
    }

    private static bool RepairPlayerPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        var changed = false;

        try
        {
            var spriteRenderer = FindPlayerSpriteRenderer(root);
            if (spriteRenderer == null)
            {
                Debug.LogWarning("Round2LightingRepair could not find Player sprite renderer.");
                return false;
            }

            var litMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteLitMaterialPath);
            if (litMaterial != null && spriteRenderer.sharedMaterial != litMaterial)
            {
                spriteRenderer.sharedMaterial = litMaterial;
                changed = true;
            }

            var shadowCaster = spriteRenderer.GetComponent<ShadowCaster2D>();
            if (shadowCaster == null)
            {
                shadowCaster = spriteRenderer.gameObject.AddComponent<ShadowCaster2D>();
                changed = true;
            }

            var so = new SerializedObject(shadowCaster);
            changed |= SetBool(so, "m_UseRendererSilhouette", true);
            changed |= SetBool(so, "m_Enabled", false);
            changed |= SetBool(so, "m_CastsShadows", false);
            changed |= SetBool(so, "m_SelfShadows", false);
            changed |= SetBool(so, "m_HasRenderer", true);
            changed |= SetIntChanged(so, "m_CastingOption", 3);
            changed |= SetIntChanged(so, "m_ShadowCastingSource", 2);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return changed;
    }

    private static SpriteRenderer FindPlayerSpriteRenderer(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.gameObject.name == "MC")
            {
                return renderer;
            }
        }

        return root.GetComponentInChildren<SpriteRenderer>(true);
    }

    private static int RepairLevel2WashLights()
    {
        var scene = EditorSceneManager.OpenScene(Level2ScenePath, OpenSceneMode.Single);
        var parent = FindOrCreateRoot("Neon Wash Lights");

        var specs = new[]
        {
            new WashLightSpec("Wash_Teal_Center", new Vector3(-0.9f, 0.15f, 10.01073f), new Color(0.0f, 0.95f, 0.82f, 1f), 0.42f, 3.4f),
            new WashLightSpec("Wash_Magenta_LowerRight", new Vector3(4.35f, -1.45f, 10.01073f), new Color(1.0f, 0.0f, 0.88f, 1f), 0.36f, 3.0f),
            new WashLightSpec("Wash_Cyan_UpperRight", new Vector3(4.55f, 2.15f, 10.01073f), new Color(0.2f, 0.85f, 1.0f, 1f), 0.34f, 2.9f),
        };

        var changedCount = 0;
        foreach (var spec in specs)
        {
            var lightObject = GameObject.Find(spec.Name);
            if (lightObject == null)
            {
                lightObject = new GameObject(spec.Name);
                lightObject.transform.SetParent(parent.transform);
                changedCount++;
            }

            lightObject.transform.position = spec.Position;

            var light = lightObject.GetComponent<Light2D>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light2D>();
                changedCount++;
            }

            var so = new SerializedObject(light);
            SetInt(so, "m_LightType", 3);
            SetInt(so, "m_BlendStyleIndex", 0);
            SetFloat(so, "m_FalloffIntensity", 0.72f);
            SetColor(so, "m_Color", spec.Color);
            SetFloat(so, "m_Intensity", spec.Intensity);
            SetFloat(so, "m_NormalMapDistance", 3f);
            SetInt(so, "m_NormalMapQuality", 1);
            SetBool(so, "m_UseNormalMap", true);
            SetFloat(so, "m_PointLightInnerRadius", 0f);
            SetFloat(so, "m_PointLightOuterRadius", spec.Radius);
            SetFloat(so, "m_PointLightInnerAngle", 360f);
            SetFloat(so, "m_PointLightOuterAngle", 360f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return changedCount;
    }

    private static GameObject FindOrCreateRoot(string name)
    {
        var found = GameObject.Find(name);
        if (found != null)
        {
            return found;
        }

        return new GameObject(name);
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

    private static bool SetIntChanged(SerializedObject so, string propertyName, int value)
    {
        var property = so.FindProperty(propertyName);
        if (property == null || property.intValue == value)
        {
            return false;
        }

        property.intValue = value;
        return true;
    }

    private static void SetInt(SerializedObject so, string propertyName, int value)
    {
        var property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetFloat(SerializedObject so, string propertyName, float value)
    {
        var property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetColor(SerializedObject so, string propertyName, Color value)
    {
        var property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.colorValue = value;
        }
    }

    private readonly struct WashLightSpec
    {
        public readonly string Name;
        public readonly Vector3 Position;
        public readonly Color Color;
        public readonly float Intensity;
        public readonly float Radius;

        public WashLightSpec(string name, Vector3 position, Color color, float intensity, float radius)
        {
            Name = name;
            Position = position;
            Color = color;
            Intensity = intensity;
            Radius = radius;
        }
    }
}
