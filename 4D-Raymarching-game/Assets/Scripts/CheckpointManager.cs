using UnityEngine;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    [Header("Manager Settings")]
    [Tooltip("Автоматически находить все чекпоинты в сцене")]
    public bool autoFindCheckpoints = true;

    [Tooltip("Список чекпоинтов (заполняется автоматически или вручную)")]
    public List<Checkpoint4D> checkpoints = new List<Checkpoint4D>();

    [Tooltip("Активировать стартовый чекпоинт автоматически")]
    public bool autoActivateStartCheckpoint = true;

    [Tooltip("Сохранять стартовую позицию игрока как первый чекпоинт")]
    public bool usePlayerStartPositionAsCheckpoint = true;

    [Tooltip("Клавиша для ручного респавна")]
    public KeyCode manualRespawnKey = KeyCode.R;

    [Tooltip("Клавиша для респавна на предыдущий чекпоинт")]
    public KeyCode previousCheckpointKey = KeyCode.T;

    [Header("Debug")]
    [Tooltip("Показывать отладочную информацию")]
    public bool showDebugInfo = true;

    [Tooltip("Цвет линий между чекпоинтами")]
    public Color connectionLineColor = new Color(0f, 1f, 1f, 0.5f);

    [Tooltip("Цвет активного чекпоинта")]
    public Color activeCheckpointColor = Color.green;

    [Tooltip("Цвет неактивного чекпоинта")]
    public Color inactiveCheckpointColor = Color.gray;

    // Ссылка на игрока
    private Transform playerTransform;
    private PlayerController playerController;

    // Текущий активный чекпоинт
    private Checkpoint4D currentCheckpoint;
    private int currentCheckpointIndex = -1;

    // История активации чекпоинтов
    private List<Checkpoint4D> checkpointHistory = new List<Checkpoint4D>();

    // Виртуальный стартовый чекпоинт (позиция игрока при старте)
    private CheckpointData virtualStartCheckpoint;

    // Синглтон
    private static CheckpointManager instance;
    public static CheckpointManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CheckpointManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("[CheckpointManager]");
                    instance = go.AddComponent<CheckpointManager>();
                }
            }
            return instance;
        }
    }

    // Структура для хранения данных чекпоинта
    [System.Serializable]
    private struct CheckpointData
    {
        public Vector3 position;
        public Quaternion rotation;
        public float wPosition;
        public float cameraAngle;
        public string id;
        public bool isVirtual; // Виртуальный чекпоинт (не существует в сцене)
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeCheckpoints();
        FindPlayer();
    }

    void Start()
    {
        // Создаем виртуальный чекпоинт на позиции игрока
        if (usePlayerStartPositionAsCheckpoint)
        {
            CreateVirtualStartCheckpoint();
        }

        // Активируем стартовый чекпоинт
        if (autoActivateStartCheckpoint)
        {
            ActivateStartCheckpoint();
        }
        else
        {
            if (showDebugInfo)
                Debug.Log("CheckpointManager: Автоматическая активация отключена");
        }
    }

    void Update()
    {
        HandleInput();
    }

    void InitializeCheckpoints()
    {
        if (autoFindCheckpoints)
        {
            FindAllCheckpoints();
        }

        // Назначаем ID если отсутствуют
        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (string.IsNullOrEmpty(checkpoints[i].checkpointID))
            {
                checkpoints[i].checkpointID = $"CP_{i:D2}";
            }

            // Деактивируем все чекпоинты
            checkpoints[i].Deactivate();
        }

        if (showDebugInfo)
        {
            Debug.Log($"CheckpointManager: Найдено {checkpoints.Count} физических чекпоинтов");
            foreach (var cp in checkpoints)
            {
                Debug.Log($"  - {cp.checkpointID}: {cp.name} (StartCheckpoint: {cp.isStartCheckpoint})");
            }
        }
    }

    void FindAllCheckpoints()
    {
        checkpoints.Clear();
        Checkpoint4D[] foundCheckpoints = FindObjectsOfType<Checkpoint4D>();
        checkpoints.AddRange(foundCheckpoints);

        // Сортируем по имени для предсказуемого порядка
        checkpoints.Sort((a, b) => a.name.CompareTo(b.name));
    }

    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
                playerController = player.GetComponentInParent<PlayerController>();

            if (showDebugInfo)
                Debug.Log($"CheckpointManager: Игрок найден: {player.name}, позиция: {player.transform.position}");
        }
        else
        {
            if (showDebugInfo)
                Debug.LogWarning("CheckpointManager: Игрок не найден в сцене!");
        }
    }

    void CreateVirtualStartCheckpoint()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null)
            {
                Debug.LogWarning("CheckpointManager: Не могу создать виртуальный чекпоинт - игрок не найден");
                return;
            }
        }

        // Сохраняем стартовую позицию игрока
        virtualStartCheckpoint = new CheckpointData
        {
            position = playerTransform.position,
            rotation = playerTransform.rotation,
            wPosition = GetPlayerWPosition(),
            cameraAngle = GetCameraHorizontalAngle(),
            id = "START",
            isVirtual = true
        };

        if (showDebugInfo)
            Debug.Log($"CheckpointManager: Создан виртуальный стартовый чекпоинт на позиции {virtualStartCheckpoint.position}, W: {virtualStartCheckpoint.wPosition}");
    }

    void ActivateStartCheckpoint()
    {
        // Если есть физический чекпоинт с isStartCheckpoint = true, используем его
        Checkpoint4D startCP = checkpoints.Find(cp => cp.isStartCheckpoint);

        if (startCP != null)
        {
            if (showDebugInfo)
                Debug.Log($"CheckpointManager: Найден стартовый чекпоинт {startCP.checkpointID}");

            // Обновляем данные чекпоинта позицией игрока
            if (usePlayerStartPositionAsCheckpoint && playerTransform != null)
            {
                // Перемещаем точку респавна чекпоинта на позицию игрока
                if (startCP.respawnPoint != null)
                {
                    startCP.respawnPoint.position = playerTransform.position;
                    startCP.respawnPoint.rotation = playerTransform.rotation;
                }

                // Активируем чекпоинт с текущей позицией игрока
                startCP.ActivateCheckpoint(playerTransform);

                if (showDebugInfo)
                    Debug.Log($"CheckpointManager: Стартовый чекпоинт {startCP.checkpointID} настроен на позицию игрока");
            }
            else
            {
                startCP.ActivateCheckpoint(null);
            }

            currentCheckpoint = startCP;
            currentCheckpointIndex = checkpoints.IndexOf(startCP);
            checkpointHistory.Add(startCP);
        }
        else
        {
            // Если нет физического стартового чекпоинта, используем виртуальный
            if (showDebugInfo)
                Debug.Log("CheckpointManager: Физический стартовый чекпоинт не найден, использую виртуальный");

            // Активируем первый физический чекпоинт если есть
            if (checkpoints.Count > 0)
            {
                Checkpoint4D firstCP = checkpoints[0];

                // Обновляем его позицию на позицию игрока
                if (usePlayerStartPositionAsCheckpoint && playerTransform != null && firstCP.respawnPoint != null)
                {
                    firstCP.respawnPoint.position = playerTransform.position;
                    firstCP.respawnPoint.rotation = playerTransform.rotation;
                }

                firstCP.ActivateCheckpoint(playerTransform);
                currentCheckpoint = firstCP;
                currentCheckpointIndex = 0;
                checkpointHistory.Add(firstCP);

                if (showDebugInfo)
                    Debug.Log($"CheckpointManager: Активирован первый чекпоинт {firstCP.checkpointID} как стартовый");
            }
            else
            {
                if (showDebugInfo)
                    Debug.Log("CheckpointManager: Физических чекпоинтов нет, использую только виртуальный");
            }
        }
    }

    void HandleInput()
    {
        // Ручной респавн на последний чекпоинт
        if (Input.GetKeyDown(manualRespawnKey))
        {
            RespawnAtCurrentCheckpoint();
        }

        // Респавн на предыдущий чекпоинт
        if (Input.GetKeyDown(previousCheckpointKey))
        {
            RespawnAtPreviousCheckpoint();
        }

        // Цифровые клавиши для выбора чекпоинта (1-9)
        for (int i = 0; i < 9 && i < checkpoints.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                TeleportToCheckpoint(i);
            }
        }
    }

    float GetPlayerWPosition()
    {
        if (playerController != null)
        {
            var wPositionField = typeof(PlayerController).GetField("wPosition",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (wPositionField != null)
            {
                return (float)wPositionField.GetValue(playerController);
            }
        }
        return 0f;
    }

    float GetCameraHorizontalAngle()
    {
        ThirdPersonCamera camera = FindObjectOfType<ThirdPersonCamera>();
        if (camera != null)
        {
            var angleField = typeof(ThirdPersonCamera).GetField("currentHorizontalAngle",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (angleField != null)
            {
                return (float)angleField.GetValue(camera);
            }
        }
        return 0f;
    }

    /// <summary>
    /// Активировать чекпоинт по индексу
    /// </summary>
    public void ActivateCheckpointByIndex(int index)
    {
        if (index >= 0 && index < checkpoints.Count)
        {
            ActivateCheckpoint(checkpoints[index]);
        }
        else
        {
            Debug.LogWarning($"CheckpointManager: Индекс {index} вне диапазона (0-{checkpoints.Count - 1})");
        }
    }

    /// <summary>
    /// Активировать чекпоинт по ID
    /// </summary>
    public void ActivateCheckpointByID(string id)
    {
        Checkpoint4D checkpoint = checkpoints.Find(cp => cp.checkpointID == id);
        if (checkpoint != null)
        {
            ActivateCheckpoint(checkpoint);
        }
        else
        {
            Debug.LogWarning($"CheckpointManager: Чекпоинт с ID '{id}' не найден");
        }
    }

    /// <summary>
    /// Активировать указанный чекпоинт
    /// </summary>
    public void ActivateCheckpoint(Checkpoint4D checkpoint)
    {
        if (checkpoint == null) return;

        // Проверяем, не активирован ли уже этот чекпоинт
        if (currentCheckpoint == checkpoint && checkpoint.IsActivated())
        {
            if (showDebugInfo)
                Debug.Log($"CheckpointManager: Чекпоинт {checkpoint.checkpointID} уже активирован");
            return;
        }

        // Деактивируем текущий чекпоинт
        if (currentCheckpoint != null && currentCheckpoint != checkpoint)
        {
            currentCheckpoint.Deactivate();
        }

        // Активируем новый
        currentCheckpoint = checkpoint;
        currentCheckpointIndex = checkpoints.IndexOf(checkpoint);

        // Добавляем в историю
        if (checkpointHistory.Count == 0 || checkpointHistory[checkpointHistory.Count - 1] != checkpoint)
        {
            checkpointHistory.Add(checkpoint);
        }

        // Активируем чекпоинт если он еще не активирован
        if (!checkpoint.IsActivated())
        {
            checkpoint.ActivateCheckpoint(playerTransform);
        }

        if (showDebugInfo)
        {
            Debug.Log($"CheckpointManager: Активирован чекпоинт {checkpoint.checkpointID} " +
                     $"(индекс: {currentCheckpointIndex}, история: {checkpointHistory.Count})");
        }
    }

    /// <summary>
    /// Респавн на текущем чекпоинте
    /// </summary>
    public void RespawnAtCurrentCheckpoint()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null)
            {
                Debug.LogError("CheckpointManager: Игрок не найден!");
                return;
            }
        }

        if (currentCheckpoint != null)
        {
            if (showDebugInfo)
                Debug.Log($"CheckpointManager: Респавн на чекпоинте {currentCheckpoint.checkpointID}");

            currentCheckpoint.RespawnPlayer(playerTransform);
        }
        else
        {
            // Используем виртуальный стартовый чекпоинт
            if (showDebugInfo)
                Debug.Log("CheckpointManager: Нет активного чекпоинта, использую виртуальный стартовый");

            RespawnAtVirtualStartCheckpoint();
        }
    }

    /// <summary>
    /// Респавн на предыдущем чекпоинте
    /// </summary>
    public void RespawnAtPreviousCheckpoint()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        if (checkpointHistory.Count > 1)
        {
            // Удаляем последний чекпоинт из истории
            checkpointHistory.RemoveAt(checkpointHistory.Count - 1);

            // Берем предыдущий
            Checkpoint4D previousCheckpoint = checkpointHistory[checkpointHistory.Count - 1];

            if (previousCheckpoint != null)
            {
                ActivateCheckpoint(previousCheckpoint);
                previousCheckpoint.RespawnPlayer(playerTransform);

                if (showDebugInfo)
                    Debug.Log($"CheckpointManager: Респавн на предыдущий чекпоинт {previousCheckpoint.checkpointID}");
            }
        }
        else
        {
            Debug.LogWarning("CheckpointManager: Нет предыдущих чекпоинтов! Возврат на стартовую позицию.");
            RespawnAtVirtualStartCheckpoint();
        }
    }

    /// <summary>
    /// Респавн на виртуальном стартовом чекпоинте
    /// </summary>
    private void RespawnAtVirtualStartCheckpoint()
    {
        if (playerTransform == null) return;

        // Телепортируем игрока на стартовую позицию
        playerTransform.position = virtualStartCheckpoint.position;
        playerTransform.rotation = virtualStartCheckpoint.rotation;

        // Сбрасываем скорости
        if (playerController != null)
        {
            ResetPlayerVelocity(playerController);
            SetPlayerWPosition(playerController, virtualStartCheckpoint.wPosition);
        }

        // Обновляем камеру
        ThirdPersonCamera camera = FindObjectOfType<ThirdPersonCamera>();
        if (camera != null)
        {
            camera.SetHorizontalAngle(virtualStartCheckpoint.cameraAngle);
        }

        if (showDebugInfo)
            Debug.Log($"CheckpointManager: Респавн на виртуальном стартовом чекпоинте: {virtualStartCheckpoint.position}");
    }

    /// <summary>
    /// Телепортация на чекпоинт по индексу (для отладки)
    /// </summary>
    public void TeleportToCheckpoint(int index)
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        if (index >= 0 && index < checkpoints.Count)
        {
            Checkpoint4D checkpoint = checkpoints[index];
            ActivateCheckpoint(checkpoint);
            checkpoint.RespawnPlayer(playerTransform);

            if (showDebugInfo)
                Debug.Log($"CheckpointManager: Телепортация на чекпоинт {checkpoint.checkpointID}");
        }
        else
        {
            Debug.LogWarning($"CheckpointManager: Индекс {index} вне диапазона (0-{checkpoints.Count - 1})");
        }
    }

    private void ResetPlayerVelocity(PlayerController controller)
    {
        var currentVelocityField = typeof(PlayerController).GetField("currentVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (currentVelocityField != null)
            currentVelocityField.SetValue(controller, Vector3.zero);

        var currentWVelocityField = typeof(PlayerController).GetField("currentWVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (currentWVelocityField != null)
            currentWVelocityField.SetValue(controller, 0f);

        var externalVelocityField = typeof(PlayerController).GetField("externalVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (externalVelocityField != null)
            externalVelocityField.SetValue(controller, Vector3.zero);

        var externalWVelocityField = typeof(PlayerController).GetField("externalWVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (externalWVelocityField != null)
            externalWVelocityField.SetValue(controller, 0f);

        var hasExternalVelocityField = typeof(PlayerController).GetField("hasExternalVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (hasExternalVelocityField != null)
            hasExternalVelocityField.SetValue(controller, false);

        var verticalVelocityField = typeof(PlayerController).GetField("verticalVelocity",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (verticalVelocityField != null)
            verticalVelocityField.SetValue(controller, 0f);

        var isJumpingField = typeof(PlayerController).GetField("isJumping",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (isJumpingField != null)
            isJumpingField.SetValue(controller, false);
    }

    private void SetPlayerWPosition(PlayerController controller, float wPosition)
    {
        var wPositionField = typeof(PlayerController).GetField("wPosition",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (wPositionField != null)
            wPositionField.SetValue(controller, wPosition);
    }

    /// <summary>
    /// Получить следующий чекпоинт
    /// </summary>
    public Checkpoint4D GetNextCheckpoint()
    {
        if (checkpoints.Count == 0) return null;
        int nextIndex = (currentCheckpointIndex + 1) % checkpoints.Count;
        return checkpoints[nextIndex];
    }

    /// <summary>
    /// Получить предыдущий чекпоинт
    /// </summary>
    public Checkpoint4D GetPreviousCheckpoint()
    {
        if (checkpoints.Count == 0) return null;
        int prevIndex = currentCheckpointIndex - 1;
        if (prevIndex < 0) prevIndex = checkpoints.Count - 1;
        return checkpoints[prevIndex];
    }

    /// <summary>
    /// Получить чекпоинт по имени
    /// </summary>
    public Checkpoint4D GetCheckpointByName(string name)
    {
        return checkpoints.Find(cp => cp.name == name);
    }

    /// <summary>
    /// Получить чекпоинт по ID
    /// </summary>
    public Checkpoint4D GetCheckpointByID(string id)
    {
        return checkpoints.Find(cp => cp.checkpointID == id);
    }

    /// <summary>
    /// Сбросить все чекпоинты
    /// </summary>
    public void ResetAllCheckpoints()
    {
        foreach (var checkpoint in checkpoints)
        {
            if (checkpoint != null)
                checkpoint.Deactivate();
        }

        currentCheckpoint = null;
        currentCheckpointIndex = -1;
        checkpointHistory.Clear();

        if (showDebugInfo)
            Debug.Log("CheckpointManager: Все чекпоинты сброшены");
    }

    /// <summary>
    /// Обновить список чекпоинтов
    /// </summary>
    public void RefreshCheckpoints()
    {
        FindAllCheckpoints();
        InitializeCheckpoints();
    }

    /// <summary>
    /// Получить количество чекпоинтов
    /// </summary>
    public int GetCheckpointCount()
    {
        return checkpoints.Count;
    }

    /// <summary>
    /// Получить индекс текущего чекпоинта
    /// </summary>
    public int GetCurrentCheckpointIndex()
    {
        return currentCheckpointIndex;
    }

    /// <summary>
    /// Получить текущий чекпоинт
    /// </summary>
    public Checkpoint4D GetCurrentCheckpoint()
    {
        return currentCheckpoint;
    }

    /// <summary>
    /// Проверить, есть ли активный чекпоинт
    /// </summary>
    public bool HasActiveCheckpoint()
    {
        return currentCheckpoint != null;
    }

    /// <summary>
    /// Получить позицию стартового чекпоинта
    /// </summary>
    public Vector3 GetStartPosition()
    {
        return virtualStartCheckpoint.position;
    }

    void OnDrawGizmos()
    {
        // Рисуем виртуальный стартовый чекпоинт
        if (usePlayerStartPositionAsCheckpoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(virtualStartCheckpoint.position, 0.5f);

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.Label(virtualStartCheckpoint.position + Vector3.up * 1f, "VIRTUAL START");
#endif
        }

        if (checkpoints == null || checkpoints.Count == 0) return;

        // Рисуем связи между чекпоинтами
        Gizmos.color = connectionLineColor;

        // Линия от виртуального старта к первому чекпоинту
        if (usePlayerStartPositionAsCheckpoint && checkpoints.Count > 0)
        {
            Gizmos.DrawLine(virtualStartCheckpoint.position, checkpoints[0].transform.position);
        }

        for (int i = 0; i < checkpoints.Count - 1; i++)
        {
            if (checkpoints[i] != null && checkpoints[i + 1] != null)
            {
                Gizmos.DrawLine(checkpoints[i].transform.position, checkpoints[i + 1].transform.position);
            }
        }

        // Выделяем текущий чекпоинт
        if (currentCheckpoint != null)
        {
            Gizmos.color = activeCheckpointColor;
            Gizmos.DrawWireSphere(currentCheckpoint.transform.position, currentCheckpoint.activationRadius * 1.2f);
        }

#if UNITY_EDITOR
        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] != null)
            {
                bool isStart = checkpoints[i].isStartCheckpoint;
                UnityEditor.Handles.color = checkpoints[i] == currentCheckpoint ? activeCheckpointColor : inactiveCheckpointColor;
                
                string label = $"[{i}] {checkpoints[i].checkpointID}";
                if (isStart) label += " (START)";
                if (checkpoints[i].IsActivated()) label += " [ACTIVE]";
                
                UnityEditor.Handles.Label(
                    checkpoints[i].transform.position + Vector3.up * 3f,
                    label
                );
            }
        }
#endif
    }
}

