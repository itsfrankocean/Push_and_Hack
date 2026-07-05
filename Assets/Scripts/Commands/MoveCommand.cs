using UnityEngine;

public class MoveCommand : ICommand
{
    private PlayerController player;
    private Vector3 direction;
    private bool pickedUpCard;
    private GameObject cardObject;
    private bool steppedEnemies;

    public MoveCommand(PlayerController p, Vector3 dir, bool gotCard, GameObject card)
    {
        player = p;
        direction = dir;
        pickedUpCard = gotCard;
        cardObject = card;
    }

    public bool Execute()
    {
        if (player == null)
            return false;

        player.DoMove(direction);

        if (pickedUpCard && cardObject != null)
            player.PickUpCard(cardObject);

        if (!player.KillIfOnEnemyTile())
        {
            player.StepEnemies();
            steppedEnemies = true;
            player.KillIfOnEnemyTile();
        }

        return true;
    }

    public void Undo()
    {
        if (player == null)
            return;

        player.UndoMove(direction);

        if (pickedUpCard && cardObject != null)
            player.DropCard(cardObject);

        if (steppedEnemies)
            player.UndoEnemies();
    }
}

public class WaitCommand : ICommand
{
    private PlayerController player;
    private bool steppedEnemies;

    public WaitCommand(PlayerController player)
    {
        this.player = player;
    }

    public bool Execute()
    {
        if (player == null)
            return false;

        if (!player.KillIfOnEnemyTile())
        {
            player.StepEnemies();
            steppedEnemies = true;
            player.KillIfOnEnemyTile();
        }

        return true;
    }

    public void Undo()
    {
        if (player == null)
            return;

        if (steppedEnemies)
            player.UndoEnemies();
    }
}
