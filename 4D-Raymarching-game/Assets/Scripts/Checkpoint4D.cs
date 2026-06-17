using UnityEngine;
using UnityEngine.Events;
using Unity.Mathematics;
using System.Collections;

public class Checkpoint4D : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Уникальный идентификатор чекпоинта")]
    public string checkpointID = "checkpoint_1";

    [Tooltip("Автоматически активировать при входе")]
    public bool autoActivate = true;

    [Tooltip("Это стартовый чекпоинт (активирован по умолчанию)")]
    public bool isStartCheckpoint = false;

    [Tooltip("Радиус активации чекпоинта")]
    public float activationRadius = 2f;

    [Tooltip("Сохранять 4D позицию игрока")]
    public bool saveWPosition = true;

    [Tooltip("Сохранять горизонтальный угол камеры")]
    public bool saveCameraAngle = true;

    [Header("Respawn Point")]
    [Tooltip("Точка респавна (если не указана, используется первый дочерний объект)")]
    public Transform respawnPoint;

    [Tooltip("Использовать поворот точки респавна")]
    public bool useRespawnRotation = true;

    [Header("Visual Settings")]
    [Tooltip("Цвет неактивного чекпоинта")]
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Tooltip("Цвет активного чекпоинта")]
    public Color activeColor = new Color(0f, 1f, 0.5f, 0.8f);

    [Tooltip("Префаб эффекта активации")]
    public GameObject activationEffect;

    [Tooltip("Звук активации")]
    public AudioClip activationSound;

    [Tooltip("Высота парения эффекта")]
    public float floatHeight = 0.5f;

    [Tooltip("Скорость парения")]
    public float floatSpeed = 2f;

    [Tooltip("Скорость вращения")]
    public float rotationSpeed = 30f;

    [Header("Respawn Settings")]
    [Tooltip("Направление взгляда после респавна (0 = использовать поворот точки)")]
    public Vector3 respawnDirection = Vector3.zero;

    [Tooltip("Задержка перед респавном")]
    public float respawnDelay = 0.5f;

    [Header("Debug")]
    [Tooltip("Показывать отладочную информацию")]
    public bool showDebugInfo = true;

    [Tooltip("Цвет зоны активации в редакторе")]
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);

    [Header("Events")]
    public UnityEvent<Checkpoint4D> onActivated;
    public UnityEvent<Transform> onPlayerRespawned;

    // Сохраненные данные
    private Vector3 savedPosition3D;
    private float savedPositionW;
    private float savedCameraHorizontalAngle;
    private Quaternion savedRotation;
    private bool isActivated;
    private Vector3 startPosition;
    private Material checkpointMaterial;
    private Renderer checkpointRenderer;

    // Ссылки на игрока
    private PlayerRayMarchCollider playerCollider;
    private Transform playerTransform;

    // Ссылка на менеджер чекпоинтов
    private static Checkpoint4D lastActivatedCheckpoint;

    void Start()
    {
        startPosition = transform.position;
        InitializeVisuals();
        FindRespawnPoint();
        FindPlayerReferences();

        // Активируем только если это стартовый чекпоинт
        if (isStartCheckpoint)
        {
            if (lastActivatedCheckpoint == null)
            {
                // Для стартового чекпоинта сохраняем его позицию как начальную
                SaveRespawnPointData();
                ActivateCheckpoint(null);
                if (showDebugInfo)
                    Debug.Log($"Стартовый чекпоинт {checkpointID} активирован. Точка респавна: {savedPosition3D}");
            }
        }
        else
        {
            isActivated = false;
            UpdateCheckpointColor();
        }
    }

    void Update()
    {
        UpdateVisualEffects();

        if (autoActivate && !isActivated)
        {
            CheckPlayerProximity();
        }
    }

    void FindRespawnPoint()
    {
        // Если точка респавна не назначена вручную, ищем её среди дочерних объектов
        if (respawnPoint == null)
        {
            // Ищем первый дочерний объект
            if (transform.childCount > 0)
            {
                respawnPoint = transform.GetChild(0);
                if (showDebugInfo)
                    Debug.Log($"Чекпоинт {checkpointID}: Точка респавна найдена автоматически: {respawnPoint.name}");
            }
            else
            {
                // Если нет дочерних объектов, создаем точку респавна
                GameObject respawnObj = new GameObject("RespawnPoint");
                respawnObj.transform.SetParent(transform);
                respawnObj.transform.localPosition = Vector3.zero;
                respawnObj.transform.localRotation = Quaternion.identity;
                respawnPoint = respawnObj.transform;

                if (showDebugInfo)
                    Debug.Log($"Чекпоинт {checkpointID}: Точка респавна создана автоматически");
            }
        }
        else
        {
            if (showDebugInfo)
                Debug.Log($"Чекпоинт {checkpointID}: Используется назначенная точка респавна: {respawnPoint.name}");
        }
    }

    void FindPlayerReferences()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerCollider = player.GetComponent<PlayerRayMarchCollider>();

            if (playerCollider == null)
                playerCollider = player.GetComponentInChildren<PlayerRayMarchCollider>();

            if (playerCollider == null && showDebugInfo)
            {
                Debug.LogWarning($"Чекпоинт {checkpointID}: PlayerRayMarchCollider не найден на игроке! " +
                               "Чекпоинт будет проверять позицию самого игрока.");
            }
        }
        else
        {
            Debug.LogWarning($"Чекпоинт {checkpointID}: Игрок с тегом 'Player' не найден в сцене!");
        }
    }

    void CheckPlayerProximity()
    {
        if (playerTransform == null)
        {
            FindPlayerReferences();
            return;
        }

        if (playerCollider != null && playerCollider.rayMarchTransforms != null && playerCollider.rayMarchTransforms.Length > 0)
        {
            foreach (Transform point in playerCollider.rayMarchTransforms)
            {
                if (point == null) continue;

                if (CheckPointProximity(point.position))
                    return;
            }
        }
        else
        {
            CheckPointProximity(playerTransform.position);
        }
    }

    bool CheckPointProximity(Vector3 pointPosition)
    {
        float distance = Vector3.Distance(transform.position, pointPosition);

        if (showDebugInfo && distance < activationRadius * 1.5f)
        {
            Debug.Log($"Чекпоинт {checkpointID}: Дистанция до игрока: {distance:F2} (радиус: {activationRadius})");
        }

        if (distance <= activationRadius)
        {
            if (showDebugInfo)
                Debug.Log($"Чекпоинт {checkpointID}: Игрок вошел в зону! Активируем...");

            SaveRespawnPointData();
            ActivateCheckpoint(playerTransform);
            return true;
        }

        return false;
    }

    void InitializeVisuals()
    {
        checkpointRenderer = GetComponent<Renderer>();
        if (checkpointRenderer != null)
        {
            checkpointMaterial = checkpointRenderer.material;
            UpdateCheckpointColor();
        }
    }

    void UpdateVisualEffects()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        if (isActivated)
        {
            float scale = 1f + Mathf.Sin(Time.time * 3f) * 0.1f;
            transform.localScale = Vector3.one * scale;
        }
        else
        {
            float scale = 1f + Mathf.Sin(Time.time * 2f) * 0.05f;
            transform.localScale = Vector3.one * scale;
        }
    }

    void UpdateCheckpointColor()
    {
        if (checkpointMaterial != null)
        {
            checkpointMaterial.color = isActivated ? activeColor : inactiveColor;
        }
    }

    public void ActivateCheckpoint(Transform player)
    {
        if (isActivated && player != null)
        {
            if (showDebugInfo)
                Debug.Log($"Чекпоинт {checkpointID} уже активирован");
            return;
        }

        if (player != null)
        {
            // Сохраняем W позицию игрока и угол камеры
            SavePlayerAdditionalData(player);
        }
        else
        {
            savedPositionW = 0f;
            savedCameraHorizontalAngle = 0f;
        }

        // Деактивируем предыдущий чекпоинт
        if (lastActivatedCheckpoint != null && lastActivatedCheckpoint != this)
        {
            lastActivatedCheckpoint.Deactivate();
        }

        // Активируем текущий
        isActivated = true;
        lastActivatedCheckpoint = this;
        UpdateCheckpointColor();

        PlayActivationEffects();

        onActivated?.Invoke(this);

        if (showDebugInfo)
            Debug.Log($"Чекпоинт {checkpointID} активирован! Точка респавна: {savedPosition3D}, W: {savedPositionW}");
    }

    void SavePlayerAdditionalData(Transform player)
    {
        if (saveWPosition)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
                playerController = player.GetComponentInParent<PlayerController>();

            if (playerController != null)
            {
                var wPositionField = typeof(PlayerController).GetField("wPosition",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (wPositionField != null)
                {
                    savedPositionW = (float)wPositionField.GetValue(playerController);
                }
            }
        }
        else
        {
            savedPositionW = 0f;
        }

        if (saveCameraAngle)
        {
            ThirdPersonCamera camera = FindObjectOfType<ThirdPersonCamera>();
            if (camera != null)
            {
                var angleField = typeof(ThirdPersonCamera).GetField("currentHorizontalAngle",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (angleField != null)
                {
                    savedCameraHorizontalAngle = (float)angleField.GetValue(camera);
                }
            }
        }
    }

    void PlayActivationEffects()
    {
        Vector3 effectPosition = respawnPoint != null ? respawnPoint.position : transform.position;

        if (activationEffect != null)
        {
            Instantiate(activationEffect, effectPosition, Quaternion.identity);
        }

        if (activationSound != null)
        {
            AudioSource.PlayClipAtPoint(activationSound, effectPosition);
        }
    }

    public void Deactivate()
    {
        isActivated = false;
        UpdateCheckpointColor();

        if (showDebugInfo)
            Debug.Log($"Чекпоинт {checkpointID} деактивирован");
    }

    public void RespawnPlayer(Transform player)
    {
        if (player == null)
        {
            Debug.LogError($"Чекпоинт {checkpointID}: Попытка респавна с null игроком!");
            return;
        }

        StartCoroutine(RespawnRoutine(player));
    }

    IEnumerator RespawnRoutine(Transform player)
    {
        if (showDebugInfo)
            Debug.Log($"Чекпоинт {checkpointID}: Начинаю респавн игрока через {respawnDelay} сек...");

        // Задержка перед респавном
        if (respawnDelay > 0)
        {
            yield return new WaitForSeconds(respawnDelay);
        }

        // Получаем позицию и поворот для респавна
        Vector3 respawnPosition;
        Quaternion respawnRotation;

        if (respawnPoint != null)
        {
            respawnPosition = respawnPoint.position;
            respawnRotation = respawnPoint.rotation;
        }
        else
        {
            respawnPosition = savedPosition3D;
            respawnRotation = savedRotation;
        }

        if (showDebugInfo)
            Debug.Log($"Чекпоинт {checkpointID}: Респавн на позиции {respawnPosition}");

        // НАПРЯМУЮ устанавливаем позицию игрока
        player.position = respawnPosition;

        // Устанавливаем поворот
        if (useRespawnRotation)
        {
            if (respawnDirection != Vector3.zero)
            {
                player.rotation = Quaternion.LookRotation(respawnDirection.normalized, Vector3.up);
            }
            else
            {
                player.rotation = respawnRotation;
            }
        }

        // Теперь работаем с PlayerController
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController == null)
            playerController = player.GetComponentInParent<PlayerController>();

        if (playerController != null)
        {
            // Сбрасываем ТОЛЬКО скорости, но НЕ позицию!
            ResetPlayerVelocity(playerController);

            // Устанавливаем W позицию
            if (saveWPosition)
            {
                SetPlayerWPosition(playerController, savedPositionW);
            }

            // Сбрасываем состояние прыжка
            ResetJumpState(playerController);
        }

        // Обновляем камеру
        UpdateCameraAfterRespawn(respawnRotation);

        // Эффект респавна
        if (activationEffect != null)
        {
            Instantiate(activationEffect, respawnPosition, Quaternion.identity);
        }

        onPlayerRespawned?.Invoke(player);

        if (showDebugInfo)
            Debug.Log($"Игрок респавнен на чекпоинте {checkpointID}. Позиция: {respawnPosition}, Поворот: {respawnRotation.eulerAngles}, W: {savedPositionW}");
    }

    void ResetPlayerVelocity(PlayerController controller)
    {
        // Сбрасываем только скорости через рефлексию
        var currentVelocityField = typeof(PlayerController).GetField("currentVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (currentVelocityField != null)
        {
            currentVelocityField.SetValue(controller, Vector3.zero);
        }

        var currentWVelocityField = typeof(PlayerController).GetField("currentWVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (currentWVelocityField != null)
        {
            currentWVelocityField.SetValue(controller, 0f);
        }

        // Сбрасываем внешнюю скорость
        var externalVelocityField = typeof(PlayerController).GetField("externalVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (externalVelocityField != null)
        {
            externalVelocityField.SetValue(controller, Vector3.zero);
        }

        var externalWVelocityField = typeof(PlayerController).GetField("externalWVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (externalWVelocityField != null)
        {
            externalWVelocityField.SetValue(controller, 0f);
        }

        var hasExternalVelocityField = typeof(PlayerController).GetField("hasExternalVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (hasExternalVelocityField != null)
        {
            hasExternalVelocityField.SetValue(controller, false);
        }
    }

    void SetPlayerWPosition(PlayerController controller, float wPosition)
    {
        var wPositionField = typeof(PlayerController).GetField("wPosition",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (wPositionField != null)
        {
            wPositionField.SetValue(controller, wPosition);
        }
    }

    void ResetJumpState(PlayerController controller)
    {
        var verticalVelocityField = typeof(PlayerController).GetField("verticalVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (verticalVelocityField != null)
        {
            verticalVelocityField.SetValue(controller, 0f);
        }

        var isJumpingField = typeof(PlayerController).GetField("isJumping",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (isJumpingField != null)
        {
            isJumpingField.SetValue(controller, false);
        }
    }

    void UpdateCameraAfterRespawn(Quaternion respawnRotation)
    {
        ThirdPersonCamera camera = FindObjectOfType<ThirdPersonCamera>();
        if (camera != null)
        {
            if (respawnDirection != Vector3.zero)
            {
                camera.SetHorizontalAngle(respawnDirection.GetHorizontalAngle());
            }
            else if (saveCameraAngle)
            {
                camera.SetHorizontalAngle(savedCameraHorizontalAngle);
            }
            else if (useRespawnRotation)
            {
                float angle = respawnRotation.eulerAngles.y;
                camera.SetHorizontalAngle(angle);
            }
        }
    }

    public void RefreshPlayerReferences()
    {
        FindPlayerReferences();
    }

    public static void RespawnAtLastCheckpoint(Transform player)
    {
        if (lastActivatedCheckpoint != null)
        {
            lastActivatedCheckpoint.RespawnPlayer(player);
        }
        else
        {
            Debug.LogWarning("Нет активных чекпоинтов! Использую ResetPlayer().");

            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
                playerController = player.GetComponentInParent<PlayerController>();

            if (playerController != null)
            {
                playerController.ResetPlayer();
            }
        }
    }

    public static Checkpoint4D GetLastCheckpoint()
    {
        return lastActivatedCheckpoint;
    }

    public static void ResetAllCheckpoints()
    {
        lastActivatedCheckpoint = null;
        var allCheckpoints = FindObjectsOfType<Checkpoint4D>();
        foreach (var cp in allCheckpoints)
        {
            cp.Deactivate();
        }
    }

    public bool IsActivated()
    {
        return isActivated;
    }

    /// <summary>
    /// Активировать чекпоинт (вызывается из CheckpointManager)
    /// </summary>
    public void ActivateFromManager()
    {
        if (!isActivated)
        {
            // Сохраняем данные точки респавна
            SaveRespawnPointData();

            // Деактивируем другие чекпоинты через менеджер
            if (CheckpointManager.Instance != null)
            {
                Checkpoint4D currentCP = CheckpointManager.Instance.GetCurrentCheckpoint();
                if (currentCP != null && currentCP != this)
                {
                    currentCP.Deactivate();
                }
            }

            // Активируем
            isActivated = true;
            lastActivatedCheckpoint = this;
            UpdateCheckpointColor();
            PlayActivationEffects();
            onActivated?.Invoke(this);

            if (showDebugInfo)
                Debug.Log($"Чекпоинт {checkpointID} активирован через менеджер");
        }
    }

    // Метод для сохранения данных точки респавна
    private void SaveRespawnPointData()
    {
        if (respawnPoint != null)
        {
            savedPosition3D = respawnPoint.position;
            savedRotation = respawnPoint.rotation;
        }
        else
        {
            savedPosition3D = transform.position;
            savedRotation = transform.rotation;
        }
    }

    void OnDrawGizmos()
    {
        // Зона активации
        Gizmos.color = isActivated ? new Color(0f, 1f, 0f, 0.3f) : gizmoColor;
        Gizmos.DrawSphere(transform.position, activationRadius);

        // Точка респавна
        if (respawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(respawnPoint.position, 0.3f);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(respawnPoint.position, respawnPoint.forward * 1f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, respawnPoint.position);
        }

        // Сам чекпоинт
        Gizmos.color = isActivated ? activeColor : inactiveColor;
        Gizmos.DrawSphere(transform.position, 0.5f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Vector3.up * 3f);

#if UNITY_EDITOR
        UnityEditor.Handles.color = isActivated ? Color.green : Color.white;
        string label = $"Checkpoint: {checkpointID}\nRadius: {activationRadius}";
        if (respawnPoint != null)
            label += $"\nRespawn: {respawnPoint.name}";
        label += $"\n{(isActivated ? "ACTIVE" : "inactive")}";
        if (isStartCheckpoint) label += "\n[START]";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, label);
#endif
    }

    void OnDestroy()
    {
        if (lastActivatedCheckpoint == this)
        {
            lastActivatedCheckpoint = null;
        }
    }
}

public static class VectorExtensions
{
    public static float GetHorizontalAngle(this Vector3 direction)
    {
        if (direction == Vector3.zero) return 0f;

        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        return angle;
    }
}

