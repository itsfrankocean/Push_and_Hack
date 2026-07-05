using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    /* =========================================================
     * 1) Inspector 설정 값
     * ========================================================= */
    [Header("이동 설정")]
    public float movespeed = 5f;
    public Transform movePoint;

    [Header("점프(바운스) + 애니메이션")]
    public Transform spriteTransform;
    public float jumpHeight = 0.5f;

    [Header("충돌/상호작용 레이어")]
    public LayerMask whatStopsMovement;
    public LayerMask whatIsPushable;

    [Header("아이템 상태")]
    public bool hasKeyCard = false;
    public float pushPoseDuration = 0.10f;

    [Header("이펙트")]
    public GameObject dustAnimPrefab; 
    public Vector3 dustOffset = new Vector3(0f, -0.3f, 0f); // 먼지가 발밑에서 터지도록 Y축을 살짝 내림
    public float dustDestroyTime = 0.3f; // 애니메이션 길이에 맞춰 삭제할 시간
    public int dustSortingOrderOffset = -1;

    [Header("충돌 체크 박스 크기")]
    public Vector2 moveCheckBoxSize = new Vector2(0.64f, 0.4f);

    [Header("입력 잠금용 박스")]
    public PushableBox[] pushableBoxes;

    [Header("Grid Alignment")]
    public bool alignStartToPushableGrid = true;
    [Min(0.01f)]
    public float gridCellSize = 1f;
    [Min(0f)]
    public float maxStartGridSnapDistance = 0.75f;

    [Header("사망 연출")]
    public Sprite deathSprite;
    public float electricDeathDuration = 0.25f;
    public float electricDeathArcHeight = 0.6f;
    public float electricDeathResetDelay = 0.15f;

    [Header("Undo 입력")]
    public KeyCode undoKey = KeyCode.Q;
    [Min(0.01f)]
    public float undoHoldRepeatDelay = 0.18f;

    [Header("Wait Turn")]
    public KeyCode waitTurnKey = KeyCode.Space;

    private bool isDying = false;
    private Vector3 previousTilePosition;
    private Vector3 lastMoveDirection = Vector3.zero;
    private Collider2D playerCol;
    private Coroutine electricDeathRoutine;
    private Grid cachedGrid;

    /* =========================================================
     * 2) 내부 참조/상수
     * ========================================================= */
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private PlayerCombat combat;
    private Sprite originalSprite;

    private static readonly int HashIsMoving = Animator.StringToHash("isMoving");
    private static readonly int HashIsPushing = Animator.StringToHash("isPushing");

    private const float ARRIVE_THRESHOLD = 0.05f;
    private const float ARRIVE_THRESHOLD_SQR = ARRIVE_THRESHOLD * ARRIVE_THRESHOLD;
    private const float OVERLAP_RADIUS = 0.2f;
    private const float ENEMY_TILE_MATCH_THRESHOLD_SQR = 0.01f;

    private Vector3 spriteOriginalLocalPos;
    private bool moveInProgress = false;
    private float moveProgress = 0f;
    private float currentMoveDistance = 1f;

    private bool pushLocked = false;
    private Coroutine pushRoutine = null;
    private float nextUndoTime = 0f;

    public bool IsMoving => movePoint != null && !IsAtDestination();

    /* =========================================================
     * 3) 초기화 및 업데이트
     * ========================================================= */
    private void Awake()
    {
        AutoFindPushableBoxesIfNeeded();
        AlignStartToPushableGrid();
    }

    private void Start()
    {
        playerCol = GetComponent<Collider2D>();
        combat = GetComponent<PlayerCombat>();

        if (movePoint != null)
            previousTilePosition = movePoint.position;

        if (movePoint != null) movePoint.parent = null;

        if (spriteTransform == null)
        {
            Transform mc = transform.Find("MC");
            if (mc != null) spriteTransform = mc;
        }

        if (spriteTransform != null)
        {
            spriteOriginalLocalPos = spriteTransform.localPosition;
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
            animator = spriteTransform.GetComponent<Animator>();

            if (spriteRenderer != null)
                originalSprite = spriteRenderer.sprite;
        }
    }

    // PlayerController.cs의 Update 함수 수정
    private void Update()
    {
        if (movePoint == null) return;
        if (isDying) return;
        if (combat != null && combat.currentState == PlayerCombat.PlayerState.Aiming)
        {
            transform.position = Vector3.MoveTowards(transform.position, movePoint.position, movespeed * Time.deltaTime);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, movePoint.position, movespeed * Time.deltaTime);

        bool atDestination = IsAtDestination();
        UpdateBounce(atDestination);

        if (!atDestination) return;
        if (pushLocked) return;
        if (IsTurnManagerBusy()) return;
        if (AreBoxesBusy()) return;
        if (!CanUsePlayerInput()) return;

        HandleInput();
    }

    private bool IsAtDestination()
    {
        //현재 위치와 목표 위치 차이 벡터의 제곱 길이를 구해서, 미리 정한 임계값 이하면 도착했다고 판단
        return (transform.position - movePoint.position).sqrMagnitude <= ARRIVE_THRESHOLD_SQR;
    }

    private void HandleInput()
    {
        if (HandleUndoInput())
            return;

        if (Input.GetKeyDown(waitTurnKey))
        {
            TryWaitTurn();
            return;
        }

        Vector3 direction = ReadArrowMoveDirection();

        if (direction == Vector3.zero)
            return;

        if (direction.x != 0f && spriteRenderer != null)
            spriteRenderer.flipX = direction.x < 0f;

        TryMove(direction);
    }

    private bool HandleUndoInput()
    {
        if (Input.GetKeyDown(undoKey))
        {
            TryUndoLastCommand();
            nextUndoTime = Time.unscaledTime + GetUndoRepeatDelay();
            return true;
        }

        if (!Input.GetKey(undoKey))
            return false;

        if (Time.unscaledTime < nextUndoTime)
            return true;

        TryUndoLastCommand();
        nextUndoTime = Time.unscaledTime + GetUndoRepeatDelay();
        return true;
    }

    private float GetUndoRepeatDelay()
    {
        return Mathf.Max(0.01f, undoHoldRepeatDelay);
    }

    private Vector3 ReadArrowMoveDirection()
    {
        bool left = Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.RightArrow);
        bool up = Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.DownArrow);

        if (left != right)
            return left ? Vector3.left : Vector3.right;

        if (up != down)
            return up ? Vector3.up : Vector3.down;

        return Vector3.zero;
    }

    /* =========================================================
     * 4) 상호작용 체크 및 커맨드(Command) 발행
     * ========================================================= */
    private void TryMove(Vector3 direction)
    {
        Vector3 targetPos = movePoint.position + direction;
        Vector2 moveCheckCenter = GetMoveCheckCenter(targetPos);

        // 1) 벽/엘리베이터 체크
        Collider2D hitWall = Physics2D.OverlapBox(moveCheckCenter, moveCheckBoxSize, 0f, whatStopsMovement);
        if (hitWall != null)
        {
            if (hitWall.TryGetComponent(out Elevator elevator))
            {
                TryOpenElevator(elevator);
            }

            return;
        }

        // 2) 박스 밀기 체크
        Collider2D hitBox = Physics2D.OverlapBox(moveCheckCenter, moveCheckBoxSize, 0f, whatIsPushable);
        if (hitBox != null && hitBox.TryGetComponent(out PushableBox box))
        {
            if (CommandManager.Instance != null)
            {
                CommandManager.Instance.ExecuteCommand(new PushCommand(this, box, direction));
            }

            return;
        }

        // 3) 아이템 체크
        bool gotCard = false;
        GameObject cardObj = null;

        Collider2D[] hits = Physics2D.OverlapBoxAll(moveCheckCenter, moveCheckBoxSize, 0f);
        foreach (Collider2D hit in hits)
        {
            if (hit != null && hit.CompareTag("KeyCard"))
            {
                gotCard = true;
                cardObj = hit.gameObject;
                break;
            }
        }

        // 4) 일반 이동
        if (CommandManager.Instance != null)
        {
            CommandManager.Instance.ExecuteCommand(new MoveCommand(this, direction, gotCard, cardObj));
        }
    }

    private Vector2 GetMoveCheckCenter(Vector3 targetPos)
    {
        if (playerCol == null)
            return targetPos;

        return (Vector2)targetPos + playerCol.offset;
    }

    private void TryUndoLastCommand()
    {
        if (CommandManager.Instance == null)
        {
            Debug.LogWarning("CommandManager.Instance가 없습니다.");
            return;
        }

        CommandManager.Instance.UndoLastCommand();
    }

    private void TryWaitTurn()
    {
        if (CommandManager.Instance == null)
        {
            Debug.LogWarning("CommandManager.Instance is not set.");
            return;
        }

        CommandManager.Instance.ExecuteCommand(new WaitCommand(this));
    }

    private void TryOpenElevator(Elevator elevator)
    {
        if (hasKeyCard) { elevator.OpenDoor(); Debug.Log("문이 열립니다."); }
        else { AudioManager.I.PlayOneShot(AudioManager.I.sfxElevatorError, 1f); Debug.Log("카드키가 필요합니다!"); }
    }


    /* =========================================================
     * 5) 커맨드(Command)들이 플레이어를 조종할 수 있게 열어둔 Public 함수들
     * ========================================================= */

    // [이동 관련]
    public void DoMove(Vector3 dir)
    {
        previousTilePosition = movePoint.position; // 이동 전 위치 저장
        lastMoveDirection = dir;

        movePoint.position += dir;
        BeginMoveVisual(dir);
    }

    public void UndoMove(Vector3 dir)
    {
        movePoint.position -= dir; // 반대로 이동
        PlayUndoVisuals(dir);      // Undo 전용 이펙트
    }

    // [아이템 관련]
    public Vector3 GetCurrentTilePosition()
    {
        return movePoint != null ? movePoint.position : transform.position;
    }

    public void WarpTo(Vector3 position)
    {
        if (movePoint != null)
            movePoint.position = position;

        transform.position = position;
        previousTilePosition = position;
        lastMoveDirection = Vector3.zero;
        moveInProgress = false;
        moveProgress = 0f;

        if (spriteTransform != null)
            spriteTransform.localPosition = spriteOriginalLocalPos;

        if (animator != null)
        {
            animator.SetBool(HashIsMoving, false);
            animator.SetBool(HashIsPushing, false);
        }
    }

    public void PickUpCard(GameObject cardObj)
    {
        cardObj.SetActive(false);
        hasKeyCard = true;
        AudioManager.I.PlayOneShot(AudioManager.I.sfxCardKey, 1f);
    }

    public void DropCard(GameObject cardObj)
    {
        cardObj.SetActive(true);
        hasKeyCard = false;
    }

    // [박스 푸시 관련]
    public void DoPushAnim()
    {
        StartPushPose();
        
    }

    // [턴 관련]
    public void StepEnemies()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.StepTurn();
    }

    public void UndoEnemies()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.UndoTurn();
    }


    /* =========================================================
     * 6) 시각적 피드백 (애니메이션 / 사운드 / 이펙트)
     * ========================================================= */
    private void BeginMoveVisual(Vector3 direction)
    {
        if (spriteTransform == null) return;
        moveInProgress = true;
        moveProgress = 0f;
        currentMoveDistance = direction.magnitude == 0 ? 1f : direction.magnitude;

        AudioManager.I.PlayOneShot(AudioManager.I.sfxPlayerFootstep, 0.8f);

        if (dustAnimPrefab != null)
        {
            Vector3 spawnPos = transform.position + dustOffset;
            
            Quaternion dustRotation = Quaternion.identity;
            if (spriteRenderer != null && spriteRenderer.flipX)
            {
                dustRotation = Quaternion.Euler(0, 180, 0);
            }

            GameObject dust = Instantiate(dustAnimPrefab, spawnPos, dustRotation);
            ApplyDustSorting(dust);
            Destroy(dust, dustDestroyTime);
        }

        if (animator != null)
        {
            animator.SetBool(HashIsPushing, false);
            animator.SetBool(HashIsMoving, true);
        }
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

    private void PlayUndoVisuals(Vector3 reverseDir)
    {
        if (reverseDir.x != 0f && spriteRenderer != null)
            spriteRenderer.flipX = reverseDir.x < 0f;

        if (animator != null)
        {
            animator.SetTrigger("UndoSpecialAnim");
        }
    }

    private void UpdateBounce(bool atDestination)
    {
        if (spriteTransform == null) return;
        if (atDestination)
        {
            spriteTransform.localPosition = spriteOriginalLocalPos;
            if (moveInProgress && animator != null) animator.SetBool(HashIsMoving, false);
            moveInProgress = false;
            moveProgress = 0f;
            return;
        }
        if (!moveInProgress) return;
        
        moveProgress += (movespeed * Time.deltaTime) / currentMoveDistance;
        moveProgress = Mathf.Clamp01(moveProgress);
        float yOffset = Mathf.Sin(moveProgress * Mathf.PI) * jumpHeight;
        spriteTransform.localPosition = spriteOriginalLocalPos + Vector3.up * yOffset;
    }

    private void StartPushPose()
    {
        if (pushRoutine != null) StopCoroutine(pushRoutine);
        pushRoutine = StartCoroutine(PushPoseRoutine());
    }

    private IEnumerator PushPoseRoutine()
    {
        pushLocked = true;
        if (animator != null) { animator.SetBool(HashIsMoving, false); animator.SetBool(HashIsPushing, true); }
        yield return null;
        if (pushPoseDuration > 0f) yield return new WaitForSeconds(pushPoseDuration);
        if (animator != null) animator.SetBool(HashIsPushing, false);
        pushLocked = false;
        pushRoutine = null;
    }

    /* =========================================================
     * 7) 턴 기반 적 연동 로직 (에러 수정 완료)
     * ========================================================= */

    private void AutoFindPushableBoxesIfNeeded()
    {
        if (pushableBoxes == null || pushableBoxes.Length == 0)
        {
            pushableBoxes = FindObjectsOfType<PushableBox>();
        }
    }

    private void AlignStartToPushableGrid()
    {
        if (!alignStartToPushableGrid || movePoint == null)
            return;

        if (!TryGetPushableGridOffset(out Vector2 gridOffset))
            return;

        Vector3 currentPosition = movePoint.position;
        Vector3 alignedPosition = AlignPositionToGrid(currentPosition, gridOffset);
        float maxDistance = maxStartGridSnapDistance;

        if ((alignedPosition - currentPosition).sqrMagnitude > maxDistance * maxDistance)
            return;

        transform.position = alignedPosition;
        movePoint.position = alignedPosition;
    }

    private bool TryGetPushableGridOffset(out Vector2 gridOffset)
    {
        gridOffset = Vector2.zero;

        if (pushableBoxes == null)
            return false;

        float cellSize = GetGridCellSize();

        for (int i = 0; i < pushableBoxes.Length; i++)
        {
            PushableBox box = pushableBoxes[i];
            if (box == null || !box.gameObject.activeInHierarchy)
                continue;

            Vector3 boxPosition = box.transform.position;
            gridOffset = new Vector2(
                GetGridAxisOffset(boxPosition.x, cellSize),
                GetGridAxisOffset(boxPosition.y, cellSize));
            return true;
        }

        return false;
    }

    private Vector3 AlignPositionToGrid(Vector3 position, Vector2 gridOffset)
    {
        float cellSize = GetGridCellSize();
        position.x = AlignAxisToGrid(position.x, gridOffset.x, cellSize);
        position.y = AlignAxisToGrid(position.y, gridOffset.y, cellSize);
        return position;
    }

    private float AlignAxisToGrid(float value, float gridOffset, float cellSize)
    {
        return Mathf.Round((value - gridOffset) / cellSize) * cellSize + gridOffset;
    }

    private float GetGridAxisOffset(float value, float cellSize)
    {
        return Mathf.Repeat(value, cellSize);
    }

    private float GetGridCellSize()
    {
        return Mathf.Max(0.01f, Mathf.Abs(gridCellSize));
    }

    private bool AreBoxesBusy()
    {
        if (pushableBoxes == null) return false;

        for (int i = 0; i < pushableBoxes.Length; i++)
        {
            if (pushableBoxes[i] != null && pushableBoxes[i].IsBusy)
                return true;
        }

        return false;
    }

    private bool CanUsePlayerInput()
    {
        return GameStateManager.Instance == null ||
               GameStateManager.Instance.CanPlayerMoveInput;
    }

    private bool IsTurnManagerBusy()
    {
        return TurnManager.Instance != null && TurnManager.Instance.IsBusy;
    }

    public void DieFromElectric()
    {
        if (isDying) return;

        StartDeathRoutine();
    }

    public void DieFromEnemy()
    {
        if (isDying)
            return;

        if (AudioManager.I != null)
            AudioManager.I.PlayOneShot(AudioManager.I.sfxPlayerDeath, 1f);

        StartDeathRoutine();
    }

    private void StartDeathRoutine()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Dead);

        if (electricDeathRoutine != null)
            StopCoroutine(electricDeathRoutine);

        electricDeathRoutine = StartCoroutine(ElectricDeathRoutine());
    }

    public bool KillIfOnEnemyTile()
    {
        if (IsEnemyAtTile(GetCurrentTilePosition()))
        {
            DieFromEnemy();
            return true;
        }

        return false;
    }

    private bool IsEnemyAtTile(Vector3 tilePosition)
    {
        PatrolEnemy[] enemies = FindObjectsByType<PatrolEnemy>(FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            PatrolEnemy enemy = enemies[i];

            if (enemy == null || !enemy.isActiveAndEnabled)
                continue;

            if (IsSameTile(enemy.GetCurrentTilePosition(), tilePosition))
                return true;
        }

        return false;
    }

    private bool IsSameTile(Vector3 a, Vector3 b)
    {
        if (TryGetGrid(out Grid grid))
            return grid.WorldToCell(a) == grid.WorldToCell(b);

        Vector2 delta = new Vector2(a.x - b.x, a.y - b.y);
        return delta.sqrMagnitude <= ENEMY_TILE_MATCH_THRESHOLD_SQR;
    }

    private bool TryGetGrid(out Grid grid)
    {
        if (cachedGrid == null)
            cachedGrid = FindFirstObjectByType<Grid>();

        grid = cachedGrid;
        return grid != null;
    }

    public void ReviveAfterDeath()
    {
        if (electricDeathRoutine != null)
        {
            StopCoroutine(electricDeathRoutine);
            electricDeathRoutine = null;
        }

        isDying = false;
        pushLocked = false;
        moveInProgress = false;
        moveProgress = 0f;

        if (playerCol != null)
            playerCol.enabled = true;

        if (spriteTransform != null)
            spriteTransform.localPosition = spriteOriginalLocalPos;

        if (spriteRenderer != null && originalSprite != null)
            spriteRenderer.sprite = originalSprite;

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool(HashIsMoving, false);
            animator.SetBool(HashIsPushing, false);
        }

        if (movePoint != null)
            transform.position = movePoint.position;
    }

    private IEnumerator ElectricDeathRoutine()
    {
        isDying = true;

        // 입력/충돌 중복 방지
        pushLocked = true;

        if (playerCol != null)
            playerCol.enabled = false;

        // 이동 애니메이션 정지
        moveInProgress = false;
        moveProgress = 0f;

        if (animator != null)
        {
            animator.SetBool(HashIsMoving, false);
            animator.SetBool(HashIsPushing, false);
            animator.enabled = false; // 애니메이터 끄고 사망 스프라이트 고정
        }

        // 사망 스프라이트 교체
        if (spriteRenderer != null && deathSprite != null)
        {
            spriteRenderer.sprite = deathSprite;
        }

        // 바운스 위치 원위치
        if (spriteTransform != null)
        {
            spriteTransform.localPosition = spriteOriginalLocalPos;
        }

        // 현재 위치(전기타일 위) -> 직전 타일로 포물선
        Vector3 start = transform.position;
        Vector3 end = previousTilePosition;

        float elapsed = 0f;

        while (elapsed < electricDeathDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / electricDeathDuration);

            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * electricDeathArcHeight;

            transform.position = pos;
            yield return null;
        }

        transform.position = end;

        yield return new WaitForSeconds(electricDeathResetDelay);

        electricDeathRoutine = null;
    }
}
