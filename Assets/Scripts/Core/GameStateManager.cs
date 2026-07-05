using UnityEngine;

public enum GameState
{
    StageIntro,
    Playing,
    Aiming,
    Targeting,
    Paused,
    StageClear,
    Dead
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("초기 상태")]
    [SerializeField] private GameState initialState = GameState.Playing;

    public GameState CurrentState { get; private set; }

    public bool IsPlaying => CurrentState == GameState.Playing;
    public bool IsAiming => CurrentState == GameState.Aiming;
    public bool IsTargeting => CurrentState == GameState.Targeting;
    public bool IsPaused => CurrentState == GameState.Paused;
    public bool IsStageIntro => CurrentState == GameState.StageIntro;
    public bool IsStageClear => CurrentState == GameState.StageClear;
    public bool IsDead => CurrentState == GameState.Dead;

    public bool CanPlayerMoveInput => CurrentState == GameState.Playing;
    public bool CanUndoInput => CurrentState == GameState.Playing;

    public bool CanCombatInput =>
        CurrentState == GameState.Playing ||
        CurrentState == GameState.Aiming;

    public bool CanPopupInput =>
        CurrentState == GameState.Playing ||
        CurrentState == GameState.Aiming;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentState = initialState;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        Debug.Log($"GameState 변경: {newState}");
    }
}
