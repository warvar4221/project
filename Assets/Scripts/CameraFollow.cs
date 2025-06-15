// CameraFollow.cs — исправлено: поворот только при повороте игрока, движение камеры только при влево/вправо, без дёрганий сверху
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Camera Position")]
    public float height = 10f;
    public float distance = 10f;
    public float verticalAngle = 45f;

    [Header("Smoothness")]
    public float followSmoothness = 0.125f;
    public float rotateSmoothness = 10f;

    [Header("Fixed Top View")]
    public bool fixedTopView = false;
    public float topViewMargin = 2f;

    private Transform playerTransform;
    private Quaternion targetRotation;
    private Vector3 targetPosition;
    private MazeGenerator mazeGenerator;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        mazeGenerator = FindObjectOfType<MazeGenerator>();

        if (mazeGenerator == null)
            Debug.LogError("MazeGenerator not found in scene!");
    }

    void LateUpdate()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                targetRotation = Quaternion.Euler(verticalAngle, playerTransform.eulerAngles.y, 0f);
                transform.rotation = targetRotation;
                transform.position = CalculateCameraPosition();
            }
            else
            {
                return;
            }
        }

        if (fixedTopView)
        {
            UpdateFixedTopView();
        }
        else
        {
            targetPosition = CalculateCameraPosition();
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSmoothness);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSmoothness * Time.deltaTime);
        }
    }

    void UpdateFixedTopView()
    {
        if (mazeGenerator == null) return;

        float mazeWidth = mazeGenerator.width * mazeGenerator.cellSize;
        float mazeHeight = mazeGenerator.height * mazeGenerator.cellSize;
        Vector3 mazeCenter = new Vector3(mazeWidth / 2, 0, mazeHeight / 2);

        if (cam.orthographic)
        {
            float maxDimension = Mathf.Max(mazeWidth, mazeHeight);
            cam.orthographicSize = maxDimension / 2 + topViewMargin;
            transform.position = mazeCenter + Vector3.up * (maxDimension * 0.7f);
        }
        else
        {
            float requiredHeight = Mathf.Max(mazeWidth, mazeHeight) / (2 * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
            requiredHeight += topViewMargin;
            transform.position = mazeCenter + Vector3.up * requiredHeight;
        }

        transform.rotation = Quaternion.Euler(90f, 0, 0);
    }

    Vector3 CalculateCameraPosition()
    {
        Vector3 offsetDirection = Quaternion.Euler(-verticalAngle, 0, 0) * Vector3.forward;
        Vector3 finalOffset = offsetDirection * distance + Vector3.up * height;
        Vector3 rotatedOffset = targetRotation * finalOffset;
        return playerTransform.position + rotatedOffset;
    }

    public void RotateToDirection(Quaternion newPlayerRotation)
    {
        if (fixedTopView) return;

        // Обновляем направление камеры только при явном повороте игрока
        targetRotation = Quaternion.Euler(verticalAngle, newPlayerRotation.eulerAngles.y, 0f);
    }

    public void ToggleCameraMode()
    {
        fixedTopView = !fixedTopView;
    }
}