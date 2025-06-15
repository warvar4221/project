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

        // ������ �����
        if (Input.GetKeyDown(KeyCode.W))
        {
            inputDir = fixedView ? Vector2Int.up : WorldToLocal(Vector2Int.up);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            inputDir = fixedView ? Vector2Int.down : WorldToLocal(Vector2Int.down);
            if (!fixedView)
            {
                // ������� �� 180 ��������
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

        // ���� ���� ������ ����� ������� � ��� � ����������
        if (inputDir != Vector2Int.zero)
        {
            queuedDirection = inputDir;
        }

        // ���� �������� ��� � ���� ����������� � ���������� ���
        if (!mover.IsMoving && queuedDirection != Vector2Int.zero)
        {
            if (mover.CanMove(queuedDirection))
            {
                mover.RequestMove(queuedDirection);
                queuedDirection = Vector2Int.zero; // �������� ������� ����� ������
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
