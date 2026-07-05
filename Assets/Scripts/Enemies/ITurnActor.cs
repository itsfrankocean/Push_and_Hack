public interface ITurnActor
{
    bool IsBusy { get; }

    void StepTurn();
    void UndoTurn();
}