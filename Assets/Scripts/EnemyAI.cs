using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(GridMover))]
public class EnemyAI : MonoBehaviour
{
    [Header("Настройки обнаружения и погони")]
    public int detectionRange = 10;
    public float chaseDuration = 15f;
    public float minDistanceToPlayer = 0.5f;

    private enum State { Patrol, Chasing }
    private State state = State.Patrol;

    private GridMover mover;
    private MazeGenerator maze;
    private Transform player;

    private float chaseTimer = 0f;
    private readonly Queue<Vector2Int> playerTrail = new();
    private List<Vector2Int> currentPath = new();

    private bool isApproachingTrailStart = false;
    private Vector2Int lastPlayerGridPos;

    private Vector2Int lastDirection = Vector2Int.zero;
    private readonly Queue<Vector2Int> visitedCells = new();
    private readonly int visitedMax = 6;


    void Start()
    {
        mover = GetComponent<GridMover>();
        maze = FindObjectOfType<MazeGenerator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogError("[EnemyAI] Игрок с тегом 'Player' не найден. Отключаю скрипт.");
            enabled = false;
        }
    }

    void Update()
    {
        if (player == null) return;

        bool seesPlayer = CanSeePlayer();

        if (seesPlayer)
        {
            if (state == State.Patrol)
            {
                // --- НАЧАЛО ПОГОНИ ---
                Debug.Log("[Enemy] Обнаружен игрок! Прерываю патрулирование.");

                // ===== ГЛАВНОЕ ИСПРАВЛЕНИЕ: НЕМЕДЛЕННО ОСТАНАВЛИВАЕМ ТЕКУЩЕЕ ДВИЖЕНИЕ =====
                mover.StopMove();
                // =========================================================================

                state = State.Chasing;

                playerTrail.Clear();
                currentPath.Clear();

                // Теперь mover.GetCurrentCell() вернет актуальную и стабильную позицию
                Vector2Int enemyCell = mover.GetCurrentCell();
                Vector2Int firstSightingCell = maze.WorldToGrid(player.position);

                currentPath = AStarPath(enemyCell, firstSightingCell);
                isApproachingTrailStart = true;

                lastPlayerGridPos = firstSightingCell;
                playerTrail.Enqueue(firstSightingCell);
            }
            chaseTimer = chaseDuration;
        }
        else if (state == State.Chasing)
        {
            chaseTimer -= Time.deltaTime;
            if (chaseTimer <= 0f)
            {
                Debug.Log("[Enemy] Время погони вышло. Возвращаюсь к патрулированию.");
                state = State.Patrol;
                isApproachingTrailStart = false;
                currentPath.Clear();
                playerTrail.Clear();
            }
        }

        if (state == State.Chasing)
        {
            Vector2Int playerCell = maze.WorldToGrid(player.position);
            if (playerCell != lastPlayerGridPos)
            {
                playerTrail.Enqueue(playerCell);
                lastPlayerGridPos = playerCell;
            }
        }

        switch (state)
        {
            case State.Patrol:
                Patrol(mover.GetCurrentCell());
                break;
            case State.Chasing:
                PerformChase(mover.GetCurrentCell());
                break;
        }
    }

    private void PerformChase(Vector2Int currentCell)
    {
        if (!mover.IsMoving && (currentPath == null || currentPath.Count == 0))
        {
            if (isApproachingTrailStart)
            {
                Debug.Log("[Enemy] Достиг начальной точки следа. Начинаю следование по крошкам.");
                isApproachingTrailStart = false;
            }

            if (!isApproachingTrailStart && playerTrail.Count > 0)
            {
                Vector2Int nextCrumb = playerTrail.Peek();

                if (currentCell == nextCrumb)
                {
                    playerTrail.Dequeue();
                    if (playerTrail.Count > 0)
                    {
                        nextCrumb = playerTrail.Peek();
                    }
                    else
                    {
                        return;
                    }
                }

                List<Vector2Int> newPath = AStarPath(currentCell, nextCrumb);

                if (newPath != null && newPath.Count > 0)
                {
                    currentPath = newPath;
                }
                else
                {
                    Debug.LogWarning($"[EnemyAI] Не удалось построить путь из {currentCell} к крошке {nextCrumb}. Пропускаю ее.");
                    playerTrail.Dequeue();
                }
            }
        }

        FollowCurrentPath(currentCell);
    }

    private void FollowCurrentPath(Vector2Int currentCell)
    {
        if (currentPath == null || currentPath.Count == 0 || mover.IsMoving) return;

        Vector2Int nextCell = currentPath[0];
        Vector2Int direction = nextCell - currentCell;

        if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) > 1)
        {
            Debug.LogError($"[EnemyAI] ОШИБКА ПУТИ A*! Попытка не-соседнего хода из {currentCell} в {nextCell}. Путь сброшен.");
            currentPath.Clear();
            if (isApproachingTrailStart)
            {
                state = State.Patrol;
                isApproachingTrailStart = false;
            }
            return;
        }

        if (mover.CanMove(direction))
        {
            mover.RequestMove(direction);
            currentPath.RemoveAt(0);
        }
        else
        {
            Debug.LogWarning($"[Enemy] Путь в {direction} из {currentCell} заблокирован. Пересчитываю маршрут.");
            currentPath.Clear();
        }
    }

    // Остальные методы (Patrol, AStarPath, CanSeePlayer и т.д.) без изменений...
    #region Utility Functions (A*, CanSeePlayer, etc. - No Changes)
    private void Patrol(Vector2Int currentCell)
    {
        if (!mover.IsMoving)
        {
            Vector2Int dir = GetSmartPatrolDirection(currentCell);
            if (dir != Vector2Int.zero) mover.RequestMove(dir);
        }
    }

    bool CanSeePlayer()
    {
        if (Vector3.Distance(transform.position, player.position) > detectionRange) return false;
        return !Physics.Linecast(transform.position + Vector3.up * 0.5f, player.position + Vector3.up * 0.5f, LayerMask.GetMask("Wall"));
    }

    Vector2Int GetSmartPatrolDirection(Vector2Int current)
    {
        var dirs = GetShuffledDirections();
        foreach (var dir in dirs)
        {
            Vector2Int next = current + dir;
            if (dir == -lastDirection) continue;
            if (maze.IsCellWalkable(next.x, next.y) && !visitedCells.Contains(next))
            {
                if (visitedCells.Count >= visitedMax) visitedCells.Dequeue();
                visitedCells.Enqueue(next);
                lastDirection = dir;
                return dir;
            }
        }
        foreach (var dir in dirs)
        {
            if (maze.IsCellWalkable(current.x + dir.x, current.y + dir.y) && dir != -lastDirection)
            {
                lastDirection = dir;
                return dir;
            }
        }
        if (maze.IsCellWalkable(current.x - lastDirection.x, current.y - lastDirection.y))
        {
            lastDirection = -lastDirection;
            return lastDirection;
        }
        return Vector2Int.zero;
    }

    List<Vector2Int> AStarPath(Vector2Int start, Vector2Int goal)
    {
        var path = new List<Vector2Int>();
        if (!maze.IsCellWalkable(start.x, start.y) || !maze.IsCellWalkable(goal.x, goal.y)) return path;

        var openSet = new PriorityQueue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float> { [start] = 0 };

        openSet.Enqueue(start, Vector2Int.Distance(start, goal));

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();
            if (current == goal)
            {
                var tempPath = new List<Vector2Int>();
                var step = goal;
                while (cameFrom.ContainsKey(step))
                {
                    tempPath.Add(step);
                    step = cameFrom[step];
                }
                tempPath.Reverse();
                return tempPath;
            }

            foreach (var direction in GetDirections())
            {
                var neighbor = current + direction;
                if (!maze.IsCellWalkable(neighbor.x, neighbor.y)) continue;

                var tentativeGScore = gScore.GetValueOrDefault(current, float.PositiveInfinity) + 1;
                if (tentativeGScore < gScore.GetValueOrDefault(neighbor, float.PositiveInfinity))
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    openSet.Enqueue(neighbor, tentativeGScore + Vector2Int.Distance(neighbor, goal));
                }
            }
        }
        return path;
    }

    Vector2Int[] GetDirections() => new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    List<Vector2Int> GetShuffledDirections()
    {
        var dirs = new List<Vector2Int>(GetDirections());
        for (int i = 0; i < dirs.Count; i++)
        {
            int j = Random.Range(i, dirs.Count);
            (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
        }
        return dirs;
    }

    void OnDrawGizmos()
    {
        if (maze == null || mover == null || !Application.isPlaying) return;

        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = isApproachingTrailStart ? Color.magenta : Color.red;
            Vector3 prevPos = maze.GridToWorld(mover.GetCurrentCell());
            foreach (var cell in currentPath)
            {
                Vector3 worldPos = maze.GridToWorld(cell);
                Gizmos.DrawLine(prevPos + Vector3.up * 0.2f, worldPos + Vector3.up * 0.2f);
                prevPos = worldPos;
            }
        }

        if (playerTrail != null && playerTrail.Count > 0)
        {
            Gizmos.color = Color.yellow;
            Vector3 lastPoint = transform.position;
            if (currentPath != null && currentPath.Any())
            {
                lastPoint = maze.GridToWorld(currentPath.Last());
            }

            foreach (var cell in playerTrail)
            {
                Vector3 worldPos = maze.GridToWorld(cell);
                Gizmos.DrawLine(lastPoint + Vector3.up * 0.2f, worldPos + Vector3.up * 0.2f);
                Gizmos.DrawSphere(worldPos + Vector3.up * 0.5f, 0.2f);
                lastPoint = worldPos;
            }
        }
    }
    #endregion
}