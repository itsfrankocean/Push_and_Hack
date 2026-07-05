using UnityEngine;
using System.Collections;

public class AmmoPopupAnimator : MonoBehaviour
{
    public RectTransform rect;
    public AmmoPopupUI ammoPopupUI;

    public float showX = 250f;   // visible position
    public float hideX = -170f;  // hidden position, leaving the edge visible
    public float speed = 8f;

    private bool isVisible = true;

    void Start()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (ammoPopupUI == null)
            ammoPopupUI = GetComponent<AmmoPopupUI>();

        isVisible = ammoPopupUI == null || ammoPopupUI.HasAmmoEntries();
        SetAnchoredX(isVisible ? showX : hideX);
    }

    void Update()
    {
        if (!CanUsePopupInput())
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Toggle();
        }
    }

    private bool CanUsePopupInput()
    {
        return GameStateManager.Instance == null ||
               GameStateManager.Instance.CanPopupInput;
    }

    public void Toggle()
    {
        isVisible = !isVisible;
        StopAllCoroutines();
        StartCoroutine(Slide());
    }

    public void Show()
    {
        isVisible = true;
        StopAllCoroutines();
        StartCoroutine(Slide());
    }

    public void Hide()
    {
        isVisible = false;
        StopAllCoroutines();
        StartCoroutine(Slide());
    }

    private void SetAnchoredX(float x)
    {
        if (rect == null)
            return;

        rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
    }

    IEnumerator Slide()
    {
        if (rect == null)
            yield break;

        float targetX = isVisible ? showX : hideX;

        while (Mathf.Abs(rect.anchoredPosition.x - targetX) > 0.1f)
        {
            float newX = Mathf.Lerp(rect.anchoredPosition.x, targetX, Time.deltaTime * speed);
            rect.anchoredPosition = new Vector2(newX, rect.anchoredPosition.y);
            yield return null;
        }

        rect.anchoredPosition = new Vector2(targetX, rect.anchoredPosition.y);
    }
}
