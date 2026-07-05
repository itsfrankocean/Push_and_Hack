using System.Collections.Generic;
using UnityEngine;

public class ShootCommand : ICommand
{
    private readonly PlayerCombat combat;

    private int usedAmmoIndex = -1;
    private bool consumedAmmo = false;
    private bool steppedEnemies = false;
    private bool undone = false;

    private GameObject spawnedProjectile;
    private bool hasTransformSwap = false;
    private PlayerController swappedPlayer;
    private PushableBox swappedBox;
    private Vector3 playerPositionBeforeSwap;
    private Vector3 boxPositionBeforeSwap;
    private bool hasPulledBox = false;
    private PushableBox pulledBox;
    private Vector3 boxPositionBeforePull;
    private TargetDisplacementSelection displacementSelection;
    private bool hasDisplacedTarget = false;
    private IProjectileDisplaceable displacedTarget;
    private MonoBehaviour displacedTargetBehaviour;
    private Vector3 targetPositionBeforeDisplace;
    private readonly List<DestroyedBoxState> destroyedBoxes = new List<DestroyedBoxState>();

    public bool IsUndone => undone;

    public ShootCommand(PlayerCombat combat)
    {
        this.combat = combat;
    }

    public bool Execute()
    {
        if (combat == null)
            return false;

        if (!combat.CanStartShootCommand())
            return false;

        if (combat.UsesAmmo)
            usedAmmoIndex = combat.GetCurrentAmmoIndexForUndo();

        if (!combat.TryConsumeAmmoForShot())
            return false;

        consumedAmmo = combat.UsesAmmo;

        bool started = combat.TryStartShootSequence(this);

        if (!started)
        {
            if (consumedAmmo)
                combat.RestoreAmmoForUndo(usedAmmoIndex, 1);

            consumedAmmo = false;
            return false;
        }

        StepEnemiesForShotTurn();

        return true;
    }

    public void RegisterProjectile(GameObject projectile)
    {
        spawnedProjectile = projectile;
    }

    public bool CanApplyProjectileEffect()
    {
        return !undone;
    }

    public void RecordDestroyedBox(PushableBox box)
    {
        if (undone || box == null || !box.CanRestoreAfterDamage)
            return;

        for (int i = 0; i < destroyedBoxes.Count; i++)
        {
            if (destroyedBoxes[i].Box == box)
                return;
        }

        destroyedBoxes.Add(new DestroyedBoxState(box, box.GetCurrentTilePosition()));
    }

    public void RecordTransformSwap(
        PlayerController player,
        PushableBox box,
        Vector3 playerPositionBefore,
        Vector3 boxPositionBefore
    )
    {
        if (undone || player == null || box == null)
            return;

        swappedPlayer = player;
        swappedBox = box;
        playerPositionBeforeSwap = playerPositionBefore;
        boxPositionBeforeSwap = boxPositionBefore;
        hasTransformSwap = true;
    }

    public void Undo()
    {
        undone = true;

        if (displacementSelection != null)
            displacementSelection.CancelFromUndo();

        if (spawnedProjectile != null)
            Object.Destroy(spawnedProjectile);

        if (hasTransformSwap)
        {
            if (swappedPlayer != null)
                swappedPlayer.WarpTo(playerPositionBeforeSwap);

            if (swappedBox != null)
                swappedBox.WarpTo(boxPositionBeforeSwap);
        }

        if (hasPulledBox && pulledBox != null)
            pulledBox.WarpTo(boxPositionBeforePull);

        if (hasDisplacedTarget && displacedTargetBehaviour != null && displacedTarget != null)
            displacedTarget.WarpTo(targetPositionBeforeDisplace);

        for (int i = destroyedBoxes.Count - 1; i >= 0; i--)
            destroyedBoxes[i].Restore();

        if (steppedEnemies && TurnManager.Instance != null)
            TurnManager.Instance.UndoTurn();

        if (combat == null)
            return;

        if (!consumedAmmo)
            return;

        combat.RestoreAmmoForUndo(usedAmmoIndex, 1);
    }

    public void RecordPulledBox(PushableBox box, Vector3 boxPositionBefore)
    {
        if (undone || box == null)
            return;

        pulledBox = box;
        boxPositionBeforePull = boxPositionBefore;
        hasPulledBox = true;
    }

    public void RegisterDisplacementSelection(TargetDisplacementSelection selection)
    {
        if (undone)
            return;

        displacementSelection = selection;
    }

    public void RecordDisplacedTarget(IProjectileDisplaceable target, Vector3 targetPositionBefore)
    {
        if (undone || target == null)
            return;

        MonoBehaviour targetBehaviour = target as MonoBehaviour;
        if (targetBehaviour == null)
            return;

        displacedTarget = target;
        displacedTargetBehaviour = targetBehaviour;
        targetPositionBeforeDisplace = targetPositionBefore;
        hasDisplacedTarget = true;
        displacementSelection = null;
    }

    private void StepEnemiesForShotTurn()
    {
        if (TurnManager.Instance == null)
            return;

        TurnManager.Instance.StepTurn();
        steppedEnemies = true;

        if (combat == null)
            return;

        PlayerController player = combat.GetComponent<PlayerController>();
        if (player != null)
            player.KillIfOnEnemyTile();
    }

    private readonly struct DestroyedBoxState
    {
        public readonly PushableBox Box;
        private readonly Vector3 position;

        public DestroyedBoxState(PushableBox box, Vector3 position)
        {
            Box = box;
            this.position = position;
        }

        public void Restore()
        {
            if (Box != null)
                Box.RestoreAfterDamage(position);
        }
    }
}
