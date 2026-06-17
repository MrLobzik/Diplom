using UnityEngine;
using UnityEngine.Events;

public class PauseManager4D : MonoBehaviour
{
    [Header("Pause Settings")]
    [Tooltip("Клавиша для паузы/снятия с паузы")]
    public KeyCode pauseKey = KeyCode.Escape;

    [Tooltip("Останавливать время при паузе")]
    public bool stopTimeOnPause = true;

    [Tooltip("Блокировать курсор при паузе")]
    public bool unlockCursorOnPause = true;

    [Tooltip("Показывать курсор при паузе")]
    public bool showCursorOnPause = true;

    [Header("Audio Settings")]
    [Tooltip("Приглушать звук при паузе")]
    public bool muteAudioOnPause = false;

    [Tooltip("Громкость звука при паузе (0-1)")]
    [Range(0f, 1f)]
    public float pauseVolume = 0f;

    [Header("UI Settings")]
    [Tooltip("UI панель паузы (активируется/деактивируется)")]
    public GameObject pauseMenuUI;

    [Tooltip("Анимировать появление меню паузы")]
    public bool animatePauseMenu = false;

    [Tooltip("Скорость анимации")]
    public float animationSpeed = 5f;

    [Header("Events")]
    public UnityEvent onGamePaused;
    public UnityEvent onGameResumed;
    public UnityEvent<bool> onPauseStateChanged; // Передает текущее состояние паузы

    // Состояние паузы
    private bool isPaused = false;
    private float previousTimeScale;
    private float previousVolume;
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    // Синглтон для доступа из других скриптов
    private static PauseManager4D instance;
    public static PauseManager4D Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PauseManager4D>();
                if (instance == null)
                {
                    GameObject go = new GameObject("[PauseManager4D]");
                    instance = go.AddComponent<PauseManager4D>();
                }
            }
            return instance;
        }
    }

    // Публичное свойство для проверки состояния паузы
    public bool IsPaused => isPaused;

    void Awake()
    {
        // Настройка синглтона
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Сохраняем начальные настройки
        previousTimeScale = Time.timeScale;
        previousVolume = AudioListener.volume;
    }

    void Start()
    {
        // Скрываем меню паузы при старте
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
    }

    void Update()
    {
        // Обработка нажатия клавиши паузы
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }

        // Альтернативные способы паузы (можно раскомментировать)
        // if (Input.GetKeyDown(KeyCode.P)) TogglePause();
        // if (Input.GetButtonDown("Cancel")) TogglePause(); // Для геймпада
    }

    /// <summary>
    /// Переключает состояние паузы
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Ставит игру на паузу
    /// </summary>
    public void PauseGame()
    {
        if (isPaused) return;

        isPaused = true;

        // Сохраняем текущие настройки
        previousTimeScale = Time.timeScale;
        previousVolume = AudioListener.volume;
        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        // Останавливаем время
        if (stopTimeOnPause)
        {
            Time.timeScale = 0f;
        }

        // Настройка курсора
        if (unlockCursorOnPause)
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (showCursorOnPause)
        {
            Cursor.visible = true;
        }

        // Приглушаем звук
        if (muteAudioOnPause)
        {
            AudioListener.volume = pauseVolume;
        }

        // Показываем меню паузы
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);

            if (animatePauseMenu)
            {
                StartCoroutine(AnimatePauseMenu(true));
            }
        }

        // Вызываем события
        onGamePaused?.Invoke();
        onPauseStateChanged?.Invoke(true);

        Debug.Log("Игра на паузе");
    }

    /// <summary>
    /// Снимает игру с паузы
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;

        // Восстанавливаем время
        if (stopTimeOnPause)
        {
            Time.timeScale = previousTimeScale;
        }

        // Восстанавливаем курсор
        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisible;

        // Восстанавливаем звук
        if (muteAudioOnPause)
        {
            AudioListener.volume = previousVolume;
        }

        // Скрываем меню паузы
        if (pauseMenuUI != null)
        {
            if (animatePauseMenu)
            {
                StartCoroutine(AnimatePauseMenu(false));
            }
            else
            {
                pauseMenuUI.SetActive(false);
            }
        }

        // Вызываем события
        onGameResumed?.Invoke();
        onPauseStateChanged?.Invoke(false);

        Debug.Log("Игра продолжена");
    }

    /// <summary>
    /// Принудительно ставит на паузу (игнорирует текущее состояние)
    /// </summary>
    public void ForcePause()
    {
        if (!isPaused)
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Принудительно снимает с паузы (игнорирует текущее состояние)
    /// </summary>
    public void ForceResume()
    {
        if (isPaused)
        {
            ResumeGame();
        }
    }

    /// <summary>
    /// Устанавливает состояние паузы
    /// </summary>
    public void SetPaused(bool paused)
    {
        if (paused && !isPaused)
        {
            PauseGame();
        }
        else if (!paused && isPaused)
        {
            ResumeGame();
        }
    }

    /// <summary>
    /// Анимация появления/скрытия меню паузы
    /// </summary>
    private System.Collections.IEnumerator AnimatePauseMenu(bool show)
    {
        if (pauseMenuUI == null) yield break;

        CanvasGroup canvasGroup = pauseMenuUI.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = pauseMenuUI.AddComponent<CanvasGroup>();
        }

        float targetAlpha = show ? 1f : 0f;
        float startAlpha = canvasGroup.alpha;

        if (show)
        {
            pauseMenuUI.SetActive(true);
            canvasGroup.alpha = 0f;
        }

        float elapsed = 0f;
        float duration = 1f / animationSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Используем unscaledDeltaTime для работы при паузе
            float progress = elapsed / duration;

            // Плавное появление с easing
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedProgress);

            // Опционально: масштабирование для эффекта
            if (show)
            {
                pauseMenuUI.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, easedProgress);
            }
            else
            {
                pauseMenuUI.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.8f, easedProgress);
            }

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (!show)
        {
            pauseMenuUI.SetActive(false);
            pauseMenuUI.transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// Устанавливает Time.timeScale вручную (с учетом паузы)
    /// </summary>
    public void SetTimeScale(float scale)
    {
        previousTimeScale = scale;
        if (!isPaused)
        {
            Time.timeScale = scale;
        }
    }

    // Очистка при выходе из игры
    void OnDestroy()
    {
        // Восстанавливаем настройки при выходе
        if (isPaused)
        {
            Time.timeScale = previousTimeScale;
            AudioListener.volume = previousVolume;
            Cursor.lockState = previousLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }

    // Отладка в редакторе
    void OnGUI()
    {
        if (!Application.isPlaying) return;

        // Небольшая индикация паузы в углу экрана
        if (isPaused)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.yellow;
            style.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(Screen.width / 2 - 50, 10, 100, 30), "PAUSED", style);

            // Подсказка
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(Screen.width / 2 - 100, 40, 200, 20),
                $"Нажмите {pauseKey} для продолжения", style);
        }
    }
}

