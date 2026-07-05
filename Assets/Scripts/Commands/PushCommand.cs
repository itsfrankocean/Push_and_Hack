using UnityEngine;

public class PushCommand : ICommand
{
    private PlayerController player;
    private PushableBox box;
    private Vector3 direction;

    public PushCommand(PlayerController p, PushableBox b, Vector3 dir)
    {
        player = p;
        box = b;
        direction = dir;
    }

    public bool Execute()
    {
        if (player == null || box == null)
            return false;

        bool moved = box.Move(direction);

        if (!moved)
            return false;

        player.DoPushAnim();
        player.StepEnemies();
        player.KillIfOnEnemyTile();

        return true;
    }

    public void Undo()
    {
        if (player == null || box == null)
            return;

        box.UndoMove(-direction);
        player.UndoEnemies();
    }
}
