using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class StageUIController : MonoBehaviour
{
    private static readonly HashSet<KeyCode> gameplayInputBlockedUntilRelease = new HashSet<KeyCode>();

    [Header("Intro UI")]
    public GameObject stageIntroPanel;
    public CanvasGroup stageIntroGroup;
    public TMP_Text stageIntroText;
    public TMP_Text stageNameText;

    [Header("Intro Ammo Preview")]
    public AmmoPopupUI introAmmoSource;

    [Header("Controls Hint")]
    public Sprite controlsHintBackgroundSprite;
    public bool showControlsHintAfterIntro = true;
    public string controlsHintSceneName = "Level1";
    public string controlsHintShootingSceneName = "Level3";
    public string controlsHintSecondShootingSceneName = "Level4";
    public Vector2 controlsHintSize = new Vector2(520f, 230f);
    public Vector2 controlsHintShootingSize = new Vector2(620f, 340f);
    public Vector2 controlsHintPosition = new Vector2(-70f, -82f);
    public KeyCode infoPopupToggleKey = KeyCode.E;
    public string infoPopupToggleHintText = "E키로 설명창 넣고 빼기";
    public float infoPopupSlideSpeed = 8f;

    [Header("Stage 3 Info Popups")]
    public bool showStage3InfoPopups = true;
    public string stage3InfoPopupSceneName = "Level3";
    public Sprite stageInfoPopupBackgroundSprite;
    public Sprite stageInfoIronBoxSprite;
    public Sprite stageInfoGoldBoxSprite;
    public Sprite stageInfoGoldBulletSprite;
    public Sprite stageInfoPurpleBulletSprite;
    public Vector2 stageInfoPopupSize = new Vector2(400f, 220f);
    public Vector2 stageInfoBoxPopupPosition = new Vector2(250f, 610f);
    public Vector2 stageInfoBulletPopupPosition = new Vector2(250f, 380f);

    [Header("Stage 5 Info Popup")]
    public bool showStage5InfoPopup = true;
    public string stage5InfoPopupSceneName = "Level5";
    public string stage5InfoPopupSecondSceneName = "Level5 1";
    public Sprite stageInfoRemoteBulletSprite;
    public Vector2 stage5InfoPopupPosition = new Vector2(250f, 380f);
    public Vector2 stage5InfoBulletIconSize = new Vector2(118f, 44f);

    [Header("Intro Fade Settings")]
    public float fadeInTime = 0.35f;
    public float holdTime = 1.5f;
    public float fadeOutTime = 0.35f;

    [Header("Clear UI")]
    public GameObject stageClearPanel;

    [Header("Clear UI Buttons")]
    public Button retryButton;
    public Button nextButton;

    [Header("Clear UI Keyboard Control")]
    public bool enableStageClearKeyboardInput = true;
    public int defaultSelectedClearButtonIndex = 1; // 0 = Retry, 1 = Next
    public KeyCode stageClearSubmitKey = KeyCode.Z;

    [Header("Clear Button Hover Visual")]
    public bool useManualHoverVisual = true;
    public Color hoveredButtonColor = new Color(0.1f, 0.85f, 1f, 1f);
    public float hoveredButtonScale = 1.08f;

    [Header("Clear UI SFX")]
    public AudioClip clearButtonSwapSound;
    public AudioClip clearButtonSelectSound;
    public AudioSource clearButtonAudioSource;
    public float clearButtonSwapSoundVolume = 1f;
    public float clearButtonSelectSoundVolume = 1f;

    [Header("Pause Menu")]
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode pauseSubmitKey = KeyCode.Z;
    public string pauseTitle = "PAUSE";
    public string[] pauseButtonLabels = { "RESUME", "RETRY", "SETTINGS", "MAIN MENU" };
    public Color pauseButtonBackgroundColor = new Color(0f, 0.12f, 0.14f, 0.18f);
    public Color pauseButtonFrameColor = new Color(0.78f, 0.95f, 1f, 0.95f);
    public Color pauseButtonAccentColor = new Color(0.08f, 0.9f, 1f, 0.95f);
    public Color pauseButtonHoverColor = new Color(0.04f, 0.65f, 0.9f, 0.32f);
    public Color pauseButtonPressedColor = new Color(0.08f, 0.9f, 1f, 0.42f);
    public float pauseButtonFrameThickness = 3f;
    public float pauseTitleFontScale = 1.35f;
    public float pauseButtonFontScale = 1.15f;
    public Color pauseSelectedTextColor = new Color(0.1f, 0.85f, 1f, 1f);
    public float pauseSelectedTextScale = 1.15f;
    public Vector2 pausePanelPosition = new Vector2(0f, -20f);
    public Vector2 pauseButtonSize = new Vector2(680f, 124f);
    public float pauseButtonSpacing = 36f;
    public string mainMenuSceneName = "MainMenu";

    [Header("Pause Settings")]
    public string pauseSettingsTitle = "SETTINGS";
    public string pauseBrightnessLabel = "BRIGHTNESS";
    public string pauseMusicVolumeLabel = "MUSIC";
    public string pauseSoundVolumeLabel = "SFX";
    public string pauseSettingsCloseLabel = "CLOSE";
    public Vector2 pauseSettingsPanelSize = new Vector2(760f, 640f);
    public Vector2 pauseSettingsPanelPosition = new Vector2(0f, -25f);
    public Vector2 pauseSettingsRowSize = new Vector2(680f, 82f);
    public Vector2 pauseSettingsCloseButtonSize = new Vector2(280f, 68f);
    public float pauseSettingsRowSpacing = 22f;
    public float pauseSettingsKeyboardStep = 0.05f;

    [Header("Game Over UI")]
    public bool enableGameOverUI = true;
    public string gameOverTitleText = "Game Over!";
    public string gameOverUndoButtonText = "Undo";
    public string gameOverRetryButtonText = "Retry";
    public KeyCode gameOverUndoKey = KeyCode.Q;
    public int defaultSelectedGameOverButtonIndex = 0; // 0 = Undo, 1 = Retry

    [Header("Gameplay Block")]
    public MonoBehaviour playerController;
    [Range(0f, 1f)]
    public float introGameplayBlockRatio = 0.5f;

    private Coroutine introRoutine;
    private Coroutine introGameplayBlockRoutine;
    private string pendingNextSceneName = "";
    private RectTransform stageIntroRect;
    private RectTransform stageIntroAmmoListRoot;
    private Vector2 originalStageIntroSize;
    private bool hasOriginalStageIntroSize = false;
    private GameObject controlsHintPanel;
    private RectTransform controlsHintRect;
    private CanvasGroup controlsHintGroup;
    private Coroutine controlsHintRoutine;
    private GameObject stage3BoxInfoPopup;
    private GameObject stage3BulletInfoPopup;
    private GameObject stage5BulletInfoPopup;
    private Coroutine stage3InfoPopupRoutine;
    private bool areInfoPopupsVisible = true;

    private Button[] clearButtons;
    private int selectedClearButtonIndex = -1;
    private Button currentHoveredClearButton;

    private GameObject pauseMenuPanel;
    private Button[] pauseButtons;
    private int selectedPauseButtonIndex = -1;
    private Button currentHoveredPauseButton;
    private GameObject pauseSettingsPanel;
    private Button[] pauseSettingsButtons;
    private Slider[] pauseSettingsSliders;
    private TMP_Text[] pauseSettingsValueTexts;
    private int selectedPauseSettingsIndex = -1;
    private Button currentHoveredPauseSettingsButton;
    private TMP_Text pauseTemplateText;
    private GameState stateBeforePause = GameState.Playing;
    private float timeScaleBeforePause = 1f;
    private bool isPausedByMenu = false;
    private bool isPauseSettingsOpen = false;
    private bool isPauseSceneActionRunning = false;
    private Coroutine resumePauseRoutine;

    private TMP_Text stageClearTitleText;
    private TMP_Text retryButtonText;
    private TMP_Text nextButtonText;
    private string originalStageClearTitleText;
    private string originalRetryButtonText;
    private string originalNextButtonText;
    private bool hasOriginalStagePanelText = false;
    private bool isGameOverPanelOpen = false;
    private bool isGameOverSceneActionRunning = false;
    private Coroutine gameOverUndoRoutine;

    private readonly Dictionary<Button, Vector3> originalButtonScales = new Dictionary<Button, Vector3>();
    private readonly Dictionary<Button, Color> originalButtonColors = new Dictionary<Button, Color>();

    private const int PauseSettingsBrightnessIndex = 0;
    private const int PauseSettingsMusicIndex = 1;
    private const int PauseSettingsSoundIndex = 2;
    private const int PauseSettingsCloseIndex = 3;
    private const float MainMenuTemplateFontSize = 35f;
    private const float StageIntroAmmoPanelExtraHeight = 145f;
    private const float StageIntroAmmoListY = -130f;
    private const float StageIntroAmmoSlotSpacing = 48f;
    private const float StageIntroAmmoSlotWidth = 245f;
    private const float StageIntroAmmoSlotHeight = 122f;
    private const float StageIntroAmmoIconWidth = 136f;
    private const float StageIntroAmmoIconHeight = 86f;
    private const float StageIntroAmmoCountWidth = 104f;
    private const float StageIntroAmmoCountFontSize = 64f;
    private const float ControlsHintKeySize = 34f;
    private const float ControlsHintRowWidth = 410f;
    private const float ControlsHintLabelStartX = 180f;
    private static readonly FontStyles MainMenuFontStyle = FontStyles.Bold | FontStyles.Italic;
    private static readonly FontStyles PauseMenuFontStyle = FontStyles.Italic;

    private struct Stage3InfoRowData
    {
        public readonly Sprite icon;
        public readonly string description;
        public readonly Vector2 iconSize;

        public Stage3InfoRowData(Sprite icon, string description, Vector2 iconSize)
        {
            this.icon = icon;
            this.description = description;
            this.iconSize = iconSize;
        }
    }

    private void Awake()
    {
        Time.timeScale = 1f;

        if (stageClearPanel != null)
            stageClearPanel.SetActive(false);

        if (stageIntroPanel != null)
            stageIntroPanel.SetActive(false);

        CacheStageIntroElements();
        CacheClearButtons();
        CacheStagePanelTextElements();
        CachePauseTemplateText();
        CreateControlsHintPanel();
        CreatePauseMenuPanel();
        CreatePauseSettingsPanel();
    }

    private void Start()
    {
        ShowStageIntro();
    }

    private void Update()
    {
        ReleaseGameplayInputBlocks();

        if (HandlePauseToggleInput())
            return;

        if (isPausedByMenu)
        {
            if (isPauseSettingsOpen)
                HandlePauseSettingsKeyboardInput();
            else
                HandlePauseMenuKeyboardInput();

            return;
        }

        SyncGameOverPanelState();

        if (isGameOverPanelOpen)
        {
            HandleGameOverKeyboardInput();
            return;
        }

        if (HandleInfoPopupToggleInput())
            return;

        HandleStageClearKeyboardInput();
    }

    private void OnDisable()
    {
        if (resumePauseRoutine != null)
        {
            StopCoroutine(resumePauseRoutine);
            resumePauseRoutine = null;
        }

        if (gameOverUndoRoutine != null)
        {
            StopCoroutine(gameOverUndoRoutine);
            gameOverUndoRoutine = null;
        }

        if (controlsHintRoutine != null)
        {
            StopCoroutine(controlsHintRoutine);
            controlsHintRoutine = null;
        }

        if (stage3InfoPopupRoutine != null)
        {
            StopCoroutine(stage3InfoPopupRoutine);
            stage3InfoPopupRoutine = null;
        }

        if (isPausedByMenu)
            Time.timeScale = 1f;

        isPauseSettingsOpen = false;
        ClearCurrentClearButtonHover();
        ClearCurrentPauseButtonHover();
        ClearCurrentPauseSettingsButtonHover();
        RestoreStageClearPanelText();
        RestoreAllButtonVisuals();

        if (pauseSettingsPanel != null)
            pauseSettingsPanel.SetActive(false);

        if (controlsHintPanel != null)
            controlsHintPanel.SetActive(false);

        if (stage3BoxInfoPopup != null)
            stage3BoxInfoPopup.SetActive(false);

        if (stage3BulletInfoPopup != null)
            stage3BulletInfoPopup.SetActive(false);

        if (stage5BulletInfoPopup != null)
            stage5BulletInfoPopup.SetActive(false);
    }

    public void ShowStageIntro()
    {
        if (stageIntroPanel == null)
            return;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.StageIntro);

        CacheStageIntroElements();

        if (introRoutine != null)
            StopCoroutine(introRoutine);

        if (introGameplayBlockRoutine != null)
        {
            StopCoroutine(introGameplayBlockRoutine);
            introGameplayBlockRoutine = null;
        }

        HideControlsHintImmediate();
        HideStage3InfoPopupsImmediate();
        HideStage5InfoPopupImmediate();
        introRoutine = StartCoroutine(StageIntroFadeRoutine());
    }

    private IEnumerator StageIntroFadeRoutine()
    {
        RefreshStageIntroAmmoPreview();
        stageIntroPanel.SetActive(true);
        StartIntroGameplayBlock();

        if (stageIntroGroup != null)
        {
            stageIntroGroup.interactable = false;
            stageIntroGroup.blocksRaycasts = false;
            stageIntroGroup.alpha = 0f;

            yield return FadeCanvasGroup(stageIntroGroup, 0f, 1f, fadeInTime);
            yield return new WaitForSecondsRealtime(holdTime);
            yield return FadeCanvasGroup(stageIntroGroup, 1f, 0f, fadeOutTime);
        }
        else
        {
            yield return new WaitForSecondsRealtime(holdTime);
        }

        stageIntroPanel.SetActive(false);

        if (introGameplayBlockRoutine != null)
        {
            StopCoroutine(introGameplayBlockRoutine);
            EndIntroGameplayBlock();
        }

        ShowControlsHintAfterIntro();
        ShowStage3InfoPopupsAfterIntro();
        ShowStage5InfoPopupAfterIntro();
        introRoutine = null;
    }

    private void StartIntroGameplayBlock()
    {
        SetGameplayBlocked(true);

        float blockDuration = GetIntroDuration() * Mathf.Clamp01(introGameplayBlockRatio);
        introGameplayBlockRoutine = StartCoroutine(IntroGameplayBlockRoutine(blockDuration));
    }

    private IEnumerator IntroGameplayBlockRoutine(float blockDuration)
    {
        if (blockDuration > 0f)
            yield return new WaitForSecondsRealtime(blockDuration);

        EndIntroGameplayBlock();
    }

    private void EndIntroGameplayBlock()
    {
        introGameplayBlockRoutine = null;
        SetGameplayBlocked(false);

        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentState == GameState.StageIntro)
        {
            GameStateManager.Instance.SetState(GameState.Playing);
        }
    }

    private float GetIntroDuration()
    {
        if (stageIntroGroup == null)
            return Mathf.Max(0f, holdTime);

        return Mathf.Max(0f, fadeInTime) +
               Mathf.Max(0f, holdTime) +
               Mathf.Max(0f, fadeOutTime);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null)
            yield break;

        if (duration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        group.alpha = to;
    }

    private void CacheStageIntroElements()
    {
        if (stageIntroPanel == null)
            return;

        if (stageIntroGroup == null)
            stageIntroGroup = stageIntroPanel.GetComponent<CanvasGroup>();

        if (stageIntroRect == null)
            stageIntroRect = stageIntroPanel.GetComponent<RectTransform>();

        if (!hasOriginalStageIntroSize && stageIntroRect != null)
        {
            originalStageIntroSize = stageIntroRect.sizeDelta;
            hasOriginalStageIntroSize = true;
        }

        if (stageNameText == null)
            stageNameText = FindTMPTextByName(stageIntroPanel.transform, "StageNameText");

        if (stageIntroText == null)
            stageIntroText = FindTMPTextByName(stageIntroPanel.transform, "StageFloorText");
    }

    private void RefreshStageIntroAmmoPreview()
    {
        CacheStageIntroElements();

        AmmoPopupUI ammoSource = ResolveIntroAmmoSource();
        bool hasAmmo = ammoSource != null && ammoSource.HasAmmoEntries();

        ApplyStageIntroAmmoPanelSize(hasAmmo);

        if (!hasAmmo)
        {
            if (stageIntroAmmoListRoot != null)
                stageIntroAmmoListRoot.gameObject.SetActive(false);

            return;
        }

        EnsureStageIntroAmmoListRoot();

        if (stageIntroAmmoListRoot == null)
            return;

        stageIntroAmmoListRoot.gameObject.SetActive(true);
        ClearStageIntroAmmoSlots();

        for (int i = 0; i < ammoSource.ammoList.Length; i++)
        {
            AmmoPopupUI.AmmoData ammo = ammoSource.ammoList[i];

            if (ammo == null)
                continue;

            CreateStageIntroAmmoSlot(stageIntroAmmoListRoot, ammo, ammoSource);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(stageIntroAmmoListRoot);
    }

    private AmmoPopupUI ResolveIntroAmmoSource()
    {
        if (introAmmoSource != null)
            return introAmmoSource;

        introAmmoSource = FindFirstObjectByType<AmmoPopupUI>();
        return introAmmoSource;
    }

    private void ApplyStageIntroAmmoPanelSize(bool hasAmmo)
    {
        if (stageIntroRect == null || !hasOriginalStageIntroSize)
            return;

        Vector2 size = originalStageIntroSize;

        if (hasAmmo)
            size.y += StageIntroAmmoPanelExtraHeight;

        stageIntroRect.sizeDelta = size;
    }

    private void EnsureStageIntroAmmoListRoot()
    {
        if (stageIntroAmmoListRoot != null || stageIntroPanel == null)
            return;

        Transform existingRoot = stageIntroPanel.transform.Find("StageIntroAmmoList");

        if (existingRoot != null)
        {
            stageIntroAmmoListRoot = existingRoot as RectTransform;
            return;
        }

        GameObject listObject = new GameObject("StageIntroAmmoList", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        listObject.transform.SetParent(stageIntroPanel.transform, false);

        stageIntroAmmoListRoot = listObject.GetComponent<RectTransform>();
        stageIntroAmmoListRoot.anchorMin = new Vector2(0.5f, 0.5f);
        stageIntroAmmoListRoot.anchorMax = new Vector2(0.5f, 0.5f);
        stageIntroAmmoListRoot.pivot = new Vector2(0.5f, 0.5f);
        stageIntroAmmoListRoot.anchoredPosition = new Vector2(0f, StageIntroAmmoListY);
        stageIntroAmmoListRoot.sizeDelta = new Vector2(960f, StageIntroAmmoSlotHeight);

        HorizontalLayoutGroup layout = listObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = StageIntroAmmoSlotSpacing;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private void ClearStageIntroAmmoSlots()
    {
        if (stageIntroAmmoListRoot == null)
            return;

        for (int i = stageIntroAmmoListRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = stageIntroAmmoListRoot.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private void CreateStageIntroAmmoSlot(RectTransform parent, AmmoPopupUI.AmmoData ammo, AmmoPopupUI ammoSource)
    {
        GameObject slotObject = new GameObject(
            string.IsNullOrEmpty(ammo.ammoName) ? "AmmoSlot" : ammo.ammoName + "Slot",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup),
            typeof(LayoutElement)
        );
        slotObject.transform.SetParent(parent, false);

        RectTransform slotRect = slotObject.GetComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(StageIntroAmmoSlotWidth, StageIntroAmmoSlotHeight);

        LayoutElement slotLayout = slotObject.GetComponent<LayoutElement>();
        slotLayout.preferredWidth = StageIntroAmmoSlotWidth;
        slotLayout.preferredHeight = StageIntroAmmoSlotHeight;

        HorizontalLayoutGroup slotGroup = slotObject.GetComponent<HorizontalLayoutGroup>();
        slotGroup.childAlignment = TextAnchor.MiddleCenter;
        slotGroup.spacing = 10f;
        slotGroup.childControlWidth = false;
        slotGroup.childControlHeight = false;
        slotGroup.childForceExpandWidth = false;
        slotGroup.childForceExpandHeight = false;

        CreateStageIntroAmmoIcon(slotObject.transform, ammo, ammoSource);
        CreateStageIntroAmmoCountText(slotObject.transform, ammo, ammoSource);
    }

    private void CreateStageIntroAmmoIcon(Transform parent, AmmoPopupUI.AmmoData ammo, AmmoPopupUI ammoSource)
    {
        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        iconObject.transform.SetParent(parent, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(StageIntroAmmoIconWidth, StageIntroAmmoIconHeight);

        LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
        iconLayout.preferredWidth = StageIntroAmmoIconWidth;
        iconLayout.preferredHeight = StageIntroAmmoIconHeight;

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = ammo.icon;
        iconImage.enabled = ammo.icon != null;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        if (ammoSource != null && ammoSource.currentAmmoIcon != null)
            iconImage.color = ammoSource.currentAmmoIcon.color;
    }

    private void CreateStageIntroAmmoCountText(Transform parent, AmmoPopupUI.AmmoData ammo, AmmoPopupUI ammoSource)
    {
        GameObject countObject = new GameObject("Count", typeof(RectTransform), typeof(LayoutElement));
        countObject.transform.SetParent(parent, false);

        RectTransform countRect = countObject.GetComponent<RectTransform>();
        countRect.sizeDelta = new Vector2(StageIntroAmmoCountWidth, StageIntroAmmoSlotHeight);

        LayoutElement countLayout = countObject.GetComponent<LayoutElement>();
        countLayout.preferredWidth = StageIntroAmmoCountWidth;
        countLayout.preferredHeight = StageIntroAmmoSlotHeight;

        TextMeshProUGUI countText = countObject.AddComponent<TextMeshProUGUI>();
        ApplyStageIntroAmmoCountTextStyle(countText, ammoSource != null ? ammoSource.ammoCountText : null);
        countText.text = "x" + ammo.count.ToString();
    }

    private void ApplyStageIntroAmmoCountTextStyle(TextMeshProUGUI target, TMP_Text template)
    {
        if (target == null)
            return;

        TMP_Text fallbackTemplate = stageNameText != null ? stageNameText : stageIntroText;
        TMP_Text sourceTemplate = template != null ? template : fallbackTemplate;

        if (sourceTemplate != null)
        {
            target.font = sourceTemplate.font;
            target.fontSharedMaterial = sourceTemplate.fontSharedMaterial;
            target.fontStyle = sourceTemplate.fontStyle;
            target.color = sourceTemplate.color;
        }
        else
        {
            target.color = Color.white;
        }

        target.alignment = TextAlignmentOptions.MidlineLeft;
        target.fontSize = StageIntroAmmoCountFontSize;
        target.textWrappingMode = TextWrappingModes.NoWrap;
        target.overflowMode = TextOverflowModes.Overflow;
        target.raycastTarget = false;
    }

    private void CreateControlsHintPanel()
    {
        if (controlsHintPanel != null)
            return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Transform parent = parentCanvas != null ? parentCanvas.transform : transform;

        controlsHintPanel = new GameObject(
            "ControlsHintPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)
        );
        controlsHintPanel.transform.SetParent(parent, false);

        RectTransform panelRect = controlsHintPanel.GetComponent<RectTransform>();
        controlsHintRect = panelRect;
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        Vector2 panelSize = GetControlsHintPanelSize();
        panelRect.anchoredPosition = controlsHintPosition;
        panelRect.sizeDelta = panelSize;

        Image panelImage = controlsHintPanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0.08f, 0.09f, 0.68f);
        panelImage.raycastTarget = false;

        controlsHintGroup = controlsHintPanel.GetComponent<CanvasGroup>();
        controlsHintGroup.alpha = 0f;
        controlsHintGroup.interactable = false;
        controlsHintGroup.blocksRaycasts = false;

        Sprite backgroundSprite = ResolveControlsHintBackgroundSprite();

        if (backgroundSprite != null)
            CreateControlsHintBackgroundFrame(controlsHintPanel.transform, backgroundSprite);
        else
            CreatePauseButtonFrame(controlsHintPanel.transform, panelSize);

        CreateControlsHintContent(controlsHintPanel.transform);

        if (!ShouldShowStage3InfoPopups())
            CreateInfoPopupToggleHint(controlsHintPanel.transform, panelSize, false);

        controlsHintPanel.SetActive(false);
    }

    private Sprite ResolveControlsHintBackgroundSprite()
    {
        if (controlsHintBackgroundSprite != null)
            return controlsHintBackgroundSprite;

        AmmoPopupUI ammoSource = ResolveIntroAmmoSource();

        if (ammoSource == null)
            return null;

        Image[] images = ammoSource.GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image == null || image.sprite == null)
                continue;

            if (image.sprite.name.StartsWith("ammo_popup_bg"))
            {
                controlsHintBackgroundSprite = image.sprite;
                return controlsHintBackgroundSprite;
            }
        }

        return null;
    }

    private void CreateControlsHintBackgroundFrame(Transform parent, Sprite backgroundSprite)
    {
        GameObject frameObject = new GameObject(
            "AmmoPopupStyleFrame",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        frameObject.transform.SetParent(parent, false);

        RectTransform rect = frameObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = frameObject.GetComponent<Image>();
        image.sprite = backgroundSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private void ShowStage3InfoPopupsAfterIntro()
    {
        if (!ShouldShowStage3InfoPopups())
            return;

        Transform parent = ResolveStage3InfoPopupParent();

        if (stage3BoxInfoPopup == null)
        {
            stage3BoxInfoPopup = CreateStage3InfoPopup(parent, "Stage3BoxInfoPopup", stageInfoBoxPopupPosition);
            CreateInfoPopupToggleHint(stage3BoxInfoPopup.transform, ResolveStage3InfoPopupSize(), true);
            CreateStage3InfoContent(
                stage3BoxInfoPopup.transform,
                new Stage3InfoRowData(stageInfoGoldBoxSprite, "총알로 격파 가능,\n플레이어가 밀 수 있음", new Vector2(58f, 78f)),
                new Stage3InfoRowData(stageInfoIronBoxSprite, "총알로 격파 불가능,\n플레이어가 밀 수 있음", new Vector2(58f, 78f))
            );
        }
        else
        {
            stage3BoxInfoPopup.SetActive(true);
        }

        if (stage3BulletInfoPopup == null)
        {
            stage3BulletInfoPopup = CreateStage3InfoPopup(parent, "Stage3BulletInfoPopup", stageInfoBulletPopupPosition);
            CreateStage3InfoContent(
                stage3BulletInfoPopup.transform,
                new Stage3InfoRowData(stageInfoGoldBulletSprite, "상자 격파", new Vector2(118f, 44f)),
                new Stage3InfoRowData(stageInfoPurpleBulletSprite, "플레이어와 물체의 위치 교환", new Vector2(118f, 44f))
            );
        }
        else
        {
            stage3BulletInfoPopup.SetActive(true);
        }

        areInfoPopupsVisible = true;
        SetStage3InfoPopupsVisible(true, true);
    }

    private void HideStage3InfoPopupsImmediate()
    {
        if (stage3InfoPopupRoutine != null)
        {
            StopCoroutine(stage3InfoPopupRoutine);
            stage3InfoPopupRoutine = null;
        }

        if (stage3BoxInfoPopup != null)
            stage3BoxInfoPopup.SetActive(false);

        if (stage3BulletInfoPopup != null)
            stage3BulletInfoPopup.SetActive(false);

        areInfoPopupsVisible = false;
    }

    private void ShowStage5InfoPopupAfterIntro()
    {
        if (!ShouldShowStage5InfoPopup())
            return;

        Transform parent = ResolveStage3InfoPopupParent();

        if (stage5BulletInfoPopup == null)
        {
            stage5BulletInfoPopup = CreateStage3InfoPopup(parent, "Stage5BulletInfoPopup", stage5InfoPopupPosition);
            CreateInfoPopupToggleHint(stage5BulletInfoPopup.transform, ResolveStage3InfoPopupSize(), true);
            CreateStage3InfoContent(
                stage5BulletInfoPopup.transform,
                new Stage3InfoRowData(stageInfoRemoteBulletSprite, "물체 원격 이동", stage5InfoBulletIconSize)
            );
        }
        else
        {
            stage5BulletInfoPopup.SetActive(true);
        }

        areInfoPopupsVisible = true;
        SetStage5InfoPopupVisible(true, true);
    }

    private void HideStage5InfoPopupImmediate()
    {
        if (stage3InfoPopupRoutine != null)
        {
            StopCoroutine(stage3InfoPopupRoutine);
            stage3InfoPopupRoutine = null;
        }

        if (stage5BulletInfoPopup != null)
            stage5BulletInfoPopup.SetActive(false);

        areInfoPopupsVisible = false;
    }

    private bool ShouldShowStage5InfoPopup()
    {
        if (!showStage5InfoPopup)
            return false;

        string sceneName = SceneManager.GetActiveScene().name;

        return (!string.IsNullOrEmpty(stage5InfoPopupSceneName) && sceneName == stage5InfoPopupSceneName) ||
               (!string.IsNullOrEmpty(stage5InfoPopupSecondSceneName) && sceneName == stage5InfoPopupSecondSceneName);
    }

    private bool ShouldShowStage3InfoPopups()
    {
        return showStage3InfoPopups &&
               !string.IsNullOrEmpty(stage3InfoPopupSceneName) &&
               SceneManager.GetActiveScene().name == stage3InfoPopupSceneName;
    }

    private Transform ResolveStage3InfoPopupParent()
    {
        AmmoPopupUI ammoSource = ResolveIntroAmmoSource();
        Canvas ammoCanvas = ammoSource != null ? ammoSource.GetComponentInParent<Canvas>() : null;

        if (ammoCanvas != null)
            return ammoCanvas.transform;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        return parentCanvas != null ? parentCanvas.transform : transform;
    }

    private GameObject CreateStage3InfoPopup(Transform parent, string objectName, Vector2 anchoredPosition)
    {
        Vector2 popupSize = ResolveStage3InfoPopupSize();
        GameObject popupObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        popupObject.transform.SetParent(parent, false);

        RectTransform rect = popupObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = popupSize;

        Image image = popupObject.GetComponent<Image>();
        image.color = new Color(0f, 0.08f, 0.09f, 0.68f);
        image.raycastTarget = false;

        Sprite backgroundSprite = ResolveStageInfoPopupBackgroundSprite();

        if (backgroundSprite != null)
            CreateControlsHintBackgroundFrame(popupObject.transform, backgroundSprite);
        else
            CreatePauseButtonFrame(popupObject.transform, popupSize);

        return popupObject;
    }

    private Vector2 ResolveStage3InfoPopupSize()
    {
        AmmoPopupUI ammoSource = ResolveIntroAmmoSource();
        RectTransform ammoRect = ammoSource != null ? ammoSource.GetComponent<RectTransform>() : null;

        if (ammoRect != null && ammoRect.sizeDelta.x > 0f && ammoRect.sizeDelta.y > 0f)
            return ammoRect.sizeDelta;

        return stageInfoPopupSize;
    }

    private Sprite ResolveStageInfoPopupBackgroundSprite()
    {
        if (stageInfoPopupBackgroundSprite != null)
            return stageInfoPopupBackgroundSprite;

        if (controlsHintBackgroundSprite != null)
            return controlsHintBackgroundSprite;

        return ResolveControlsHintBackgroundSprite();
    }

    private void CreateStage3InfoContent(Transform parent, params Stage3InfoRowData[] rows)
    {
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentObject.transform.SetParent(parent, false);

        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(34f, 34f);
        contentRect.offsetMax = new Vector2(-16f, -34f);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (rows == null)
            return;

        for (int i = 0; i < rows.Length; i++)
        {
            string rowName = i == 0 ? "FirstRow" : i == 1 ? "SecondRow" : "Row" + (i + 1).ToString();
            CreateStage3InfoRow(contentObject.transform, rowName, rows[i]);
        }
    }

    private void CreateInfoPopupToggleHint(Transform parent, Vector2 popupSize, bool placeAbove)
    {
        GameObject hintObject = new GameObject("InfoPopupToggleHint", typeof(RectTransform));
        hintObject.transform.SetParent(parent, false);

        RectTransform rect = hintObject.GetComponent<RectTransform>();
        rect.anchorMin = placeAbove ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = placeAbove ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 1f);
        rect.anchoredPosition = placeAbove ? new Vector2(0f, 8f) : new Vector2(0f, -8f);
        rect.sizeDelta = new Vector2(Mathf.Max(260f, popupSize.x), 46f);

        TextMeshProUGUI text = hintObject.AddComponent<TextMeshProUGUI>();
        ApplyInfoPopupToggleHintStyle(text);
        text.text = FormatInfoPopupToggleHintText(infoPopupToggleHintText);
    }

    private string FormatInfoPopupToggleHintText(string hintText)
    {
        if (string.IsNullOrEmpty(hintText))
            return "";

        if (hintText[0] != 'e' && hintText[0] != 'E')
            return hintText;

        return "<size=38>" + hintText[0] + "</size>" + hintText.Substring(1);
    }

    private void ApplyInfoPopupToggleHintStyle(TMP_Text targetText)
    {
        if (targetText == null)
            return;

        TMP_Text sourceTemplate = stageIntroText != null ? stageIntroText : stageNameText;

        if (sourceTemplate == null)
            sourceTemplate = pauseTemplateText;

        if (sourceTemplate != null)
        {
            targetText.font = sourceTemplate.font;
            targetText.fontSharedMaterial = sourceTemplate.fontSharedMaterial;
        }

        targetText.fontStyle = FontStyles.Bold;
        targetText.fontSize = 24f;
        targetText.enableAutoSizing = true;
        targetText.fontSizeMin = 18f;
        targetText.fontSizeMax = 24f;
        targetText.characterSpacing = 0f;
        targetText.wordSpacing = 0f;
        targetText.lineSpacing = 0f;
        targetText.color = new Color(0.72f, 1f, 1f, 0.92f);
        targetText.alignment = TextAlignmentOptions.Center;
        targetText.richText = true;
        targetText.textWrappingMode = TextWrappingModes.NoWrap;
        targetText.overflowMode = TextOverflowModes.Overflow;
        targetText.raycastTarget = false;
    }

    private void CreateStage3InfoRow(Transform parent, string rowName, Stage3InfoRowData rowData)
    {
        GameObject rowObject = new GameObject(rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowObject.transform.SetParent(parent, false);

        RectTransform rect = rowObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(350f, 68f);

        LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
        rowLayout.preferredWidth = 350f;
        rowLayout.preferredHeight = 68f;

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 12f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateStage3InfoIcon(rowObject.transform, rowData.icon, rowData.iconSize);
        CreateStage3InfoText(rowObject.transform, rowData.description);
    }

    private void CreateStage3InfoIcon(Transform parent, Sprite sprite, Vector2 size)
    {
        GameObject iconObject = new GameObject(
            "Icon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement)
        );
        iconObject.transform.SetParent(parent, false);

        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        LayoutElement layout = iconObject.GetComponent<LayoutElement>();
        layout.preferredWidth = Mathf.Max(size.x, 96f);
        layout.preferredHeight = 68f;

        Image image = iconObject.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
        image.enabled = sprite != null;
    }

    private void CreateStage3InfoText(Transform parent, string description)
    {
        GameObject textObject = new GameObject("Description", typeof(RectTransform), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(230f, 68f);

        LayoutElement layout = textObject.GetComponent<LayoutElement>();
        layout.preferredWidth = 230f;
        layout.preferredHeight = 68f;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        ApplyStage3InfoTextStyle(text, description.Contains("\n"));
        text.text = description;
    }

    private void ApplyStage3InfoTextStyle(TMP_Text targetText, bool preserveManualLineBreaks)
    {
        if (targetText == null)
            return;

        TMP_Text sourceTemplate = stageIntroText != null ? stageIntroText : stageNameText;

        if (sourceTemplate == null)
            sourceTemplate = pauseTemplateText;

        if (sourceTemplate != null)
        {
            targetText.font = sourceTemplate.font;
            targetText.fontSharedMaterial = sourceTemplate.fontSharedMaterial;
        }

        targetText.fontStyle = FontStyles.Bold;
        targetText.fontSize = 27f;
        targetText.enableAutoSizing = true;
        targetText.fontSizeMin = 18f;
        targetText.fontSizeMax = 27f;
        targetText.characterSpacing = 0f;
        targetText.wordSpacing = 0f;
        targetText.lineSpacing = -8f;
        targetText.color = new Color(0.84f, 1f, 1f, 0.95f);
        targetText.alignment = TextAlignmentOptions.MidlineLeft;
        targetText.textWrappingMode = preserveManualLineBreaks ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
        targetText.overflowMode = TextOverflowModes.Overflow;
        targetText.raycastTarget = false;
    }

    private void CreateControlsHintContent(Transform parent)
    {
        bool isShootingHint = IsControlsHintShootingScene();
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentObject.transform.SetParent(parent, false);

        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(64f, isShootingHint ? 34f : 28f);
        contentRect.offsetMax = new Vector2(-44f, isShootingHint ? -34f : -28f);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (isShootingHint)
        {
            CreateSingleKeyHintRow(contentObject.transform, "ShootModeRow", "Z", "사격 모드 가동/격발", 21f, 34f, 34f);
            CreateSingleKeyHintRow(contentObject.transform, "ShootCancelRow", "X", "사격 모드 해제", 22f, 34f, 34f);
            CreateArrowKeyHintRow(contentObject.transform, "ShootDirectionRow", "사격 방향 변경", 22f, 70f);
            CreateSingleKeyHintRow(contentObject.transform, "AmmoMenuRow", "TAB", "탄창 메뉴 넣기/빼기", 21f, 34f, 34f);
            CreateDoubleKeyHintRow(contentObject.transform, "AmmoSwapRow", "A", "D", "총알 종류 변경", 22f, 34f, 34f);
            return;
        }

        CreateMovementHintRow(contentObject.transform);
        CreateSingleKeyHintRow(contentObject.transform, "UndoRow", "Q", "UNDO");
        CreateSingleKeyHintRow(contentObject.transform, "MenuRow", "ESC", "메뉴창");
    }

    private void CreateMovementHintRow(Transform parent)
    {
        CreateArrowKeyHintRow(parent, "MoveRow", "이동/밀기", 28f, 74f);
    }

    private void CreateArrowKeyHintRow(Transform parent, string rowName, string label, float labelFontSize, float rowHeight)
    {
        GameObject rowObject = CreateControlsHintRow(parent, rowName, rowHeight);

        GameObject arrowGroup = new GameObject("ArrowKeys", typeof(RectTransform), typeof(LayoutElement));
        arrowGroup.transform.SetParent(rowObject.transform, false);

        RectTransform arrowRect = arrowGroup.GetComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(122f, rowHeight);

        LayoutElement arrowLayout = arrowGroup.GetComponent<LayoutElement>();
        arrowLayout.preferredWidth = 122f;
        arrowLayout.preferredHeight = rowHeight;

        float verticalOffset = rowHeight >= 74f ? 19f : 18f;
        CreateKeycap(arrowGroup.transform, "UpKey", "↑", new Vector2(0f, verticalOffset), new Vector2(ControlsHintKeySize, ControlsHintKeySize));
        CreateKeycap(arrowGroup.transform, "LeftKey", "←", new Vector2(-39f, -verticalOffset), new Vector2(ControlsHintKeySize, ControlsHintKeySize));
        CreateKeycap(arrowGroup.transform, "DownKey", "↓", new Vector2(0f, -verticalOffset), new Vector2(ControlsHintKeySize, ControlsHintKeySize));
        CreateKeycap(arrowGroup.transform, "RightKey", "→", new Vector2(39f, -verticalOffset), new Vector2(ControlsHintKeySize, ControlsHintKeySize));

        CreateControlsHintSpacer(rowObject.transform, ControlsHintLabelStartX - arrowLayout.preferredWidth);
        CreateControlsHintLabel(rowObject.transform, label, labelFontSize);
    }

    private void CreateSingleKeyHintRow(Transform parent, string rowName, string keyText, string label, float labelFontSize = -1f, float rowHeight = 42f, float labelHeight = 42f)
    {
        GameObject rowObject = CreateControlsHintRow(parent, rowName, rowHeight);
        RectTransform keyRect = CreateKeycap(
            rowObject.transform,
            keyText + "Key",
            keyText,
            Vector2.zero,
            GetSingleKeycapSize(keyText)
        );

        LayoutElement keyLayout = keyRect.GetComponent<LayoutElement>();

        if (keyLayout != null)
        {
            keyLayout.preferredWidth = keyRect.sizeDelta.x;
            keyLayout.preferredHeight = keyRect.sizeDelta.y;
        }

        float resolvedLabelFontSize = labelFontSize > 0f ? labelFontSize : label == "UNDO" ? 35f : 27f;
        CreateControlsHintSpacer(rowObject.transform, ControlsHintLabelStartX - keyRect.sizeDelta.x);
        CreateControlsHintLabel(rowObject.transform, label, resolvedLabelFontSize, labelHeight);
    }

    private void CreateDoubleKeyHintRow(Transform parent, string rowName, string firstKeyText, string secondKeyText, string label, float labelFontSize, float rowHeight = 42f, float labelHeight = 42f)
    {
        GameObject rowObject = CreateControlsHintRow(parent, rowName, rowHeight);

        GameObject keyGroup = new GameObject("KeyGroup", typeof(RectTransform), typeof(LayoutElement));
        keyGroup.transform.SetParent(rowObject.transform, false);

        RectTransform groupRect = keyGroup.GetComponent<RectTransform>();
        groupRect.sizeDelta = new Vector2(88f, ControlsHintKeySize);

        LayoutElement groupLayout = keyGroup.GetComponent<LayoutElement>();
        groupLayout.preferredWidth = 88f;
        groupLayout.preferredHeight = ControlsHintKeySize;

        CreateKeycap(keyGroup.transform, firstKeyText + "Key", firstKeyText, new Vector2(-23f, 0f), new Vector2(42f, ControlsHintKeySize));
        CreateKeycap(keyGroup.transform, secondKeyText + "Key", secondKeyText, new Vector2(23f, 0f), new Vector2(42f, ControlsHintKeySize));

        CreateControlsHintSpacer(rowObject.transform, ControlsHintLabelStartX - groupLayout.preferredWidth);
        CreateControlsHintLabel(rowObject.transform, label, labelFontSize, labelHeight);
    }

    private Vector2 GetSingleKeycapSize(string keyText)
    {
        if (keyText == "TAB")
            return new Vector2(70f, ControlsHintKeySize);

        return keyText.Length > 1 ? new Vector2(62f, ControlsHintKeySize) : new Vector2(42f, ControlsHintKeySize);
    }

    private GameObject CreateControlsHintRow(Transform parent, string rowName, float height)
    {
        GameObject rowObject = new GameObject(rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowObject.transform.SetParent(parent, false);

        RectTransform rect = rowObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(ControlsHintRowWidth, height);

        LayoutElement layoutElement = rowObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = ControlsHintRowWidth;
        layoutElement.preferredHeight = height;

        HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 0f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return rowObject;
    }

    private void CreateControlsHintSpacer(Transform parent, float width)
    {
        if (width <= 0f)
            return;

        GameObject spacerObject = new GameObject("LabelSpacer", typeof(RectTransform), typeof(LayoutElement));
        spacerObject.transform.SetParent(parent, false);

        RectTransform rect = spacerObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, 1f);

        LayoutElement layout = spacerObject.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 1f;
    }

    private RectTransform CreateKeycap(Transform parent, string objectName, string label, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject keyObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement)
        );
        keyObject.transform.SetParent(parent, false);

        RectTransform rect = keyObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        LayoutElement layout = keyObject.GetComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;

        Image image = keyObject.GetComponent<Image>();
        image.color = new Color(0f, 0.12f, 0.14f, 0.96f);
        image.raycastTarget = false;

        CreateKeycapFrame(keyObject.transform, size);
        CreateKeycapLabel(keyObject.transform, label, GetKeycapLabelFontSize(label));

        return rect;
    }

    private float GetKeycapLabelFontSize(string label)
    {
        if (label == "ESC" || label == "TAB")
            return 17f;

        if (label == "Q" || label == "Z" || label == "X" || label == "W" || label == "A" || label == "S" || label == "D")
            return 29f;

        return label.Length > 1 ? 14f : 24f;
    }

    private void CreateKeycapFrame(Transform parent, Vector2 size)
    {
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        Color frameColor = new Color(0.52f, 1f, 1f, 0.9f);
        Color shadeColor = new Color(0f, 0.03f, 0.04f, 0.95f);
        Color highlightColor = new Color(0.85f, 1f, 1f, 0.62f);

        CreateControlsHintPiece(parent, "KeyTop", new Vector2(0f, halfHeight - 1f), new Vector2(size.x - 5f, 2f), frameColor, 0f);
        CreateControlsHintPiece(parent, "KeyBottom", new Vector2(0f, -halfHeight + 1f), new Vector2(size.x - 5f, 2f), shadeColor, 0f);
        CreateControlsHintPiece(parent, "KeyLeft", new Vector2(-halfWidth + 1f, 0f), new Vector2(2f, size.y - 5f), frameColor, 0f);
        CreateControlsHintPiece(parent, "KeyRight", new Vector2(halfWidth - 1f, 0f), new Vector2(2f, size.y - 5f), shadeColor, 0f);
        CreateControlsHintPiece(parent, "KeyInnerTop", new Vector2(0f, halfHeight - 7f), new Vector2(size.x - 16f, 2f), highlightColor, 0f);
        CreateControlsHintPiece(parent, "KeyInnerLeft", new Vector2(-halfWidth + 7f, 0f), new Vector2(2f, size.y - 16f), highlightColor, 0f);
    }

    private void CreateKeycapLabel(Transform parent, string label, float fontSize)
    {
        GameObject textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        ApplyControlsHintTextStyle(text, fontSize, MainMenuFontStyle, new Color(0.86f, 1f, 1f, 1f));
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
    }

    private void CreateControlsHintLabel(Transform parent, string label, float fontSize, float height = 42f)
    {
        GameObject labelObject = new GameObject("ActionLabel", typeof(RectTransform), typeof(LayoutElement));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(230f, height);

        LayoutElement layout = labelObject.GetComponent<LayoutElement>();
        layout.preferredWidth = 230f;
        layout.preferredHeight = height;

        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();
        ApplyControlsHintTextStyle(text, fontSize, FontStyles.Bold, new Color(0.84f, 1f, 1f, 0.95f));
        text.text = label;
        text.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void ApplyControlsHintTextStyle(TMP_Text targetText, float fontSize, FontStyles fontStyle, Color color)
    {
        if (targetText == null)
            return;

        TMP_Text sourceTemplate = stageIntroText != null ? stageIntroText : stageNameText;

        if (sourceTemplate == null)
            sourceTemplate = pauseTemplateText;

        if (sourceTemplate != null)
        {
            targetText.font = sourceTemplate.font;
            targetText.fontSharedMaterial = sourceTemplate.fontSharedMaterial;
        }

        targetText.fontStyle = fontStyle;
        targetText.fontSize = fontSize;
        targetText.enableAutoSizing = false;
        targetText.fontSizeMin = 12f;
        targetText.fontSizeMax = 72f;
        targetText.characterSpacing = 0f;
        targetText.wordSpacing = 0f;
        targetText.lineSpacing = 0f;
        targetText.color = color;
        targetText.textWrappingMode = TextWrappingModes.NoWrap;
        targetText.overflowMode = TextOverflowModes.Overflow;
        targetText.raycastTarget = false;
    }

    private Image CreateControlsHintPiece(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, Color color, float rotation)
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

    private void ShowControlsHintAfterIntro()
    {
        if (!ShouldShowControlsHint())
        {
            HideControlsHintImmediate();
            return;
        }

        CreateControlsHintPanel();

        if (controlsHintPanel == null)
            return;

        areInfoPopupsVisible = true;
        SetControlsHintVisible(true, true);
    }

    private bool ShouldShowControlsHint()
    {
        if (!showControlsHintAfterIntro)
            return false;

        if (string.IsNullOrEmpty(controlsHintSceneName))
            return true;

        string sceneName = SceneManager.GetActiveScene().name;

        return sceneName == controlsHintSceneName || IsControlsHintShootingScene(sceneName);
    }

    private bool IsControlsHintShootingScene()
    {
        return IsControlsHintShootingScene(SceneManager.GetActiveScene().name);
    }

    private bool IsControlsHintShootingScene(string sceneName)
    {
        return (!string.IsNullOrEmpty(controlsHintShootingSceneName) && sceneName == controlsHintShootingSceneName) ||
               (!string.IsNullOrEmpty(controlsHintSecondShootingSceneName) && sceneName == controlsHintSecondShootingSceneName);
    }

    private Vector2 GetControlsHintPanelSize()
    {
        return IsControlsHintShootingScene() ? controlsHintShootingSize : controlsHintSize;
    }

    private void HideControlsHintImmediate()
    {
        if (controlsHintRoutine != null)
        {
            StopCoroutine(controlsHintRoutine);
            controlsHintRoutine = null;
        }

        if (controlsHintGroup != null)
            controlsHintGroup.alpha = 0f;

        if (controlsHintPanel != null)
            controlsHintPanel.SetActive(false);

        areInfoPopupsVisible = false;
    }

    private bool HandleInfoPopupToggleInput()
    {
        if (!CanUseInfoPopupInput())
            return false;

        if (!Input.GetKeyDown(infoPopupToggleKey))
            return false;

        if (!HasToggleableInfoPopups())
            return false;

        areInfoPopupsVisible = !areInfoPopupsVisible;
        SetToggleableInfoPopupsVisible(areInfoPopupsVisible, false);
        return true;
    }

    private bool CanUseInfoPopupInput()
    {
        return GameStateManager.Instance == null ||
               GameStateManager.Instance.CanPopupInput;
    }

    private bool HasToggleableInfoPopups()
    {
        return (ShouldShowControlsHint() && controlsHintPanel != null) ||
               (ShouldShowStage3InfoPopups() && (stage3BoxInfoPopup != null || stage3BulletInfoPopup != null)) ||
               (ShouldShowStage5InfoPopup() && stage5BulletInfoPopup != null);
    }

    private void SetToggleableInfoPopupsVisible(bool visible, bool immediate)
    {
        if (ShouldShowControlsHint() && controlsHintPanel != null)
            SetControlsHintVisible(visible, immediate);

        if (ShouldShowStage3InfoPopups())
            SetStage3InfoPopupsVisible(visible, immediate);

        if (ShouldShowStage5InfoPopup())
            SetStage5InfoPopupVisible(visible, immediate);
    }

    private void SetControlsHintVisible(bool visible, bool immediate)
    {
        if (controlsHintPanel == null)
            return;

        if (controlsHintRect == null)
            controlsHintRect = controlsHintPanel.GetComponent<RectTransform>();

        if (controlsHintRect == null)
            return;

        controlsHintPanel.SetActive(true);

        if (controlsHintGroup != null)
            controlsHintGroup.alpha = 1f;

        if (controlsHintRoutine != null)
        {
            StopCoroutine(controlsHintRoutine);
            controlsHintRoutine = null;
        }

        float targetX = visible ? controlsHintPosition.x : ResolveRightHiddenPopupX(controlsHintRect);
        Vector2 targetPosition = new Vector2(targetX, controlsHintPosition.y);

        if (immediate)
        {
            controlsHintRect.anchoredPosition = targetPosition;
            return;
        }

        controlsHintRoutine = StartCoroutine(SlideControlsHintRoutine(targetPosition));
    }

    private IEnumerator SlideControlsHintRoutine(Vector2 targetPosition)
    {
        if (controlsHintRect == null)
        {
            controlsHintRoutine = null;
            yield break;
        }

        yield return SlideRectAnchoredPosition(controlsHintRect, targetPosition);
        controlsHintRoutine = null;
    }

    private void SetStage3InfoPopupsVisible(bool visible, bool immediate)
    {
        RectTransform boxRect = GetPopupRect(stage3BoxInfoPopup);
        RectTransform bulletRect = GetPopupRect(stage3BulletInfoPopup);

        if (boxRect == null && bulletRect == null)
            return;

        if (stage3InfoPopupRoutine != null)
        {
            StopCoroutine(stage3InfoPopupRoutine);
            stage3InfoPopupRoutine = null;
        }

        if (stage3BoxInfoPopup != null)
            stage3BoxInfoPopup.SetActive(true);

        if (stage3BulletInfoPopup != null)
            stage3BulletInfoPopup.SetActive(true);

        Vector2 boxTarget = boxRect != null
            ? new Vector2(visible ? stageInfoBoxPopupPosition.x : ResolveLeftHiddenPopupX(boxRect), stageInfoBoxPopupPosition.y)
            : Vector2.zero;

        Vector2 bulletTarget = bulletRect != null
            ? new Vector2(visible ? stageInfoBulletPopupPosition.x : ResolveLeftHiddenPopupX(bulletRect), stageInfoBulletPopupPosition.y)
            : Vector2.zero;

        if (immediate)
        {
            if (boxRect != null)
                boxRect.anchoredPosition = boxTarget;

            if (bulletRect != null)
                bulletRect.anchoredPosition = bulletTarget;

            return;
        }

        stage3InfoPopupRoutine = StartCoroutine(SlideStage3InfoPopupsRoutine(boxRect, boxTarget, bulletRect, bulletTarget));
    }

    private void SetStage5InfoPopupVisible(bool visible, bool immediate)
    {
        RectTransform bulletRect = GetPopupRect(stage5BulletInfoPopup);

        if (bulletRect == null)
            return;

        if (stage3InfoPopupRoutine != null)
        {
            StopCoroutine(stage3InfoPopupRoutine);
            stage3InfoPopupRoutine = null;
        }

        stage5BulletInfoPopup.SetActive(true);

        Vector2 target = new Vector2(
            visible ? stage5InfoPopupPosition.x : ResolveLeftHiddenPopupX(bulletRect),
            stage5InfoPopupPosition.y
        );

        if (immediate)
        {
            bulletRect.anchoredPosition = target;
            return;
        }

        stage3InfoPopupRoutine = StartCoroutine(SlideStage5InfoPopupRoutine(bulletRect, target));
    }

    private IEnumerator SlideStage5InfoPopupRoutine(RectTransform bulletRect, Vector2 target)
    {
        yield return SlideRectAnchoredPosition(bulletRect, target);
        stage3InfoPopupRoutine = null;
    }

    private IEnumerator SlideStage3InfoPopupsRoutine(RectTransform boxRect, Vector2 boxTarget, RectTransform bulletRect, Vector2 bulletTarget)
    {
        float speed = ResolveInfoPopupSlideSpeed();
        bool boxDone = boxRect == null;
        bool bulletDone = bulletRect == null;

        while (!boxDone || !bulletDone)
        {
            if (boxRect != null)
                boxDone = MoveRectTowards(boxRect, boxTarget, speed);

            if (bulletRect != null)
                bulletDone = MoveRectTowards(bulletRect, bulletTarget, speed);

            yield return null;
        }

        stage3InfoPopupRoutine = null;
    }

    private IEnumerator SlideRectAnchoredPosition(RectTransform rect, Vector2 targetPosition)
    {
        float speed = ResolveInfoPopupSlideSpeed();

        while (!MoveRectTowards(rect, targetPosition, speed))
            yield return null;
    }

    private bool MoveRectTowards(RectTransform rect, Vector2 targetPosition, float speed)
    {
        if (rect == null)
            return true;

        if (Vector2.Distance(rect.anchoredPosition, targetPosition) <= 0.1f)
        {
            rect.anchoredPosition = targetPosition;
            return true;
        }

        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition, Time.deltaTime * speed);
        return false;
    }

    private RectTransform GetPopupRect(GameObject popup)
    {
        return popup != null ? popup.GetComponent<RectTransform>() : null;
    }

    private float ResolveLeftHiddenPopupX(RectTransform rect)
    {
        AmmoPopupAnimator ammoAnimator = ResolveAmmoPopupAnimator();

        if (ammoAnimator != null)
            return ammoAnimator.hideX;

        float width = rect != null ? rect.sizeDelta.x : stageInfoPopupSize.x;
        return -width * 0.5f + ResolveInfoPopupHiddenEdgeWidth();
    }

    private float ResolveRightHiddenPopupX(RectTransform rect)
    {
        float width = rect != null ? rect.sizeDelta.x : GetControlsHintPanelSize().x;
        return Mathf.Max(0f, width - ResolveInfoPopupHiddenEdgeWidth());
    }

    private float ResolveInfoPopupHiddenEdgeWidth()
    {
        const float fallbackEdgeWidth = 30f;
        AmmoPopupAnimator ammoAnimator = ResolveAmmoPopupAnimator();

        if (ammoAnimator == null)
            return fallbackEdgeWidth;

        RectTransform ammoRect = ammoAnimator.rect != null ? ammoAnimator.rect : ammoAnimator.GetComponent<RectTransform>();

        if (ammoRect == null || ammoRect.sizeDelta.x <= 0f)
            return fallbackEdgeWidth;

        float edgeWidth = ammoAnimator.hideX + ammoRect.sizeDelta.x * (1f - ammoRect.pivot.x);

        if (edgeWidth <= 0f || edgeWidth >= ammoRect.sizeDelta.x)
            return fallbackEdgeWidth;

        return edgeWidth;
    }

    private float ResolveInfoPopupSlideSpeed()
    {
        AmmoPopupAnimator ammoAnimator = ResolveAmmoPopupAnimator();

        if (ammoAnimator != null && ammoAnimator.speed > 0f)
            return ammoAnimator.speed;

        return Mathf.Max(0.01f, infoPopupSlideSpeed);
    }

    private AmmoPopupAnimator ResolveAmmoPopupAnimator()
    {
        AmmoPopupUI ammoSource = ResolveIntroAmmoSource();
        AmmoPopupAnimator ammoAnimator = ammoSource != null ? ammoSource.GetComponent<AmmoPopupAnimator>() : null;

        if (ammoAnimator == null)
            ammoAnimator = FindFirstObjectByType<AmmoPopupAnimator>();

        return ammoAnimator;
    }

    private bool HandlePauseToggleInput()
    {
        if (!Input.GetKeyDown(pauseKey))
            return false;

        if (isPausedByMenu)
        {
            if (isPauseSettingsOpen)
            {
                ClosePauseSettings(true);
                return true;
            }

            ResumeFromPauseMenu();
            return true;
        }

        if (!CanOpenPauseMenu())
            return false;

        OpenPauseMenu();
        return true;
    }

    private bool CanOpenPauseMenu()
    {
        if (isPauseSceneActionRunning)
            return false;

        if (isGameOverPanelOpen)
            return false;

        if (stageClearPanel != null && stageClearPanel.activeInHierarchy)
            return false;

        if (GameStateManager.Instance == null)
            return true;

        GameState state = GameStateManager.Instance.CurrentState;
        return state == GameState.Playing || state == GameState.Aiming;
    }

    private void OpenPauseMenu()
    {
        CreatePauseMenuPanel();

        if (pauseMenuPanel == null)
            return;

        isPausedByMenu = true;
        isPauseSceneActionRunning = false;

        timeScaleBeforePause = Time.timeScale;

        if (GameStateManager.Instance != null)
        {
            stateBeforePause = GameStateManager.Instance.CurrentState;
            GameStateManager.Instance.SetState(GameState.Paused);
        }
        else
        {
            stateBeforePause = GameState.Playing;
        }

        Time.timeScale = 0f;
        SetGameplayBlocked(true);

        pauseMenuPanel.SetActive(true);
        if (pauseSettingsPanel != null)
            pauseSettingsPanel.SetActive(false);

        isPauseSettingsOpen = false;
        CachePauseButtonVisuals();
        SelectPauseButton(0);
    }

    private void ResumeFromPauseMenu()
    {
        PlayClearButtonSelectSound();
        ClosePauseMenu(false);

        if (resumePauseRoutine != null)
            StopCoroutine(resumePauseRoutine);

        resumePauseRoutine = StartCoroutine(RestorePauseStateNextFrame());
    }

    private void ClosePauseMenu(bool restorePreviousState)
    {
        isPausedByMenu = false;
        isPauseSceneActionRunning = false;
        isPauseSettingsOpen = false;

        ClearCurrentPauseSettingsButtonHover();
        ClearCurrentPauseButtonHover();
        RestorePauseSettingsButtonVisuals();
        RestorePauseButtonVisuals();

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (pauseSettingsPanel != null)
            pauseSettingsPanel.SetActive(false);

        if (restorePreviousState)
            RestorePausedGameplayState();
    }

    private IEnumerator RestorePauseStateNextFrame()
    {
        yield return null;

        RestorePausedGameplayState();
        resumePauseRoutine = null;
    }

    private void RestorePausedGameplayState()
    {
        Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
        SetGameplayBlocked(false);

        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentState == GameState.Paused)
        {
            GameStateManager.Instance.SetState(stateBeforePause);
        }
    }

    private void HandlePauseMenuKeyboardInput()
    {
        if (pauseButtons == null || pauseButtons.Length == 0)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MovePauseButtonSelection(-1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            MovePauseButtonSelection(1);
            return;
        }

        if (Input.GetKeyDown(pauseSubmitKey))
        {
            SubmitSelectedPauseButton();
        }
    }

    private void HandlePauseSettingsKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MovePauseSettingsSelection(-1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MovePauseSettingsSelection(1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            AdjustSelectedPauseSetting(-pauseSettingsKeyboardStep);
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            AdjustSelectedPauseSetting(pauseSettingsKeyboardStep);
            return;
        }

        if (Input.GetKeyDown(pauseSubmitKey))
            SubmitSelectedPauseSettingsButton();
    }

    private void AdjustSelectedPauseSetting(float delta)
    {
        if (selectedPauseSettingsIndex < 0 || selectedPauseSettingsIndex >= PauseSettingsCloseIndex)
            return;

        if (pauseSettingsSliders == null ||
            selectedPauseSettingsIndex >= pauseSettingsSliders.Length ||
            pauseSettingsSliders[selectedPauseSettingsIndex] == null)
        {
            return;
        }

        pauseSettingsSliders[selectedPauseSettingsIndex].value =
            Mathf.Clamp01(pauseSettingsSliders[selectedPauseSettingsIndex].value + delta);

        PlayClearButtonSwapSound();
    }

    private void MovePauseButtonSelection(int direction)
    {
        if (pauseButtons == null || pauseButtons.Length == 0)
            return;

        int startIndex = selectedPauseButtonIndex;

        if (startIndex < 0)
            startIndex = 0;

        for (int i = 1; i <= pauseButtons.Length; i++)
        {
            int nextIndex = startIndex + direction * i;

            if (nextIndex < 0)
                nextIndex = pauseButtons.Length - 1;

            if (nextIndex >= pauseButtons.Length)
                nextIndex = 0;

            if (IsUsableButton(pauseButtons[nextIndex]))
            {
                int previousIndex = selectedPauseButtonIndex;

                SelectPauseButton(nextIndex);

                if (nextIndex != previousIndex)
                    PlayClearButtonSwapSound();

                return;
            }
        }
    }

    private void MovePauseSettingsSelection(int direction)
    {
        if (pauseSettingsButtons == null || pauseSettingsButtons.Length == 0)
            return;

        int startIndex = selectedPauseSettingsIndex;

        if (startIndex < 0)
            startIndex = 0;

        for (int i = 1; i <= pauseSettingsButtons.Length; i++)
        {
            int nextIndex = startIndex + direction * i;

            if (nextIndex < 0)
                nextIndex = pauseSettingsButtons.Length - 1;

            if (nextIndex >= pauseSettingsButtons.Length)
                nextIndex = 0;

            if (IsUsableButton(pauseSettingsButtons[nextIndex]))
            {
                int previousIndex = selectedPauseSettingsIndex;

                SelectPauseSettingsButton(nextIndex);

                if (nextIndex != previousIndex)
                    PlayClearButtonSwapSound();

                return;
            }
        }
    }

    private void SelectPauseButton(int index)
    {
        if (pauseButtons == null || pauseButtons.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, pauseButtons.Length - 1);

        if (!IsUsableButton(pauseButtons[index]))
            return;

        ClearCurrentPauseButtonHover();

        selectedPauseButtonIndex = index;
        currentHoveredPauseButton = pauseButtons[index];

        CacheButtonVisual(currentHoveredPauseButton);

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(currentHoveredPauseButton.gameObject);

        currentHoveredPauseButton.Select();
        ApplyButtonHoverVisual(currentHoveredPauseButton);
    }

    private void SelectPauseSettingsButton(int index)
    {
        if (pauseSettingsButtons == null || pauseSettingsButtons.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, pauseSettingsButtons.Length - 1);

        if (!IsUsableButton(pauseSettingsButtons[index]))
            return;

        ClearCurrentPauseSettingsButtonHover();

        selectedPauseSettingsIndex = index;
        currentHoveredPauseSettingsButton = pauseSettingsButtons[index];

        CacheButtonVisual(currentHoveredPauseSettingsButton);

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem != null)
            eventSystem.SetSelectedGameObject(currentHoveredPauseSettingsButton.gameObject);

        currentHoveredPauseSettingsButton.Select();
        ApplyButtonHoverVisual(currentHoveredPauseSettingsButton);
    }

    private void SubmitSelectedPauseButton()
    {
        BlockGameplayInputUntilRelease(pauseSubmitKey);

        if (pauseButtons == null ||
            selectedPauseButtonIndex < 0 ||
            selectedPauseButtonIndex >= pauseButtons.Length ||
            !IsUsableButton(pauseButtons[selectedPauseButtonIndex]))
        {
            SelectPauseButton(0);
            return;
        }

        pauseButtons[selectedPauseButtonIndex].onClick.Invoke();
    }

    private void SubmitSelectedPauseSettingsButton()
    {
        BlockGameplayInputUntilRelease(pauseSubmitKey);

        if (pauseSettingsButtons == null ||
            selectedPauseSettingsIndex < 0 ||
            selectedPauseSettingsIndex >= pauseSettingsButtons.Length ||
            !IsUsableButton(pauseSettingsButtons[selectedPauseSettingsIndex]))
        {
            SelectPauseSettingsButton(0);
            return;
        }

        pauseSettingsButtons[selectedPauseSettingsIndex].onClick.Invoke();
    }

    private void CreatePauseMenuPanel()
    {
        if (pauseMenuPanel != null)
            return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Transform parent = parentCanvas != null ? parentCanvas.transform : transform;

        pauseMenuPanel = new GameObject("PauseMenuPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
        pauseMenuPanel.transform.SetParent(parent, false);

        RectTransform panelRect = pauseMenuPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = pausePanelPosition;
        panelRect.sizeDelta = new Vector2(pauseButtonSize.x, GetPausePanelHeight());

        VerticalLayoutGroup layout = pauseMenuPanel.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = pauseButtonSpacing;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreatePauseTitle();
        CreatePauseButtons();

        pauseMenuPanel.SetActive(false);
    }

    private void CreatePauseTitle()
    {
        GameObject titleObject = new GameObject("PauseTitle", typeof(RectTransform));
        titleObject.transform.SetParent(pauseMenuPanel.transform, false);

        LayoutElement layout = titleObject.AddComponent<LayoutElement>();
        layout.preferredWidth = pauseButtonSize.x;
        layout.preferredHeight = pauseButtonSize.y;

        TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
        ApplyPauseMenuTextStyle(titleText);
        titleText.text = pauseTitle;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = pauseTemplateText != null ? (pauseTemplateText.fontSize + 4f) * pauseTitleFontScale : 64f;
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Overflow;
        titleText.color = Color.white;
    }

    private void CreatePauseButtons()
    {
        pauseButtons = new Button[4];

        pauseButtons[0] = CreatePauseButton(GetPauseButtonLabel(0, "RESUME"));
        pauseButtons[0].onClick.AddListener(ResumeFromPauseMenu);

        pauseButtons[1] = CreatePauseButton(GetPauseButtonLabel(1, "RETRY"));
        pauseButtons[1].onClick.AddListener(RetryFromPauseMenu);

        pauseButtons[2] = CreatePauseButton(GetPauseButtonLabel(2, "SETTINGS"));
        pauseButtons[2].onClick.AddListener(OpenPauseSettings);

        pauseButtons[3] = CreatePauseButton(GetPauseButtonLabel(3, "MAIN MENU"));
        pauseButtons[3].onClick.AddListener(ReturnToMainMenuFromPause);
    }

    private Button CreatePauseButton(string label)
    {
        GameObject buttonObject = new GameObject(
            label + "Button",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement)
        );

        buttonObject.transform.SetParent(pauseMenuPanel.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = pauseButtonSize;

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = pauseButtonSize.x;
        layout.preferredHeight = pauseButtonSize.y;

        Image image = buttonObject.GetComponent<Image>();
        image.color = pauseButtonBackgroundColor;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;

        Image hoverOverlay = CreatePauseButtonHoverOverlay(buttonObject.transform);
        StageButtonHoverEffect hoverEffect = buttonObject.AddComponent<StageButtonHoverEffect>();
        hoverEffect.Configure(hoverOverlay, pauseButtonHoverColor, pauseButtonPressedColor);

        CreatePauseButtonFrame(buttonObject.transform, pauseButtonSize);

        GameObject textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = textObject.AddComponent<TextMeshProUGUI>();
        ApplyPauseMenuTextStyle(labelText);
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = Mathf.Min(pauseTemplateText != null ? pauseTemplateText.fontSize * pauseButtonFontScale : 56f, pauseButtonSize.x * 0.2f);
        labelText.enableWordWrapping = false;
        labelText.overflowMode = TextOverflowModes.Overflow;
        labelText.color = Color.white;
        labelText.raycastTarget = false;

        hoverEffect.ConfigureLabel(labelText, pauseSelectedTextColor, pauseSelectedTextScale);
        hoverEffect.SetHighlightWhenSelected(true);

        return button;
    }

    private void CreatePauseSettingsPanel()
    {
        if (pauseSettingsPanel != null)
            return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        Transform parent = parentCanvas != null ? parentCanvas.transform : transform;

        pauseSettingsPanel = new GameObject(
            "PauseSettingsPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        pauseSettingsPanel.transform.SetParent(parent, false);

        RectTransform panelRect = pauseSettingsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = pauseSettingsPanelPosition;
        panelRect.sizeDelta = pauseSettingsPanelSize;

        Image panelImage = pauseSettingsPanel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0.09f, 0.1f, 0.86f);
        panelImage.raycastTarget = true;

        CreatePauseSettingsPanelFrame(pauseSettingsPanel.transform);
        CreatePauseSettingsContent(pauseSettingsPanel.transform);
        pauseSettingsPanel.SetActive(false);
    }

    private void CreatePauseSettingsContent(Transform parent)
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
        layout.spacing = pauseSettingsRowSpacing;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreatePauseSettingsTitle(contentObject.transform);

        pauseSettingsButtons = new Button[4];
        pauseSettingsSliders = new Slider[3];
        pauseSettingsValueTexts = new TMP_Text[3];

        pauseSettingsButtons[PauseSettingsBrightnessIndex] = CreatePauseSettingsSliderRow(
            contentObject.transform,
            "PauseBrightnessRow",
            pauseBrightnessLabel,
            GameSettings.Brightness,
            PauseSettingsBrightnessIndex
        );

        pauseSettingsButtons[PauseSettingsMusicIndex] = CreatePauseSettingsSliderRow(
            contentObject.transform,
            "PauseMusicRow",
            pauseMusicVolumeLabel,
            GameSettings.MusicVolume,
            PauseSettingsMusicIndex
        );

        pauseSettingsButtons[PauseSettingsSoundIndex] = CreatePauseSettingsSliderRow(
            contentObject.transform,
            "PauseSoundRow",
            pauseSoundVolumeLabel,
            GameSettings.SoundVolume,
            PauseSettingsSoundIndex
        );

        pauseSettingsButtons[PauseSettingsCloseIndex] = CreatePauseSettingsCloseButton(contentObject.transform);
    }

    private void CreatePauseSettingsTitle(Transform parent)
    {
        GameObject titleObject = new GameObject("PauseSettingsTitle", typeof(RectTransform));
        titleObject.transform.SetParent(parent, false);

        LayoutElement layout = titleObject.AddComponent<LayoutElement>();
        layout.preferredWidth = pauseSettingsRowSize.x;
        layout.preferredHeight = 86f;

        TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
        ApplyPauseSettingsTextStyle(titleText);
        titleText.text = pauseSettingsTitle;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = (MainMenuTemplateFontSize + 4f) * 1.82f;
        titleText.enableWordWrapping = false;
        titleText.overflowMode = TextOverflowModes.Overflow;
        titleText.color = Color.white;
        titleText.raycastTarget = false;
    }

    private Button CreatePauseSettingsSliderRow(Transform parent, string objectName, string label, float value, int settingIndex)
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
        rowRect.sizeDelta = pauseSettingsRowSize;

        LayoutElement layout = rowObject.GetComponent<LayoutElement>();
        layout.preferredWidth = pauseSettingsRowSize.x;
        layout.preferredHeight = pauseSettingsRowSize.y;

        Image image = rowObject.GetComponent<Image>();
        image.color = pauseButtonBackgroundColor;
        image.raycastTarget = true;

        Button button = rowObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => SelectPauseSettingsRow(settingIndex));

        Image hoverOverlay = CreatePauseButtonHoverOverlay(rowObject.transform);
        StageButtonHoverEffect hoverEffect = rowObject.AddComponent<StageButtonHoverEffect>();
        hoverEffect.Configure(hoverOverlay, pauseButtonHoverColor, pauseButtonPressedColor);

        CreatePauseButtonFrame(rowObject.transform, pauseSettingsRowSize);
        TMP_Text labelText = CreatePauseSettingsRowLabel(rowObject.transform, label);
        hoverEffect.ConfigureLabel(labelText, pauseSelectedTextColor, pauseSelectedTextScale);
        hoverEffect.SetHighlightWhenSelected(true);

        Slider slider = CreatePauseSettingsSlider(rowObject.transform, value, settingIndex);
        pauseSettingsSliders[settingIndex] = slider;

        return button;
    }

    private TMP_Text CreatePauseSettingsRowLabel(Transform parent, string label)
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
        ApplyPauseSettingsTextStyle(labelText);
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.fontSize = MainMenuTemplateFontSize * 1.22f;
        labelText.enableWordWrapping = false;
        labelText.overflowMode = TextOverflowModes.Overflow;
        labelText.color = Color.white;
        labelText.raycastTarget = false;

        return labelText;
    }

    private Slider CreatePauseSettingsSlider(Transform parent, float value, int settingIndex)
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
        ConfigurePauseSettingsSliderTrigger(sliderObject.GetComponent<EventTrigger>(), settingIndex);

        GameObject trackObject = CreatePauseSliderImage("Track", sliderObject.transform, new Color(0.62f, 0.95f, 1f, 0.28f), true);
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

        GameObject fillObject = CreatePauseSliderImage("Fill", fillAreaObject.transform, pauseButtonAccentColor, false);
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

        GameObject handleObject = CreatePauseSliderImage("Handle", handleAreaObject.transform, new Color(0.78f, 0.95f, 1f, 1f), true);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(22f, 28f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleObject.GetComponent<Image>();
        slider.onValueChanged.AddListener(newValue => ApplyPauseSettingValue(settingIndex, newValue));

        CreatePauseSettingsValueText(parent, settingIndex, value);
        return slider;
    }

    private void ConfigurePauseSettingsSliderTrigger(EventTrigger trigger, int settingIndex)
    {
        if (trigger == null)
            return;

        EventTrigger.Entry pointerDown = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerDown
        };
        pointerDown.callback.AddListener(_ => SelectPauseSettingsRow(settingIndex));
        trigger.triggers.Add(pointerDown);
    }

    private GameObject CreatePauseSliderImage(string objectName, Transform parent, Color color, bool raycastTarget)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;

        return imageObject;
    }

    private void CreatePauseSettingsValueText(Transform parent, int settingIndex, float value)
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
        ApplyPauseSettingsTextStyle(valueText);
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.fontSize = MainMenuTemplateFontSize * 0.82f;
        valueText.enableWordWrapping = false;
        valueText.overflowMode = TextOverflowModes.Overflow;
        valueText.color = pauseButtonAccentColor;
        valueText.raycastTarget = false;

        pauseSettingsValueTexts[settingIndex] = valueText;
        UpdatePauseSettingsValueText(settingIndex, value);
    }

    private Button CreatePauseSettingsCloseButton(Transform parent)
    {
        GameObject buttonObject = new GameObject(
            "PauseSettingsCloseButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement)
        );
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = pauseSettingsCloseButtonSize;

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = pauseSettingsCloseButtonSize.x;
        layout.preferredHeight = pauseSettingsCloseButtonSize.y;

        Image image = buttonObject.GetComponent<Image>();
        image.color = pauseButtonBackgroundColor;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => ClosePauseSettings(true));

        Image hoverOverlay = CreatePauseButtonHoverOverlay(buttonObject.transform);
        StageButtonHoverEffect hoverEffect = buttonObject.AddComponent<StageButtonHoverEffect>();
        hoverEffect.Configure(hoverOverlay, pauseButtonHoverColor, pauseButtonPressedColor);

        CreatePauseButtonFrame(buttonObject.transform, pauseSettingsCloseButtonSize);

        GameObject textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = textObject.AddComponent<TextMeshProUGUI>();
        ApplyPauseSettingsTextStyle(labelText);
        labelText.text = pauseSettingsCloseLabel;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = MainMenuTemplateFontSize * 1.2f;
        labelText.enableWordWrapping = false;
        labelText.overflowMode = TextOverflowModes.Overflow;
        labelText.color = Color.white;
        labelText.raycastTarget = false;

        hoverEffect.ConfigureLabel(labelText, pauseSelectedTextColor, pauseSelectedTextScale);
        hoverEffect.SetHighlightWhenSelected(true);
        return button;
    }

    private void CreatePauseSettingsPanelFrame(Transform parent)
    {
        float width = pauseSettingsPanelSize.x;
        float height = pauseSettingsPanelSize.y;
        float line = Mathf.Max(1f, pauseButtonFrameThickness);
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        float inset = 34f;
        float cornerLength = 130f;
        float verticalLength = 92f;

        CreatePauseFramePiece(parent, "DialogTopLine", new Vector2(0f, halfHeight - inset), new Vector2(width - 210f, line), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "DialogBottomLine", new Vector2(0f, -halfHeight + inset), new Vector2(width - 210f, line), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "DialogTopLeftH", new Vector2(-halfWidth + inset + cornerLength * 0.5f, halfHeight - inset), new Vector2(cornerLength, line * 1.5f), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "DialogTopLeftV", new Vector2(-halfWidth + inset, halfHeight - inset - verticalLength * 0.5f), new Vector2(line * 1.5f, verticalLength), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "DialogTopRightH", new Vector2(halfWidth - inset - cornerLength * 0.5f, halfHeight - inset), new Vector2(cornerLength, line * 1.5f), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "DialogTopRightV", new Vector2(halfWidth - inset, halfHeight - inset - verticalLength * 0.5f), new Vector2(line * 1.5f, verticalLength), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "DialogBottomLeftH", new Vector2(-halfWidth + inset + cornerLength * 0.5f, -halfHeight + inset), new Vector2(cornerLength, line * 1.5f), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "DialogBottomLeftV", new Vector2(-halfWidth + inset, -halfHeight + inset + verticalLength * 0.5f), new Vector2(line * 1.5f, verticalLength), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "DialogBottomRightH", new Vector2(halfWidth - inset - cornerLength * 0.5f, -halfHeight + inset), new Vector2(cornerLength, line * 1.5f), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "DialogBottomRightV", new Vector2(halfWidth - inset, -halfHeight + inset + verticalLength * 0.5f), new Vector2(line * 1.5f, verticalLength), pauseButtonFrameColor, 0f);

        CreatePauseHatchGroup(parent, "DialogTopHatch", new Vector2(-halfWidth + 190f, halfHeight - inset + 2f), -20f);
        CreatePauseHatchGroup(parent, "DialogBottomHatch", new Vector2(halfWidth - 250f, -halfHeight + inset + 2f), -20f);
    }

    private Image CreatePauseButtonHoverOverlay(Transform parent)
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

    private void CreatePauseButtonFrame(Transform parent, Vector2 buttonSize)
    {
        float width = buttonSize.x;
        float height = buttonSize.y;
        float line = Mathf.Max(1f, pauseButtonFrameThickness);
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        float sideInset = Mathf.Min(32f, width * 0.09f);
        float verticalInset = Mathf.Min(16f, height * 0.13f);
        float cornerLength = Mathf.Clamp(width * 0.14f, 46f, 96f);
        float verticalLength = Mathf.Clamp(height * 0.45f, 42f, 56f);
        float centerLineWidth = width * 0.48f;

        CreatePauseFramePiece(parent, "TopLineLeft", new Vector2(-width * 0.15f, halfHeight - verticalInset), new Vector2(centerLineWidth, line), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "TopLineRight", new Vector2(width * 0.18f, halfHeight - verticalInset), new Vector2(centerLineWidth, line), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "BottomLineLeft", new Vector2(-width * 0.18f, -halfHeight + verticalInset), new Vector2(centerLineWidth, line), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "BottomLineRight", new Vector2(width * 0.15f, -halfHeight + verticalInset), new Vector2(centerLineWidth, line), pauseButtonFrameColor, 0f);

        CreatePauseFramePiece(parent, "TopLeftCornerH", new Vector2(-halfWidth + sideInset + cornerLength * 0.5f, halfHeight - verticalInset), new Vector2(cornerLength, line * 1.4f), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "TopLeftCornerV", new Vector2(-halfWidth + sideInset, halfHeight - verticalInset - verticalLength * 0.5f), new Vector2(line * 1.4f, verticalLength), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "TopRightCornerH", new Vector2(halfWidth - sideInset - cornerLength * 0.5f, halfHeight - verticalInset), new Vector2(cornerLength, line * 1.4f), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "TopRightCornerV", new Vector2(halfWidth - sideInset, halfHeight - verticalInset - verticalLength * 0.5f), new Vector2(line * 1.4f, verticalLength), pauseButtonFrameColor, 0f);

        CreatePauseFramePiece(parent, "BottomLeftCornerH", new Vector2(-halfWidth + sideInset + cornerLength * 0.5f, -halfHeight + verticalInset), new Vector2(cornerLength, line * 1.4f), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "BottomLeftCornerV", new Vector2(-halfWidth + sideInset, -halfHeight + verticalInset + verticalLength * 0.5f), new Vector2(line * 1.4f, verticalLength), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "BottomRightCornerH", new Vector2(halfWidth - sideInset - cornerLength * 0.5f, -halfHeight + verticalInset), new Vector2(cornerLength, line * 1.4f), pauseButtonFrameColor, 0f);
        CreatePauseFramePiece(parent, "BottomRightCornerV", new Vector2(halfWidth - sideInset, -halfHeight + verticalInset + verticalLength * 0.5f), new Vector2(line * 1.4f, verticalLength), pauseButtonFrameColor, 0f);

        CreatePauseFramePiece(parent, "TopLeftSlant", new Vector2(-halfWidth + sideInset + 14f, halfHeight - verticalInset - 6f), new Vector2(30f, line * 1.6f), pauseButtonFrameColor, -45f);
        CreatePauseFramePiece(parent, "TopRightSlant", new Vector2(halfWidth - sideInset - 14f, halfHeight - verticalInset - 6f), new Vector2(30f, line * 1.6f), pauseButtonFrameColor, 45f);
        CreatePauseFramePiece(parent, "BottomLeftSlant", new Vector2(-halfWidth + sideInset + 14f, -halfHeight + verticalInset + 6f), new Vector2(30f, line * 1.6f), pauseButtonFrameColor, 45f);
        CreatePauseFramePiece(parent, "BottomRightSlant", new Vector2(halfWidth - sideInset - 14f, -halfHeight + verticalInset + 6f), new Vector2(30f, line * 1.6f), pauseButtonFrameColor, -45f);

        CreatePauseHatchGroup(parent, "TopHatch", new Vector2(-halfWidth + Mathf.Min(150f, width * 0.22f), halfHeight - verticalInset + 2f), -20f);
        CreatePauseHatchGroup(parent, "BottomHatch", new Vector2(halfWidth - Mathf.Min(190f, width * 0.28f), -halfHeight + verticalInset + 2f), -20f);
    }

    private void CreatePauseHatchGroup(Transform parent, string groupName, Vector2 startPosition, float rotation)
    {
        const int hatchCount = 7;
        const float hatchSpacing = 10f;

        for (int i = 0; i < hatchCount; i++)
        {
            Vector2 position = startPosition + new Vector2(i * hatchSpacing, 0f);
            CreatePauseFramePiece(parent, $"{groupName}_{i}", position, new Vector2(8f, 2f), pauseButtonAccentColor, rotation);
        }
    }

    private Image CreatePauseFramePiece(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, Color color, float rotation)
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

    private float GetPausePanelHeight()
    {
        int itemCount = 5; // title + four buttons
        int gapCount = Mathf.Max(0, itemCount - 1);

        return pauseButtonSize.y * itemCount + pauseButtonSpacing * gapCount;
    }

    private string GetPauseButtonLabel(int index, string fallback)
    {
        if (pauseButtonLabels != null &&
            index >= 0 &&
            index < pauseButtonLabels.Length &&
            !string.IsNullOrEmpty(pauseButtonLabels[index]))
        {
            return pauseButtonLabels[index];
        }

        return fallback;
    }

    private void CachePauseTemplateText()
    {
        if (pauseTemplateText != null)
            return;

        if (retryButton != null)
            pauseTemplateText = retryButton.GetComponentInChildren<TMP_Text>(true);

        if (pauseTemplateText == null && nextButton != null)
            pauseTemplateText = nextButton.GetComponentInChildren<TMP_Text>(true);

        if (pauseTemplateText == null && stageClearPanel != null)
            pauseTemplateText = stageClearPanel.GetComponentInChildren<TMP_Text>(true);
    }

    private void ApplyPauseTextStyle(TMP_Text targetText)
    {
        if (targetText == null || pauseTemplateText == null)
            return;

        targetText.font = pauseTemplateText.font;
        targetText.fontSharedMaterial = pauseTemplateText.fontSharedMaterial;
        targetText.fontSize = pauseTemplateText.fontSize;
        targetText.fontStyle = pauseTemplateText.fontStyle;
        targetText.enableAutoSizing = pauseTemplateText.enableAutoSizing;
        targetText.fontSizeMin = pauseTemplateText.fontSizeMin;
        targetText.fontSizeMax = pauseTemplateText.fontSizeMax;
        targetText.characterSpacing = pauseTemplateText.characterSpacing;
        targetText.wordSpacing = pauseTemplateText.wordSpacing;
        targetText.lineSpacing = pauseTemplateText.lineSpacing;
    }

    private void ApplyPauseMenuTextStyle(TMP_Text targetText)
    {
        if (targetText == null)
            return;

        ApplyPauseTextStyle(targetText);

        targetText.fontStyle = PauseMenuFontStyle;
        targetText.enableAutoSizing = false;
        targetText.fontSizeMin = 18f;
        targetText.fontSizeMax = 72f;
        targetText.characterSpacing = 0f;
        targetText.wordSpacing = 0f;
        targetText.lineSpacing = 0f;
    }

    private void ApplyPauseSettingsTextStyle(TMP_Text targetText)
    {
        if (targetText == null)
            return;

        ApplyPauseTextStyle(targetText);

        targetText.fontSize = MainMenuTemplateFontSize;
        targetText.fontStyle = MainMenuFontStyle;
        targetText.enableAutoSizing = false;
        targetText.fontSizeMin = 18f;
        targetText.fontSizeMax = 72f;
        targetText.characterSpacing = 0f;
        targetText.wordSpacing = 0f;
        targetText.lineSpacing = 0f;
    }

    private void CachePauseButtonVisuals()
    {
        if (pauseButtons == null)
            return;

        for (int i = 0; i < pauseButtons.Length; i++)
        {
            CacheButtonVisual(pauseButtons[i]);
        }
    }

    private void CachePauseSettingsButtonVisuals()
    {
        if (pauseSettingsButtons == null)
            return;

        for (int i = 0; i < pauseSettingsButtons.Length; i++)
        {
            CacheButtonVisual(pauseSettingsButtons[i]);
        }
    }

    private void ClearCurrentPauseButtonHover()
    {
        if (currentHoveredPauseButton == null)
            return;

        RestoreButtonVisual(currentHoveredPauseButton);

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem != null &&
            eventSystem.currentSelectedGameObject == currentHoveredPauseButton.gameObject)
        {
            eventSystem.SetSelectedGameObject(null);
        }

        currentHoveredPauseButton = null;
    }

    private void RestorePauseButtonVisuals()
    {
        if (pauseButtons == null)
            return;

        for (int i = 0; i < pauseButtons.Length; i++)
        {
            RestoreButtonVisual(pauseButtons[i]);
        }
    }

    private void ClearCurrentPauseSettingsButtonHover()
    {
        if (currentHoveredPauseSettingsButton == null)
            return;

        RestoreButtonVisual(currentHoveredPauseSettingsButton);

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem != null &&
            eventSystem.currentSelectedGameObject == currentHoveredPauseSettingsButton.gameObject)
        {
            eventSystem.SetSelectedGameObject(null);
        }

        currentHoveredPauseSettingsButton = null;
    }

    private void RetryFromPauseMenu()
    {
        if (isPauseSceneActionRunning)
            return;

        isPauseSceneActionRunning = true;
        PlayClearButtonSelectSound();
        PreparePauseSceneLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void RestorePauseSettingsButtonVisuals()
    {
        if (pauseSettingsButtons == null)
            return;

        for (int i = 0; i < pauseSettingsButtons.Length; i++)
        {
            RestoreButtonVisual(pauseSettingsButtons[i]);
        }
    }

    private void OpenPauseSettings()
    {
        PlayClearButtonSelectSound();
        CreatePauseSettingsPanel();
        RefreshPauseSettingsValues();

        ClearCurrentPauseButtonHover();
        RestorePauseButtonVisuals();

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (pauseSettingsPanel != null)
            pauseSettingsPanel.SetActive(true);

        isPauseSettingsOpen = true;
        CachePauseSettingsButtonVisuals();
        SelectPauseSettingsButton(0);
    }

    private void ClosePauseSettings(bool playSound)
    {
        if (playSound)
            PlayClearButtonSelectSound();

        isPauseSettingsOpen = false;

        ClearCurrentPauseSettingsButtonHover();
        RestorePauseSettingsButtonVisuals();

        if (pauseSettingsPanel != null)
            pauseSettingsPanel.SetActive(false);

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        SelectPauseButton(2);
    }

    private void SelectPauseSettingsRow(int settingIndex)
    {
        if (pauseSettingsButtons == null || settingIndex < 0 || settingIndex >= pauseSettingsButtons.Length)
            return;

        SelectPauseSettingsButton(settingIndex);
    }

    private void ApplyPauseSettingValue(int settingIndex, float value)
    {
        value = Mathf.Clamp01(value);

        if (settingIndex == PauseSettingsBrightnessIndex)
            GameSettings.SetBrightness(value);
        else if (settingIndex == PauseSettingsMusicIndex)
            GameSettings.SetMusicVolume(value);
        else if (settingIndex == PauseSettingsSoundIndex)
            GameSettings.SetSoundVolume(value);

        UpdatePauseSettingsValueText(settingIndex, value);
    }

    private void RefreshPauseSettingsValues()
    {
        GameSettings.Load();

        SetPauseSettingsSliderValue(PauseSettingsBrightnessIndex, GameSettings.Brightness);
        SetPauseSettingsSliderValue(PauseSettingsMusicIndex, GameSettings.MusicVolume);
        SetPauseSettingsSliderValue(PauseSettingsSoundIndex, GameSettings.SoundVolume);
    }

    private void SetPauseSettingsSliderValue(int settingIndex, float value)
    {
        value = Mathf.Clamp01(value);

        if (pauseSettingsSliders != null &&
            settingIndex >= 0 &&
            settingIndex < pauseSettingsSliders.Length &&
            pauseSettingsSliders[settingIndex] != null)
        {
            pauseSettingsSliders[settingIndex].SetValueWithoutNotify(value);
        }

        UpdatePauseSettingsValueText(settingIndex, value);
    }

    private void UpdatePauseSettingsValueText(int settingIndex, float value)
    {
        if (pauseSettingsValueTexts == null ||
            settingIndex < 0 ||
            settingIndex >= pauseSettingsValueTexts.Length ||
            pauseSettingsValueTexts[settingIndex] == null)
        {
            return;
        }

        pauseSettingsValueTexts[settingIndex].text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }

    private void ReturnToMainMenuFromPause()
    {
        if (isPauseSceneActionRunning)
            return;

        isPauseSceneActionRunning = true;
        PlayClearButtonSelectSound();
        PreparePauseSceneLoad();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void PreparePauseSceneLoad()
    {
        isPausedByMenu = false;
        isPauseSettingsOpen = false;

        ClearCurrentPauseSettingsButtonHover();
        ClearCurrentPauseButtonHover();
        RestorePauseSettingsButtonVisuals();
        RestorePauseButtonVisuals();

        if (pauseSettingsPanel != null)
            pauseSettingsPanel.SetActive(false);

        Time.timeScale = 1f;
        SetGameplayBlocked(true);

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Playing);
    }

    private void SyncGameOverPanelState()
    {
        bool shouldShow = enableGameOverUI &&
                          GameStateManager.Instance != null &&
                          GameStateManager.Instance.CurrentState == GameState.Dead;

        if (shouldShow && !isGameOverPanelOpen)
        {
            ShowGameOverPanel();
            return;
        }

        if (!shouldShow && isGameOverPanelOpen)
            HideGameOverPanel();
    }

    private void ShowGameOverPanel()
    {
        if (stageClearPanel == null)
            return;

        isGameOverPanelOpen = true;
        isGameOverSceneActionRunning = false;

        CacheClearButtons();
        CacheStagePanelTextElements();
        ApplyGameOverPanelText();

        stageClearPanel.SetActive(true);
        SetGameplayBlocked(true);
        SelectClearButton(defaultSelectedGameOverButtonIndex);
    }

    private void HideGameOverPanel()
    {
        isGameOverPanelOpen = false;
        isGameOverSceneActionRunning = false;

        ClearCurrentClearButtonHover();
        RestoreAllButtonVisuals();
        RestoreStageClearPanelText();

        if (stageClearPanel != null)
            stageClearPanel.SetActive(false);
    }

    private void HandleGameOverKeyboardInput()
    {
        if (isGameOverSceneActionRunning)
            return;

        if (stageClearPanel == null || !stageClearPanel.activeInHierarchy)
            return;

        if (Input.GetKeyDown(gameOverUndoKey))
        {
            SelectClearButton(defaultSelectedGameOverButtonIndex);
            UndoFromGameOverPanel();
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveClearButtonSelection(-1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveClearButtonSelection(1);
            return;
        }

        if (Input.GetKeyDown(stageClearSubmitKey))
            SubmitSelectedClearButton();
    }

    public void ShowStageClear(string nextSceneName = "")
    {
        isGameOverPanelOpen = false;
        isGameOverSceneActionRunning = false;
        pendingNextSceneName = nextSceneName;
        RestoreStageClearPanelText();

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.StageClear);

        if (stageClearPanel != null)
            stageClearPanel.SetActive(true);

        SetGameplayBlocked(true);

        CacheClearButtons();
        SelectClearButton(defaultSelectedClearButtonIndex);
    }

    private void HandleStageClearKeyboardInput()
    {
        if (!enableStageClearKeyboardInput)
            return;

        if (stageClearPanel == null || !stageClearPanel.activeInHierarchy)
            return;

        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentState != GameState.StageClear)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveClearButtonSelection(-1);
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveClearButtonSelection(1);
            return;
        }

        if (Input.GetKeyDown(stageClearSubmitKey))
        {
            SubmitSelectedClearButton();
        }
    }

    private void CacheClearButtons()
    {
        List<Button> buttonList = new List<Button>();

        if (retryButton != null)
            buttonList.Add(retryButton);

        if (nextButton != null)
            buttonList.Add(nextButton);

        if (buttonList.Count > 0)
        {
            clearButtons = buttonList.ToArray();
        }
        else if (stageClearPanel != null)
        {
            clearButtons = stageClearPanel.GetComponentsInChildren<Button>(true);
        }
        else
        {
            clearButtons = new Button[0];
        }

        CacheAllButtonVisuals();
    }

    private void CacheAllButtonVisuals()
    {
        if (clearButtons == null)
            return;

        for (int i = 0; i < clearButtons.Length; i++)
        {
            CacheButtonVisual(clearButtons[i]);
        }
    }

    private void CacheStagePanelTextElements()
    {
        if (stageClearPanel == null)
            return;

        if (stageClearTitleText == null)
            stageClearTitleText = FindTMPTextByName(stageClearPanel.transform, "StageClearText");

        if (retryButtonText == null && retryButton != null)
            retryButtonText = retryButton.GetComponentInChildren<TMP_Text>(true);

        if (nextButtonText == null && nextButton != null)
            nextButtonText = nextButton.GetComponentInChildren<TMP_Text>(true);

        if (stageClearTitleText == null)
            stageClearTitleText = stageClearPanel.GetComponentInChildren<TMP_Text>(true);

        if (hasOriginalStagePanelText)
            return;

        originalStageClearTitleText = stageClearTitleText != null ? stageClearTitleText.text : "";
        originalRetryButtonText = retryButtonText != null ? retryButtonText.text : "";
        originalNextButtonText = nextButtonText != null ? nextButtonText.text : "";
        hasOriginalStagePanelText = true;
    }

    private TMP_Text FindTMPTextByName(Transform root, string objectName)
    {
        if (root == null)
            return null;

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == objectName)
                return texts[i];
        }

        return null;
    }

    private void ApplyGameOverPanelText()
    {
        CacheStagePanelTextElements();

        if (stageClearTitleText != null)
            stageClearTitleText.text = gameOverTitleText;

        if (retryButtonText != null)
            retryButtonText.text = gameOverUndoButtonText;

        if (nextButtonText != null)
            nextButtonText.text = gameOverRetryButtonText;
    }

    private void RestoreStageClearPanelText()
    {
        if (!hasOriginalStagePanelText)
            return;

        if (stageClearTitleText != null)
            stageClearTitleText.text = originalStageClearTitleText;

        if (retryButtonText != null)
            retryButtonText.text = originalRetryButtonText;

        if (nextButtonText != null)
            nextButtonText.text = originalNextButtonText;
    }

    private void MoveClearButtonSelection(int direction)
    {
        if (clearButtons == null || clearButtons.Length == 0)
            CacheClearButtons();

        if (clearButtons == null || clearButtons.Length == 0)
            return;

        int startIndex = selectedClearButtonIndex;

        if (startIndex < 0)
            startIndex = Mathf.Clamp(defaultSelectedClearButtonIndex, 0, clearButtons.Length - 1);

        for (int i = 1; i <= clearButtons.Length; i++)
        {
            int nextIndex = startIndex + direction * i;

            if (nextIndex < 0)
                nextIndex = clearButtons.Length - 1;

            if (nextIndex >= clearButtons.Length)
                nextIndex = 0;

            if (IsUsableButton(clearButtons[nextIndex]))
            {
                int previousIndex = selectedClearButtonIndex;

                SelectClearButton(nextIndex);

                if (nextIndex != previousIndex)
                    PlayClearButtonSwapSound();

                return;
            }
        }
    }

    private void SelectClearButton(int index)
    {
        if (clearButtons == null || clearButtons.Length == 0)
            CacheClearButtons();

        if (clearButtons == null || clearButtons.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, clearButtons.Length - 1);

        if (!IsUsableButton(clearButtons[index]))
        {
            bool foundUsableButton = false;

            for (int i = 0; i < clearButtons.Length; i++)
            {
                if (IsUsableButton(clearButtons[i]))
                {
                    index = i;
                    foundUsableButton = true;
                    break;
                }
            }

            if (!foundUsableButton)
                return;
        }

        Button targetButton = clearButtons[index];

        ClearCurrentClearButtonHover();

        selectedClearButtonIndex = index;
        currentHoveredClearButton = targetButton;

        CacheButtonVisual(targetButton);

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(targetButton.gameObject);
        }

        targetButton.Select();
        ApplyButtonHoverVisual(targetButton);
    }

    private void SubmitSelectedClearButton()
    {
        if (clearButtons == null || clearButtons.Length == 0)
            CacheClearButtons();

        if (clearButtons == null || clearButtons.Length == 0)
            return;

        if (selectedClearButtonIndex < 0 ||
            selectedClearButtonIndex >= clearButtons.Length ||
            !IsUsableButton(clearButtons[selectedClearButtonIndex]))
        {
            SelectClearButton(defaultSelectedClearButtonIndex);
            return;
        }

        Button selectedButton = clearButtons[selectedClearButtonIndex];

        if (!IsUsableButton(selectedButton))
            return;

        selectedButton.onClick.Invoke();
    }

    private void ClearCurrentClearButtonHover()
    {
        if (currentHoveredClearButton == null)
            return;

        RestoreButtonVisual(currentHoveredClearButton);

        EventSystem eventSystem = EventSystem.current;

        if (eventSystem != null &&
            eventSystem.currentSelectedGameObject == currentHoveredClearButton.gameObject)
        {
            eventSystem.SetSelectedGameObject(null);
        }

        currentHoveredClearButton = null;
    }

    private bool IsUsableButton(Button button)
    {
        return button != null &&
               button.gameObject.activeInHierarchy &&
               button.interactable;
    }

    private void CacheButtonVisual(Button button)
    {
        if (button == null)
            return;

        if (!originalButtonScales.ContainsKey(button))
            originalButtonScales.Add(button, button.transform.localScale);

        Graphic targetGraphic = GetButtonGraphic(button);

        if (targetGraphic != null && !originalButtonColors.ContainsKey(button))
            originalButtonColors.Add(button, targetGraphic.color);
    }

    private void ApplyButtonHoverVisual(Button button)
    {
        if (!useManualHoverVisual || button == null)
            return;

        if (UsesOverlayHover(button))
            return;

        CacheButtonVisual(button);

        Graphic targetGraphic = GetButtonGraphic(button);

        if (targetGraphic != null)
            targetGraphic.color = hoveredButtonColor;

        if (originalButtonScales.TryGetValue(button, out Vector3 originalScale))
            button.transform.localScale = originalScale * hoveredButtonScale;
    }

    private void RestoreButtonVisual(Button button)
    {
        if (!useManualHoverVisual || button == null)
            return;

        if (UsesOverlayHover(button))
            return;

        Graphic targetGraphic = GetButtonGraphic(button);

        if (targetGraphic != null)
        {
            if (originalButtonColors.TryGetValue(button, out Color originalColor))
                targetGraphic.color = originalColor;
        }

        if (originalButtonScales.TryGetValue(button, out Vector3 originalScale))
            button.transform.localScale = originalScale;
    }

    private bool UsesOverlayHover(Button button)
    {
        return button != null && button.GetComponent<StageButtonHoverEffect>() != null;
    }

    private void RestoreAllButtonVisuals()
    {
        if (clearButtons == null)
        {
            RestorePauseButtonVisuals();
            RestorePauseSettingsButtonVisuals();
            return;
        }

        for (int i = 0; i < clearButtons.Length; i++)
        {
            RestoreButtonVisual(clearButtons[i]);
        }

        RestorePauseButtonVisuals();
        RestorePauseSettingsButtonVisuals();
    }

    private Graphic GetButtonGraphic(Button button)
    {
        if (button == null)
            return null;

        if (button.targetGraphic != null)
            return button.targetGraphic;

        Graphic ownGraphic = button.GetComponent<Graphic>();

        if (ownGraphic != null)
            return ownGraphic;

        return button.GetComponentInChildren<Graphic>(true);
    }

    private void PlayClearButtonSwapSound()
    {
        if (AudioManager.I != null && AudioManager.I.sfxMenuBeep != null)
        {
            AudioManager.I.PlayOneShot(AudioManager.I.sfxMenuBeep, clearButtonSwapSoundVolume);
            return;
        }

        if (clearButtonSwapSound == null)
            return;

        EnsureClearButtonAudioSource();

        if (clearButtonAudioSource != null)
            clearButtonAudioSource.PlayOneShot(clearButtonSwapSound, GameSettings.ScaleSoundVolume(clearButtonSwapSoundVolume));
    }

    private void PlayClearButtonSelectSound()
    {
        AudioClip selectClip = clearButtonSelectSound;

        if (AudioManager.I != null && AudioManager.I.sfxMenuSelect != null)
            selectClip = AudioManager.I.sfxMenuSelect;

        AudioManager.PlayDetachedOneShot(selectClip, clearButtonSelectSoundVolume);
    }

    private void EnsureClearButtonAudioSource()
    {
        if (clearButtonAudioSource != null)
            return;

        clearButtonAudioSource = GetComponent<AudioSource>();

        if (clearButtonAudioSource == null)
            clearButtonAudioSource = gameObject.AddComponent<AudioSource>();
    }

    public void OnClickNext()
    {
        if (isGameOverPanelOpen)
        {
            RetryFromGameOverPanel();
            return;
        }

        PlayClearButtonSelectSound();

        if (TryGetExplicitNextSceneName(out string explicitSceneName))
        {
            string sceneName = explicitSceneName;
            PrepareForSceneLoad();

            Debug.Log($"다음 씬으로 이동: {sceneName}");
            SceneManager.LoadScene(explicitSceneName);

            return;
        }

        int currentIndex = SceneManager.GetActiveScene().buildIndex;

        if (currentIndex < 0)
        {
            Debug.LogWarning("현재 씬이 Build Settings에 등록되어 있지 않습니다.");
            return;
        }

        int nextIndex = currentIndex + 1;

        Debug.Log(
            $"현재 씬 index: {currentIndex}, 다음 씬 index: {nextIndex}, 등록된 씬 수: {SceneManager.sceneCountInBuildSettings}"
        );

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("다음 씬이 없습니다. Build Settings에 다음 스테이지 씬을 추가하세요.");
            return;
        }

        PrepareForSceneLoad();

        SceneManager.LoadScene(nextIndex);
    }

    private bool TryGetExplicitNextSceneName(out string sceneName)
    {
        sceneName = string.Empty;

        if (string.IsNullOrWhiteSpace(pendingNextSceneName))
            return false;

        sceneName = pendingNextSceneName.Trim();
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (sceneName == currentSceneName)
        {
            Debug.LogWarning($"Next scene '{sceneName}' is the current scene. Falling back to Build Settings order.");
            sceneName = string.Empty;
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"Next scene '{sceneName}' is not available in Build Settings. Falling back to Build Settings order.");
            sceneName = string.Empty;
            return false;
        }

        return true;
    }

    public void OnClickRetry()
    {
        if (isGameOverPanelOpen)
        {
            UndoFromGameOverPanel();
            return;
        }

        PlayClearButtonSelectSound();

        PrepareForSceneLoad();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void UndoFromGameOverPanel()
    {
        if (isGameOverSceneActionRunning)
            return;

        isGameOverSceneActionRunning = true;
        PlayClearButtonSelectSound();

        if (CommandManager.Instance == null || !CommandManager.Instance.UndoLastCommand())
        {
            isGameOverSceneActionRunning = false;
            Debug.LogWarning("No command is available to undo from Game Over.");
            return;
        }

        PlayerController player = FindFirstObjectByType<PlayerController>();

        if (player != null)
            player.ReviveAfterDeath();

        if (gameOverUndoRoutine != null)
            StopCoroutine(gameOverUndoRoutine);

        gameOverUndoRoutine = StartCoroutine(CompleteGameOverUndoAfterInputRelease());
    }

    private IEnumerator CompleteGameOverUndoAfterInputRelease()
    {
        yield return new WaitForEndOfFrame();

        while (Input.GetKey(stageClearSubmitKey) || Input.GetKey(gameOverUndoKey))
        {
            yield return null;
        }

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Playing);

        SetGameplayBlocked(false);
        HideGameOverPanel();

        gameOverUndoRoutine = null;
    }

    private void RetryFromGameOverPanel()
    {
        if (isGameOverSceneActionRunning)
            return;

        isGameOverSceneActionRunning = true;
        PlayClearButtonSelectSound();
        PrepareGameOverSceneLoad();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void PrepareGameOverSceneLoad()
    {
        isGameOverPanelOpen = false;

        ClearCurrentClearButtonHover();
        RestoreAllButtonVisuals();

        if (stageClearPanel != null)
            stageClearPanel.SetActive(false);

        RestoreStageClearPanelText();

        Time.timeScale = 1f;
        SetGameplayBlocked(true);

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Playing);
    }

    private void PrepareForSceneLoad()
    {
        ClearCurrentClearButtonHover();
        RestoreAllButtonVisuals();
        RestoreStageClearPanelText();

        SetGameplayBlocked(true);

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.StageClear);
    }

    private void SetGameplayBlocked(bool blocked)
    {
        if (playerController != null)
        {
            playerController.enabled = !blocked;

            PlayerCombat combat = playerController.GetComponent<PlayerCombat>();
            if (combat != null)
                combat.enabled = !blocked;
        }
    }

    public static bool IsGameplayInputBlocked(KeyCode key)
    {
        return gameplayInputBlockedUntilRelease.Contains(key);
    }

    private static void BlockGameplayInputUntilRelease(KeyCode key)
    {
        gameplayInputBlockedUntilRelease.Add(key);
    }

    private static void ReleaseGameplayInputBlocks()
    {
        gameplayInputBlockedUntilRelease.RemoveWhere(key => !Input.GetKey(key));
    }
}
