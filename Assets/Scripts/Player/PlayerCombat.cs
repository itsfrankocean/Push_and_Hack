using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    public enum PlayerState
    {
        Normal,
        Aiming
    }

    [Header("플레이어 상태")]
    public PlayerState currentState = PlayerState.Normal;

    [Header("스프라이트 설정")]
    public SpriteRenderer playerSpriteRenderer;
    public Sprite readySprite;
    public Sprite idleSprite;

    [Header("상하 조준 스프라이트")]
    public Sprite readyUpSprite;
    public Sprite readyDownSprite;

    [Header("조준 설정")]
    public float maxRayDistance = 1000f;
    public LayerMask hittableLayers;
    public Color highlightColor = Color.cyan;

    [Header("발사체 설정")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public bool requireAmmoToShoot = true;

    [Header("탄약 UI")]
    public AmmoPopupUI ammoPopupUI;

    [Header("효과")]
    public LineRenderer aimLine;
    public Color aimLineColor = Color.red;
    public GameObject muzzleFlashObject;
    public float flashDuration = 0.05f;

    [Header("사격 연출")]
    public float shootShakeDuration = 0.04f;
    public float shootShakeMagnitude = 0.08f;
    public float recoilDistance = 0.08f;
    public float recoilKickTime = 0.035f;
    public float recoilReturnTime = 0.055f;
    public Vector2 muzzleFlashScaleRange = new Vector2(0.9f, 1.35f);
    public float muzzleFlashRandomRotation = 18f;
    public float tracerDuration = 0.055f;
    public float tracerWidth = 0.06f;
    public Color tracerColor = new Color(1f, 0.12f, 0.02f, 0.95f);

    private bool isShooting = false;

    private readonly Vector3 MuzzlePosRight = new Vector3(0.398f, 0.132f, 0f);
    private readonly Vector3 MuzzlePosLeft = new Vector3(-0.444f, 0.132f, 0f);
    private readonly Vector3 MuzzlePosDown = new Vector3(-0.034f, -0.422f, 0f);
    private readonly Vector3 MuzzlePosUp = new Vector3(-0.034f, 0.653f, 0f);

    private Vector2 lookDirection = Vector2.right;

    private GameObject currentHighlightedObject;
    private SpriteRenderer currentHighlightedRenderer;
    private readonly Dictionary<GameObject, Color> originalColors = new Dictionary<GameObject, Color>();

    private Animator playerAnimator;
    private PlayerController playerController;
    private MaterialPropertyBlock aimLinePropertyBlock;
    private Gradient aimLineGradient;
    private readonly GradientColorKey[] aimLineColorKeys = new GradientColorKey[2];
    private readonly GradientAlphaKey[] aimLineAlphaKeys = new GradientAlphaKey[2];
    private Coroutine muzzleFlashRoutine;
    private Coroutine recoilRoutine;
    private Coroutine tracerRoutine;
    private LineRenderer shotTracerLine;
    private Material shotTracerMaterial;
    private Vector3 muzzleFlashOriginalScale = Vector3.one;

    public bool UsesAmmo => requireAmmoToShoot;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();

        if (playerSpriteRenderer != null)
            playerAnimator = playerSpriteRenderer.GetComponent<Animator>();

        if (aimLine != null)
        {
            SetLineAlpha(1f);
            aimLine.enabled = false;
        }

        if (muzzleFlashObject != null)
        {
            muzzleFlashOriginalScale = muzzleFlashObject.transform.localScale;
            muzzleFlashObject.SetActive(false);
        }

        if (playerSpriteRenderer != null && idleSprite != null)
            playerSpriteRenderer.sprite = idleSprite;
    }

    private void Update()
    {
        transform.rotation = Quaternion.identity;

        if (TargetDisplacementSelection.ShouldBlockCombatInput)
            return;

        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentState == GameState.Paused)
        {
            return;
        }

        if (!CanUseCombatInput())
        {
            if (currentState == PlayerState.Aiming)
                ExitAimingMode(false);

            return;
        }

        switch (currentState)
        {
            case PlayerState.Normal:
                HandleNormalMovementDirection();

                if (IsGameplayKeyDown(KeyCode.Z))
                    EnterAimingMode();

                break;

            case PlayerState.Aiming:
                if (!isShooting)
                {
                    HandleAimingState();
                }
                else
                {
                    Vector2 arrowDirection = ReadArrowAimDirection();

                    if (arrowDirection != Vector2.zero)
                        lookDirection = arrowDirection;

                    UpdatePlayerAppearance();

                    if (aimLine != null)
                        aimLine.enabled = false;

                    ClearHighlight();
                }

                if (IsGameplayKeyDown(KeyCode.X))
                    ExitAimingMode();

                break;
        }
    }

    private bool IsGameplayKeyDown(KeyCode key)
    {
        return !StageUIController.IsGameplayInputBlocked(key) &&
               Input.GetKeyDown(key);
    }

    private bool CanUseCombatInput()
    {
        return GameStateManager.Instance == null ||
               GameStateManager.Instance.CanCombatInput;
    }

    private void HandleNormalMovementDirection()
    {
        Vector2 arrowDirection = ReadArrowAimDirection();

        if (arrowDirection != Vector2.zero)
            lookDirection = arrowDirection;
    }

    private void HandleAimingState()
    {
        Vector2 arrowDirection = ReadArrowAimDirection();

        if (arrowDirection != Vector2.zero)
            lookDirection = arrowDirection;

        UpdateAimingSystem();

        if (IsGameplayKeyDown(KeyCode.Z) && !isShooting)
        {
            TryExecuteShootCommand();
        }
    }

    private void TryExecuteShootCommand()
    {
        if (CommandManager.Instance == null)
        {
            Debug.LogWarning("CommandManager.Instance가 없습니다.");
            return;
        }

        CommandManager.Instance.ExecuteCommand(new ShootCommand(this));
    }

    private Vector2 ReadArrowAimDirection()
    {
        bool left = Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.RightArrow);
        bool up = Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.DownArrow);

        if (left != right)
            return left ? Vector2.left : Vector2.right;

        if (up != down)
            return up ? Vector2.up : Vector2.down;

        return Vector2.zero;
    }

    public bool CanStartShootCommand()
    {
        if (currentState != PlayerState.Aiming)
            return false;

        if (isShooting)
            return false;

        if (TurnManager.Instance != null && TurnManager.Instance.IsBusy)
            return false;

        if (firePoint == null)
        {
            Debug.LogWarning("firePoint가 설정되어 있지 않습니다.");
            return false;
        }

        if (GetProjectilePrefabForCurrentAmmo() == null)
        {
            Debug.LogWarning("현재 탄약의 Projectile Prefab이 설정되어 있지 않습니다.");
            return false;
        }

        return true;
    }

    public int GetCurrentAmmoIndexForUndo()
    {
        if (ammoPopupUI == null)
            return -1;

        return ammoPopupUI.GetCurrentAmmoIndex();
    }

    public bool TryConsumeAmmoForShot()
    {
        if (!requireAmmoToShoot)
            return true;

        if (ammoPopupUI == null)
        {
            Debug.LogWarning("PlayerCombat에 AmmoPopupUI가 설정되어 있지 않습니다.");
            return false;
        }

        if (!ammoPopupUI.TryUseCurrentAmmo(1))
        {
            Debug.Log("선택한 탄약이 부족합니다.");
            return false;
        }

        return true;
    }

    public void RestoreAmmoForUndo(int ammoIndex, int amount)
    {
        if (!requireAmmoToShoot)
            return;

        if (ammoPopupUI == null)
            return;

        ammoPopupUI.AddAmmo(ammoIndex, amount);
    }

    public bool TryStartShootSequence(ShootCommand shootCommand = null)
    {
        if (!CanStartShootCommand())
            return false;

        StartCoroutine(ShootSequence(shootCommand));
        return true;
    }

    private GameObject GetProjectilePrefabForCurrentAmmo()
    {
        GameObject selectedProjectile = null;

        if (ammoPopupUI != null)
            selectedProjectile = ammoPopupUI.GetCurrentProjectilePrefab();

        if (selectedProjectile != null)
            return selectedProjectile;

        return bulletPrefab;
    }

    private IEnumerator ShootSequence(ShootCommand shootCommand)
    {
        isShooting = true;

        // 사격 사운드와 카메라 흔들림
        if (AudioManager.I != null)
            AudioManager.I.PlayOneShot(AudioManager.I.sfxGunShoot);

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(shootShakeDuration, shootShakeMagnitude);
        }

        SetLineAlpha(0f);

        if (aimLine != null)
            aimLine.enabled = false;

        ClearHighlight();

        if (muzzleFlashObject != null && firePoint != null)
        {
            muzzleFlashObject.transform.position = firePoint.position;

            float flashAngle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
            muzzleFlashObject.transform.rotation = Quaternion.Euler(0f, 0f, flashAngle);

            if (muzzleFlashRoutine != null)
                StopCoroutine(muzzleFlashRoutine);

            muzzleFlashRoutine = StartCoroutine(ShowMuzzleFlash(flashAngle));
        }

        PlayRecoil();

        GameObject projectilePrefab = GetProjectilePrefabForCurrentAmmo();

        if (projectilePrefab != null && firePoint != null)
        {
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
            PlayShotTracer(firePoint.position, GetShotTracerEndPoint());

            GameObject bullet = Instantiate(
                projectilePrefab,
                firePoint.position,
                Quaternion.Euler(0f, 0f, angle)
            );

            if (shootCommand != null)
                shootCommand.RegisterProjectile(bullet);

            Projectile projectile = bullet.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Initialize(this, playerController, shootCommand);

            float timer = 0f;

            while (bullet != null && timer < 2f)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            Debug.LogWarning("Projectile Prefab 또는 firePoint가 설정되어 있지 않습니다.");
        }

        isShooting = false;

        if (currentState == PlayerState.Aiming && aimLine != null)
        {
            aimLine.enabled = true;

            float duration = 0.1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                SetLineAlpha(alpha);

                yield return null;
            }

            SetLineAlpha(1f);
        }
    }

    private void SetLineAlpha(float alpha)
    {
        if (aimLine == null)
            return;

        Color color = GetAimLineColor(alpha);

        aimLine.startColor = color;
        aimLine.endColor = color;

        if (aimLineGradient == null)
            aimLineGradient = new Gradient();

        aimLineColorKeys[0] = new GradientColorKey(color, 0f);
        aimLineColorKeys[1] = new GradientColorKey(color, 1f);
        aimLineAlphaKeys[0] = new GradientAlphaKey(alpha, 0f);
        aimLineAlphaKeys[1] = new GradientAlphaKey(alpha, 1f);

        aimLineGradient.SetKeys(aimLineColorKeys, aimLineAlphaKeys);
        aimLine.colorGradient = aimLineGradient;

        if (aimLinePropertyBlock == null)
            aimLinePropertyBlock = new MaterialPropertyBlock();

        aimLine.GetPropertyBlock(aimLinePropertyBlock);
        aimLinePropertyBlock.SetColor("_BaseColor", color);
        aimLinePropertyBlock.SetColor("_Color", color);
        aimLinePropertyBlock.SetColor("_RendererColor", color);
        aimLine.SetPropertyBlock(aimLinePropertyBlock);

        Material material = aimLine.material;
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        if (material.HasProperty("_RendererColor"))
            material.SetColor("_RendererColor", color);
    }

    private Color GetAimLineColor(float alpha)
    {
        Color color = aimLineColor;

        if (IsWhite(color) || IsClearBlack(color))
            color = Color.red;

        color.a = alpha;
        return color;
    }

    private bool IsWhite(Color color)
    {
        return Mathf.Approximately(color.r, 1f) &&
               Mathf.Approximately(color.g, 1f) &&
               Mathf.Approximately(color.b, 1f);
    }

    private bool IsClearBlack(Color color)
    {
        return Mathf.Approximately(color.r, 0f) &&
               Mathf.Approximately(color.g, 0f) &&
               Mathf.Approximately(color.b, 0f) &&
               Mathf.Approximately(color.a, 0f);
    }

    private void UpdatePlayerAppearance()
    {
        if (playerSpriteRenderer == null || firePoint == null)
            return;

        if (lookDirection.y > 0.5f)
        {
            if (readyUpSprite != null)
                playerSpriteRenderer.sprite = readyUpSprite;

            playerSpriteRenderer.flipX = false;
            firePoint.localPosition = MuzzlePosUp;
        }
        else if (lookDirection.y < -0.5f)
        {
            if (readyDownSprite != null)
                playerSpriteRenderer.sprite = readyDownSprite;

            playerSpriteRenderer.flipX = false;
            firePoint.localPosition = MuzzlePosDown;
        }
        else if (lookDirection.x < 0f)
        {
            if (readySprite != null)
                playerSpriteRenderer.sprite = readySprite;

            playerSpriteRenderer.flipX = true;
            firePoint.localPosition = MuzzlePosLeft;
        }
        else
        {
            if (readySprite != null)
                playerSpriteRenderer.sprite = readySprite;

            playerSpriteRenderer.flipX = false;
            firePoint.localPosition = MuzzlePosRight;
        }
    }

    private void UpdateAimingSystem()
    {
        UpdatePlayerAppearance();

        if (aimLine == null || isShooting || firePoint == null)
            return;

        SetLineAlpha(1f);
        aimLine.SetPosition(0, firePoint.position);

        RaycastHit2D hit = Physics2D.Raycast(
            firePoint.position,
            lookDirection,
            maxRayDistance,
            hittableLayers
        );

        if (hit.collider != null)
        {
            aimLine.SetPosition(1, hit.point);
            HandleHighlight(hit.collider);
        }
        else
        {
            Vector2 farPoint = (Vector2)firePoint.position + lookDirection * maxRayDistance;
            aimLine.SetPosition(1, farPoint);
            ClearHighlight();
        }

        if (muzzleFlashObject != null)
        {
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
            muzzleFlashObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private IEnumerator ShowMuzzleFlash(float baseAngle)
    {
        if (muzzleFlashObject == null)
            yield break;

        float scale = Random.Range(muzzleFlashScaleRange.x, muzzleFlashScaleRange.y);
        float angleOffset = Random.Range(-muzzleFlashRandomRotation, muzzleFlashRandomRotation);

        muzzleFlashObject.transform.localScale = muzzleFlashOriginalScale * scale;
        muzzleFlashObject.transform.rotation = Quaternion.Euler(0f, 0f, baseAngle + angleOffset);
        muzzleFlashObject.SetActive(true);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, flashDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float pulse = Mathf.Lerp(scale, 0.15f, t);
            muzzleFlashObject.transform.localScale = muzzleFlashOriginalScale * pulse;

            yield return null;
        }

        muzzleFlashObject.SetActive(false);
        muzzleFlashObject.transform.localScale = muzzleFlashOriginalScale;
        muzzleFlashRoutine = null;
    }

    private void PlayRecoil()
    {
        if (playerSpriteRenderer == null)
            return;

        if (recoilRoutine != null)
            StopCoroutine(recoilRoutine);

        recoilRoutine = StartCoroutine(RecoilRoutine());
    }

    private IEnumerator RecoilRoutine()
    {
        Transform target = playerSpriteRenderer.transform;
        Vector3 basePosition = target.localPosition;
        Vector3 recoilPosition = basePosition - (Vector3)(lookDirection.normalized * recoilDistance);

        float elapsed = 0f;
        float kickDuration = Mathf.Max(0.001f, recoilKickTime);

        while (elapsed < kickDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / kickDuration);
            target.localPosition = Vector3.Lerp(basePosition, recoilPosition, t);
            yield return null;
        }

        elapsed = 0f;
        float returnDuration = Mathf.Max(0.001f, recoilReturnTime);

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            target.localPosition = Vector3.Lerp(recoilPosition, basePosition, t);
            yield return null;
        }

        target.localPosition = basePosition;
        recoilRoutine = null;
    }

    private Vector3 GetShotTracerEndPoint()
    {
        if (firePoint == null)
            return transform.position;

        RaycastHit2D hit = Physics2D.Raycast(
            firePoint.position,
            lookDirection,
            maxRayDistance,
            hittableLayers
        );

        if (hit.collider != null)
            return hit.point;

        return (Vector2)firePoint.position + lookDirection * maxRayDistance;
    }

    private void PlayShotTracer(Vector3 start, Vector3 end)
    {
        EnsureShotTracerLine();

        if (shotTracerLine == null)
            return;

        if (tracerRoutine != null)
            StopCoroutine(tracerRoutine);

        tracerRoutine = StartCoroutine(ShotTracerRoutine(start, end));
    }

    private IEnumerator ShotTracerRoutine(Vector3 start, Vector3 end)
    {
        shotTracerLine.positionCount = 2;
        shotTracerLine.SetPosition(0, start);
        shotTracerLine.SetPosition(1, end);
        shotTracerLine.startWidth = tracerWidth;
        shotTracerLine.endWidth = tracerWidth * 0.25f;
        shotTracerLine.enabled = true;

        float elapsed = 0f;
        float duration = Mathf.Max(0.001f, tracerDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(tracerColor.a, 0f, elapsed / duration);
            SetShotTracerColor(alpha);
            yield return null;
        }

        shotTracerLine.enabled = false;
        tracerRoutine = null;
    }

    private void EnsureShotTracerLine()
    {
        if (shotTracerLine != null)
            return;

        GameObject tracerObject = new GameObject("ShotTracerLine");
        tracerObject.transform.SetParent(transform, false);

        shotTracerLine = tracerObject.AddComponent<LineRenderer>();
        shotTracerLine.useWorldSpace = true;
        shotTracerLine.positionCount = 2;
        shotTracerLine.numCapVertices = 2;
        shotTracerLine.numCornerVertices = 0;
        shotTracerLine.textureMode = LineTextureMode.Stretch;
        shotTracerLine.sortingLayerID = playerSpriteRenderer != null
            ? playerSpriteRenderer.sortingLayerID
            : 0;
        shotTracerLine.sortingOrder = playerSpriteRenderer != null
            ? playerSpriteRenderer.sortingOrder + 2
            : 10;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader != null)
        {
            shotTracerMaterial = new Material(shader);
            shotTracerLine.material = shotTracerMaterial;
        }

        SetShotTracerColor(tracerColor.a);
        shotTracerLine.enabled = false;
    }

    private void SetShotTracerColor(float alpha)
    {
        if (shotTracerLine == null)
            return;

        Color color = tracerColor;
        color.a = alpha;

        shotTracerLine.startColor = color;
        shotTracerLine.endColor = new Color(color.r, color.g, color.b, 0f);

        if (shotTracerMaterial == null)
            return;

        if (shotTracerMaterial.HasProperty("_Color"))
            shotTracerMaterial.SetColor("_Color", color);

        if (shotTracerMaterial.HasProperty("_BaseColor"))
            shotTracerMaterial.SetColor("_BaseColor", color);
    }

    private void HandleHighlight(Collider2D hitCollider)
    {
        if (!TryResolveHighlightTarget(hitCollider, out GameObject target, out SpriteRenderer sr))
        {
            ClearHighlight();
            return;
        }

        if (currentHighlightedObject == target && currentHighlightedRenderer == sr)
            return;

        ClearHighlight();

        currentHighlightedObject = target;
        currentHighlightedRenderer = sr;

        if (!originalColors.ContainsKey(target))
            originalColors.Add(target, sr.color);

        sr.color = highlightColor;
    }

    private void ClearHighlight()
    {
        if (currentHighlightedObject == null)
            return;

        if (currentHighlightedRenderer != null && originalColors.ContainsKey(currentHighlightedObject))
        {
            currentHighlightedRenderer.color = originalColors[currentHighlightedObject];
        }

        currentHighlightedObject = null;
        currentHighlightedRenderer = null;
    }

    private bool TryResolveHighlightTarget(Collider2D hitCollider, out GameObject target, out SpriteRenderer spriteRenderer)
    {
        target = null;
        spriteRenderer = null;

        if (hitCollider == null)
            return false;

        PushableBox pushableBox = hitCollider.GetComponentInParent<PushableBox>();
        if (pushableBox != null)
        {
            target = pushableBox.gameObject;
            spriteRenderer = pushableBox.GetComponentInChildren<SpriteRenderer>();
            return spriteRenderer != null;
        }

        if (hitCollider.attachedRigidbody != null)
            target = hitCollider.attachedRigidbody.gameObject;
        else
            target = hitCollider.gameObject;

        spriteRenderer = target.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
            spriteRenderer = hitCollider.GetComponentInChildren<SpriteRenderer>();

        return spriteRenderer != null;
    }

    private void EnterAimingMode()
    {
        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentState != GameState.Playing)
        {
            return;
        }

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (playerController != null && playerController.IsMoving)
            return;

        if (AudioManager.I != null)
            AudioManager.I.PlayOneShot(AudioManager.I.sfxAimEnter, 1f);

        currentState = PlayerState.Aiming;
        isShooting = false;

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Aiming);

        if (aimLine != null)
        {
            SetLineAlpha(1f);
            aimLine.enabled = true;
        }

        if (playerAnimator != null)
            playerAnimator.enabled = false;

        UpdatePlayerAppearance();
        UpdateAimingSystem();
    }

    private void ExitAimingMode(bool returnToPlayingState = true)
    {
        currentState = PlayerState.Normal;
        isShooting = false;

        if (aimLine != null)
            aimLine.enabled = false;

        ClearHighlight();

        if (playerAnimator != null)
            playerAnimator.enabled = true;

        if (playerSpriteRenderer != null && idleSprite != null)
            playerSpriteRenderer.sprite = idleSprite;

        if (returnToPlayingState &&
            GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentState == GameState.Aiming)
        {
            GameStateManager.Instance.SetState(GameState.Playing);
        }
    }
}
