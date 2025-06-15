using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Размеры лабиринта")]
    public int width = 15;
    public int height = 15;
    public float cellSize = 2f;

    [Header("Настройки генерации")]
    [Tooltip("Сколько дополнительных проходов создать для большей связности")]
    public int extraPassagesCount = 15;

    [Header("Объекты для спавна")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject playerPrefab;
    public GameObject pelletPrefab;
    public GameObject enemyPrefab;

    [Header("Настройки спавна")]
    public float minEnemyDistanceFromPlayer = 4f;
    public int enemyCount = 3;
    public int pelletCount = 30;

    private bool[,] maze;
    private readonly List<Vector2Int> pathCells = new List<Vector2Int>();
    private Vector2Int playerSpawnPosition;

    void Start()
    {
        // Убедимся, что размеры нечетные для корректной работы алгоритма
        if (width % 2 == 0) width++;
        if (height % 2 == 0) height++;

        GenerateMaze();
        SpawnMaze();

        playerSpawnPosition = GetRandomFreeCell();
        SpawnPlayer(playerSpawnPosition);
        SpawnEnemies(playerSpawnPosition);
        SpawnPellets();
    }

    public bool IsCellWalkable(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;
        return maze[x, y];
    }

    public Vector3 GridToWorld(Vector2Int cell)
    {
        return new Vector3(cell.x * cellSize, 1f, cell.y * cellSize);
    }

    public Vector2Int WorldToGrid(Vector3 position)
    {
        return new Vector2Int(
            Mathf.RoundToInt(position.x / cellSize),
            Mathf.RoundToInt(position.z / cellSize)
        );
    }

    void GenerateMaze()
    {
        maze = new bool[width, height];
        var stack = new Stack<Vector2Int>();
        var rng = new System.Random();

        // Начинаем с нечетной координаты
        Vector2Int current = new Vector2Int(1, 1);
        maze[current.x, current.y] = true;
        stack.Push(current);
        pathCells.Add(current);

        var directions = new Vector2Int[] {
            Vector2Int.up * 2,
            Vector2Int.right * 2,
            Vector2Int.down * 2,
            Vector2Int.left * 2
        };

        while (stack.Count > 0)
        {
            current = stack.Pop();
            directions = Shuffle(directions, rng);

            foreach (var dir in directions)
            {
                Vector2Int next = current + dir;
                if (IsInBounds(next) && !maze[next.x, next.y])
                {
                    maze[next.x, next.y] = true;
                    Vector2Int mid = current + dir / 2;
                    maze[mid.x, mid.y] = true;

                    pathCells.Add(mid);
                    pathCells.Add(next);

                    stack.Push(current); // Повторно добавляем текущую, чтобы создать больше ветвлений
                    stack.Push(next);
                    break; // Переходим к следующей клетке, чтобы сделать лабиринт более ветвистым
                }
            }
        }

        // Создаем дополнительные проходы для лучшей связности
        AddExtraPassages(rng, extraPassagesCount);
    }

    /// <summary>
    /// Улучшенный метод для создания дополнительных проходов в лабиринте.
    /// </summary>
    void AddExtraPassages(System.Random rng, int count)
    {
        if (count <= 0) return;

        int passagesAdded = 0;
        int attempts = 0;
        int maxAttempts = width * height; // Защита от бесконечного цикла

        while (passagesAdded < count && attempts < maxAttempts)
        {
            attempts++;
            // Выбираем случайную внутреннюю точку
            int x = rng.Next(1, width - 1);
            int y = rng.Next(1, height - 1);

            // Нас интересуют только стены
            if (maze[x, y]) continue;

            // Проверяем, разделяет ли стена два коридора (горизонтально или вертикально)
            bool isHorizontalPassage = IsInBounds(new Vector2Int(x - 1, y)) && maze[x - 1, y] && IsInBounds(new Vector2Int(x + 1, y)) && maze[x + 1, y];
            bool isVerticalPassage = IsInBounds(new Vector2Int(x, y - 1)) && maze[x, y - 1] && IsInBounds(new Vector2Int(x, y + 1)) && maze[x, y + 1];

            if (isHorizontalPassage || isVerticalPassage)
            {
                maze[x, y] = true;
                pathCells.Add(new Vector2Int(x, y));
                passagesAdded++;
            }
        }
        Debug.Log($"[MazeGenerator] Создано {passagesAdded} дополнительных проходов.");
    }

    void SpawnMaze()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize);
                Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                if (!maze[x, y])
                    Instantiate(wallPrefab, pos + Vector3.up, Quaternion.identity, transform);
            }
        }
    }

    void SpawnPlayer(Vector2Int cell)
    {
        Instantiate(playerPrefab, GridToWorld(cell), Quaternion.identity);
    }

    void SpawnEnemies(Vector2Int playerCell)
    {
        var rng = new System.Random();
        int attempts = 0, spawned = 0;

        while (spawned < enemyCount && attempts < 1000)
        {
            Vector2Int cell = pathCells[rng.Next(pathCells.Count)];
            if (Vector2Int.Distance(cell, playerCell) >= minEnemyDistanceFromPlayer)
            {
                Instantiate(enemyPrefab, GridToWorld(cell), Quaternion.identity);
                spawned++;
            }
            attempts++;
        }
    }

    void SpawnPellets()
    {
        var rng = new System.Random();
        int placed = 0;
        int attempts = 0;
        // Используем HashSet для отслеживания уже занятых клеток
        var occupiedCells = new HashSet<Vector2Int> { playerSpawnPosition };

        while (placed < pelletCount && attempts < 1000)
        {
            attempts++;
            Vector2Int cell = pathCells[rng.Next(pathCells.Count)];

            // Не спавним рядом с краями и если клетка уже занята
            if (cell.x < 2 || cell.y < 2 || cell.x > width - 3 || cell.y > height - 3 || occupiedCells.Contains(cell)) continue;

            Vector3 pos = GridToWorld(cell);
            Instantiate(pelletPrefab, pos, Quaternion.identity, transform);
            occupiedCells.Add(cell); // Помечаем клетку как занятую
            placed++;
        }
    }

    Vector2Int GetRandomFreeCell()
    {
        var rng = new System.Random();
        for (int i = 0; i < 100; i++)
        {
            Vector2Int cell = pathCells[rng.Next(pathCells.Count)];
            if (cell.x > 1 && cell.y > 1 && cell.x < width - 2 && cell.y < height - 2)
                return cell;
        }
        return new Vector2Int(1, 1);
    }

    bool IsInBounds(Vector2Int pos) => pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;

    Vector2Int[] Shuffle(Vector2Int[] array, System.Random rng)
    {
        for (int i = 0; i < array.Length - 1; i++)
        {
            int j = rng.Next(i, array.Length);
            (array[i], array[j]) = (array[j], array[i]);
        }
        return array;
    }
}