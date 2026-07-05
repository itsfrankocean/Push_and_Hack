using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPPerTextStyle : MonoBehaviour
{
    [Header("Face")]
    public Color faceColor = Color.white;
    [Range(-1f, 1f)] public float faceDilate = 0.03f;

    [Header("Outline")]
    public Color outlineColor = Color.black;
    [Range(0f, 1f)] public float outlineThickness = 0.22f;

    private TextMeshProUGUI tmp;
    private Material runtimeMaterial;

    private static readonly int FaceColorID = Shader.PropertyToID("_FaceColor");
    private static readonly int FaceDilateID = Shader.PropertyToID("_FaceDilate");
    private static readonly int OutlineColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineWidthID = Shader.PropertyToID("_OutlineWidth");

    private void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();

        runtimeMaterial = new Material(tmp.fontMaterial);
        tmp.fontMaterial = runtimeMaterial;

        ApplyStyle();
    }

    private void OnValidate()
    {
        if (Application.isPlaying && runtimeMaterial != null)
            ApplyStyle();
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }

    public void ApplyStyle()
    {
        runtimeMaterial.SetColor(FaceColorID, faceColor);
        runtimeMaterial.SetFloat(FaceDilateID, faceDilate);
        runtimeMaterial.SetColor(OutlineColorID, outlineColor);
        runtimeMaterial.SetFloat(OutlineWidthID, outlineThickness);

        tmp.UpdateMeshPadding();
    }
}
