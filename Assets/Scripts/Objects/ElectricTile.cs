using UnityEngine;

public class ElectricTile : MonoBehaviour
{
    [Header("이미지 설정")]
    public Sprite disabledSprite;
    private Sprite enabledSprite;

    [Header("전기 상태")]
    public bool isActive = true;

    private Animator anim;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            enabledSprite = spriteRenderer.sprite;
        }
    }

    public void SetElectricState(bool state)
    {
        isActive = state;

        if (anim != null)
            anim.enabled = state;

        if (col != null)
            col.enabled = state;

        if (spriteRenderer != null)
        {
            if (state)
            {
                spriteRenderer.sprite = enabledSprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                if (disabledSprite != null)
                    spriteRenderer.sprite = disabledSprite;

                spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        if (!other.CompareTag("Player")) return;

        Debug.Log("플레이어가 감전되었습니다!");
        AudioManager.I.PlayOneShot(AudioManager.I.sfxPlayerDeath, 1f);

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.DieFromElectric();
        }
        else
        {
            Debug.LogError("PlayerController를 찾지 못했습니다!");
        }
    }
}