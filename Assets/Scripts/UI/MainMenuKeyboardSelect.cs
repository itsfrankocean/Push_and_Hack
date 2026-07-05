using UnityEngine;
using UnityEngine.UI;

public class MainMenuKeyboardSelector : MonoBehaviour
{
    [Header("Menu Buttons")]
    public Button[] buttons;

    private int currentIndex = 0;

    void Start()
    {
        currentIndex = 0;
        RefreshSelection();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveSelection(-1);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveSelection(1);
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            PressCurrentButton();
        }
    }

    void MoveSelection(int direction)
    {
        int nextIndex = currentIndex + direction;

        if (nextIndex < 0 || nextIndex >= buttons.Length)
            return;

        currentIndex = nextIndex;
        RefreshSelection();
    }

    void RefreshSelection()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            MenuButtonHover hover = buttons[i].GetComponent<MenuButtonHover>();

            if (hover != null)
                hover.SetSelected(i == currentIndex);
        }
    }

    void PressCurrentButton()
    {
        if (buttons == null || buttons.Length == 0)
            return;

        if (buttons[currentIndex] != null)
            buttons[currentIndex].onClick.Invoke();
    }
}