using System.Collections.Generic;
using UnityEngine;

public class PatrolEnemy : MonoBehaviour, IDamageable, ITurnActor, IProjectileDisplaceable
{
    public enum PatrolDirection
    {
        Horizontal,
        Vertical
    }

    [Header("Patrol")]
    public PatrolDirection direction = PatrolDirection.Horizontal;
    public bool startsMovingForward = true;
    public bool startFacingLeft = false;
    [Min(0.01f)]
    public float stepSize = 1f;
    public float moveSpeed = 5f;

    [Header("Collision")]
    public LayerMask whatStopsMovement;
    public Vector2 moveCheckBoxSize = new Vector2(0.64f, 0.4f);

    [Header("Visual")]
    public Transform spriteTransform;
    public bool flipXWhenMovingLeft = true;
    public bool flipYWhenMovingDown = false;
    public float jumpHeight = 0.35f;

    [Header("Dust Effect")]
    public GameObject dustAnimPrefab;
    public Vector3 dustOffset = Vector3.zero;
    public float dustDestroyTime = 0.45f;
    public int dustSortingOrderOffset = -1;

    [Header("Player Contact")]
    public bool killPlayerOnTouch = true;

    [Header("Health")]
    public int health = 10;

    private const float ArriveThreshold = 0.05f;
    private const float ArriveThresholdSqr = ArriveThreshold * ArriveThreshold;
    private const float TileMatchThresholdSqr = 0.01f;

    private Vector3 targetPosition;
    private int directionSign;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Collider2D ownCollider;
    private PlayerController cachedPlayer;
    private Grid cachedGrid;

    private Vector3 spriteOriginalLocalPosition;
    private bool moveInProgress;
    private float moveProgress;
    private float currentMoveDistance = 1f;

    private struct TurnRecord
    {
        public Vector3 TargetPosition;
        public int DirectionSign;
        public bool FlipX;
        public bool FlipY;
    }

    private readonly Stack<TurnRecord> history = new Stack<TurnRecord>();

    public bool IsBusy
    {
        get
        {
            return (transform.position - targetPosition).sqrMagnitude > ArriveThresholdSqr;
        }
    }

    private void Awake()
    {
        targetPosition = transform.position;
        directionSign = startsMovingForward ? 1 : -1;

        if (direction == PatrolDirection.Horizontal && startFacingLeft)
            directionSign = -1;

        ownCollider = GetComponent<Collider2D>();
        EnsureDefaultCollisionMask();
        CacheVisualComponents();
        ApplyFacing();
    }

    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        bool arrived = !IsBusy;
        UpdateMoveVisual(arrived);

        if (animator != null)
            animator.SetFloat("Speed", arrived ? 0f : 1f);

        if (arrived)
            KillPlayerIfTouching();
    }

    public void StepTurn()
    {
        if (IsBusy)
            return;

        SaveTurnRecord();

        Vector3 step = GetMoveDirection() * GetStepSize();

        if (TryStartMove(step))
            return;

        ReverseDirection();
        step = GetMoveDirection() * GetStepSize();

        if (TryStartMove(step))
            return;

        KillPlayerIfTouching();
    }

    private bool TryStartMove(Vector3 step)
    {
        Vector3 nextPosition = targetPosition + step;

        if (IsBlocked(nextPosition))
        {
            return false;
        }

        targetPosition = nextPosition;
        BeginMoveVisual(step);
        ApplyFacing(step);
        KillPlayerIfAt(targetPosition);

        return true;
    }

    private float GetStepSize()
    {
        if (Mathf.Approximately(stepSize, 0f))
            return 1f;

        return Mathf.Abs(stepSize);
    }

    public void UndoTurn()
    {
        if (history.Count == 0)
            return;

        TurnRecord record = history.Pop();

        targetPosition = record.TargetPosition;
        directionSign = record.DirectionSign;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = record.FlipX;
            spriteRenderer.flipY = record.FlipY;
        }

        ResetMoveVisual();
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
            Destroy(gameObject);
    }

    public Vector3 GetCurrentTilePosition()
    {
        return targetPosition;
    }

    public bool Displace(Vector3 direction)
    {
        if (!CanDisplace(direction))
            return false;

        targetPosition += direction;
        BeginMoveVisual(direction);
        ApplyFacing(direction);

        return true;
    }

    public SpriteRenderer GetDisplacementRenderer()
    {
        if (spriteRenderer != null)
            return spriteRenderer;

        return GetComponentInChildren<SpriteRenderer>(true);
    }

    public void WarpTo(Vector3 position)
    {
        targetPosition = position;
        transform.position = position;
        moveInProgress = false;
        moveProgress = 0f;

        if (spriteTransform != null)
            spriteTransform.localPosition = spriteOriginalLocalPosition;

        if (animator != null)
            animator.SetFloat("Speed", 0f);
    }

    public bool CanDisplace(Vector3 moveDirection)
    {
        if (IsBusy)
            return false;

        Vector3 nextPosition = targetPosition + moveDirection;
        return !IsBlocked(nextPosition);
    }

    private void SaveTurnRecord()
    {
        history.Push(new TurnRecord
        {
            TargetPosition = targetPosition,
            DirectionSign = directionSign,
            FlipX = spriteRenderer != null && spriteRenderer.flipX,
            FlipY = spriteRenderer != null && spriteRenderer.flipY
        });
    }

    private void EnsureDefaultCollisionMask()
    {
        if (whatStopsMovement.value != 0)
            return;

        int wallsLayer = LayerMask.NameToLayer("Walls");
        if (wallsLayer >= 0)
            whatStopsMovement = 1 << wallsLayer;
    }

    private void CacheVisualComponents()
    {
        if (spriteTransform == null)
        {
            SpriteRenderer childRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (childRenderer != null)
                spriteTransform = childRenderer.transform;
        }

        if (spriteTransform != null)
        {
            spriteOriginalLocalPosition = spriteTransform.localPosition;
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
            animator = spriteTransform.GetComponent<Animator>();
        }

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (spriteRenderer == null)
            Debug.LogWarning($"[{name}] SpriteRenderer was not found; facing flips will be skipped.");
    }

    private Vector3 GetMoveDirection()
    {
        if (direction == PatrolDirection.Horizontal)
            return directionSign > 0 ? Vector3.right : Vector3.left;

        return directionSign > 0 ? Vector3.up : Vector3.down;
    }

    private void ReverseDirection()
    {
        directionSign *= -1;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
        ApplyFacing(GetMoveDirection());
    }

    private void ApplyFacing(Vector3 moveDirection)
    {
        if (spriteRenderer == null)
            return;

        if (Mathf.Abs(moveDirection.x) > 0.001f && flipXWhenMovingLeft)
            spriteRenderer.flipX = moveDirection.x < 0f;

        if (Mathf.Abs(moveDirection.y) > 0.001f && flipYWhenMovingDown)
            spriteRenderer.flipY = moveDirection.y < 0f;
    }

    private bool IsBlocked(Vector3 position)
    {
        if (IsBlockedByPushableBox(position))
            return true;

        if (whatStopsMovement.value == 0)
            return false;

        Collider2D[] hits = Physics2D.OverlapBoxAll(GetCheckCenter(position), moveCheckBoxSize, 0f, whatStopsMovement);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
                continue;

            if (hit == ownCollider ||
                hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsBlockedByPushableBox(Vector3 position)
    {
        PushableBox[] boxes = FindObjectsByType<PushableBox>(FindObjectsSortMode.None);

        for (int i = 0; i < boxes.Length; i++)
        {
            PushableBox box = boxes[i];

            if (box == null || !box.isActiveAndEnabled)
                continue;

            Transform boxTransform = box.transform;

            if (boxTransform == transform || boxTransform.IsChildOf(transform))
                continue;

            if (IsSameTile(box.GetCurrentTilePosition(), position) ||
                IsSameTile(boxTransform.position, position))
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 GetCheckCenter(Vector3 position)
    {
        if (ownCollider is BoxCollider2D boxCollider)
            return (Vector2)position + boxCollider.offset;

        return position;
    }

    private void BeginMoveVisual(Vector3 step)
    {
        moveInProgress = true;
        moveProgress = 0f;
        currentMoveDistance = Mathf.Max(step.magnitude, 0.001f);
        PlayDustEffect(step);
    }

    private void PlayDustEffect(Vector3 moveDirection)
    {
        ResolveDustPrefabIfNeeded();

        if (dustAnimPrefab == null)
            return;

        Vector3 spawnPosition = transform.position + dustOffset;
        GameObject dust = Instantiate(dustAnimPrefab, spawnPosition, GetDustRotation());
        ApplyDustSorting(dust);
        Destroy(dust, Mathf.Max(0.01f, dustDestroyTime));
    }

    private void ResolveDustPrefabIfNeeded()
    {
        if (dustAnimPrefab != null)
            return;

        PlayerController player = FindPlayer();
        if (player != null)
            dustAnimPrefab = player.dustAnimPrefab;
    }

    private Quaternion GetDustRotation()
    {
        if (spriteRenderer != null && spriteRenderer.flipX)
            return Quaternion.Euler(0f, 180f, 0f);

        return Quaternion.identity;
    }

    private void ApplyDustSorting(GameObject dust)
    {
        if (dust == null || spriteRenderer == null)
            return;

        SpriteRenderer[] dustRenderers = dust.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < dustRenderers.Length; i++)
        {
            if (dustRenderers[i] == null)
                continue;

            dustRenderers[i].sortingLayerID = spriteRenderer.sortingLayerID;
            dustRenderers[i].sortingOrder = spriteRenderer.sortingOrder + dustSortingOrderOffset;
        }
    }

    private void UpdateMoveVisual(bool arrived)
    {
        if (arrived)
        {
            ResetMoveVisual();
            return;
        }

        if (spriteTransform == null || !moveInProgress)
            return;

        moveProgress += (moveSpeed * Time.deltaTime) / currentMoveDistance;
        moveProgress = Mathf.Clamp01(moveProgress);

        float yOffset = Mathf.Sin(moveProgress * Mathf.PI) * jumpHeight;
        spriteTransform.localPosition = spriteOriginalLocalPosition + Vector3.up * yOffset;
    }

    private void ResetMoveVisual()
    {
        if (spriteTransform != null)
            spriteTransform.localPosition = spriteOriginalLocalPosition;

        moveInProgress = false;
        moveProgress = 0f;
    }

    private void KillPlayerIfAt(Vector3 tilePosition)
    {
        if (!killPlayerOnTouch)
            return;

        PlayerController player = FindPlayer();
        if (player == null)
            return;

        if (!IsSameTile(player.GetCurrentTilePosition(), tilePosition))
            return;

        player.DieFromEnemy();
    }

    private void KillPlayerIfTouching()
    {
        if (!killPlayerOnTouch)
            return;

        PlayerController player = FindPlayer();
        if (player == null)
            return;

        if (IsSameTile(player.GetCurrentTilePosition(), targetPosition))
            player.DieFromEnemy();
    }

    private PlayerController FindPlayer()
    {
        if (cachedPlayer != null)
            return cachedPlayer;

        cachedPlayer = FindFirstObjectByType<PlayerController>();
        return cachedPlayer;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryKillPlayerFromCollider(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryKillPlayerFromCollider(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null)
            return;

        TryKillPlayerFromCollider(collision.collider);
    }

    private void TryKillPlayerFromCollider(Collider2D other)
    {
        if (!killPlayerOnTouch || other == null)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null && IsSameTile(player.GetCurrentTilePosition(), targetPosition))
            player.DieFromEnemy();
    }

    private bool IsSameTile(Vector3 a, Vector3 b)
    {
        if (TryGetGrid(out Grid grid))
            return grid.WorldToCell(a) == grid.WorldToCell(b);

        Vector2 delta = new Vector2(a.x - b.x, a.y - b.y);
        return delta.sqrMagnitude <= TileMatchThresholdSqr;
    }

    private bool TryGetGrid(out Grid grid)
    {
        if (cachedGrid == null)
            cachedGrid = FindFirstObjectByType<Grid>();

        grid = cachedGrid;
        return grid != null;
    }
}
