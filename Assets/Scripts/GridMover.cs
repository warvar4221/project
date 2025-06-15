using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GridMover : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float cellSize = 2f;
    public bool isPlayer = false;

    private MazeGenerator maze;
    private Vector2Int currentCell;
    private Vector2Int targetCell;
    private Vector2Int requestedDirection;
    private bool isMoving = false;
    private float moveProgress = 0f;

    public bool IsMoving => isMoving;

    void Start()
    {
        maze = FindObjectOfType<MazeGenerator>();
        currentCell = maze.WorldToGrid(transform.position);
        targetCell = currentCell;
        transform.position = maze.GridToWorld(currentCell);
    }

    void Update()
    {
        if (isMoving)
        {
            moveProgress += moveSpeed * Time.deltaTime / cellSize;
            if (moveProgress >= 1f)
            {
                moveProgress = 0f;
                isMoving = false;
                currentCell = targetCell;
                transform.position = maze.GridToWorld(currentCell);
            }
            else
            {
                Vector3 from = maze.GridToWorld(currentCell);
                Vector3 to = maze.GridToWorld(targetCell);
                transform.position = Vector3.Lerp(from, to, moveProgress);
            }
        }
        else
        {
            if (requestedDirection != Vector2Int.zero && CanMove(requestedDirection))
            {
                StartMove(requestedDirection);
            }
        }
    }

    private void StartMove(Vector2Int dir)
    {
        if (Mathf.Abs(dir.x) + Mathf.Abs(dir.y) != 1)
        {
            Debug.LogWarning("GridMover: Invalid diagonal move attempted: " + dir);
            return;
        }
        targetCell = currentCell + dir;
        isMoving = true;
        moveProgress = 0f;
        transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y));
    }

    public void RequestMove(Vector2Int dir)
    {
        if (dir != Vector2Int.zero)
            requestedDirection = dir;
    }

    public void ClearRequestedDirection()
    {
        requestedDirection = Vector2Int.zero;
    }

    // ===== НОВЫЙ МЕТОД ДЛЯ ОСТАНОВКИ ДВИЖЕНИЯ =====
    /// <summary>
    /// Немедленно останавливает текущее движение и возвращает объект в исходную клетку.
    /// </summary>
    public void StopMove()
    {
        if (isMoving)
        {
            isMoving = false;
            moveProgress = 0f;
            // Возвращаем на позицию клетки, из которой началось движение
            transform.position = maze.GridToWorld(currentCell);
            // Целевая клетка теперь та же, где мы и находимся
            targetCell = currentCell;
        }
        // Также сбрасываем любой "запомненный" запрос на движение
        ClearRequestedDirection();
    }
    // =================================================

    public Vector2Int GetCurrentCell() => currentCell;

    public bool CanMove(Vector2Int dir)
    {
        Vector2Int next = currentCell + dir;
        return maze.IsCellWalkable(next.x, next.y);
    }
}