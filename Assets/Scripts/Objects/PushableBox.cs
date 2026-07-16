using UnityEngine;
using System.Collections;

public class PushableBox : MonoBehaviour, IDamageable, IProjectileDisplaceable
{
    private const string KeyCardTag = "KeyCard";

    [Header("설정")]
    public LayerMask whatStopsMovement;
    public float moveSpeed = 5f;

    [Header("충돌 체크")]
    public Vector2 moveCheckBoxSize = new Vector2(0.55f, 0.45f);

    [Header("점프 효과")]
    public Transform spriteTransform;
    public float jumpHeight = 0.5f;
    private Vector3 originalPos;
    private Vector3 targetPos;

    [Header("이펙트")]
    public ParticleSystem pushParticle;
    public GameObject hitAnimPrefab;
    public Vector3 hitEffectOffset = new Vector3(0f, 0f, 0f);

    [Header("Displace Afterimage")]
    public bool showDisplaceAfterimage = true;
    public float displaceAfterimageInterval = 0.04f;
    public float displaceAfterimageLifetime = 0.18f;
    [Range(0f, 1f)]
    public float displaceAfterimageAlpha = 0.45f;
    public Color displaceAfterimageTint = new Color(1f, 0.15f, 0.05f, 1f);

    public GameObject debrisPrefab;

    [Header("디버그")]
    public bool showMoveCheckBox = true;

    private bool isUndoing = false;
    private bool useMoveBounce = true;
    private bool useDisplaceAfterimage = false;
    private float nextDisplaceAfterimageTime = 0f;
    private Vector3 lastMoveDirection = Vector3.zero;
    private GameObject activeDebris;
    private SpriteRenderer cachedSpriteRenderer;

    private const float ARRIVE_THRESHOLD = 0.05f;
    private const float ARRIVE_THRESHOLD_SQR = ARRIVE_THRESHOLD * ARRIVE_THRESHOLD;

    public bool IsBusy
    {
        get
        {
            return (transform.position - targetPos).sqrMagnitude > ARRIVE_THRESHOLD_SQR;
        }
    }

    void Start()
    {
        targetPos = transform.position;

        if (spriteTransform == null)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) spriteTransform = sr.transform;
        }

        if (spriteTransform != null)
        {
            originalPos = spriteTransform.localPosition;
            cachedSpriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        }

        if (cachedSpriteRenderer == null)
            cachedSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        if (spriteTransform != null)
        {
            float dist = Vector3.Distance(transform.position, targetPos);

            if (dist <= 0.05f)
            {
                spriteTransform.localPosition = originalPos;
                isUndoing = false;
                useDisplaceAfterimage = false;
            }
            else
            {
                if (!isUndoing && useMoveBounce)
                {
                    float totalDistance = Mathf.Max(Vector3.Distance(transform.position, targetPos), 0.0001f);
                    float progress = 1f - Mathf.Clamp01(dist);
                    float yOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
                    spriteTransform.localPosition = originalPos + new Vector3(0f, yOffset, 0f);
                }
                else
                {
                    spriteTransform.localPosition = originalPos;
                }

                UpdateDisplaceAfterimage();
            }
        }
    }

    public virtual bool CanRestoreAfterDamage => true;

    public Vector3 GetCurrentTilePosition()
    {
        return targetPos;
    }

    public void WarpTo(Vector3 position)
    {
        transform.position = position;
        targetPos = position;
        isUndoing = false;
        useMoveBounce = true;
        useDisplaceAfterimage = false;
        lastMoveDirection = Vector3.zero;

        if (spriteTransform != null)
            spriteTransform.localPosition = originalPos;
    }

    public virtual void TakeDamage(int damage)
    {
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.15f, 0.2f);
        }

        // [사운드] 나무상자 파괴 시 crate_break 재생 
        if (AudioManager.I != null)
        {
            AudioManager.I.PlayOneShot(AudioManager.I.sfxCrateBreak, 1.5f);
        }

        Debug.Log("박스가 총에 맞아 부서짐!");

        if (debrisPrefab != null)
        {
            activeDebris = Instantiate(debrisPrefab, transform.position, Quaternion.identity);
        }

        gameObject.SetActive(false);
    }

    public void RestoreAfterDamage(Vector3 position)
    {
        if (activeDebris != null)
        {
            Destroy(activeDebris);
            activeDebris = null;
        }

        gameObject.SetActive(true);
        WarpTo(position);
    }

    public bool Move(Vector3 direction)
    {
        return Move(direction, true, true, false, GetPushSound());
    }

    private bool Move(Vector3 direction, bool playImpactEffects, bool bounceDuringMove, bool showAfterimage, AudioClip moveSound)
    {
        if (Vector3.Distance(transform.position, targetPos) > 0.05f)
            return false;

        if (!CanDisplace(direction))
            return false;

        isUndoing = false;
        useMoveBounce = bounceDuringMove;
        useDisplaceAfterimage = showAfterimage;
        nextDisplaceAfterimageTime = 0f;
        lastMoveDirection = direction;
        targetPos += direction;

        //[사운드] 나무상자 밀기 소리 
        if (AudioManager.I != null)
        {
            AudioManager.I.PlayOneShot(moveSound, 0.9f);
        }

        if (!playImpactEffects)
            return true;

        Quaternion hitRotation = Quaternion.identity;

        if (direction.x > 0) hitRotation = Quaternion.Euler(0, 180, 0);
        else if (direction.x < 0) hitRotation = Quaternion.Euler(0, 0, 0);
        else if (direction.y > 0) hitRotation = Quaternion.Euler(0, 0, -90);
        else if (direction.y < 0) hitRotation = Quaternion.Euler(0, 0, 90);

        if (pushParticle != null)
        {
            Vector3 centerOffset = spriteTransform != null ? spriteTransform.localPosition : Vector3.zero;
            pushParticle.transform.localPosition = centerOffset - (direction * 0.5f);
            pushParticle.transform.localRotation = hitRotation;
            pushParticle.Play();
        }

        if (hitAnimPrefab != null)
        {
            Vector3 centerOffset = spriteTransform != null ? spriteTransform.localPosition : Vector3.zero;
            Vector3 spawnPos = transform.position + centerOffset - (direction * 0.5f) + hitEffectOffset;
            GameObject effect = Instantiate(hitAnimPrefab, spawnPos, hitRotation);
            Destroy(effect, 0.3f);
        }

        return true;
    }

    private void UpdateDisplaceAfterimage()
    {
        if (!useDisplaceAfterimage || !showDisplaceAfterimage)
            return;

        if (cachedSpriteRenderer == null || cachedSpriteRenderer.sprite == null)
            return;

        if (Time.time < nextDisplaceAfterimageTime)
            return;

        nextDisplaceAfterimageTime = Time.time + Mathf.Max(0.01f, displaceAfterimageInterval);
        SpawnDisplaceAfterimage();
    }

    private void SpawnDisplaceAfterimage()
    {
        GameObject afterimage = new GameObject("DisplaceAfterimage");
        afterimage.transform.position = cachedSpriteRenderer.transform.position;
        afterimage.transform.rotation = cachedSpriteRenderer.transform.rotation;
        afterimage.transform.localScale = cachedSpriteRenderer.transform.lossyScale;

        SpriteRenderer afterimageRenderer = afterimage.AddComponent<SpriteRenderer>();
        afterimageRenderer.sprite = cachedSpriteRenderer.sprite;
        afterimageRenderer.flipX = cachedSpriteRenderer.flipX;
        afterimageRenderer.flipY = cachedSpriteRenderer.flipY;
        afterimageRenderer.sharedMaterial = cachedSpriteRenderer.sharedMaterial;
        afterimageRenderer.sortingLayerID = cachedSpriteRenderer.sortingLayerID;
        afterimageRenderer.sortingOrder = cachedSpriteRenderer.sortingOrder - 1;

        Color color = displaceAfterimageTint;
        color.a = displaceAfterimageAlpha;
        afterimageRenderer.color = color;

        StartCoroutine(FadeAndDestroyAfterimage(
            afterimageRenderer,
            Mathf.Max(0.01f, displaceAfterimageLifetime),
            color
        ));
    }

    private IEnumerator FadeAndDestroyAfterimage(SpriteRenderer afterimageRenderer, float lifetime, Color startColor)
    {
        if (afterimageRenderer == null)
            yield break;

        float elapsed = 0f;
        Transform afterimageTransform = afterimageRenderer.transform;
        Vector3 startScale = afterimageTransform.localScale;
        Vector3 endScale = startScale * 1.08f;

        while (elapsed < lifetime)
        {
            if (afterimageRenderer == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);

            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, t);
            afterimageRenderer.color = color;
            afterimageTransform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        if (afterimageRenderer != null)
            Destroy(afterimageRenderer.gameObject);
    }

    public bool CanDisplace(Vector3 direction)
    {
        if (IsBusy)
            return false;

        Vector3 checkPos = targetPos + direction;

        Collider2D hit = Physics2D.OverlapBox(checkPos, moveCheckBoxSize, 0f, whatStopsMovement);
        if (hit != null)
            return false;

        return !HasKeyCardAt(checkPos);
    }

    public bool Displace(Vector3 direction)
    {
        return Move(direction, false, false, true, GetDisplaceSound());
    }

    public SpriteRenderer GetDisplacementRenderer()
    {
        return GetComponentInChildren<SpriteRenderer>();
    }

    private bool HasKeyCardAt(Vector3 checkPos)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(checkPos, moveCheckBoxSize, 0f);

        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.CompareTag(KeyCardTag))
                return true;
        }

        return false;
    }

    protected virtual AudioClip GetPushSound()
    {
        return AudioManager.I != null ? AudioManager.I.sfxWoodPush : null;
    }

    protected virtual AudioClip GetDisplaceSound()
    {
        if (AudioManager.I == null)
            return null;

        return AudioManager.I.sfxWoodDisplacePush != null
            ? AudioManager.I.sfxWoodDisplacePush
            : GetPushSound();
    }

    public void UndoMove(Vector3 reverseDirection)
    {
        isUndoing = true;
        useMoveBounce = false;
        useDisplaceAfterimage = false;
        lastMoveDirection = reverseDirection;
        targetPos += reverseDirection;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showMoveCheckBox) return;

        Vector3 dir = lastMoveDirection;
        if (dir == Vector3.zero) return;

        Vector3 checkPos = Application.isPlaying ? transform.position + dir : transform.position + dir;

        Collider2D hit = Physics2D.OverlapBox(checkPos, moveCheckBoxSize, 0f, whatStopsMovement);
        Gizmos.color = hit != null ? Color.red : Color.green;
        Gizmos.DrawWireCube(checkPos, new Vector3(moveCheckBoxSize.x, moveCheckBoxSize.y, 0f));
    }
}
