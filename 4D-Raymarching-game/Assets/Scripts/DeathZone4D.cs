using UnityEngine;

public class DeathZone4D : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Включить постоянное движение объекта")]
    public bool enableMovement = true;

    [Tooltip("Скорость движения объекта (не затухает)")]
    public Vector4 velocity = Vector4.zero;

    [Tooltip("Множитель скорости")]
    [Range(0.1f, 10f)]
    public float speedMultiplier = 1f;

    [Header("Rotation Settings")]
    [Tooltip("Включить вращение объекта")]
    public bool enableRotation = false;

    [Tooltip("Скорость вращения в 3D (градусы в секунду)")]
    public Vector3 rotationSpeed3D = Vector3.zero;

    [Tooltip("Скорость вращения в 4D плоскостях (градусы в секунду)")]
    public Vector3 rotationSpeedW = Vector3.zero;

    [Header("Movement Bounds")]
    [Tooltip("Если true, объект будет отскакивать от границ")]
    public bool bounceAtBounds = false;

    [Tooltip("Границы движения по XYZ")]
    public Vector3 boundsMin = new Vector3(-10f, -10f, -10f);
    public Vector3 boundsMax = new Vector3(10f, 10f, 10f);

    [Tooltip("Границы движения по W")]
    public float boundsWMin = -10f;
    public float boundsWMax = 10f;

    [Header("Path Settings")]
    [Tooltip("Если true, объект движется по заданным точкам")]
    public bool followPath = false;

    [Tooltip("Точки пути (в 4D пространстве)")]
    public Vector4[] pathPoints = new Vector4[0];

    [Tooltip("Скорость движения по пути (единиц в секунду)")]
    public float pathSpeed = 5f;

    [Tooltip("Зациклить путь")]
    public bool loopPath = true;

    [Tooltip("Двигаться по пути туда-обратно")]
    public bool pingPong = false;

    // Приватные переменные
    private Shape4D shape4D;
    private int currentPathIndex = 0;
    private bool pathForward = true;
    private float pathProgress = 0f;

    void Start()
    {
        shape4D = GetComponent<Shape4D>();
        if (shape4D == null)
        {
            Debug.LogWarning($"DeathZone4D на объекте {gameObject.name}: не найден компонент Shape4D. Движение по W не будет применяться.");
        }
    }

    void Update()
    {
        if (followPath && pathPoints.Length > 0)
        {
            MoveAlongPath();
        }
        else if (enableMovement)
        {
            MoveWithConstantVelocity();
        }

        if (enableRotation)
        {
            ApplyRotation();
        }

        // Применяем движение к компоненту Shape4D
        ApplyMovementToShape4D();
    }

    void MoveWithConstantVelocity()
    {
        // Двигаем объект в 3D пространстве
        Vector3 movement3D = new Vector3(velocity.x, velocity.y, velocity.z) * speedMultiplier * Time.deltaTime;
        transform.Translate(movement3D, Space.World);

        // Проверяем границы и отскакиваем если нужно
        if (bounceAtBounds)
        {
            CheckBoundsAndBounce();
        }
    }

    void MoveAlongPath()
    {
        if (pathPoints.Length == 0) return;

        // Получаем текущую и следующую точку
        int nextIndex = pathForward ? currentPathIndex + 1 : currentPathIndex - 1;

        // Проверяем выход за границы массива
        if (nextIndex >= pathPoints.Length || nextIndex < 0)
        {
            if (pingPong)
            {
                // Разворачиваем направление
                pathForward = !pathForward;
                nextIndex = pathForward ? currentPathIndex + 1 : currentPathIndex - 1;
            }
            else if (loopPath)
            {
                // Зацикливаем
                if (pathForward)
                    nextIndex = 0;
                else
                    nextIndex = pathPoints.Length - 1;
            }
            else
            {
                // Останавливаемся на последней точке
                return;
            }
        }

        Vector4 currentPoint = pathPoints[currentPathIndex];
        Vector4 nextPoint = pathPoints[nextIndex];

        // Двигаемся к следующей точке
        float distance = Vector4.Distance(currentPoint, nextPoint);
        if (distance > 0)
        {
            pathProgress += pathSpeed * Time.deltaTime / distance;

            if (pathProgress >= 1f)
            {
                // Достигли следующей точки
                pathProgress = 0f;
                currentPathIndex = nextIndex;

                // Применяем позицию следующей точки
                Vector4 targetPos = pathPoints[currentPathIndex];
                SetPosition(targetPos);
            }
            else
            {
                // Интерполируем между точками
                Vector4 interpolatedPos = Vector4.Lerp(currentPoint, nextPoint, pathProgress);
                SetPosition(interpolatedPos);
            }
        }
    }

    void CheckBoundsAndBounce()
    {
        Vector3 pos = transform.position;
        Vector3 newVelocity3D = new Vector3(velocity.x, velocity.y, velocity.z);
        bool bounced = false;

        // Проверка по X
        if (pos.x < boundsMin.x || pos.x > boundsMax.x)
        {
            newVelocity3D.x = -newVelocity3D.x;
            pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);
            bounced = true;
        }

        // Проверка по Y
        if (pos.y < boundsMin.y || pos.y > boundsMax.y)
        {
            newVelocity3D.y = -newVelocity3D.y;
            pos.y = Mathf.Clamp(pos.y, boundsMin.y, boundsMax.y);
            bounced = true;
        }

        // Проверка по Z
        if (pos.z < boundsMin.z || pos.z > boundsMax.z)
        {
            newVelocity3D.z = -newVelocity3D.z;
            pos.z = Mathf.Clamp(pos.z, boundsMin.z, boundsMax.z);
            bounced = true;
        }

        // Проверка по W
        float currentW = GetWPosition();
        float newVelocityW = velocity.w;
        if (currentW < boundsWMin || currentW > boundsWMax)
        {
            newVelocityW = -newVelocityW;
            currentW = Mathf.Clamp(currentW, boundsWMin, boundsWMax);
            SetWPosition(currentW);
            bounced = true;
        }

        if (bounced)
        {
            velocity = new Vector4(newVelocity3D.x, newVelocity3D.y, newVelocity3D.z, newVelocityW);
            transform.position = pos;
        }
    }

    void ApplyRotation()
    {
        // 3D вращение
        if (rotationSpeed3D != Vector3.zero)
        {
            transform.Rotate(rotationSpeed3D * Time.deltaTime);
        }

        // 4D вращение
        if (rotationSpeedW != Vector3.zero && shape4D != null)
        {
            shape4D.rotationW += rotationSpeedW * Time.deltaTime;
        }
    }

    void ApplyMovementToShape4D()
    {
        if (shape4D != null && enableMovement && !followPath)
        {
            // Применяем движение по W если не используем путь
            float currentW = shape4D.positionW;
            currentW += velocity.w * speedMultiplier * Time.deltaTime;
            shape4D.positionW = currentW;
        }
    }

    void SetPosition(Vector4 newPosition)
    {
        // Устанавливаем 3D позицию
        transform.position = new Vector3(newPosition.x, newPosition.y, newPosition.z);

        // Устанавливаем W позицию
        SetWPosition(newPosition.w);
    }

    float GetWPosition()
    {
        if (shape4D != null)
            return shape4D.positionW;
        return 0f;
    }

    void SetWPosition(float w)
    {
        if (shape4D != null)
            shape4D.positionW = w;
    }

    // Метод для респавна игрока (существующая функциональность)
    public void RespawnPlayer(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("DeathZone: RespawnPlayer вызван без Transform игрока.");
            return;
        }

        // Используем чекпоинт вместо прямого респавна
        Checkpoint4D.RespawnAtLastCheckpoint(playerTransform);
    }

    private System.Collections.IEnumerator RespawnRoutine(PlayerController playerController)
    {
        yield return null;
        playerController.ResetPlayer();
        Debug.Log($"Player {playerController.gameObject.name} was sent back to start by {gameObject.name}");
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        // Отображаем направление движения
        if (enableMovement)
        {
            Gizmos.color = Color.red;
            Vector3 dir = new Vector3(velocity.x, velocity.y, velocity.z).normalized;
            if (dir.magnitude > 0)
            {
                Gizmos.DrawRay(transform.position, dir * 2f);

                // Стрелка на конце
                Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 + 30, 0) * Vector3.forward;
                Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 - 30, 0) * Vector3.forward;
                Vector3 end = transform.position + dir * 2f;
                Gizmos.DrawRay(end, right * 0.5f);
                Gizmos.DrawRay(end, left * 0.5f);
            }

            // W-ось
            if (Mathf.Abs(velocity.w) > 0.01f)
            {
                Gizmos.color = Color.cyan;
                Vector3 wDir = velocity.w > 0 ? Vector3.up : Vector3.down;
                Gizmos.DrawRay(transform.position, wDir * Mathf.Abs(velocity.w) * 0.5f);
            }
        }

        // Отображаем границы
        if (bounceAtBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = (boundsMin + boundsMax) * 0.5f;
            Vector3 size = boundsMax - boundsMin;
            Gizmos.DrawWireCube(center, size);
        }

        // Отображаем путь
        if (followPath && pathPoints.Length > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < pathPoints.Length; i++)
            {
                Vector3 point = new Vector3(pathPoints[i].x, pathPoints[i].y, pathPoints[i].z);
                Gizmos.DrawSphere(point, 0.3f);

                if (i < pathPoints.Length - 1)
                {
                    Vector3 nextPoint = new Vector3(pathPoints[i + 1].x, pathPoints[i + 1].y, pathPoints[i + 1].z);
                    Gizmos.DrawLine(point, nextPoint);
                }
                else if (loopPath)
                {
                    Vector3 firstPoint = new Vector3(pathPoints[0].x, pathPoints[0].y, pathPoints[0].z);
                    Gizmos.DrawLine(point, firstPoint);
                }
            }
        }
    }
}

