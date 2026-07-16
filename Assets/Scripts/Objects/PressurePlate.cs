using System.Collections.Generic;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("눌렀을 때 꺼질 전기 타일")]
    public ElectricTile[] targetElectricTiles;

    [Header("눌렀을 때 켜질 전기 타일")]
    public ElectricTile[] targetElectricTilesToActivate;

    [Header("스케일 애니메이션")]
    public float pressedScale = 0.9f;
    public float lerpSpeed = 10f;

    private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();

    private const float TileMatchThresholdSqr = 0.01f;

    private Vector3 initialScale;
    private bool isPressed = false;
    private Grid cachedGrid;

    private void Start()
    {
        initialScale = transform.localScale;
        isPressed = ShouldBePressed();
        ApplyElectricTileStates();
    }

    private void Update()
    {
        CleanupInvalidOccupants();
        RefreshPressedState();

        Vector3 targetScale = isPressed ? initialScale * pressedScale : initialScale;
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * lerpSpeed
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPatrolEnemyOccupant(other))
        {
            RefreshPressedState();
            return;
        }

        if (!IsValidTriggerOccupant(other)) return;

        occupants.Add(other);
        RefreshPressedState();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPatrolEnemyOccupant(other))
        {
            RefreshPressedState();
            return;
        }

        if (!IsValidTriggerOccupant(other)) return;

        occupants.Remove(other);
        RefreshPressedState();
    }

    private bool IsValidTriggerOccupant(Collider2D other)
    {
        if (other == null)
            return false;

        return other.CompareTag("Player") ||
               other.CompareTag("Box") ||
               other.GetComponentInParent<PlayerController>() != null ||
               other.GetComponentInParent<PushableBox>() != null;
    }

    private bool IsPatrolEnemyOccupant(Collider2D other)
    {
        return other != null && other.GetComponentInParent<PatrolEnemy>() != null;
    }

    private void CleanupInvalidOccupants()
    {
        if (occupants.Count == 0) return;

        int removedCount = occupants.RemoveWhere(c =>
            c == null ||
            !c.enabled ||
            !c.gameObject.activeInHierarchy ||
            !IsValidTriggerOccupant(c)
        );

        if (removedCount > 0)
        {
            RefreshPressedState();
        }
    }

    private void RefreshPressedState()
    {
        bool shouldBePressed = ShouldBePressed();
        SetPressed(shouldBePressed);
    }

    private bool ShouldBePressed()
    {
        return occupants.Count > 0 || HasPatrolEnemyOnPlate();
    }

    private bool HasPatrolEnemyOnPlate()
    {
        PatrolEnemy[] enemies = FindObjectsByType<PatrolEnemy>(FindObjectsSortMode.None);
        Vector3 platePosition = transform.position;

        for (int i = 0; i < enemies.Length; i++)
        {
            PatrolEnemy enemy = enemies[i];

            if (enemy == null || !enemy.isActiveAndEnabled)
                continue;

            if (IsSameTile(enemy.GetCurrentTilePosition(), platePosition))
                return true;
        }

        return false;
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

    private void SetPressed(bool pressed)
    {
        if (isPressed == pressed) return;

        isPressed = pressed;

        PlayPlateSound();
        ApplyElectricTileStates();
    }

    private void PlayPlateSound()
    {
        if (AudioManager.I == null) return;

        AudioClip clip = isPressed
            ? AudioManager.I.sfxPlateDown
            : AudioManager.I.sfxPlateUp;

        AudioManager.I.PlayOneShot(clip, 0.9f);
    }

    private void ApplyElectricTileStates()
    {
        SetElectricTiles(targetElectricTiles, !isPressed);
        SetElectricTiles(targetElectricTilesToActivate, isPressed);
    }

    private void SetElectricTiles(ElectricTile[] tiles, bool active)
    {
        if (tiles == null) return;

        foreach (ElectricTile tile in tiles)
        {
            if (tile == null) continue;

            tile.SetElectricState(active);
        }
    }
}
