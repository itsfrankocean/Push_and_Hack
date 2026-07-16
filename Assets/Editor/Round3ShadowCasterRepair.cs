using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

internal static class Round3ShadowCasterRepair
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

    [MenuItem("Tools/Lighting/Apply Round 3 Shadow Fix")]
    public static void Apply()
    {
        var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            foreach (var caster in root.GetComponentsInChildren<ShadowCaster2D>(true))
            {
                if (caster.gameObject.name != "MC")
                {
                    continue;
                }

                var renderer = caster.GetComponent<SpriteRenderer>();
                var so = new SerializedObject(caster);
                SetBool(so, "m_HasRenderer", renderer != null);
                SetBool(so, "m_UseRendererSilhouette", true);
                SetBool(so, "m_Enabled", false);
                SetBool(so, "m_CastsShadows", false);
                SetBool(so, "m_SelfShadows", false);
                SetInt(so, "m_CastingOption", 3);
                SetInt(so, "m_ShadowCastingSource", 2);
                SetFloat(so, "m_ShadowMesh.m_TrimEdge", 0f);
                SetInt(so, "m_PreviousShadowCastingSource", -1);
                SetInt(so, "m_PreviousEdgeProcessing", -1);
                SetObject(so, "m_PreviousShadowShape2DSource", null);

                var meshProperty = so.FindProperty("m_ShadowMesh.m_Mesh");
                if (meshProperty != null)
                {
                    meshProperty.objectReferenceValue = null;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("Round3ShadowCasterRepair: MC ShadowCaster2D set to Casting Option Cast And Self Shadow with SpriteRenderer shape provider.");
            }

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetBool(SerializedObject so, string path, bool value)
    {
        var property = so.FindProperty(path);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void SetInt(SerializedObject so, string path, int value)
    {
        var property = so.FindProperty(path);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetFloat(SerializedObject so, string path, float value)
    {
        var property = so.FindProperty(path);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetObject(SerializedObject so, string path, Object value)
    {
        var property = so.FindProperty(path);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }
}
