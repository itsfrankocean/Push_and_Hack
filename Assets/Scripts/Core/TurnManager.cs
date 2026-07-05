using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("턴 액터 목록")]
    [Tooltip("비워두면 씬 안의 ITurnActor를 자동으로 찾습니다.")]
    [SerializeField] private MonoBehaviour[] actorBehaviours;

    private readonly List<ITurnActor> actors = new List<ITurnActor>();

    public bool IsBusy
    {
        get
        {
            RemoveInvalidActors();

            for (int i = 0; i < actors.Count; i++)
            {
                if (actors[i] != null && actors[i].IsBusy)
                    return true;
            }

            return false;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RefreshActors();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RefreshActors()
    {
        actors.Clear();

        if (actorBehaviours != null && actorBehaviours.Length > 0)
        {
            RegisterActorsFromInspector();
        }
        else
        {
            AutoFindActors();
        }

        RemoveInvalidActors();
    }

    private void RegisterActorsFromInspector()
    {
        for (int i = 0; i < actorBehaviours.Length; i++)
        {
            MonoBehaviour behaviour = actorBehaviours[i];

            if (behaviour == null)
                continue;

            ITurnActor actor = behaviour as ITurnActor;

            if (actor == null)
            {
                Debug.LogWarning($"{behaviour.name}에는 ITurnActor가 구현되어 있지 않습니다.");
                continue;
            }

            if (!actors.Contains(actor))
                actors.Add(actor);
        }
    }

    private void AutoFindActors()
    {
        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            ITurnActor actor = behaviours[i] as ITurnActor;

            if (actor == null)
                continue;

            if (!actors.Contains(actor))
                actors.Add(actor);
        }
    }

    public void StepTurn()
    {
        RemoveInvalidActors();

        if (IsBusy)
            return;

        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i] != null)
                actors[i].StepTurn();
        }
    }

    public void UndoTurn()
    {
        RemoveInvalidActors();

        for (int i = 0; i < actors.Count; i++)
        {
            if (actors[i] != null)
                actors[i].UndoTurn();
        }
    }

    private void RemoveInvalidActors()
    {
        actors.RemoveAll(actor =>
        {
            MonoBehaviour behaviour = actor as MonoBehaviour;

            return behaviour == null || !behaviour.isActiveAndEnabled;
        });
    }
}