using UnityEngine;

public class CameraFollow4D : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Цель, за которой следует камера (обычно игрок)")]
    public Transform target;

    [Tooltip("Смещение позиции камеры относительно цели")]
    public Vector3 offset = new Vector3(0, 5f, -10f);

    [Header("Smooth Settings")]
    [Tooltip("Время сглаживания движения камеры")]
    [Range(0f, 1f)]
    public float smoothTime = 0.3f;

    [Tooltip("Максимальная скорость камеры")]
    public float maxSpeed = 100f;

    [Header("Look Settings")]
    [Tooltip("Смотреть на цель")]
    public bool lookAtTarget = true;

    [Tooltip("Точка, на которую смотрит камера (смещение от цели)")]
    public Vector3 lookAtOffset = new Vector3(0, 1f, 0);

    [Header("Advanced Settings")]
    [Tooltip("Сглаживать движение по осям отдельно")]
    public bool useSeparateAxisSmoothing = false;

    [Tooltip("Время сглаживания по X")]
    public float smoothTimeX = 0.3f;

    [Tooltip("Время сглаживания по Y")]
    public float smoothTimeY = 0.3f;

    [Tooltip("Время сглаживания по Z")]
    public float smoothTimeZ = 0.3f;

    [Tooltip("Фиксированная высота камеры (если > -999)")]
    public float fixedHeight = -999f;

    [Tooltip("Минимальная дистанция до цели")]
    public float minDistance = 2f;

    [Tooltip("Максимальная дистанция до цели")]
    public float maxDistance = 20f;

    [Header("Collision Settings")]
    [Tooltip("Камера избегает препятствий")]
    public bool avoidObstacles = false;

    [Tooltip("Слой препятствий")]
    public LayerMask obstacleLayer = -1;

    [Tooltip("Радиус проверки препятствий")]
    public float checkRadius = 0.5f;

    [Tooltip("Минимальная дистанция при обходе препятствий")]
    public float minObstacleDistance = 1f;

    private Vector3 currentVelocity;
    private float velocityX, velocityY, velocityZ;
    private Vector3 desiredPosition;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (target == null)
        {
            // Автоматически найти игрока, если цель не назначена
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                // Попробовать найти PlayerController
                PlayerController pc = FindObjectOfType<PlayerController>();
                if (pc != null)
                    target = pc.transform;
            }
            else
            {
                target = player.transform;
            }

            if (target == null)
                Debug.LogWarning("CameraFollow4D: Цель не найдена!");
        }

        // Установить начальную позицию камеры
        if (target != null)
        {
            transform.position = target.position + offset;
            if (lookAtTarget)
                transform.LookAt(target.position + lookAtOffset);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        FollowTarget();
    }

    void FollowTarget()
    {
        // Вычисляем желаемую позицию
        desiredPosition = target.position + offset;

        // Если задана фиксированная высота
        if (fixedHeight > -999f)
        {
            desiredPosition.y = fixedHeight;
        }

        // Плавное движение
        if (useSeparateAxisSmoothing)
        {
            // Раздельное сглаживание по осям
            float newX = Mathf.SmoothDamp(transform.position.x, desiredPosition.x, ref velocityX, smoothTimeX, maxSpeed);
            float newY = Mathf.SmoothDamp(transform.position.y, desiredPosition.y, ref velocityY, smoothTimeY, maxSpeed);
            float newZ = Mathf.SmoothDamp(transform.position.z, desiredPosition.z, ref velocityZ, smoothTimeZ, maxSpeed);

            transform.position = new Vector3(newX, newY, newZ);
        }
        else
        {
            // Общее сглаживание
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime, maxSpeed);
        }

        // Избегание препятствий
        if (avoidObstacles)
        {
            AvoidObstacles();
        }

        // Поворот камеры
        if (lookAtTarget)
        {
            Vector3 lookTarget = target.position + lookAtOffset;

            // Ограничиваем дистанцию
            float distance = Vector3.Distance(transform.position, lookTarget);
            if (distance < minDistance)
            {
                Vector3 direction = (transform.position - lookTarget).normalized;
                transform.position = lookTarget + direction * minDistance;
            }
            else if (distance > maxDistance)
            {
                Vector3 direction = (transform.position - lookTarget).normalized;
                transform.position = lookTarget + direction * maxDistance;
            }

            transform.LookAt(lookTarget);
        }
    }

    void AvoidObstacles()
    {
        Vector3 directionToTarget = (target.position + lookAtOffset) - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        RaycastHit hit;
        if (Physics.SphereCast(transform.position, checkRadius, directionToTarget.normalized, out hit, distanceToTarget, obstacleLayer))
        {
            // Перемещаем камеру перед препятствием
            Vector3 newPosition = hit.point + hit.normal * minObstacleDistance;
            float distanceToNew = Vector3.Distance(transform.position, newPosition);

            if (distanceToNew < distanceToTarget)
            {
                transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * 10f);
            }
        }
    }

    // Публичный метод для смены цели
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    // Публичный метод для смены смещения
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    // Публичный метод для телепортации камеры к цели
    public void SnapToTarget()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            if (lookAtTarget)
                transform.LookAt(target.position + lookAtOffset);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position + lookAtOffset, 0.3f);
            Gizmos.DrawLine(transform.position, target.position + lookAtOffset);
        }
    }
}

