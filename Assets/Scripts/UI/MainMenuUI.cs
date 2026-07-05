using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    private static readonly Color DefaultSelectedTextColor = new Color(0.1f, 0.85f, 1f, 1f);

    [Header("Keyboard Menu")]
    public Button[] menuButtons;
    public KeyCode submitKey = KeyCode.Z;
    public KeyCode backKey = KeyCode.X;
    public Color selectedTextColor = DefaultSelectedTextColor;
    public float selectedTextScale = 1.15f;

    [Header("Menu SFX")]
    public AudioClip menuSwapSound;
    public AudioClip menuSelectSound;
    public AudioSource menuAudioSource;
    public float menuSwapSoundVolume = 1f;
    public float menuSelectSoundVolume = 1f;

    [Header("Stage Select")]
    public string[] stageSceneNames = { "Level1", "Level2", "Level3", "Level4", "Level5", "Level6", "Level7", "Level8" };
    public string[] stageButtonLabels = { "STAGE 1", "STAGE 2", "STAGE 3", "STAGE 4", "STAGE 5", "STAGE 6", "STAGE 7", "STAGE 8" };
    public Color stageButtonBackgroundColor = new Color(0f, 0.12f, 0.14f, 0.18f);
    public Color stageButtonFrameColor = new Color(0.78f, 0.95f, 1f, 0.95f);
    public Color stageButtonAccentColor = new Color(0.08f, 0.9f, 1f, 0.95f);
    public Color stageButtonHoverColor = new Color(0.04f, 0.65f, 0.9f, 0.32f);
    public Color stageButtonPressedColor = new Color(0.08f, 0.9f, 1f, 0.42f);
    public float stageButtonFrameThickness = 3f;
    public Image menuPanelImage;
    public TMP_Text titleText;
    public Vector2 stageSelectPanelPosition = new Vector2(0f, -20f);
    public Vector2 stageSelectButtonSize = new Vector2(680f, 124f);
    public float stageSelectButtonSpacing = 36f;
    public string stageSelectTitle = "STAGE SELECT";
    public string stageSelectBackLabel = "BACK";
    public string stageSelectNextLabel = "NEXT";
    public string stageSelectPreviousLabel = "PREV";
    public float stageSelectNavigationButtonSpacing = 24f;

    [Header("Exit Confirmation")]
    public string exitConfirmQuestion = "ARE YOU REALLY GOING TO EXIT?";
    public string exitConfirmYesLabel = "YES";
    public string exitConfirmNoLabel = "NO";
    public Vector2 exitConfirmPanelSize = new Vector2(760f, 430f);
    public Vector2 exitConfirmPanelPosition = new Vector2(0f, -25f);
    public Vector2 exitConfirmButtonSize = new Vector2(320f, 78f);
    public float exitConfirmButtonSpacing = 24f;

    [Header("Settings")]
    public string settingsTitle = "SETTINGS";
    public string brightnessLabel = "BRIGHTNESS";
    public string musicVolumeLabel = "MUSIC";
    public string soundVolumeLabel = "SFX";
    public string settingsCloseLabel = "CLOSE";
    public Vector2 settingsPanelSize = new Vector2(760f, 640f);
    public Vector2 settingsPanelPosition = new Vector2(0f, -25f);
    public Vector2 settingsRowSize = new Vector2(680f, 82f);
    public Vector2 settingsCloseButtonSize = new Vector2(280f, 68f);
    public float settingsRowSpacing = 22f;
    public float settingsKeyboardStep = 0.05f;

    private TMP_Text[] menuLabels;
    private Color[] originalTextColors;
    private Vector3[] originalTextScales;
    private int selectedIndex = 0;
    private Button[] mainMenuButtons;
    private Button[] stageSelectButtons;
    private Button[] stagePageButtons;
    private TMP_Text[] stagePageButtonLabels;
    private Button[] exitConfirmButtons;
    private Button stageSelectPageButton;
    private TMP_Text stageSelectPageButtonLabel;
    private GameObject stageSelectPanel;
    private GameObject exitConfirmPanel;
    private GameObject settingsPanel;
    private Button[] settingsButtons;
    private Slider[] settingsSliders;
    private TMP_Text[] settingsValueTexts;
    private TMP_Text templateMenuLabel;
    private int currentStagePage;

    private const int StageButtonsPerPage = 4;
    private const int StageBackButtonIndex = StageButtonsPerPage;
    private const int StagePageButtonIndex = StageButtonsPerPage + 1;
    private const int SettingsBrightnessIndex = 0;
    private const int SettingsMusicIndex = 1;
    private const int SettingsSoundIndex = 2;
    private const int SettingsCloseIndex = 3;

    private void OnValidate()
    {
        selectedTextColor = DefaultSelectedTextColor;
    }

    private void Awake()
    {
        selectedTextColor = DefaultSelectedTextColor;
        GameSettings.Load();
        GameSettings.ApplyAll();
        CacheMenuButtons();
        mainMenuButtons = menuButtons;
        CacheMenuPanelImage();
        CacheTitleText();
        CacheTemplateMenuLabel();
        CreateStageSelectPanel();
        CreateExitConfirmPanel();
        CreateSettingsPanel();
        CacheMenuVisuals();
    }

    private void Start()
    {
        ShowMainMenu(false);
    }

    private void Update()
    {
        HandleKeyboardInput();
    }

    private void OnDisable()
    {
        RestoreAllButtonVisuals();
    }

    private void CacheMenuButtons()
    {
        if (menuButtons != null && menuButtons.Length > 0)
            return;

        menuButtons = new[]
        {
            FindButtonByName("PlayButton"),
            FindButtonByName("SettingButton"),
            FindButtonByName("SettingsButton"),
            FindButtonByName("ExitButton")
        };

        menuButtons = System.Array.FindAll(menuButtons, button => button != null);
    }

    private void CacheTemplateMenuLabel()
    {
        if (mainMenuButtons == null)
            return;

        for (int i = 0; i < mainMenuButtons.Length; i++)
        {
            if (mainMenuButtons[i] == null)
                continue;

            templateMenuLabel = mainMenuButtons[i].GetComponentInChildren<TMP_Text>(true);

            if (templateMenuLabel != null)
                return;
        }
    }

    private void CacheMenuPanelImage()
    {
        if (menuPanelImage != null)
            return;

        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == "MenuPanel")
            {
                menuPanelImage = children[i].GetComponent<Image>();
                return;
            }
        }
    }

    private void CacheTitleText()
    {
        if (titleText != null)
            return;

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == "TitleText")
            {
                titleText = texts[i];
                return;
            }
        }

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].text == "PUSH & HACK")
            {
                titleText = texts[i];
                return;
            }
        }
    }

    private Button FindButtonByName(string buttonName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == buttonName)
                return buttons[i];
        }

        return null;
    }

    private void CacheMenuVisuals()
    {
        if (menuButtons == null)
            return;

        menuLabels = new TMP_Text[menuButtons.Length];
        originalTextColors = new Color[menuButtons.Length];
        originalTextScales = new Vector3[menuButtons.Length];

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null)
                continue;

            TMP_Text label = menuButtons[i].GetComponentInChildren<TMP_Text>(true);
            menuLabels[i] = label;

            if (label == null)
                continue;

            originalTextColors[i] = label.color;
            originalTextScales[i] = label.transform.localScale;
        }
    }

    private void SetCurrentMenuButtons(Button[] buttons, int defaultIndex)
    {
        RestoreAllButtonVisuals();

        menuButtons = buttons != null
            ? System.Array.FindAll(buttons, button => button != null)
            : new Button[0];
        DisableAutomaticNavigation(menuButtons);

        selectedIndex = 0;
        CacheMenuVisuals();

        if (menuButtons.Length == 0)
            return;

        SelectButton(Mathf.Clamp(defaultIndex, 0, menuButtons.Length - 1));
    }

    private void HandleKeyboardInput()
    {
        if (IsSettingsOpen() && Input.GetKeyDown(backKey))
        {
            CloseSettings(true);
            return;
        }

        if (IsExitConfirmOpen() && Input.GetKeyDown(backKey))
        {
            CloseExitConfirm(true);
            return;
        }

        if (IsStageSelectOpen() && Input.GetKeyDown(backKey))
        {
            BackToMainMenu();
            return;
        }

        if (menuButtons == null || menuButtons.Length == 0)
            return;

        if (IsSettingsOpen())
        {
            HandleSettingsKeyboardInput();
            return;
        }

        if (IsStageSelectOpen())
        {
            HandleStageSelectKeyboardInput();
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelection(-1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelection(1);
            return;
        }

        if (Input.GetKeyDown(submitKey))
        {
            SubmitSelectedButton();
        }
    }

    private void HandleStageSelectKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveStageSelectionVertical(-1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveStageSelectionVertical(1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveStageNavigationHorizontal(-1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveStageNavigationHorizontal(1);
            return;
        }

        if (Input.GetKeyDown(submitKey))
        {
            SubmitSelectedButton();
        }
    }

    private void HandleSettingsKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MoveSelection(-1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MoveSelection(1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            AdjustSelectedSetting(-settingsKeyboardStep);
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            AdjustSelectedSetting(settingsKeyboardStep);
            return;
        }

        if (Input.GetKeyDown(submitKey))
        {
            SubmitSelectedButton();
        }
    }

    private void AdjustSelectedSetting(float delta)
    {
        if (selectedIndex < 0 || selectedIndex >= SettingsCloseIndex)
            return;

        if (settingsSliders == null ||
            selectedIndex >= settingsSliders.Length ||
            settingsSliders[selectedIndex] == null)
        {
            return;
        }

        settingsSliders[selectedIndex].value = Mathf.Clamp01(settingsSliders[selectedIndex].value + delta);
        PlayMenuSwapSound();
    }

    private bool IsStageSelectOpen()
    {
        return stageSelectPanel != null && stageSelectPanel.activeInHierarchy;
    }

    private bool IsExitConfirmOpen()
    {
        return exitConfirmPanel != null && exitConfirmPanel.activeInHierarchy;
    }

    private bool IsSettingsOpen()
    {
        return settingsPanel != null && settingsPanel.activeInHierarchy;
    }

    private void MoveSelection(int direction)
    {
        if (menuButtons == null || menuButtons.Length == 0)
            return;

        int startIndex = selectedIndex;

        for (int i = 1; i <= menuButtons.Length; i++)
        {
            int nextIndex = startIndex + direction * i;

            if (nextIndex < 0)
                nextIndex = menuButtons.Length - 1;

            if (nextIndex >= menuButtons.Length)
                nextIndex = 0;

            if (IsUsableButton(menuButtons[nextIndex]))
            {
                SelectButton(nextIndex);
                PlayMenuSwapSound();
                return;
            }
        }
    }

    private void MoveStageSelectionVertical(int direction)
    {
        if (menuButtons == null || menuButtons.Length == 0)
            return;

        int lastVerticalIndex = Mathf.Min(StageBackButtonIndex, menuButtons.Length - 1);
        int startIndex = selectedIndex == StagePageButtonIndex ? StageBackButtonIndex : selectedIndex;
        int verticalButtonCount = lastVerticalIndex + 1;

        for (int i = 1; i <= verticalButtonCount; i++)
        {
            int nextIndex = (startIndex + direction * i) % verticalButtonCount;

            if (nextIndex < 0)
                nextIndex += verticalButtonCount;

            if (IsUsableButton(menuButtons[nextIndex]))
            {
                SelectButton(nextIndex);
                PlayMenuSwapSound();
                return;
            }
        }
    }

    private void MoveStageNavigationHorizontal(int direction)
    {
        if (menuButtons == null || menuButtons.Length <= StagePageButtonIndex)
            return;

        if (direction > 0 &&
            selectedIndex == StageBackButtonIndex &&
            IsUsableButton(menuButtons[StagePageButtonIndex]))
        {
            SelectButton(StagePageButtonIndex);
            PlayMenuSwapSound();
            return;
        }

        if (direction < 0 &&
            selectedIndex == StagePageButtonIndex &&
            IsUsableButton(menuButtons[StageBackButtonIndex]))
        {
            SelectButton(StageBackButtonIndex);
            PlayMenuSwapSound();
        }
    }

    private void SelectButton(int index)
    {
        if (menuButtons == null || menuButtons.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, menuButtons.Length - 1);

        if (!IsUsableButton(menuButtons[index]))
            return;

        selectedIndex = index;
        RefreshButtonVisuals();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(menuButtons[selectedIndex].gameObject);
    }

    private void SubmitSelectedButton()
    {
        if (menuButtons == null ||
            selectedIndex < 0 ||
            selectedIndex >= menuButtons.Length ||
            !IsUsableButton(menuButtons[selectedIndex]))
        {
            return;
        }

        menuButtons[selectedIndex].onClick.Invoke();
    }

    private bool IsUsableButton(Button button)
    {
        return button != null &&
               button.gameObject.activeInHierarchy &&
               button.interactable;
    }

    private void DisableAutomaticNavigation(Button[] buttons)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            Navigation navigation = buttons[i].navigation;
            navigation.mode = Navigation.Mode.None;
            buttons[i].navigation = navigation;
        }
    }

    private void RefreshButtonVisuals()
    {
        RestoreAllButtonVisuals();

        if (menuLabels == null ||
            selectedIndex < 0 ||
            selectedIndex >= menuLabels.Length ||
            menuLabels[selectedIndex] == null)
        {
            return;
        }

        menuLabels[selectedIndex].color = selectedTextColor;
        menuLabels[selectedIndex].transform.localScale =
            originalTextScales[selectedIndex] * selectedTextScale;
    }

    private void RestoreAllButtonVisuals()
    {
        if (menuLabels == null)
            return;

        for (int i = 0; i < menuLabels.Length; i++)
        {
            if (menuLabels[i] == null)
                continue;

            menuLabels[i].color = originalTextColors[i];
            menuLabels[i].transform.localScale = originalTextScales[i];
        }
    }

    private void PlayMenuSwapSound()
    {
        if (AudioManager.I != null && AudioManager.I.sfxMenuBeep != null)
        {
            AudioManager.I.PlayOneShot(AudioManager.I.sfxMenuBeep, menuSwapSoundVolume);
            return;
        }

        if (menuSwapSound == null)
            return;

        EnsureMenuAudioSource();

        if (menuAudioSource != null)
            menuAudioSource.PlayOneShot(menuSwapSound, GameSettings.ScaleSoundVolume(menuSwapSoundVolume));
    }

    private void PlayMenuSelectSound()
    {
        if (AudioManager.I != null && AudioManager.I.sfxMenuSelect != null)
        {
            AudioManager.I.PlayOneShot(AudioManager.I.sfxMenuSelect, menuSelectSoundVolume);
            return;
        }

        AudioManager.PlayDetachedOneShot(menuSelectSound, menuSelectSoundVolume);
    }

    private void EnsureMenuAudioSource()
    {
        if (menuAudioSource != null)
            return;

        menuAudioSource = GetComponent<AudioSource>();

        if (menuAudioSource == null)
            menuAudioSource = gameObject.AddComponent<AudioSource>();
    }

    private void CreateStageSelectPanel()
    {
        if (stageSelectPanel != null)
            return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Transform parent = parentCanvas != null ? parentCanvas.transform : transform;

        stageSelectPanel = new GameObject("StageSelectPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
        stageSelectPanel.transform.SetParent(parent, false);

        RectTransform panelRect = stageSelectPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = stageSelectPanelPosition;
        panelRect.sizeDelta = new Vector2(stageSelectButtonSize.x, GetStageSelectPanelHeight());

        VerticalLayoutGroup layout = stageSelectPanel.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = stageSelectButtonSpacing;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateStageSelectTitle();
        CreateStageSelectButtons();

        stageSelectPanel.SetActive(false);
    }

    private void CreateStageSelectTitle()
    {
        GameObject titleObject = new GameObject("StageSelectTitle", typeof(RectTransform));
        titleObject.transform.SetParent(stageSelectPanel.transform, false);

        LayoutElement layout = titleObject.AddComponent<LayoutElement>();
        layout.preferredWidth = stageSelectButtonSize.x;
        layout.preferredHeight = stageSelectButtonSize.y;

        TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
        ApplyTemplateTextStyle(titleText);
        titleText.text = stageSelectTitle;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = templateMenuLabel != null ? (templateMenuLabel.fontSize + 4f) * 2f : 78f;
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Overflow;
        titleText.color = Color.white;
    }

    private float GetStageSelectPanelHeight()
    {
        int itemCount = StageButtonsPerPage + 2; // title + page buttons + navigation row
        int gapCount = Mathf.Max(0, itemCount - 1);

        return stageSelectButtonSize.y * itemCount + stageSelectButtonSpacing * gapCount;
    }

    private void CreateStageSelectButtons()
    {
        stagePageButtons = new Button[StageButtonsPerPage];
        stagePageButtonLabels = new TMP_Text[StageButtonsPerPage];
        stageSelectButtons = new Button[StageButtonsPerPage + 2];

        for (int i = 0; i < StageButtonsPerPage; i++)
        {
            Button button = CreateStageSelectButton($"StageButton_{i + 1}", string.Empty);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            stagePageButtons[i] = button;
            stagePageButtonLabels[i] = label;
            stageSelectButtons[i] = button;
        }

        GameObject navigationRow = new GameObject(
            "StageSelectNavigationRow",
            typeof(RectTransform),
            typeof(LayoutElement)
        );
        navigationRow.transform.SetParent(stageSelectPanel.transform, false);

        RectTransform navigationRect = navigationRow.GetComponent<RectTransform>();
        navigationRect.sizeDelta = stageSelectButtonSize;

        LayoutElement navigationLayout = navigationRow.GetComponent<LayoutElement>();
        navigationLayout.preferredWidth = stageSelectButtonSize.x;
        navigationLayout.preferredHeight = stageSelectButtonSize.y;

        Vector2 navigationButtonSize = new Vector2(
            (stageSelectButtonSize.x - stageSelectNavigationButtonSpacing) * 0.5f,
            stageSelectButtonSize.y
        );
        float navigationButtonOffset = (navigationButtonSize.x + stageSelectNavigationButtonSpacing) * 0.5f;

        Button backButton = CreateStageSelectButton(navigationRow.transform, "StageSelectBackButton", stageSelectBackLabel, navigationButtonSize);
        SetNavigationButtonRect(backButton, -navigationButtonOffset, navigationButtonSize);
        backButton.onClick.AddListener(BackToMainMenu);
        stageSelectButtons[StageBackButtonIndex] = backButton;

        stageSelectPageButton = CreateStageSelectButton(navigationRow.transform, "StageSelectPageButton", stageSelectNextLabel, navigationButtonSize);
        SetNavigationButtonRect(stageSelectPageButton, navigationButtonOffset, navigationButtonSize);
        stageSelectPageButton.transform.SetAsLastSibling();
        stageSelectPageButton.onClick.AddListener(ShowNextStagePage);
        stageSelectPageButtonLabel = stageSelectPageButton.GetComponentInChildren<TMP_Text>(true);
        stageSelectButtons[StagePageButtonIndex] = stageSelectPageButton;

        RefreshStageSelectPage();
    }

    private Button CreateStageSelectButton(string objectName, string label)
    {
        return CreateStageSelectButton(stageSelectPanel.transform, objectName, label, stageSelectButtonSize);
    }

    private Button CreateStageSelectButton(Transform parent, string objectName, string label, Vector2 buttonSize)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement)
        );

        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = buttonSize;

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = buttonSize.x;
        layout.preferredHeight = buttonSize.y;

        Image image = buttonObject.GetComponent<Image>();
        image.color = stageButtonBackgroundColor;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        Image hoverOverlay = CreateStageButtonHoverOverlay(buttonObject.transform);
        StageButtonHoverEffect hoverEffect = buttonObject.AddComponent<StageButtonHoverEffect>();
        hoverEffect.Configure(hoverOverlay, stageButtonHoverColor, stageButtonPressedColor);

        CreateStageButtonFrame(buttonObject.transform, buttonSize);

        GameObject textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = textObject.AddComponent<TextMeshProUGUI>();
        ApplyTemplateTextStyle(labelText);
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = Mathf.Min(templateMenuLabel != null ? templateMenuLabel.fontSize * 2f : 70f, buttonSize.x * 0.24f);
        labelText.enableWordWrapping = false;
        labelText.overflowMode = TextOverflowModes.Overflow;
        labelText.color = Color.white;
        labelText.raycastTarget = false;

        return button;
    }

    private void SetNavigationButtonRect(Button button, float anchoredX, Vector2 buttonSize)
    {
        if (button == null)
            return;

        RectTransform rect = button.GetComponent<RectTransform>();

        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(anchoredX, 0f);
        rect.sizeDelta = buttonSize;
    }

    private void CreateExitConfirmPanel()
    {
        if (exitConfirmPanel != null)
            return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Transform parent = parentCanvas != null ? parentCanvas.transform : transform;

        exitConfirmPanel = new GameObject(
            "ExitConfirmPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        exitConfirmPanel.transform.SetParent(parent, false);

        RectTransform panelRect = exitConfirmPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = exitConfirmPanelPosition;
        panelRect.sizeDelta = exitConfirmPanelSize;

        Image panelImage = exitConfirmPanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0.09f, 0.1f, 0.86f);
        panelImage.raycastTarget = true;

        CreateExitConfirmFrame(exitConfirmPanel.transform);
        CreateExitConfirmContent(exitConfirmPanel.transform);

        exitConfirmPanel.SetActive(false);
    }

    private void CreateExitConfirmContent(Transform parent)
    {
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentObject.transform.SetParent(parent, false);

        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(64f, 54f);
        contentRect.offsetMax = new Vector2(-64f, -46f);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = exitConfirmButtonSpacing;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateExitConfirmQuestion(contentObject.transform);

        exitConfirmButtons = new[]
        {
            CreateExitConfirmButton(contentObject.transform, "ExitConfirmYesButton", exitConfirmYesLabel, ConfirmExit),
            CreateExitConfirmButton(contentObject.transform, "ExitConfirmNoButton", exitConfirmNoLabel, () => CloseExitConfirm(true))
        };
    }

    private void CreateExitConfirmQuestion(Transform parent)
    {
        GameObject questionObject = new GameObject("Question", typeof(RectTransform));
        questionObject.transform.SetParent(parent, false);

        LayoutElement layout = questionObject.AddComponent<LayoutElement>();
        layout.preferredWidth = exitConfirmPanelSize.x - 128f;
        layout.preferredHeight = 142f;

        TextMeshProUGUI questionText = questionObject.AddComponent<TextMeshProUGUI>();
        ApplyTemplateTextStyle(questionText);
        questionText.text = exitConfirmQuestion;
        questionText.alignment = TextAlignmentOptions.Center;
        questionText.color = Color.white;
        questionText.enableAutoSizing = true;
        questionText.fontSizeMin = 26f;
        questionText.fontSizeMax = templateMenuLabel != null ? templateMenuLabel.fontSize * 1.35f : 48f;
        questionText.enableWordWrapping = true;
        questionText.overflowMode = TextOverflowModes.Truncate;
        questionText.raycastTarget = false;
    }

    private Button CreateExitConfirmButton(Transform parent, string objectName, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement)
        );

        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = exitConfirmButtonSize;

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = exitConfirmButtonSize.x;
        layout.preferredHeight = exitConfirmButtonSize.y;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0f, 0.18f, 0.2f, 0.24f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(action);

        Image hoverOverlay = CreateStageButtonHoverOverlay(buttonObject.transform);
        StageButtonHoverEffect hoverEffect = buttonObject.AddComponent<StageButtonHoverEffect>();
        hoverEffect.Configure(hoverOverlay, stageButtonHoverColor, stageButtonPressedColor);

        CreateExitConfirmButtonFrame(buttonObject.transform);

        GameObject textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = textObject.AddComponent<TextMeshProUGUI>();
        ApplyTemplateTextStyle(labelText);
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = templateMenuLabel != null ? templateMenuLabel.fontSize * 1.55f : 54f;
        labelText.enableWordWrapping = false;
        labelText.overflowMode = TextOverflowModes.Overflow;
        labelText.color = Color.white;
        labelText.raycastTarget = false;

        return button;
    }

    private void CreateExitConfirmFrame(Transform parent)
    {
        float width = exitConfirmPanelSize.x;
        float height = exitConfirmPanelSize.y;
        float line = Mathf.Max(1f, stageButtonFrameThickness);
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        float inset = 34f;
        float cornerLength = 130f;
        float verticalLength = 92f;

        CreateStageFramePiece(parent, "DialogTopLine", new Vector2(0f, halfHeight - inset), new Vector2(width - 210f, line), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogBottomLine", new Vector2(0f, -halfHeight + inset), new Vector2(width - 210f, line), stageButtonFrameColor, 0f);

        CreateStageFramePiece(parent, "DialogTopLeftH", new Vector2(-halfWidth + inset + cornerLength * 0.5f, halfHeight - inset), new Vector2(cornerLength, line * 1.5f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogTopLeftV", new Vector2(-halfWidth + inset, halfHeight - inset - verticalLength * 0.5f), new Vector2(line * 1.5f, verticalLength), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogTopRightH", new Vector2(halfWidth - inset - cornerLength * 0.5f, halfHeight - inset), new Vector2(cornerLength, line * 1.5f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogTopRightV", new Vector2(halfWidth - inset, halfHeight - inset - verticalLength * 0.5f), new Vector2(line * 1.5f, verticalLength), stageButtonFrameColor, 0f);

        CreateStageFramePiece(parent, "DialogBottomLeftH", new Vector2(-halfWidth + inset + cornerLength * 0.5f, -halfHeight + inset), new Vector2(cornerLength, line * 1.5f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogBottomLeftV", new Vector2(-halfWidth + inset, -halfHeight + inset + verticalLength * 0.5f), new Vector2(line * 1.5f, verticalLength), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogBottomRightH", new Vector2(halfWidth - inset - cornerLength * 0.5f, -halfHeight + inset), new Vector2(cornerLength, line * 1.5f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogBottomRightV", new Vector2(halfWidth - inset, -halfHeight + inset + verticalLength * 0.5f), new Vector2(line * 1.5f, verticalLength), stageButtonFrameColor, 0f);

        CreateStageHatchGroup(parent, "DialogTopHatch", new Vector2(-halfWidth + 190f, halfHeight - inset + 2f), -20f);
        CreateStageHatchGroup(parent, "DialogBottomHatch", new Vector2(halfWidth - 250f, -halfHeight + inset + 2f), -20f);
    }

    private void CreateExitConfirmButtonFrame(Transform parent)
    {
        float width = exitConfirmButtonSize.x;
        float height = exitConfirmButtonSize.y;
        float line = Mathf.Max(1f, stageButtonFrameThickness);
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        float inset = 10f;
        float cornerLength = 58f;

        CreateStageFramePiece(parent, "ButtonTopLine", new Vector2(0f, halfHeight - inset), new Vector2(width - 96f, line), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "ButtonBottomLine", new Vector2(0f, -halfHeight + inset), new Vector2(width - 96f, line), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "ButtonTopLeft", new Vector2(-halfWidth + inset + cornerLength * 0.5f, halfHeight - inset), new Vector2(cornerLength, line * 1.4f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "ButtonTopRight", new Vector2(halfWidth - inset - cornerLength * 0.5f, halfHeight - inset), new Vector2(cornerLength, line * 1.4f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "ButtonBottomLeft", new Vector2(-halfWidth + inset + cornerLength * 0.5f, -halfHeight + inset), new Vector2(cornerLength, line * 1.4f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "ButtonBottomRight", new Vector2(halfWidth - inset - cornerLength * 0.5f, -halfHeight + inset), new Vector2(cornerLength, line * 1.4f), stageButtonFrameColor, 0f);
    }

    private Image CreateStageButtonHoverOverlay(Transform parent)
    {
        GameObject overlayObject = new GameObject(
            "HoverOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        overlayObject.transform.SetParent(parent, false);

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = overlayObject.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = false;

        return image;
    }

    private void CreateStageButtonFrame(Transform parent, Vector2 buttonSize)
    {
        float width = buttonSize.x;
        float height = buttonSize.y;
        float line = Mathf.Max(1f, stageButtonFrameThickness);
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        float sideInset = Mathf.Min(32f, width * 0.09f);
        float verticalInset = Mathf.Min(16f, height * 0.13f);
        float cornerLength = Mathf.Clamp(width * 0.14f, 46f, 96f);
        float verticalLength = Mathf.Clamp(height * 0.45f, 42f, 56f);
        float centerLineWidth = width * 0.48f;

        CreateStageFramePiece(parent, "TopLineLeft", new Vector2(-width * 0.15f, halfHeight - verticalInset), new Vector2(centerLineWidth, line), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "TopLineRight", new Vector2(width * 0.18f, halfHeight - verticalInset), new Vector2(centerLineWidth, line), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "BottomLineLeft", new Vector2(-width * 0.18f, -halfHeight + verticalInset), new Vector2(centerLineWidth, line), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "BottomLineRight", new Vector2(width * 0.15f, -halfHeight + verticalInset), new Vector2(centerLineWidth, line), stageButtonFrameColor, 0f);

        CreateStageFramePiece(parent, "TopLeftCornerH", new Vector2(-halfWidth + sideInset + cornerLength * 0.5f, halfHeight - verticalInset), new Vector2(cornerLength, line * 1.4f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "TopLeftCornerV", new Vector2(-halfWidth + sideInset, halfHeight - verticalInset - verticalLength * 0.5f), new Vector2(line * 1.4f, verticalLength), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "TopRightCornerH", new Vector2(halfWidth - sideInset - cornerLength * 0.5f, halfHeight - verticalInset), new Vector2(cornerLength, line * 1.4f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "TopRightCornerV", new Vector2(halfWidth - sideInset, halfHeight - verticalInset - verticalLength * 0.5f), new Vector2(line * 1.4f, verticalLength), stageButtonFrameColor, 0f);

        CreateStageFramePiece(parent, "BottomLeftCornerH", new Vector2(-halfWidth + sideInset + cornerLength * 0.5f, -halfHeight + verticalInset), new Vector2(cornerLength, line * 1.4f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "BottomLeftCornerV", new Vector2(-halfWidth + sideInset, -halfHeight + verticalInset + verticalLength * 0.5f), new Vector2(line * 1.4f, verticalLength), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "BottomRightCornerH", new Vector2(halfWidth - sideInset - cornerLength * 0.5f, -halfHeight + verticalInset), new Vector2(cornerLength, line * 1.4f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "BottomRightCornerV", new Vector2(halfWidth - sideInset, -halfHeight + verticalInset + verticalLength * 0.5f), new Vector2(line * 1.4f, verticalLength), stageButtonFrameColor, 0f);

        CreateStageFramePiece(parent, "TopLeftSlant", new Vector2(-halfWidth + sideInset + 14f, halfHeight - verticalInset - 6f), new Vector2(30f, line * 1.6f), stageButtonFrameColor, -45f);
        CreateStageFramePiece(parent, "TopRightSlant", new Vector2(halfWidth - sideInset - 14f, halfHeight - verticalInset - 6f), new Vector2(30f, line * 1.6f), stageButtonFrameColor, 45f);
        CreateStageFramePiece(parent, "BottomLeftSlant", new Vector2(-halfWidth + sideInset + 14f, -halfHeight + verticalInset + 6f), new Vector2(30f, line * 1.6f), stageButtonFrameColor, 45f);
        CreateStageFramePiece(parent, "BottomRightSlant", new Vector2(halfWidth - sideInset - 14f, -halfHeight + verticalInset + 6f), new Vector2(30f, line * 1.6f), stageButtonFrameColor, -45f);

        CreateStageHatchGroup(parent, "TopHatch", new Vector2(-halfWidth + Mathf.Min(150f, width * 0.22f), halfHeight - verticalInset + 2f), -20f);
        CreateStageHatchGroup(parent, "BottomHatch", new Vector2(halfWidth - Mathf.Min(190f, width * 0.28f), -halfHeight + verticalInset + 2f), -20f);
    }

    private void CreateStageHatchGroup(Transform parent, string groupName, Vector2 startPosition, float rotation)
    {
        const int hatchCount = 7;
        const float hatchSpacing = 10f;

        for (int i = 0; i < hatchCount; i++)
        {
            Vector2 position = startPosition + new Vector2(i * hatchSpacing, 0f);
            CreateStageFramePiece(parent, $"{groupName}_{i}", position, new Vector2(8f, 2f), stageButtonAccentColor, rotation);
        }
    }

    private Image CreateStageFramePiece(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, Color color, float rotation)
    {
        GameObject pieceObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        pieceObject.transform.SetParent(parent, false);

        RectTransform rect = pieceObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

        Image image = pieceObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    private void ApplyTemplateTextStyle(TMP_Text targetText)
    {
        if (targetText == null || templateMenuLabel == null)
            return;

        targetText.font = templateMenuLabel.font;
        targetText.fontSharedMaterial = templateMenuLabel.fontSharedMaterial;
        targetText.fontSize = templateMenuLabel.fontSize;
        targetText.fontStyle = templateMenuLabel.fontStyle;
        targetText.enableAutoSizing = templateMenuLabel.enableAutoSizing;
        targetText.fontSizeMin = templateMenuLabel.fontSizeMin;
        targetText.fontSizeMax = templateMenuLabel.fontSizeMax;
        targetText.characterSpacing = templateMenuLabel.characterSpacing;
        targetText.wordSpacing = templateMenuLabel.wordSpacing;
        targetText.lineSpacing = templateMenuLabel.lineSpacing;
    }

    private string GetStageButtonLabel(int index)
    {
        if (stageButtonLabels != null &&
            index >= 0 &&
            index < stageButtonLabels.Length &&
            !string.IsNullOrEmpty(stageButtonLabels[index]))
        {
            return stageButtonLabels[index];
        }

        return $"STAGE {index + 1}";
    }

    private string GetStageSceneName(int index)
    {
        if (stageSceneNames != null &&
            index >= 0 &&
            index < stageSceneNames.Length)
        {
            return stageSceneNames[index];
        }

        return string.Empty;
    }

    private int GetStageCount()
    {
        int sceneCount = stageSceneNames != null ? stageSceneNames.Length : 0;
        int labelCount = stageButtonLabels != null ? stageButtonLabels.Length : 0;
        return Mathf.Max(sceneCount, labelCount);
    }

    private int GetStagePageCount()
    {
        return Mathf.Max(1, Mathf.CeilToInt(GetStageCount() / (float)StageButtonsPerPage));
    }

    private void RefreshStageSelectPage()
    {
        if (stagePageButtons == null)
            return;

        int stageCount = GetStageCount();
        int pageCount = GetStagePageCount();
        currentStagePage = Mathf.Clamp(currentStagePage, 0, pageCount - 1);
        int firstStageIndex = currentStagePage * StageButtonsPerPage;

        for (int i = 0; i < stagePageButtons.Length; i++)
        {
            Button button = stagePageButtons[i];

            if (button == null)
                continue;

            int stageIndex = firstStageIndex + i;
            bool hasStage = stageIndex < stageCount;
            button.gameObject.SetActive(hasStage);
            button.onClick.RemoveAllListeners();

            if (stagePageButtonLabels != null &&
                i < stagePageButtonLabels.Length &&
                stagePageButtonLabels[i] != null)
            {
                stagePageButtonLabels[i].text = hasStage ? GetStageButtonLabel(stageIndex) : string.Empty;
            }

            if (!hasStage)
                continue;

            int capturedStageIndex = stageIndex;
            button.onClick.AddListener(() => SelectStage(capturedStageIndex));
        }

        if (stageSelectPageButton != null)
            stageSelectPageButton.interactable = pageCount > 1;

        if (stageSelectPageButtonLabel != null)
            stageSelectPageButtonLabel.text = currentStagePage < pageCount - 1 ? stageSelectNextLabel : stageSelectPreviousLabel;
    }

    private void CreateSettingsPanel()
    {
        if (settingsPanel != null)
            return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Transform parent = parentCanvas != null ? parentCanvas.transform : transform;

        settingsPanel = new GameObject(
            "SettingsPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        settingsPanel.transform.SetParent(parent, false);

        RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = settingsPanelPosition;
        panelRect.sizeDelta = settingsPanelSize;

        Image panelImage = settingsPanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0.09f, 0.1f, 0.86f);
        panelImage.raycastTarget = true;

        CreateSettingsPanelFrame(settingsPanel.transform);
        CreateSettingsContent(settingsPanel.transform);
        settingsPanel.SetActive(false);
    }

    private void CreateSettingsContent(Transform parent)
    {
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentObject.transform.SetParent(parent, false);

        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(40f, 42f);
        contentRect.offsetMax = new Vector2(-40f, -42f);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = settingsRowSpacing;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateSettingsTitle(contentObject.transform);

        settingsButtons = new Button[4];
        settingsSliders = new Slider[3];
        settingsValueTexts = new TMP_Text[3];

        settingsButtons[SettingsBrightnessIndex] = CreateSettingsSliderRow(
            contentObject.transform,
            "BrightnessRow",
            brightnessLabel,
            GameSettings.Brightness,
            SettingsBrightnessIndex
        );

        settingsButtons[SettingsMusicIndex] = CreateSettingsSliderRow(
            contentObject.transform,
            "MusicRow",
            musicVolumeLabel,
            GameSettings.MusicVolume,
            SettingsMusicIndex
        );

        settingsButtons[SettingsSoundIndex] = CreateSettingsSliderRow(
            contentObject.transform,
            "SoundRow",
            soundVolumeLabel,
            GameSettings.SoundVolume,
            SettingsSoundIndex
        );

        settingsButtons[SettingsCloseIndex] = CreateSettingsCloseButton(contentObject.transform);
    }

    private void CreateSettingsTitle(Transform parent)
    {
        GameObject titleObject = new GameObject("SettingsTitle", typeof(RectTransform));
        titleObject.transform.SetParent(parent, false);

        LayoutElement layout = titleObject.AddComponent<LayoutElement>();
        layout.preferredWidth = settingsRowSize.x;
        layout.preferredHeight = 86f;

        TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
        ApplyTemplateTextStyle(titleText);
        titleText.text = settingsTitle;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = templateMenuLabel != null ? (templateMenuLabel.fontSize + 4f) * 1.82f : 70f;
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Overflow;
        titleText.color = Color.white;
        titleText.raycastTarget = false;
    }

    private Button CreateSettingsSliderRow(Transform parent, string objectName, string label, float value, int settingIndex)
    {
        GameObject rowObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement)
        );
        rowObject.transform.SetParent(parent, false);

        RectTransform rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.sizeDelta = settingsRowSize;

        LayoutElement layout = rowObject.GetComponent<LayoutElement>();
        layout.preferredWidth = settingsRowSize.x;
        layout.preferredHeight = settingsRowSize.y;

        Image image = rowObject.GetComponent<Image>();
        image.color = stageButtonBackgroundColor;
        image.raycastTarget = true;

        Button button = rowObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => SelectSettingRow(settingIndex));

        Image hoverOverlay = CreateStageButtonHoverOverlay(rowObject.transform);
        StageButtonHoverEffect hoverEffect = rowObject.AddComponent<StageButtonHoverEffect>();
        hoverEffect.Configure(hoverOverlay, stageButtonHoverColor, stageButtonPressedColor);

        CreateStageButtonFrame(rowObject.transform, settingsRowSize);
        CreateSettingsRowLabel(rowObject.transform, label);
        Slider slider = CreateSettingsSlider(rowObject.transform, value, settingIndex);
        settingsSliders[settingIndex] = slider;

        return button;
    }

    private void CreateSettingsRowLabel(Transform parent, string label)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(68f, 0f);
        rect.sizeDelta = new Vector2(260f, 0f);

        TextMeshProUGUI labelText = labelObject.AddComponent<TextMeshProUGUI>();
        ApplyTemplateTextStyle(labelText);
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.fontSize = templateMenuLabel != null ? templateMenuLabel.fontSize * 1.22f : 44f;
        labelText.enableWordWrapping = false;
        labelText.overflowMode = TextOverflowModes.Overflow;
        labelText.color = Color.white;
        labelText.raycastTarget = false;
    }

    private Slider CreateSettingsSlider(Transform parent, float value, int settingIndex)
    {
        GameObject sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(EventTrigger));
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.offsetMin = new Vector2(348f, -10f);
        sliderRect.offsetMax = new Vector2(-150f, 10f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = Mathf.Clamp01(value);
        ConfigureSettingsSliderTrigger(sliderObject.GetComponent<EventTrigger>(), settingIndex);

        GameObject trackObject = CreateSliderImage("Track", sliderObject.transform, new Color(0.62f, 0.95f, 1f, 0.28f), true);
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0.5f);
        trackRect.anchorMax = new Vector2(1f, 0.5f);
        trackRect.offsetMin = Vector2.zero;
        trackRect.offsetMax = Vector2.zero;
        trackRect.sizeDelta = new Vector2(0f, 5f);

        GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
        fillAreaObject.transform.SetParent(sliderObject.transform, false);

        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject fillObject = CreateSliderImage("Fill", fillAreaObject.transform, stageButtonAccentColor, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(1f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.sizeDelta = new Vector2(0f, 6f);

        GameObject handleAreaObject = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleAreaObject.transform.SetParent(sliderObject.transform, false);

        RectTransform handleAreaRect = handleAreaObject.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = Vector2.zero;
        handleAreaRect.offsetMax = Vector2.zero;

        GameObject handleObject = CreateSliderImage("Handle", handleAreaObject.transform, new Color(0.78f, 0.95f, 1f, 1f), true);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(22f, 28f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleObject.GetComponent<Image>();
        slider.onValueChanged.AddListener(newValue => ApplySettingValue(settingIndex, newValue));

        CreateSettingsValueText(parent, settingIndex, value);
        return slider;
    }

    private void ConfigureSettingsSliderTrigger(EventTrigger trigger, int settingIndex)
    {
        if (trigger == null)
            return;

        EventTrigger.Entry pointerDown = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerDown
        };
        pointerDown.callback.AddListener(_ => SelectSettingRow(settingIndex));
        trigger.triggers.Add(pointerDown);
    }

    private GameObject CreateSliderImage(string objectName, Transform parent, Color color, bool raycastTarget)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;

        return imageObject;
    }

    private void CreateSettingsValueText(Transform parent, int settingIndex, float value)
    {
        GameObject valueObject = new GameObject("Value", typeof(RectTransform));
        valueObject.transform.SetParent(parent, false);

        RectTransform rect = valueObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-56f, 0f);
        rect.sizeDelta = new Vector2(76f, 0f);

        TextMeshProUGUI valueText = valueObject.AddComponent<TextMeshProUGUI>();
        ApplyTemplateTextStyle(valueText);
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.fontSize = templateMenuLabel != null ? templateMenuLabel.fontSize * 0.82f : 30f;
        valueText.enableWordWrapping = false;
        valueText.overflowMode = TextOverflowModes.Overflow;
        valueText.color = stageButtonAccentColor;
        valueText.raycastTarget = false;

        settingsValueTexts[settingIndex] = valueText;
        UpdateSettingsValueText(settingIndex, value);
    }

    private Button CreateSettingsCloseButton(Transform parent)
    {
        Button closeButton = CreateStageSelectButton(parent, "SettingsCloseButton", settingsCloseLabel, settingsCloseButtonSize);
        TMP_Text labelText = closeButton.GetComponentInChildren<TMP_Text>(true);

        if (labelText != null)
            labelText.fontSize = templateMenuLabel != null ? templateMenuLabel.fontSize * 1.2f : 42f;

        closeButton.onClick.AddListener(() => CloseSettings(true));
        return closeButton;
    }

    private void CreateSettingsPanelFrame(Transform parent)
    {
        float width = settingsPanelSize.x;
        float height = settingsPanelSize.y;
        float line = Mathf.Max(1f, stageButtonFrameThickness);
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        float inset = 34f;
        float cornerLength = 130f;
        float verticalLength = 92f;

        CreateStageFramePiece(parent, "DialogTopLine", new Vector2(0f, halfHeight - inset), new Vector2(width - 210f, line), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogBottomLine", new Vector2(0f, -halfHeight + inset), new Vector2(width - 210f, line), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogTopLeftH", new Vector2(-halfWidth + inset + cornerLength * 0.5f, halfHeight - inset), new Vector2(cornerLength, line * 1.5f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogTopLeftV", new Vector2(-halfWidth + inset, halfHeight - inset - verticalLength * 0.5f), new Vector2(line * 1.5f, verticalLength), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogTopRightH", new Vector2(halfWidth - inset - cornerLength * 0.5f, halfHeight - inset), new Vector2(cornerLength, line * 1.5f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogTopRightV", new Vector2(halfWidth - inset, halfHeight - inset - verticalLength * 0.5f), new Vector2(line * 1.5f, verticalLength), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogBottomLeftH", new Vector2(-halfWidth + inset + cornerLength * 0.5f, -halfHeight + inset), new Vector2(cornerLength, line * 1.5f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogBottomLeftV", new Vector2(-halfWidth + inset, -halfHeight + inset + verticalLength * 0.5f), new Vector2(line * 1.5f, verticalLength), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogBottomRightH", new Vector2(halfWidth - inset - cornerLength * 0.5f, -halfHeight + inset), new Vector2(cornerLength, line * 1.5f), stageButtonFrameColor, 0f);
        CreateStageFramePiece(parent, "DialogBottomRightV", new Vector2(halfWidth - inset, -halfHeight + inset + verticalLength * 0.5f), new Vector2(line * 1.5f, verticalLength), stageButtonFrameColor, 0f);

        CreateStageHatchGroup(parent, "DialogTopHatch", new Vector2(-halfWidth + 190f, halfHeight - inset + 2f), -20f);
        CreateStageHatchGroup(parent, "DialogBottomHatch", new Vector2(halfWidth - 250f, -halfHeight + inset + 2f), -20f);
    }

    private void SelectSettingRow(int settingIndex)
    {
        if (settingsButtons == null || settingIndex < 0 || settingIndex >= settingsButtons.Length)
            return;

        SelectButton(settingIndex);
    }

    private void ApplySettingValue(int settingIndex, float value)
    {
        value = Mathf.Clamp01(value);

        if (settingIndex == SettingsBrightnessIndex)
            GameSettings.SetBrightness(value);
        else if (settingIndex == SettingsMusicIndex)
            GameSettings.SetMusicVolume(value);
        else if (settingIndex == SettingsSoundIndex)
            GameSettings.SetSoundVolume(value);

        UpdateSettingsValueText(settingIndex, value);
    }

    private void UpdateSettingsValueText(int settingIndex, float value)
    {
        if (settingsValueTexts == null ||
            settingIndex < 0 ||
            settingIndex >= settingsValueTexts.Length ||
            settingsValueTexts[settingIndex] == null)
        {
            return;
        }

        settingsValueTexts[settingIndex].text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }

    private void ShowNextStagePage()
    {
        int pageCount = GetStagePageCount();

        if (pageCount <= 1)
            return;

        PlayMenuSelectSound();
        currentStagePage = currentStagePage < pageCount - 1 ? currentStagePage + 1 : 0;
        RefreshStageSelectPage();
        SetCurrentMenuButtons(stageSelectButtons, 0);
    }

    private void SetMainMenuButtonsActive(bool active)
    {
        if (mainMenuButtons == null)
            return;

        for (int i = 0; i < mainMenuButtons.Length; i++)
        {
            if (mainMenuButtons[i] != null)
                mainMenuButtons[i].gameObject.SetActive(active);
        }
    }

    private void ShowMainMenu(bool playSound)
    {
        if (playSound)
            PlayMenuSelectSound();

        if (stageSelectPanel != null)
            stageSelectPanel.SetActive(false);

        if (exitConfirmPanel != null)
            exitConfirmPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        SetMenuPanelImageVisible(true);
        SetTitleTextVisible(true);
        SetMainMenuButtonsActive(true);
        SetCurrentMenuButtons(mainMenuButtons, 0);
    }

    private void OpenStageSelect()
    {
        CreateStageSelectPanel();
        currentStagePage = 0;
        RefreshStageSelectPage();

        if (exitConfirmPanel != null)
            exitConfirmPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        SetMenuPanelImageVisible(false);
        SetTitleTextVisible(false);
        SetMainMenuButtonsActive(false);

        if (stageSelectPanel != null)
            stageSelectPanel.SetActive(true);

        SetCurrentMenuButtons(stageSelectButtons, 0);
    }

    private void BackToMainMenu()
    {
        ShowMainMenu(true);
    }

    private void OpenExitConfirm()
    {
        PlayMenuSelectSound();
        CreateExitConfirmPanel();

        if (stageSelectPanel != null)
            stageSelectPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        SetMenuPanelImageVisible(false);
        SetTitleTextVisible(true);
        SetMainMenuButtonsActive(false);

        if (exitConfirmPanel != null)
            exitConfirmPanel.SetActive(true);

        SetCurrentMenuButtons(exitConfirmButtons, 0);
    }

    private void CloseExitConfirm(bool playSound)
    {
        if (playSound)
            PlayMenuSelectSound();

        if (exitConfirmPanel != null)
            exitConfirmPanel.SetActive(false);

        SetMenuPanelImageVisible(true);
        SetTitleTextVisible(true);
        SetMainMenuButtonsActive(true);
        SetCurrentMenuButtons(mainMenuButtons, GetDefaultMainMenuReturnIndex());
    }

    private void OpenSettingsPanel()
    {
        PlayMenuSelectSound();
        CreateSettingsPanel();
        RefreshSettingsValues();

        if (stageSelectPanel != null)
            stageSelectPanel.SetActive(false);

        if (exitConfirmPanel != null)
            exitConfirmPanel.SetActive(false);

        SetMenuPanelImageVisible(false);
        SetTitleTextVisible(false);
        SetMainMenuButtonsActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        SetCurrentMenuButtons(settingsButtons, 0);
    }

    private void CloseSettings(bool playSound)
    {
        if (playSound)
            PlayMenuSelectSound();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        SetMenuPanelImageVisible(true);
        SetTitleTextVisible(true);
        SetMainMenuButtonsActive(true);
        SetCurrentMenuButtons(mainMenuButtons, GetDefaultSettingsReturnIndex());
    }

    private void RefreshSettingsValues()
    {
        GameSettings.Load();

        SetSettingsSliderValue(SettingsBrightnessIndex, GameSettings.Brightness);
        SetSettingsSliderValue(SettingsMusicIndex, GameSettings.MusicVolume);
        SetSettingsSliderValue(SettingsSoundIndex, GameSettings.SoundVolume);
    }

    private void SetSettingsSliderValue(int settingIndex, float value)
    {
        value = Mathf.Clamp01(value);

        if (settingsSliders != null &&
            settingIndex >= 0 &&
            settingIndex < settingsSliders.Length &&
            settingsSliders[settingIndex] != null)
        {
            settingsSliders[settingIndex].SetValueWithoutNotify(value);
        }

        UpdateSettingsValueText(settingIndex, value);
    }

    private int GetDefaultMainMenuReturnIndex()
    {
        return mainMenuButtons != null && mainMenuButtons.Length > 0
            ? mainMenuButtons.Length - 1
            : 0;
    }

    private int GetDefaultSettingsReturnIndex()
    {
        if (mainMenuButtons == null || mainMenuButtons.Length == 0)
            return 0;

        for (int i = 0; i < mainMenuButtons.Length; i++)
        {
            if (mainMenuButtons[i] != null &&
                (mainMenuButtons[i].name == "SettingButton" || mainMenuButtons[i].name == "SettingsButton"))
            {
                return i;
            }
        }

        return Mathf.Min(1, mainMenuButtons.Length - 1);
    }

    private void SelectStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= GetStageCount())
        {
            return;
        }

        PlayMenuSelectSound();

        string sceneName = GetStageSceneName(stageIndex);

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.Log($"Stage {stageIndex + 1} button pressed.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"Stage {stageIndex + 1} scene '{sceneName}' is not available in Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void SetMenuPanelImageVisible(bool visible)
    {
        CacheMenuPanelImage();

        if (menuPanelImage != null)
            menuPanelImage.enabled = visible;
    }

    private void SetTitleTextVisible(bool visible)
    {
        CacheTitleText();

        if (titleText != null)
            titleText.gameObject.SetActive(visible);
    }

    public void PlayGame()
    {
        PlayMenuSelectSound();
        OpenStageSelect();
    }

    public void OpenSettings()
    {
        OpenSettingsPanel();
    }

    public void ExitGame()
    {
        OpenExitConfirm();
    }

    private void ConfirmExit()
    {
        PlayMenuSelectSound();
        Debug.Log("게임 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
