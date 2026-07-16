using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

internal static class SecondaryNormalTextureRepair
{
    private const string NormalName = "_NormalMap";
    private const string SpritesRoot = "Assets/Sprites";
    private static bool repairInProgress;

    [MenuItem("Tools/Lighting/Repair Secondary Normal Textures")]
    private static void RepairFromMenu()
    {
        RepairAll(showDialog: true, forceReimportAll: true);
    }

    internal static void RepairAll(bool showDialog)
    {
        RepairAll(showDialog, forceReimportAll: false);
    }

    internal static void RepairAll(bool showDialog, bool forceReimportAll)
    {
        var fixedCount = 0;
        var checkedCount = 0;
        var reimportedCount = 0;

        repairInProgress = true;
        try
        {
            var spriteGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpritesRoot });

            foreach (var guid in spriteGuids)
            {
                var spritePath = AssetDatabase.GUIDToAssetPath(guid);
                if (!spritePath.EndsWith(".png"))
                {
                    continue;
                }

                var fileName = Path.GetFileNameWithoutExtension(spritePath);
                if (fileName.EndsWith("_Normal"))
                {
                    continue;
                }

                var directory = Path.GetDirectoryName(spritePath)?.Replace('\\', '/');
                var normalPath = $"{directory}/{fileName}_Normal.png";
                if (!File.Exists(normalPath))
                {
                    continue;
                }

                checkedCount++;
                ConfigureNormalTextureImporter(normalPath, forceReimportAll);

                var normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                if (normalTexture == null || importer == null)
                {
                    continue;
                }

                var textures = new List<UnityEngine.SecondarySpriteTexture>(
                    importer.secondarySpriteTextures ?? new UnityEngine.SecondarySpriteTexture[0]);
                var changed = false;
                var found = false;

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
                    textures.Add(new UnityEngine.SecondarySpriteTexture
                    {
                        name = NormalName,
                        texture = normalTexture
                    });
                    changed = true;
                }

                if (changed)
                {
                    importer.secondarySpriteTextures = textures.ToArray();
                    importer.SaveAndReimport();
                    fixedCount++;
                    continue;
                }

                if (forceReimportAll)
                {
                    AssetDatabase.ImportAsset(
                        spritePath,
                        ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    reimportedCount++;
                }
            }
        }
        finally
        {
            repairInProgress = false;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Secondary Normals",
                $"Checked {checkedCount} sprite/normal pairs.\nUpdated {fixedCount} sprite importers.\nForce reimported {reimportedCount} already-registered sprites.",
                "OK");
        }

        Debug.Log($"SecondaryNormalTextureRepair checked {checkedCount} sprite/normal pairs, updated {fixedCount} sprite importers, and force reimported {reimportedCount} already-registered sprites.");
    }

    private static void ConfigureNormalTextureImporter(string normalPath, bool forceReimport)
    {
        var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (normalImporter == null)
        {
            return;
        }

        var changed = false;
        if (normalImporter.textureType != TextureImporterType.Default)
        {
            normalImporter.textureType = TextureImporterType.Default;
            changed = true;
        }

        if (normalImporter.sRGBTexture)
        {
            normalImporter.sRGBTexture = false;
            changed = true;
        }

        if (changed)
        {
            normalImporter.SaveAndReimport();
            return;
        }

        if (forceReimport)
        {
            AssetDatabase.ImportAsset(
                normalPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
    }

    internal static bool IsRepairInProgress => repairInProgress;
}
