using UnityEngine;

public class Projectile : MonoBehaviour
{
    public enum ProjectileEffect
    {
        Damage,
        SwapWithPlayer,
        PullBoxTowardShooter,
        DisplaceTarget
    }

    public float speed = 20f;
    public int damage = 1;
    public float lifetime = 2f;
    public LayerMask wallLayer;
    public ProjectileEffect effect = ProjectileEffect.Damage;

    [Header("Swap Teleport VFX")]
    public GameObject teleportVfxPrefab;
    public float teleportVfxLifetime = 0.6f;
    public Vector3 teleportVfxOffset = Vector3.zero;
    public bool forceTeleportVfxToFront = true;
    public int teleportVfxSortingOrderOffset = 10;

    private PlayerController ownerController;
    private ShootCommand ownerCommand;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer glowRenderer;
    private Color glowBaseColor;
    private float glowPulseTime;
    private float glowPulseAmount;
    private float glowBaseScale;

    public void Initialize(PlayerCombat ownerCombat, PlayerController ownerPlayerController, ShootCommand shootCommand = null)
    {
        ownerController = ownerPlayerController;
        ownerCommand = shootCommand;
    }

    private void Start()
    {
        SetupProjectileVisuals();
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
        UpdateProjectileGlow();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (effect == ProjectileEffect.SwapWithPlayer)
        {
            HandleSwapProjectileHit(collision);
            return;
        }

        if (effect == ProjectileEffect.PullBoxTowardShooter)
        {
            HandlePullProjectileHit(collision);
            return;
        }

        if (effect == ProjectileEffect.DisplaceTarget)
        {
            HandleDisplaceProjectileHit(collision);
            return;
        }

        IDamageable target = collision.GetComponent<IDamageable>();
        PushableBox targetBox = collision.GetComponentInParent<PushableBox>();

        if (target == null && targetBox != null)
            target = targetBox;

        if (target != null)
        {
            if (targetBox != null && targetBox.CanRestoreAfterDamage)
                ownerCommand?.RecordDestroyedBox(targetBox);

            target.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (IsWall(collision))
        {
            Debug.Log("Hit wall: " + collision.name);
            PlayBlockedSound();
            Destroy(gameObject);
        }
    }

    private void HandleSwapProjectileHit(Collider2D collision)
    {
        if (collision == null)
            return;

        PushableBox targetBox = collision.GetComponentInParent<PushableBox>();

        if (targetBox != null)
        {
            if (TrySwapPlayerWithBox(targetBox))
                Destroy(gameObject);

            return;
        }

        if (IsWall(collision))
        {
            PlayBlockedSound();
            Destroy(gameObject);
        }
    }

    private void HandlePullProjectileHit(Collider2D collision)
    {
        if (collision == null)
            return;

        PushableBox targetBox = collision.GetComponentInParent<PushableBox>();

        if (targetBox != null)
        {
            if (TryPullBoxTowardShooter(targetBox))
            {
                Destroy(gameObject);
                return;
            }

            PlayBlockedSound();
            Destroy(gameObject);
            return;
        }

        if (IsWall(collision))
        {
            PlayBlockedSound();
            Destroy(gameObject);
        }
    }

    private void HandleDisplaceProjectileHit(Collider2D collision)
    {
        if (collision == null)
            return;

        IProjectileDisplaceable target = ResolveDisplaceableTarget(collision);

        if (target != null)
        {
            if (TryStartDisplacementSelection(target))
                return;

            PlayBlockedSound();
            Destroy(gameObject);
            return;
        }

        if (IsWall(collision))
        {
            PlayBlockedSound();
            Destroy(gameObject);
        }
    }

    private bool TrySwapPlayerWithBox(PushableBox targetBox)
    {
        if (targetBox == null || targetBox.IsBusy)
            return false;

        if (ownerController == null)
            ownerController = FindFirstObjectByType<PlayerController>();

        if (ownerController == null)
            return false;

        if (ownerCommand != null && !ownerCommand.CanApplyProjectileEffect())
            return false;

        Vector3 playerPosition = ownerController.GetCurrentTilePosition();
        Vector3 boxPosition = targetBox.GetCurrentTilePosition();

        PlayTeleportVfx(playerPosition, ownerController.GetComponentInChildren<SpriteRenderer>());
        PlayTeleportVfx(boxPosition, targetBox.GetComponentInChildren<SpriteRenderer>());
        PlayTeleportSound();

        ownerController.WarpTo(new Vector3(boxPosition.x, boxPosition.y, playerPosition.z));
        targetBox.WarpTo(new Vector3(playerPosition.x, playerPosition.y, boxPosition.z));
        ownerCommand?.RecordTransformSwap(ownerController, targetBox, playerPosition, boxPosition);

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.08f, 0.08f);

        return true;
    }

    private bool TryPullBoxTowardShooter(PushableBox targetBox)
    {
        if (targetBox == null || targetBox.IsBusy)
            return false;

        if (ownerCommand != null && !ownerCommand.CanApplyProjectileEffect())
            return false;

        Vector3 boxPosition = targetBox.GetCurrentTilePosition();
        Vector3 pullDirection = GetCardinalDirection(-transform.right);

        if (pullDirection == Vector3.zero)
            return false;

        if (!targetBox.Move(pullDirection))
            return false;

        ownerCommand?.RecordPulledBox(targetBox, boxPosition);

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.08f, 0.08f);

        return true;
    }

    private Vector3 GetCardinalDirection(Vector3 direction)
    {
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            return direction.x >= 0f ? Vector3.right : Vector3.left;

        return direction.y >= 0f ? Vector3.up : Vector3.down;
    }

    private bool TryStartDisplacementSelection(IProjectileDisplaceable target)
    {
        if (target == null || target.IsBusy)
            return false;

        if (ownerCommand != null && !ownerCommand.CanApplyProjectileEffect())
            return false;

        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        speed = 0f;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = false;

        TrailRenderer trail = GetComponent<TrailRenderer>();
        if (trail != null)
            trail.emitting = false;

        return TargetDisplacementSelection.Begin(target, ownerCommand, gameObject);
    }

    private IProjectileDisplaceable ResolveDisplaceableTarget(Collider2D collision)
    {
        if (collision == null)
            return null;

        PushableBox box = collision.GetComponentInParent<PushableBox>();
        if (box != null)
            return box;

        PatrolEnemy enemy = collision.GetComponentInParent<PatrolEnemy>();
        if (enemy != null)
            return enemy;

        return null;
    }

    private void SetupProjectileVisuals()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        ProjectileVisualStyle visualStyle = GetVisualStyle();

        if (spriteRenderer != null)
            spriteRenderer.color = visualStyle.spriteColor;

        SetupTrail(visualStyle);
        SetupGlow(visualStyle);
    }

    private ProjectileVisualStyle GetVisualStyle()
    {
        switch (effect)
        {
            case ProjectileEffect.SwapWithPlayer:
                return new ProjectileVisualStyle(
                    new Color(0.45f, 0.24f, 1f, 1f),
                    new Color(0.75f, 0.2f, 1f, 0.55f),
                    new Color(0.18f, 0.04f, 0.8f, 0f),
                    new Color(0.62f, 0.18f, 1f, 0.32f),
                    0.18f,
                    0.13f,
                    1.55f,
                    0.11f
                );

            case ProjectileEffect.PullBoxTowardShooter:
                return new ProjectileVisualStyle(
                    new Color(0.1f, 0.9f, 1f, 1f),
                    new Color(0.0f, 0.92f, 1f, 0.62f),
                    new Color(0.0f, 0.22f, 1f, 0f),
                    new Color(0.0f, 0.78f, 1f, 0.34f),
                    0.22f,
                    0.16f,
                    1.75f,
                    0.14f
                );

            case ProjectileEffect.DisplaceTarget:
                return new ProjectileVisualStyle(
                    new Color(1f, 0.08f, 0.05f, 1f),
                    new Color(1f, 0.02f, 0.01f, 0.9f),
                    new Color(1f, 0.0f, 0.0f, 0f),
                    new Color(1f, 0.0f, 0.0f, 0.48f),
                    0.28f,
                    0.22f,
                    1.95f,
                    0.16f
                );

            default:
                return new ProjectileVisualStyle(
                    new Color(1f, 0.82f, 0.18f, 1f),
                    new Color(1f, 0.72f, 0.08f, 0.55f),
                    new Color(1f, 0.18f, 0.02f, 0f),
                    new Color(1f, 0.68f, 0.08f, 0.28f),
                    0.12f,
                    0.11f,
                    1.45f,
                    0.09f
                );
        }
    }

    private void SetupTrail(ProjectileVisualStyle visualStyle)
    {
        TrailRenderer trail = GetComponent<TrailRenderer>();

        if (trail == null)
            return;

        trail.enabled = true;
        trail.time = visualStyle.trailTime;
        trail.startWidth = visualStyle.trailStartWidth;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.02f;
        trail.startColor = visualStyle.trailStartColor;
        trail.endColor = visualStyle.trailEndColor;

        if (spriteRenderer != null)
        {
            trail.sortingLayerID = spriteRenderer.sortingLayerID;
            trail.sortingOrder = spriteRenderer.sortingOrder - 1;
        }
    }

    private void SetupGlow(ProjectileVisualStyle visualStyle)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        GameObject glowObject = new GameObject("ProjectileGlow");
        glowObject.transform.SetParent(transform, false);
        glowObject.transform.localPosition = Vector3.zero;
        glowObject.transform.localRotation = Quaternion.identity;
        glowObject.transform.localScale = new Vector3(visualStyle.glowScale, visualStyle.glowScale, 1f);

        glowRenderer = glowObject.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = spriteRenderer.sprite;
        glowRenderer.material = spriteRenderer.material;
        glowRenderer.flipX = spriteRenderer.flipX;
        glowRenderer.flipY = spriteRenderer.flipY;
        glowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        glowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        glowRenderer.color = visualStyle.glowColor;

        glowBaseColor = visualStyle.glowColor;
        glowBaseScale = visualStyle.glowScale;
        glowPulseAmount = visualStyle.glowPulseAmount;
    }

    private void UpdateProjectileGlow()
    {
        if (glowRenderer == null)
            return;

        glowPulseTime += Time.deltaTime * 18f;
        float pulse = glowBaseScale * (1f + Mathf.Sin(glowPulseTime) * glowPulseAmount);
        Color color = glowBaseColor;
        color.a = glowBaseColor.a * (0.82f + Mathf.Sin(glowPulseTime) * 0.18f);

        glowRenderer.transform.localScale = new Vector3(pulse, pulse, 1f);
        glowRenderer.color = color;
    }

    private void PlayTeleportVfx(Vector3 position, SpriteRenderer referenceRenderer)
    {
        if (teleportVfxPrefab == null)
            return;

        GameObject vfx = Instantiate(
            teleportVfxPrefab,
            position + teleportVfxOffset,
            Quaternion.identity
        );

        ApplyTeleportVfxSorting(vfx, referenceRenderer);

        Destroy(vfx, Mathf.Max(0.01f, teleportVfxLifetime));
    }

    private void ApplyTeleportVfxSorting(GameObject vfx, SpriteRenderer referenceRenderer)
    {
        if (vfx == null)
            return;

        SpriteRenderer[] renderers = vfx.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (forceTeleportVfxToFront)
            {
                renderer.sortingLayerName = "Player";
                renderer.sortingOrder = short.MaxValue;
                continue;
            }

            if (referenceRenderer == null)
                continue;

            renderer.sortingLayerID = referenceRenderer.sortingLayerID;
            renderer.sortingOrder = referenceRenderer.sortingOrder + teleportVfxSortingOrderOffset;
        }
    }

    private void PlayTeleportSound()
    {
        if (AudioManager.I != null)
            AudioManager.I.PlayOneShot(AudioManager.I.sfxTeleport, 1f);
    }

    private bool IsWall(Collider2D collision)
    {
        return collision != null &&
               ((1 << collision.gameObject.layer) & wallLayer) != 0;
    }

    private void PlayBlockedSound()
    {
        if (AudioManager.I != null)
            AudioManager.I.PlayOneShot(AudioManager.I.sfxMetalUnbreakable, 1f);
    }

    private readonly struct ProjectileVisualStyle
    {
        public readonly Color spriteColor;
        public readonly Color trailStartColor;
        public readonly Color trailEndColor;
        public readonly Color glowColor;
        public readonly float trailTime;
        public readonly float trailStartWidth;
        public readonly float glowScale;
        public readonly float glowPulseAmount;

        public ProjectileVisualStyle(
            Color spriteColor,
            Color trailStartColor,
            Color trailEndColor,
            Color glowColor,
            float trailTime,
            float trailStartWidth,
            float glowScale,
            float glowPulseAmount
        )
        {
            this.spriteColor = spriteColor;
            this.trailStartColor = trailStartColor;
            this.trailEndColor = trailEndColor;
            this.glowColor = glowColor;
            this.trailTime = trailTime;
            this.trailStartWidth = trailStartWidth;
            this.glowScale = glowScale;
            this.glowPulseAmount = glowPulseAmount;
        }
    }
}
