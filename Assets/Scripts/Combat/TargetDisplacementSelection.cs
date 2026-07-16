using System.Collections.Generic;
using UnityEngine;

public class TargetDisplacementSelection : MonoBehaviour
{
    private const float IndicatorCellInset = 0.92f;
    private const int IndicatorSortingOffset = -1;
    private const int IndicatorSortingPrecision = 100;
    private static readonly Color IndicatorColor = new Color(1f, 0.02f, 0f, 0.78f);
    private static readonly Color IndicatorPulseColor = new Color(1f, 0.02f, 0f, 1f);

    private static readonly Vector3[] Directions =
    {
        Vector3.up,
        Vector3.down,
        Vector3.left,
        Vector3.right
    };

    private static TargetDisplacementSelection activeSelection;
    private static Sprite tileSprite;
    private static Material indicatorMaterial;
    private static bool inputReleasePending;

    private readonly List<GameObject> indicators = new List<GameObject>();
    private readonly List<Vector3> validDirections = new List<Vector3>();

    private IProjectileDisplaceable target;
    private MonoBehaviour targetBehaviour;
    private ShootCommand ownerCommand;
    private GameObject projectileObject;
    private Vector3 targetPositionBefore;
    private Grid grid;
    private Vector3 indicatorSize = Vector3.one;
    private GameState previousState;
    private bool finished;
    private float pulseTime;

    public static bool HasActiveSelection => activeSelection != null;

    public static bool ShouldBlockCombatInput
    {
        get
        {
            if (activeSelection != null)
                return true;

            if (!inputReleasePending)
                return false;

            if (IsAnySelectionInputHeld())
                return true;

            inputReleasePending = false;
            return false;
        }
    }

    public static bool Begin(
        IProjectileDisplaceable target,
        ShootCommand ownerCommand,
        GameObject projectileObject
    )
    {
        if (target == null || ownerCommand == null || !ownerCommand.CanApplyProjectileEffect())
            return false;

        if (activeSelection != null)
            activeSelection.CancelSelection();

        MonoBehaviour targetBehaviour = target as MonoBehaviour;
        if (targetBehaviour == null || target.IsBusy)
            return false;

        GameObject selectionObject = new GameObject("TargetDisplacementSelection");
        TargetDisplacementSelection selection = selectionObject.AddComponent<TargetDisplacementSelection>();
        selection.Initialize(target, targetBehaviour, ownerCommand, projectileObject);

        if (selection.validDirections.Count == 0)
        {
            selection.CancelSelection();
            return false;
        }

        activeSelection = selection;
        ownerCommand.RegisterDisplacementSelection(selection);

        return true;
    }

    public void CancelFromUndo()
    {
        CancelSelection();
    }

    private void Initialize(
        IProjectileDisplaceable target,
        MonoBehaviour targetBehaviour,
        ShootCommand ownerCommand,
        GameObject projectileObject
    )
    {
        this.target = target;
        this.targetBehaviour = targetBehaviour;
        this.ownerCommand = ownerCommand;
        this.projectileObject = projectileObject;
        targetPositionBefore = target.GetCurrentTilePosition();
        CacheGrid();

        previousState = GameStateManager.Instance != null
            ? GameStateManager.Instance.CurrentState
            : GameState.Playing;

        CacheValidDirections();
        CreateIndicators();

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Targeting);
    }

    private void Update()
    {
        if (finished)
            return;

        PulseIndicators();

        if (ownerCommand == null || !ownerCommand.CanApplyProjectileEffect())
        {
            CancelSelection();
            return;
        }

        Vector3 inputDirection = ReadDirectionInput();
        if (inputDirection != Vector3.zero)
        {
            if (IsValidDirection(inputDirection))
                TryApplyDirection(inputDirection);

            return;
        }

        if (Input.GetMouseButtonDown(0))
            TryApplyMouseSelection();
    }

    private void CacheValidDirections()
    {
        validDirections.Clear();

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector3 direction = Directions[i];

            if (target.CanDisplace(direction))
                validDirections.Add(direction);
        }
    }

    private void CreateIndicators()
    {
        EnsureTileSprite();
        SpriteRenderer targetRenderer = target.GetDisplacementRenderer();

        for (int i = 0; i < validDirections.Count; i++)
        {
            Vector3 direction = validDirections[i];
            Vector3 position = GetIndicatorCenter(targetPositionBefore + direction);

            GameObject indicator = new GameObject("DisplaceTileIndicator");
            indicator.transform.SetParent(transform, false);
            indicator.transform.position = new Vector3(position.x, position.y, targetPositionBefore.z - 0.05f);
            indicator.transform.localScale = indicatorSize;

            SpriteRenderer renderer = indicator.AddComponent<SpriteRenderer>();
            renderer.sprite = tileSprite;
            renderer.sharedMaterial = GetIndicatorMaterial();
            renderer.color = IndicatorColor;
            ApplyIndicatorSorting(renderer, indicator.transform, targetRenderer);

            indicators.Add(indicator);
        }
    }

    private void ApplyIndicatorSorting(SpriteRenderer renderer, Transform sortPoint, SpriteRenderer targetRenderer)
    {
        if (renderer == null || sortPoint == null)
            return;

        if (targetRenderer != null)
            renderer.sortingLayerID = targetRenderer.sortingLayerID;

        renderer.sortingOrder = IndicatorSortingOffset -
                                Mathf.RoundToInt(sortPoint.position.y * IndicatorSortingPrecision);

        YSortByFeet sorter = renderer.gameObject.AddComponent<YSortByFeet>();
        sorter.Configure(sortPoint, IndicatorSortingOffset, IndicatorSortingPrecision);
    }

    private static void EnsureTileSprite()
    {
        if (tileSprite != null)
            return;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.name = "DisplaceTileIndicatorTexture";

        tileSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }

    private static Material GetIndicatorMaterial()
    {
        if (indicatorMaterial != null)
            return indicatorMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        indicatorMaterial = new Material(shader)
        {
            name = "DisplaceTileIndicatorMaterial",
            hideFlags = HideFlags.HideAndDontSave
        };

        return indicatorMaterial;
    }

    private void PulseIndicators()
    {
        pulseTime += Time.deltaTime * 8f;
        float alpha = 0.68f + Mathf.Sin(pulseTime) * 0.1f;

        for (int i = 0; i < indicators.Count; i++)
        {
            GameObject indicator = indicators[i];
            if (indicator == null)
                continue;

            indicator.transform.localScale = indicatorSize;

            SpriteRenderer renderer = indicator.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color pulseColor = IndicatorPulseColor;
                pulseColor.a = alpha;
                renderer.color = pulseColor;
            }
        }
    }

    private Vector3 ReadDirectionInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            return Vector3.up;

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            return Vector3.down;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            return Vector3.left;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            return Vector3.right;

        return Vector3.zero;
    }

    private void TryApplyMouseSelection()
    {
        if (Camera.main == null)
            return;

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = targetPositionBefore.z;

        for (int i = 0; i < validDirections.Count; i++)
        {
            Vector3 direction = validDirections[i];
            Vector3 tilePosition = GetIndicatorCenter(targetPositionBefore + direction);

            float selectRadius = Mathf.Min(indicatorSize.x, indicatorSize.y) * 0.5f;
            if (Vector2.Distance(mousePosition, tilePosition) <= selectRadius)
            {
                TryApplyDirection(direction);
                return;
            }
        }
    }

    private void TryApplyDirection(Vector3 direction)
    {
        if (!IsValidDirection(direction))
            return;

        if (targetBehaviour == null || target == null || !target.Displace(direction))
            return;

        inputReleasePending = true;
        ownerCommand.RecordDisplacedTarget(target, targetPositionBefore);

        if (!(targetBehaviour is PushableBox) && CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.08f, 0.08f);

        FinishSelection();
    }

    private bool IsValidDirection(Vector3 direction)
    {
        for (int i = 0; i < validDirections.Count; i++)
        {
            if (validDirections[i] == direction)
                return true;
        }

        return false;
    }

    private void CancelSelection()
    {
        inputReleasePending = true;
        FinishSelection();
    }

    private void FinishSelection()
    {
        if (finished)
            return;

        finished = true;
        ClearIndicators();

        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentState == GameState.Targeting)
        {
            GameStateManager.Instance.SetState(previousState);
        }

        if (projectileObject != null)
            Destroy(projectileObject);

        if (activeSelection == this)
            activeSelection = null;

        Destroy(gameObject);
    }

    private void ClearIndicators()
    {
        for (int i = 0; i < indicators.Count; i++)
        {
            if (indicators[i] != null)
                Destroy(indicators[i]);
        }

        indicators.Clear();
    }

    private void CacheGrid()
    {
        grid = FindFirstObjectByType<Grid>();

        if (grid == null)
        {
            indicatorSize = new Vector3(IndicatorCellInset, IndicatorCellInset, 1f);
            return;
        }

        Vector3 cellSize = grid.cellSize;
        indicatorSize = new Vector3(
            Mathf.Abs(cellSize.x) * IndicatorCellInset,
            Mathf.Abs(cellSize.y) * IndicatorCellInset,
            1f
        );
    }

    private Vector3 GetIndicatorCenter(Vector3 worldPosition)
    {
        if (grid == null)
            return worldPosition;

        Vector3Int cellPosition = grid.WorldToCell(worldPosition);
        Vector3 center = grid.GetCellCenterWorld(cellPosition);
        return new Vector3(center.x, center.y, worldPosition.z);
    }

    private static bool IsAnySelectionInputHeld()
    {
        return Input.GetKey(KeyCode.UpArrow) ||
               Input.GetKey(KeyCode.DownArrow) ||
               Input.GetKey(KeyCode.LeftArrow) ||
               Input.GetKey(KeyCode.RightArrow) ||
               Input.GetKey(KeyCode.W) ||
               Input.GetKey(KeyCode.A) ||
               Input.GetKey(KeyCode.S) ||
               Input.GetKey(KeyCode.D) ||
               Input.GetKey(KeyCode.X) ||
               Input.GetKey(KeyCode.Escape);
    }
}
