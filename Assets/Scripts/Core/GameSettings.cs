using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameSettings
{
    private const string BrightnessKey = "Settings.Brightness";
    private const string MusicVolumeKey = "Settings.MusicVolume";
    private const string SoundVolumeKey = "Settings.SoundVolume";

    private static Canvas brightnessCanvas;
    private static Image brightnessOverlay;

    public static float Brightness { get; private set; } = 1f;
    public static float MusicVolume { get; private set; } = 1f;
    public static float SoundVolume { get; private set; } = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        Load();
        EnsureBrightnessOverlay();
        ApplyAll();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureBrightnessOverlay();
        ApplyAll();
    }

    public static void Load()
    {
        Brightness = PlayerPrefs.GetFloat(BrightnessKey, 1f);
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SoundVolume = PlayerPrefs.GetFloat(SoundVolumeKey, 1f);
    }

    public static void SetBrightness(float value)
    {
        Brightness = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(BrightnessKey, Brightness);
        PlayerPrefs.Save();
        ApplyBrightness();
    }

    public static void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    public static void SetSoundVolume(float value)
    {
        SoundVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SoundVolumeKey, SoundVolume);
        PlayerPrefs.Save();
        ApplySoundVolume();
    }

    public static float ScaleSoundVolume(float volume)
    {
        return Mathf.Max(0f, volume) * SoundVolume;
    }

    public static void ApplyAll()
    {
        ApplyBrightness();
        ApplyMusicVolume();
        ApplySoundVolume();
    }

    private static void ApplyBrightness()
    {
        EnsureBrightnessOverlay();

        if (brightnessOverlay == null)
            return;

        float darkness = (1f - Brightness) * 0.72f;
        brightnessOverlay.color = new Color(0f, 0f, 0f, darkness);
        brightnessOverlay.raycastTarget = false;
    }

    private static void ApplyMusicVolume()
    {
        AudioSource[] sources = Object.FindObjectsOfType<AudioSource>();

        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];

            if (source == null || IsSoundEffectSource(source))
                continue;

            source.volume = MusicVolume;
        }
    }

    private static void ApplySoundVolume()
    {
        if (AudioManager.I != null && AudioManager.I.sfxSource != null)
            AudioManager.I.sfxSource.volume = 1f;
    }

    private static bool IsSoundEffectSource(AudioSource source)
    {
        if (source == null)
            return true;

        if (AudioManager.I != null && source == AudioManager.I.sfxSource)
            return true;

        return source.gameObject.name.Contains("Detached One Shot Audio");
    }

    private static void EnsureBrightnessOverlay()
    {
        if (brightnessOverlay != null)
            return;

        GameObject canvasObject = new GameObject("BrightnessOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Object.DontDestroyOnLoad(canvasObject);

        brightnessCanvas = canvasObject.GetComponent<Canvas>();
        brightnessCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        brightnessCanvas.sortingOrder = 32000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject overlayObject = new GameObject("BrightnessOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        brightnessOverlay = overlayObject.GetComponent<Image>();
        brightnessOverlay.raycastTarget = false;
    }
}
