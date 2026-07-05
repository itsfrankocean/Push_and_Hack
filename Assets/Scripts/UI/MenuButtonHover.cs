using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target")]
    public TextMeshProUGUI targetText;

    [Header("Scale")]
    public float normalScale = 1f;
    public float hoverScale = 1.08f;

    [Header("Normal Glow")]
    public Color normalGlowColor = new Color(0f, 1f, 1f, 0.25f);
    public float normalUnderlayDilate = 0.05f;
    public float normalUnderlaySoftness = 0.25f;

    [Header("Hover Glow")]
    public Color hoverGlowColor = new Color(0f, 1f, 1f, 0.75f);
    public float hoverUnderlayDilate = 0.35f;
    public float hoverUnderlaySoftness = 0.65f;

    [Header("Speed")]
    public float speed = 12f;

    private RectTransform textRect;
    private Material mat;
    private bool isSelected = false;

    private static readonly int UnderlayColor = Shader.PropertyToID("_UnderlayColor");
    private static readonly int UnderlayDilate = Shader.PropertyToID("_UnderlayDilate");
    private static readonly int UnderlaySoftness = Shader.PropertyToID("_UnderlaySoftness");
    private static readonly int UnderlayOffsetX = Shader.PropertyToID("_UnderlayOffsetX");
    private static readonly int UnderlayOffsetY = Shader.PropertyToID("_UnderlayOffsetY");

    void Awake()
    {
        if (targetText == null)
            targetText = GetComponentInChildren<TextMeshProUGUI>();

        if (targetText == null) return;

        textRect = targetText.GetComponent<RectTransform>();

        mat = new Material(targetText.fontMaterial);
        targetText.fontMaterial = mat;

        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetFloat(UnderlayOffsetX, 0f);
        mat.SetFloat(UnderlayOffsetY, 0f);

        ApplyImmediate(false);
    }

    void Update()
    {
        if (targetText == null || textRect == null || mat == null) return;

        float targetScale = isSelected ? hoverScale : normalScale;
        Color targetGlow = isSelected ? hoverGlowColor : normalGlowColor;
        float targetDilate = isSelected ? hoverUnderlayDilate : normalUnderlayDilate;
        float targetSoftness = isSelected ? hoverUnderlaySoftness : normalUnderlaySoftness;

        textRect.localScale = Vector3.Lerp(textRect.localScale, Vector3.one * targetScale, Time.deltaTime * speed);

        mat.SetColor(UnderlayColor, Color.Lerp(mat.GetColor(UnderlayColor), targetGlow, Time.deltaTime * speed));
        mat.SetFloat(UnderlayDilate, Mathf.Lerp(mat.GetFloat(UnderlayDilate), targetDilate, Time.deltaTime * speed));
        mat.SetFloat(UnderlaySoftness, Mathf.Lerp(mat.GetFloat(UnderlaySoftness), targetSoftness, Time.deltaTime * speed));

        targetText.UpdateMeshPadding();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetSelected(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetSelected(false);
    }

    void ApplyImmediate(bool selected)
    {
        if (textRect == null || mat == null) return;

        textRect.localScale = Vector3.one * (selected ? hoverScale : normalScale);
        mat.SetColor(UnderlayColor, selected ? hoverGlowColor : normalGlowColor);
        mat.SetFloat(UnderlayDilate, selected ? hoverUnderlayDilate : normalUnderlayDilate);
        mat.SetFloat(UnderlaySoftness, selected ? hoverUnderlaySoftness : normalUnderlaySoftness);

        targetText.UpdateMeshPadding();
    }
}