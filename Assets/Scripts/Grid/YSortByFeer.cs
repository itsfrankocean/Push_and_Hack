using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SpriteRenderer))]
public class YSortByFeet : MonoBehaviour
{
    [Tooltip("Sort origin, usually the object's feet. Uses this transform when empty.")]
    public Transform sortPoint;

    [Tooltip("Use this to nudge an object forward or backward when it shares the same Y.")]
    public int orderOffset = 0;

    [Tooltip("Sorting precision. 100 works well for 1-unit grid tiles.")]
    public int precision = 100;

    private SpriteRenderer sr;

    public void Configure(Transform newSortPoint, int newOrderOffset = 0, int newPrecision = 100)
    {
        sortPoint = newSortPoint != null ? newSortPoint : transform;
        orderOffset = newOrderOffset;
        precision = Mathf.Max(1, newPrecision);

        if (sr == null)
            sr = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        if (sortPoint == null)
            sortPoint = transform;
    }

    private void LateUpdate()
    {
        if (sr == null)
            sr = GetComponent<SpriteRenderer>();

        if (sr == null)
            return;

        if (sortPoint == null)
            sortPoint = transform;

        sr.sortingOrder = orderOffset - Mathf.RoundToInt(sortPoint.position.y * precision);
    }
}

public static class YSortAutoInstaller
{
    private const int DefaultPrecision = 100;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        InstallInScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallInScene();
    }

    private static void InstallInScene()
    {
        InstallForPlayers();
        InstallForBoxes();
        InstallForPatrolEnemies();
        InstallForElevators();
        InstallForNamedSceneObjects();
    }

    private static void InstallForPlayers()
    {
        PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            PlayerController player = players[i];
            if (player == null)
                continue;

            Transform sortPoint = FindChildRecursive(player.transform, "SortPoint");
            InstallForRenderers(player.GetComponentsInChildren<SpriteRenderer>(true), sortPoint);
        }
    }

    private static void InstallForBoxes()
    {
        PushableBox[] boxes = Object.FindObjectsByType<PushableBox>(FindObjectsSortMode.None);

        for (int i = 0; i < boxes.Length; i++)
        {
            PushableBox box = boxes[i];
            if (box == null)
                continue;

            Transform sortPoint = FindChildRecursive(box.transform, "SortPoint");
            InstallForRenderers(box.GetComponentsInChildren<SpriteRenderer>(true), sortPoint);
        }
    }

    private static void InstallForPatrolEnemies()
    {
        PatrolEnemy[] enemies = Object.FindObjectsByType<PatrolEnemy>(FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            PatrolEnemy enemy = enemies[i];
            if (enemy == null)
                continue;

            InstallForRenderers(enemy.GetComponentsInChildren<SpriteRenderer>(true), enemy.transform);
        }
    }

    private static void InstallForElevators()
    {
        Elevator[] elevators = Object.FindObjectsByType<Elevator>(FindObjectsSortMode.None);

        for (int i = 0; i < elevators.Length; i++)
        {
            Elevator elevator = elevators[i];
            if (elevator == null)
                continue;

            Transform sortPoint = FindChildRecursive(elevator.transform, "SortPoint");
            InstallForRenderers(elevator.GetComponentsInChildren<SpriteRenderer>(true), sortPoint != null ? sortPoint : elevator.transform);
        }
    }

    private static void InstallForNamedSceneObjects()
    {
        SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!IsKnownLooseSortable(renderer))
                continue;

            Install(renderer, renderer.transform);
        }
    }

    private static bool IsKnownLooseSortable(SpriteRenderer renderer)
    {
        Transform target = renderer.transform;

        while (target != null)
        {
            if (target.name.Contains("Enemy.gun.ready") || target.name == "KeyCard")
                return true;

            target = target.parent;
        }

        return false;
    }

    private static void InstallForRenderers(SpriteRenderer[] renderers, Transform sortPoint)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
            Install(renderers[i], sortPoint);
    }

    private static void Install(SpriteRenderer renderer, Transform sortPoint)
    {
        if (renderer == null)
            return;

        YSortByFeet sorter = renderer.GetComponent<YSortByFeet>();
        if (sorter == null)
            sorter = renderer.gameObject.AddComponent<YSortByFeet>();

        sorter.Configure(sortPoint != null ? sortPoint : renderer.transform, 0, DefaultPrecision);
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
