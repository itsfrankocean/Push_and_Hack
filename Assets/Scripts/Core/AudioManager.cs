using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    
    ///���� ���� �� ���Ե� ,, �ν����� â���� ���� �巡�� �ؼ� ������ �� 
    [Header("SFX Clips")]
    public AudioClip sfxWallHit;
    public AudioClip sfxBoxPush;
    public AudioClip sfxPlayerDeath;
    public AudioClip sfxElevatorOpen;
    public AudioClip sfxElevatorError;
    public AudioClip sfxPlayerFootstep;
    public AudioClip sfxPlateDown;
    public AudioClip sfxPlateUp;
    public AudioClip sfxCardKey;
    public AudioClip sfxCrateBreak;        // �� �°� �������� �μ��� ��

    [Header("Gun / Combat SFX")]
    public AudioClip sfxGunShoot;          // �� �� ��
    public AudioClip sfxMetalUnbreakable;  // �� �°� ö���� �� �μ��� ��

    public AudioClip sfxTeleport;

    [Header("SFX Source")]
    public AudioSource sfxSource;
    public AudioClip sfxWoodPush;          // �������� ������ �Ҹ�
    public AudioClip sfxWoodDisplacePush;
    public AudioClip sfxReverse;           // �÷��̾� �ڷ� ���ư� ��

    [Header("Aim Mode SFX")]
    public AudioClip sfxAimEnter;          // ��ݸ�� ����
    public AudioClip sfxAimRotate;         // ��ݸ�忡�� ���� �ٲ� ��
    public AudioClip sfxAimExit;           // ��ݸ�� ����

    [Header("Menu SFX")]
    public AudioClip sfxMenuBeep;
    public AudioClip sfxMenuSelect;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSource();
    }

    private void EnsureAudioSource()
    {
        if (sfxSource != null) return;

        sfxSource = GetComponent<AudioSource>();

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        EnsureAudioSource();

        if (sfxSource == null) return;

        sfxSource.pitch = Random.Range(0.95f, 1.05f);
        sfxSource.PlayOneShot(clip, GameSettings.ScaleSoundVolume(volume));
    }

    public static void PlayDetachedOneShot(AudioClip clip, float volume = 1f, float maxDuration = -1f)
    {
        if (clip == null) return;

        GameObject audioObject = new GameObject("Detached One Shot Audio");
        DontDestroyOnLoad(audioObject);

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.pitch = Random.Range(0.95f, 1.05f);
        source.PlayOneShot(clip, GameSettings.ScaleSoundVolume(volume));

        float destroyDelay = Mathf.Max(clip.length, 0.1f) + 0.1f;

        if (maxDuration > 0f)
            destroyDelay = Mathf.Max(Mathf.Min(clip.length, maxDuration), 0.01f);

        Destroy(audioObject, destroyDelay);
    }
}
