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

    public GameObject debrisPrefab;

    [Header("디버그")]
    public bool showMoveCheckBox = true;

    private bool isUndoing = false;
    private Vector3 lastMoveDirection = Vector3.zero;
    private GameObject activeDebris;

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
            originalPos = spriteTransform.localPosition;
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
            }
            else
            {
                if (!isUndoing)
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
        if (Vector3.Distance(transform.position, targetPos) > 0.05f)
            return false;

        if (!CanDisplace(direction))
            return false;

        isUndoing = false;
        lastMoveDirection = direction;
        targetPos += direction;

        //[사운드] 나무상자 밀기 소리 
        if (AudioManager.I != null)
        {
            AudioManager.I.PlayOneShot(GetPushSound(), 0.9f);
        }


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
        return Move(direction);
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

    public void UndoMove(Vector3 reverseDirection)
    {
        isUndoing = true;
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
