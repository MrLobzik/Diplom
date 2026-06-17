using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Distance Settings")]
    public float distance = 10f;
    public float minDistance = 3f;
    public float maxDistance = 20f;
    public float scrollSensitivity = 5f;

    [Header("Horizontal Angle (XZ Plane)")]
    public float mouseSensitivity = 3f;

    [Header("Vertical Angle (Fixed)")]
    [Tooltip("Фиксированный вертикальный угол камеры (0 = горизонтально, 45 = сверху, -45 = снизу)")]
    [Range(-89f, 89f)]
    public float fixedVerticalAngle = 45f;

    [Header("Height Offset")]
    [Tooltip("Дополнительное смещение камеры по высоте")]
    public float heightOffset = 0f;

    [Header("Smooth Settings")]
    public float positionSmoothSpeed = 10f;
    public float rotationSmoothSpeed = 5f;

    [Header("Offset")]
    public Vector3 lookOffset = new Vector3(0, 1.5f, 0);

    private float currentHorizontalAngle = 0f;
    private Vector3 currentVelocity;
    private float targetDistance;

    void Start()
    {
        if (target == null)
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null)
                target = pc.transform;
        }

        // Начальный горизонтальный угол (направление взгляда игрока или текущий угол камеры)
        if (target != null)
        {
            currentHorizontalAngle = target.eulerAngles.y;
        }
        else
        {
            currentHorizontalAngle = transform.eulerAngles.y;
        }

        targetDistance = distance;

        // Блокируем курсор (опционально)
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Обработка ввода
        HandleInput();

        // Вычисляем позицию и поворот камеры
        MoveCamera();
    }

    void HandleInput()
    {
        // Прокрутка для изменения дистанции
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            targetDistance -= scrollInput * scrollSensitivity;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        // Горизонтальное вращение камеры (только если зажат ПКМ)
        if (Input.GetMouseButton(1)) // Правая кнопка мыши
        {
            currentHorizontalAngle += Input.GetAxis("Mouse X") * mouseSensitivity;

            // Нормализуем угол, чтобы не уходил в бесконечность
            if (currentHorizontalAngle > 360f) currentHorizontalAngle -= 360f;
            if (currentHorizontalAngle < 0f) currentHorizontalAngle += 360f;
        }

        // Плавное изменение дистанции
        distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * 5f);
    }

    void MoveCamera()
    {
        // Вычисляем позицию камеры используя горизонтальный угол и фиксированный вертикальный угол

        // Преобразуем углы в радианы
        float horizontalRad = currentHorizontalAngle * Mathf.Deg2Rad;
        float verticalRad = fixedVerticalAngle * Mathf.Deg2Rad;

        // Вычисляем направление от цели к камере
        Vector3 direction = new Vector3(
            Mathf.Sin(horizontalRad) * Mathf.Cos(verticalRad),  // X
            Mathf.Sin(verticalRad),                               // Y (фиксированный)
            Mathf.Cos(horizontalRad) * Mathf.Cos(verticalRad)    // Z
        );

        // Нормализуем направление
        direction.Normalize();

        // Желаемая позиция камеры
        Vector3 targetPosition = target.position + lookOffset + direction * distance;

        // Добавляем дополнительное смещение по высоте
        targetPosition.y += heightOffset;

        // Плавное движение
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            1f / positionSmoothSpeed
        );

        // Поворот камеры на цель
        Vector3 lookTarget = target.position + lookOffset;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmoothSpeed * Time.deltaTime
        );
    }

    // Публичный метод для установки вертикального угла из кода
    public void SetVerticalAngle(float angle)
    {
        fixedVerticalAngle = Mathf.Clamp(angle, -89f, 89f);
    }

    // Публичный метод для установки горизонтального угла из кода
    public void SetHorizontalAngle(float angle)
    {
        currentHorizontalAngle = angle;
    }

    // Публичный метод для сброса камеры за спину игрока
    public void ResetBehindTarget()
    {
        if (target != null)
        {
            currentHorizontalAngle = target.eulerAngles.y;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position + lookOffset, 0.3f);

            // Показываем направление камеры
            float horizontalRad = currentHorizontalAngle * Mathf.Deg2Rad;
            float verticalRad = fixedVerticalAngle * Mathf.Deg2Rad;

            Vector3 direction = new Vector3(
                Mathf.Sin(horizontalRad) * Mathf.Cos(verticalRad),
                Mathf.Sin(verticalRad),
                Mathf.Cos(horizontalRad) * Mathf.Cos(verticalRad)
            );

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(target.position + lookOffset, target.position + lookOffset + direction * distance);

            // Показываем плоскость XZ (горизонтальную)
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Vector3 flatDir = new Vector3(direction.x, 0, direction.z).normalized;
            Gizmos.DrawRay(target.position + lookOffset, flatDir * distance);
        }
    }
}

