using System.Collections.Generic;
using UnityEngine;

public class GlobalProjectileFactory : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("Префаб снаряда")]
    public GameObject projectilePrefab;

    [Tooltip("Начальный размер пула")]
    public int initialPoolSize = 50;

    [Tooltip("Максимальный размер пула")]
    public int maxPoolSize = 500;

    [Tooltip("Автоматически расширять пул")]
    public bool autoExpand = true;

    [Tooltip("Прогревать пул при старте")]
    public bool prewarmOnStart = true;

    // Статический экземпляр
    private static GlobalProjectileFactory _instance;
    public static GlobalProjectileFactory Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GlobalProjectileFactory>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("[GlobalProjectileFactory]");
                    _instance = go.AddComponent<GlobalProjectileFactory>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    // Пул объектов
    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    private readonly HashSet<GameObject> _activeProjectiles = new HashSet<GameObject>();
    private Transform _poolContainer;
    private int _totalCreated;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePool();
    }

    private void InitializePool()
    {
        _poolContainer = new GameObject("InactiveProjectiles").transform;
        _poolContainer.SetParent(transform);
        _poolContainer.gameObject.SetActive(false);

        if (prewarmOnStart && projectilePrefab != null)
        {
            // Предварительно создаем объекты в пуле
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateAndPoolProjectile();
            }
        }
    }

    private GameObject CreateAndPoolProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("GlobalProjectileFactory: Префаб снаряда не назначен!");
            return null;
        }

        GameObject projectile = Instantiate(projectilePrefab, _poolContainer);
        projectile.name = $"Projectile_{_totalCreated++}";

        // Гарантируем наличие необходимых компонентов
        EnsureRequiredComponents(projectile);

        projectile.SetActive(false);
        _pool.Enqueue(projectile);

        return projectile;
    }

    private void EnsureRequiredComponents(GameObject projectile)
    {
        // Добавляем недостающие компоненты с предупреждением
        if (!projectile.TryGetComponent<Shape4D>(out _))
        {
            projectile.AddComponent<Shape4D>();
            Debug.LogWarning($"Добавлен Shape4D к {projectile.name}");
        }

        if (!projectile.TryGetComponent<DeathZone4D>(out _))
        {
            projectile.AddComponent<DeathZone4D>();
            Debug.LogWarning($"Добавлен DeathZone4D к {projectile.name}");
        }

        if (!projectile.TryGetComponent<Shape4DTrigger>(out _))
        {
            projectile.AddComponent<Shape4DTrigger>();
            Debug.LogWarning($"Добавлен Shape4DTrigger к {projectile.name}");
        }

        if (!projectile.TryGetComponent<ProjectileController>(out _))
        {
            projectile.AddComponent<ProjectileController>();
            Debug.LogWarning($"Добавлен ProjectileController к {projectile.name}");
        }
    }

    /// <summary>
    /// Получить снаряд из пула
    /// </summary>
    public GameObject GetProjectile(ProjectileConfig config)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("GlobalProjectileFactory: Не назначен префаб!");
            return null;
        }

        GameObject projectile = DequeueProjectile();

        if (projectile == null)
        {
            if (autoExpand && _totalCreated < maxPoolSize)
            {
                projectile = CreateAndPoolProjectile();
                if (projectile != null)
                {
                    projectile = DequeueProjectile();
                }
            }
            else
            {
                Debug.LogWarning($"Достигнут лимит пула ({maxPoolSize})!");
                return null;
            }
        }

        if (projectile != null)
        {
            ConfigureProjectile(projectile, config);
            _activeProjectiles.Add(projectile);
        }

        return projectile;
    }

    private GameObject DequeueProjectile()
    {
        // Пропускаем уничтоженные объекты
        while (_pool.Count > 0)
        {
            GameObject projectile = _pool.Dequeue();
            if (projectile != null && !projectile.activeInHierarchy)
            {
                return projectile;
            }
        }
        return null;
    }

    private void ConfigureProjectile(GameObject projectile, ProjectileConfig config)
    {
        // Настраиваем трансформ
        projectile.transform.SetParent(null);
        projectile.transform.position = config.spawnPosition;
        projectile.transform.rotation = Quaternion.identity;
        projectile.SetActive(true);

        // Настраиваем DeathZone
        if (projectile.TryGetComponent<DeathZone4D>(out var deathZone))
        {
            deathZone.enableMovement = true;
            deathZone.enableRotation = config.enableRotation;
            deathZone.followPath = false;
            deathZone.bounceAtBounds = config.bounceAtBounds;

            Vector3 velocity3D = config.direction.normalized * config.speed * config.speedMultiplier;
            deathZone.velocity = new Vector4(velocity3D.x, velocity3D.y, velocity3D.z, config.wSpeed);

            if (config.bounceAtBounds)
            {
                deathZone.boundsMin = config.boundsMin;
                deathZone.boundsMax = config.boundsMax;
                deathZone.boundsWMin = config.boundsWMin;
                deathZone.boundsWMax = config.boundsWMax;
            }

            if (config.enableRotation)
            {
                deathZone.rotationSpeed3D = config.rotationSpeed3D;
                deathZone.rotationSpeedW = config.rotationSpeedW;
            }
        }

        // Настраиваем Shape4D
        if (projectile.TryGetComponent<Shape4D>(out var shape))
        {
            shape.positionW = config.initialW;
            shape.scaleW = config.scaleW;
            shape.rotationW = config.initialRotationW;

            if (config.overrideShapeColor)
                shape.colour = config.shapeColor;

            if (config.overrideShapeType)
                shape.shapeType = config.shapeType;
        }

        // Настраиваем Trigger
        if (projectile.TryGetComponent<Shape4DTrigger>(out var trigger))
        {
            trigger.triggerRadius = config.triggerRadius;
            trigger.oneShot = config.oneShotTrigger;
            trigger.continuousStay = config.continuousTrigger;
        }

        // Настраиваем контроллер жизненного цикла
        if (projectile.TryGetComponent<ProjectileController>(out var controller))
        {
            controller.lifetime = config.lifetime;
            controller.maxDistance = config.maxDistance;
            controller.returnOnPlayerHit = config.returnOnPlayerHit;
            controller.returnDelay = config.returnDelay;
        }
    }

    /// <summary>
    /// Вернуть снаряд в пул
    /// </summary>
    public void ReturnProjectile(GameObject projectile)
    {
        if (projectile == null) return;

        ResetProjectileState(projectile);

        projectile.SetActive(false);
        projectile.transform.SetParent(_poolContainer);

        _activeProjectiles.Remove(projectile);
        _pool.Enqueue(projectile);
    }

    /// <summary>
    /// Вернуть все активные снаряды в пул
    /// </summary>
    public void ReturnAllProjectiles()
    {
        var activeCopy = new List<GameObject>(_activeProjectiles);
        foreach (var projectile in activeCopy)
        {
            ReturnProjectile(projectile);
        }
    }

    private void ResetProjectileState(GameObject projectile)
    {
        if (projectile.TryGetComponent<DeathZone4D>(out var deathZone))
        {
            deathZone.velocity = Vector4.zero;
            deathZone.enableMovement = false;
            deathZone.enableRotation = false;
            deathZone.followPath = false;
            deathZone.bounceAtBounds = false;
        }

        if (projectile.TryGetComponent<Shape4D>(out var shape))
        {
            shape.positionW = 0f;
            shape.scaleW = 1f;
            shape.rotationW = Vector3.zero;
        }

        if (projectile.TryGetComponent<Shape4DTrigger>(out var trigger))
        {
            trigger.oneShot = false;
            trigger.continuousStay = true;
        }
    }

    // Статистика для отладки
    public int ActiveCount => _activeProjectiles.Count;
    public int PooledCount => _pool.Count;
    public int TotalCreated => _totalCreated;

    private void OnDestroy()
    {
        ReturnAllProjectiles();

        foreach (var projectile in _pool)
        {
            if (projectile != null)
                Destroy(projectile);
        }

        _pool.Clear();
        _activeProjectiles.Clear();
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (Application.isPlaying)
        {
            GUI.Label(new Rect(10, 10, 300, 20), 
                $"Projectiles - Active: {ActiveCount}, Pool: {PooledCount}, Total: {TotalCreated}");
        }
    }
#endif
}

// Конфигурация снаряда
[System.Serializable]
public class ProjectileConfig
{
    [Header("Spawn Settings")]
    public Vector3 spawnPosition;
    public Vector3 direction = Vector3.forward;
    public float speed = 10f;
    public float speedMultiplier = 1f;

    [Header("4D Settings")]
    public float initialW = 0f;
    public float wSpeed = 0f;
    public float scaleW = 1f;
    public Vector3 initialRotationW = Vector3.zero;

    [Header("Rotation Settings")]
    public bool enableRotation = false;
    public Vector3 rotationSpeed3D = Vector3.zero;
    public Vector3 rotationSpeedW = Vector3.zero;

    [Header("Bounds Settings")]
    public bool bounceAtBounds = false;
    public Vector3 boundsMin = new Vector3(-10f, -10f, -10f);
    public Vector3 boundsMax = new Vector3(10f, 10f, 10f);
    public float boundsWMin = -10f;
    public float boundsWMax = 10f;

    [Header("Trigger Settings")]
    public float triggerRadius = 1.5f;
    public bool oneShotTrigger = false;
    public bool continuousTrigger = true;

    [Header("Lifecycle Settings")]
    public float lifetime = 10f;
    public float maxDistance = 100f;
    public bool returnOnPlayerHit = true;
    public float returnDelay = 0.5f;

    [Header("Visual Override")]
    public bool overrideShapeColor = false;
    public Color shapeColor = Color.white;
    public bool overrideShapeType = false;
    public Shape4D.ShapeType shapeType;  // Исправлено: значение по умолчанию будет из префаба

    public ProjectileConfig Clone()
    {
        return (ProjectileConfig)this.MemberwiseClone();
    }
}

