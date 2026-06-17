using UnityEngine;
using UnityEngine.Events;

public class EnemyWeakPoint4D : MonoBehaviour
{
    [Header("Weak Point Settings")]
    [Tooltip("Ссылка на родительского врага (если не указана, используется parent)")]
    public GameObject enemyParent;

    [Tooltip("Задержка перед уничтожением врага")]
    public float destroyDelay = 0f;

    [Tooltip("Метод уничтожения врага")]
    public DestroyMethod destroyMethod = DestroyMethod.Destroy;

    [Header("Visual Feedback")]
    [Tooltip("Эффект при попадании в слабое место")]
    public GameObject hitEffect;

    [Tooltip("Звук при попадании в слабое место")]
    public AudioClip hitSound;

    [Tooltip("Эффект уничтожения врага")]
    public GameObject destroyEffect;

    [Tooltip("Звук уничтожения врага")]
    public AudioClip destroySound;

    [Header("Score Settings")]
    [Tooltip("Очки за уничтожение врага")]
    public int scoreValue = 100;

    [Tooltip("Множитель очков при попадании в слабое место")]
    public float weakPointMultiplier = 2f;

    [Header("Cooldown Settings")]
    [Tooltip("Время неуязвимости после активации (предотвращает множественные срабатывания)")]
    public float cooldownTime = 0.5f;

    [Header("Events")]
    public UnityEvent onWeakPointHit;
    public UnityEvent onEnemyDestroyed;

    public enum DestroyMethod
    {
        Destroy,        // Destroy(gameObject)
        Deactivate,     // SetActive(false)
        ReturnToPool,   // Для объектных пулов
        Custom          // Только события, ручное управление
    }

    private bool isOnCooldown;
    private float cooldownTimer;
    private Shape4D enemyShape4D;
    private DeathZone4D enemyDeathZone;
    private Collider enemyCollider;
    private Renderer enemyRenderer;

    void Start()
    {
        InitializeReferences();
    }

    void Update()
    {
        // Обработка кулдауна
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
            }
        }
    }

    void InitializeReferences()
    {
        // Находим родительского врага
        if (enemyParent == null)
        {
            enemyParent = transform.parent?.gameObject;
        }

        if (enemyParent != null)
        {
            // Получаем компоненты врага
            enemyShape4D = enemyParent.GetComponent<Shape4D>();
            enemyDeathZone = enemyParent.GetComponent<DeathZone4D>();
            enemyCollider = enemyParent.GetComponent<Collider>();
            enemyRenderer = enemyParent.GetComponent<Renderer>();
        }
        else
        {
            Debug.LogWarning($"EnemyWeakPoint4D на {gameObject.name}: Родительский враг не найден!");
        }
    }

    // Этот метод подключается к Shape4DTrigger.OnTriggerEnter на этом объекте
    public void OnPlayerHitWeakPoint(Transform player)
    {
        if (player == null || isOnCooldown) return;

        // Проверяем, что это действительно игрок
        if (!player.CompareTag("Player")) return;

        Debug.Log($"Игрок попал в слабое место врага: {enemyParent?.name ?? "Неизвестный враг"}");

        // Активируем кулдаун
        StartCooldown();

        // Эффект попадания
        PlayHitEffect();

        // Событие попадания
        onWeakPointHit?.Invoke();

        // Уничтожаем врага
        if (destroyDelay > 0f)
        {
            StartCoroutine(DestroyEnemyWithDelay());
        }
        else
        {
            DestroyEnemy();
        }
    }

    // Альтернативный метод для Physics.Overlap или ручного вызова
    public void OnPlayerCollision(GameObject player)
    {
        if (player != null)
        {
            OnPlayerHitWeakPoint(player.transform);
        }
    }

    private void StartCooldown()
    {
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
    }

    private void PlayHitEffect()
    {
        // Эффект попадания в слабое место
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // Звук попадания
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
    }

    private void PlayDestroyEffect()
    {
        // Эффект уничтожения
        if (destroyEffect != null && enemyParent != null)
        {
            Instantiate(destroyEffect, enemyParent.transform.position, Quaternion.identity);
        }

        // Звук уничтожения
        if (destroySound != null && enemyParent != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, enemyParent.transform.position);
        }
    }

    private System.Collections.IEnumerator DestroyEnemyWithDelay()
    {
        // Визуальная индикация перед уничтожением (опционально)
        if (enemyRenderer != null)
        {
            StartCoroutine(FlashBeforeDestroy());
        }

        yield return new WaitForSeconds(destroyDelay);
        DestroyEnemy();
    }

    private System.Collections.IEnumerator FlashBeforeDestroy()
    {
        if (enemyRenderer == null || enemyRenderer.material == null) yield break;

        Color originalColor = enemyRenderer.material.color;
        float flashDuration = destroyDelay;
        float flashSpeed = 10f;
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            float alpha = Mathf.Abs(Mathf.Sin(elapsed * flashSpeed));
            enemyRenderer.material.color = Color.Lerp(originalColor, Color.red, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Возвращаем исходный цвет
        if (enemyRenderer != null && enemyRenderer.material != null)
        {
            enemyRenderer.material.color = originalColor;
        }
    }

    private void DestroyEnemy()
    {
        if (enemyParent == null)
        {
            Debug.LogWarning("EnemyWeakPoint4D: Враг уже уничтожен или ссылка потеряна!");
            return;
        }

        Debug.Log($"Уничтожение врага: {enemyParent.name}");

        // Отключаем зону смерти врага
        if (enemyDeathZone != null)
        {
            enemyDeathZone.enabled = false;
        }

        // Отключаем коллайдер
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        // Эффект уничтожения
        PlayDestroyEffect();

        // Начисляем очки
        AddScore();

        // Событие уничтожения
        onEnemyDestroyed?.Invoke();

        // Уничтожаем в зависимости от метода
        switch (destroyMethod)
        {
            case DestroyMethod.Destroy:
                Destroy(enemyParent);
                break;

            case DestroyMethod.Deactivate:
                enemyParent.SetActive(false);
                break;

            case DestroyMethod.ReturnToPool:
                // Для объектных пулов - отправляем в пул
                if (GlobalProjectileFactory.Instance != null)
                {
                    GlobalProjectileFactory.Instance.ReturnProjectile(enemyParent);
                }
                else
                {
                    enemyParent.SetActive(false);
                }
                break;

            case DestroyMethod.Custom:
                // Только события, объект остается
                break;
        }
    }

    private void AddScore()
    {
        // Начисление очков через ScoreManager (если есть)
        int finalScore = Mathf.RoundToInt(scoreValue * weakPointMultiplier);

        // Попытка найти ScoreManager
        var scoreManager = FindObjectOfType<ScoreManager4D>();
        if (scoreManager != null)
        {
            scoreManager.AddScore(finalScore);
            Debug.Log($"Начислено очков: {finalScore} (базовые: {scoreValue}, множитель слабого места: {weakPointMultiplier}x)");
        }
        else
        {
            Debug.Log($"Очки за уничтожение: {finalScore} (ScoreManager не найден)");
        }
    }

    // Метод для ручной активации из других скриптов
    public void ActivateWeakPoint()
    {
        // Можно использовать для временной активации слабого места
        gameObject.SetActive(true);
        isOnCooldown = false;
    }

    // Метод для деактивации слабого места
    public void DeactivateWeakPoint()
    {
        gameObject.SetActive(false);
    }

    // Автоматическое подключение к триггеру
    void OnEnable()
    {
        // Автоматически подключаемся к Shape4DTrigger если он есть
        var trigger = GetComponent<Shape4DTrigger>();
        if (trigger != null)
        {
            trigger.onTriggerEnter.AddListener(OnPlayerHitWeakPoint);
        }

        // Ищем триггер у дочерних объектов
        var childTriggers = GetComponentsInChildren<Shape4DTrigger>();
        foreach (var childTrigger in childTriggers)
        {
            if (childTrigger != trigger)
            {
                childTrigger.onTriggerEnter.AddListener(OnPlayerHitWeakPoint);
            }
        }
    }

    void OnDisable()
    {
        // Отключаем слушатели
        var trigger = GetComponent<Shape4DTrigger>();
        if (trigger != null)
        {
            trigger.onTriggerEnter.RemoveListener(OnPlayerHitWeakPoint);
        }
    }

    // Визуализация в редакторе
    void OnDrawGizmos()
    {
        // Рисуем слабое место
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawSphere(transform.position, 0.3f);

        // Рисуем линию к родителю
        if (enemyParent != null || transform.parent != null)
        {
            Gizmos.color = Color.red;
            Vector3 parentPos = enemyParent != null ? enemyParent.transform.position : transform.parent.position;
            Gizmos.DrawLine(transform.position, parentPos);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.red;
        string label = "WEAK POINT";
        if (destroyDelay > 0)
        {
            label += $"\nDelay: {destroyDelay}s";
        }
        label += $"\nScore: {scoreValue}x{weakPointMultiplier}";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, label);
#endif
    }

    void OnDrawGizmosSelected()
    {
        // Подсветка радиуса действия
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 1f);

        // Индикатор кулдауна
        if (isOnCooldown)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            Gizmos.DrawSphere(transform.position, 0.4f);
        }
    }
}

// Простой менеджер очков (можно добавить в сцену)
public class ScoreManager4D : MonoBehaviour
{
    [Header("Score Settings")]
    [Tooltip("Текущий счет")]
    public int currentScore = 0;

    [Tooltip("Множитель очков")]
    public float scoreMultiplier = 1f;

    [Header("Events")]
    public UnityEvent<int> onScoreChanged;
    public UnityEvent<int> onScoreMultiplierChanged;

    private static ScoreManager4D instance;
    public static ScoreManager4D Instance => instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddScore(int points)
    {
        int finalPoints = Mathf.RoundToInt(points * scoreMultiplier);
        currentScore += finalPoints;
        onScoreChanged?.Invoke(currentScore);
    }

    public void SetScoreMultiplier(float multiplier)
    {
        scoreMultiplier = Mathf.Max(0f, multiplier);
        onScoreMultiplierChanged?.Invoke(currentScore);
    }

    public void ResetScore()
    {
        currentScore = 0;
        scoreMultiplier = 1f;
        onScoreChanged?.Invoke(currentScore);
        onScoreMultiplierChanged?.Invoke(currentScore);
    }

    public int GetScore()
    {
        return currentScore;
    }
}

