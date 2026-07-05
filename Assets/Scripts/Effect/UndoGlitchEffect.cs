using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class UndoGlitchEffect : MonoBehaviour
{
    public static UndoGlitchEffect Instance { get; private set; }

    [Header("Volume")]
    public Volume glitchVolume;

    [Header("Glitch Overlay")]
    public CanvasGroup overlayGroup;
    public RectTransform overlayRect;

    [Header("Rewind Icon")]
    public GameObject rewindIconRoot;
    public CanvasGroup rewindIconGroup;
    public RectTransform rewindIconRect;
    public float maxRewindIconAlpha = 1f;
    public float rewindIconMaxScale = 1.15f;

    [Header("Timing")]
    public float duration = 0.24f;
    public float maxVolumeWeight = 0.65f;
    public float maxOverlayAlpha = 0.75f;

    [Header("Overlay Jitter")]
    public float overlayJitterX = 45f;
    public float overlayJitterY = 10f;

    [Header("Camera Shake")]
    public bool useCameraShake = false;
    public float shakeDuration = 0.04f;
    public float shakeMagnitude = 0.025f;

    private Coroutine glitchRoutine;
    private float effectEndTime = 0f;
    private float effectLoopStartTime = 0f;

    private Vector2 overlayInitialAnchoredPosition;
    private Vector2 rewindIconInitialAnchoredPosition;
    private Vector3 rewindIconInitialScale = Vector3.one;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (overlayRect != null)
            overlayInitialAnchoredPosition = overlayRect.anchoredPosition;

        if (rewindIconRect != null)
        {
            rewindIconInitialAnchoredPosition = rewindIconRect.anchoredPosition;
            rewindIconInitialScale = rewindIconRect.localScale;
        }

        ResetEffect();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnDisable()
    {
        if (glitchRoutine != null)
        {
            StopCoroutine(glitchRoutine);
            glitchRoutine = null;
        }

        effectEndTime = 0f;
        ResetEffect();
    }

    public void Play()
    {
        PlayCameraShake();

        effectEndTime = Time.unscaledTime + duration;

        if (glitchRoutine != null)
            return;

        effectLoopStartTime = Time.unscaledTime;
        glitchRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (rewindIconRoot != null)
            rewindIconRoot.SetActive(true);

        if (rewindIconGroup != null)
            rewindIconGroup.alpha = 0f;

        float cycleDuration = Mathf.Max(0.0001f, duration);

        while (Time.unscaledTime < effectEndTime)
        {
            float elapsed = Time.unscaledTime - effectLoopStartTime;
            float t = Mathf.Repeat(elapsed, cycleDuration) / cycleDuration;

            // 전체 효과는 부드럽게 0 → 1 → 0
            float smoothPulse = Mathf.Sin(t * Mathf.PI);

            // Volume은 부드럽게만 변화
            if (glitchVolume != null)
                glitchVolume.weight = smoothPulse * maxVolumeWeight;

            // 글리치 오버레이만 랜덤 깜빡임
            if (overlayGroup != null)
            {
                float flicker = Random.value < 0.18f
                    ? Random.Range(0.05f, 0.25f)
                    : Random.Range(0.75f, 1.25f);

                overlayGroup.alpha = Mathf.Clamp01(
                    smoothPulse * maxOverlayAlpha * flicker
                );
            }

            // 글리치 오버레이만 위치 흔들림
            if (overlayRect != null)
            {
                float x = Random.Range(-overlayJitterX, overlayJitterX) * smoothPulse;
                float y = Random.Range(-overlayJitterY, overlayJitterY) * smoothPulse;

                overlayRect.anchoredPosition =
                    overlayInitialAnchoredPosition + new Vector2(x, y);
            }

            // 되감기 아이콘은 랜덤 글리치 없이 부드럽게 표시
            if (rewindIconGroup != null)
                rewindIconGroup.alpha = smoothPulse * maxRewindIconAlpha;

            if (rewindIconRect != null)
            {
                float scale = Mathf.Lerp(1f, rewindIconMaxScale, smoothPulse);

                rewindIconRect.localScale = rewindIconInitialScale * scale;
                rewindIconRect.anchoredPosition = rewindIconInitialAnchoredPosition;
            }

            yield return null;
        }

        ResetEffect();
        glitchRoutine = null;
        effectEndTime = 0f;
    }

    private void PlayCameraShake()
    {
        if (!useCameraShake)
            return;

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(shakeDuration, shakeMagnitude);
    }

    private void ResetEffect()
    {
        if (glitchVolume != null)
            glitchVolume.weight = 0f;

        if (overlayGroup != null)
            overlayGroup.alpha = 0f;

        if (overlayRect != null)
            overlayRect.anchoredPosition = overlayInitialAnchoredPosition;

        if (rewindIconGroup != null)
            rewindIconGroup.alpha = 0f;

        if (rewindIconRect != null)
        {
            rewindIconRect.anchoredPosition = rewindIconInitialAnchoredPosition;
            rewindIconRect.localScale = rewindIconInitialScale;
        }

        if (rewindIconRoot != null)
            rewindIconRoot.SetActive(false);
    }
}
