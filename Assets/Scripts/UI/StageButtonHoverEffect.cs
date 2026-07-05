using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class StageButtonHoverEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler
{
    private Image overlayImage;
    private Button button;
    private Color hoverColor;
    private Color pressedColor;
    private TMP_Text labelText;
    private Color normalLabelColor;
    private Color selectedLabelColor;
    private Vector3 normalLabelScale;
    private float selectedLabelScale = 1f;
    private bool useLabelEffect;
    private bool isPointerInside;
    private bool isSelected;
    private bool isPressed;

    private void Awake()
    {
        button = GetComponent<Button>();
        ApplyVisual();
    }

    private void OnDisable()
    {
        isPointerInside = false;
        isSelected = false;
        isPressed = false;
        ApplyVisual();
    }

    public void Configure(Image overlay, Color hover, Color pressed)
    {
        button = GetComponent<Button>();
        overlayImage = overlay;
        hoverColor = hover;
        pressedColor = pressed;
        ApplyVisual();
    }

    public void ConfigureLabel(TMP_Text label, Color selectedColor, float selectedScale)
    {
        labelText = label;
        selectedLabelColor = selectedColor;
        selectedLabelScale = Mathf.Max(0.01f, selectedScale);

        if (labelText != null)
        {
            normalLabelColor = labelText.color;
            normalLabelScale = labelText.transform.localScale;
            useLabelEffect = true;
        }

        ApplyVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        ApplyVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        isPressed = false;
        ApplyVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (CanShowEffect())
            isPressed = true;

        ApplyVisual();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        ApplyVisual();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        ApplyVisual();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        isPressed = false;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (overlayImage == null)
            return;

        if (!CanShowEffect())
        {
            overlayImage.color = Color.clear;
            ApplyLabelVisual(false);
            return;
        }

        if (isPressed)
        {
            overlayImage.color = pressedColor;
            ApplyLabelVisual(true);
            return;
        }

        bool isActive = isPointerInside || isSelected;
        overlayImage.color = isActive ? hoverColor : Color.clear;
        ApplyLabelVisual(isActive);
    }

    private void ApplyLabelVisual(bool isActive)
    {
        if (!useLabelEffect || labelText == null)
            return;

        labelText.color = isActive ? selectedLabelColor : normalLabelColor;
        labelText.transform.localScale = isActive
            ? normalLabelScale * selectedLabelScale
            : normalLabelScale;
    }

    private bool CanShowEffect()
    {
        return button == null || button.interactable;
    }
}
