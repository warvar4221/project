using UnityEngine;

[RequireComponent(typeof(GridMover))]
public class PlayerInput : MonoBehaviour
{
    private GridMover mover;
    private CameraFollow camFollow;
    private Vector2Int queuedDirection = Vector2Int.zero;

    void Start()
    {
        mover = GetComponent<GridMover>();
        camFollow = Camera.main.GetComponent<CameraFollow>();
    }

    void Update()
    {
        Vector2Int inputDir = Vector2Int.zero;
        bool fixedView = camFollow?.fixedTopView ?? true;

        // Чтение ввода
        if (Input.GetKeyDown(KeyCode.W))
        {
            inputDir = fixedView ? Vector2Int.up : WorldToLocal(Vector2Int.up);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            inputDir = fixedView ? Vector2Int.down : WorldToLocal(Vector2Int.down);
            if (!fixedView)
            {
                // Поворот на 180 градусов
                transform.Rotate(0, 180, 0);
                camFollow.RotateToDirection(transform.rotation);
            }
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            inputDir = fixedView ? Vector2Int.left : WorldToLocal(Vector2Int.left);
            if (!fixedView)
            {
                transform.Rotate(0, -90, 0);
                camFollow.RotateToDirection(transform.rotation);
            }
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            inputDir = fixedView ? Vector2Int.right : WorldToLocal(Vector2Int.right);
            if (!fixedView)
            {
                transform.Rotate(0, 90, 0);
                camFollow.RotateToDirection(transform.rotation);
            }
        }

        // Если была нажата новая команда — она в приоритете
        if (inputDir != Vector2Int.zero)
        {
            queuedDirection = inputDir;
        }

        // Если возможен ход в этом направлении — используем его
        if (!mover.IsMoving && queuedDirection != Vector2Int.zero)
        {
            if (mover.CanMove(queuedDirection))
            {
                mover.RequestMove(queuedDirection);
                queuedDirection = Vector2Int.zero; // сбросить очередь после старта
            }
        }
    }

    private Vector2Int WorldToLocal(Vector2Int input)
    {
        Vector3 forward = transform.forward;

        int fx = Mathf.RoundToInt(forward.x);
        int fz = Mathf.RoundToInt(forward.z);

        if (input == Vector2Int.up)
            return new Vector2Int(fx, fz);
        else if (input == Vector2Int.down)
            return new Vector2Int(-fx, -fz);
        else if (input == Vector2Int.left)
            return new Vector2Int(-fz, fx);
        else // right
            return new Vector2Int(fz, -fx);
    }
}
