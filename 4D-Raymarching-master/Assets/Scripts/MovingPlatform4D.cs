using UnityEngine;
using System.Collections;

public class MovingPlatform4D : MonoBehaviour
{
    [Header("Movement Points")]
    [Tooltip("Точка A (стартовая позиция)")]
    public Vector3 pointA = Vector3.zero;

    [Tooltip("Точка B (конечная позиция)")]
    public Vector3 pointB = new Vector3(5f, 0f, 0f);

    [Header("4D Movement")]
    [Tooltip("Включить движение по W измерению")]
    public bool enable4DMovement = false;

    [Tooltip("W позиция в точке A")]
    public float pointAW = 0f;

    [Tooltip("W позиция в точке B")]
    public float pointBW = 5f;

    [Header("Movement Settings")]
    [Tooltip("Скорость движения (единиц в секунду)")]
    public float speed = 3f;

    [Tooltip("Время ожидания в точке A (секунды)")]
    public float waitAtA = 0f;

    [Tooltip("Время ожидания в точке B (секунды)")]
    public float waitAtB = 1f;

    [Header("Movement Type")]
    [Tooltip("Тип движения")]
    public MovementType movementType = MovementType.PingPong;

    [Tooltip("Использовать плавное движение (Easing)")]
    public bool useEasing = true;

    [Tooltip("Кривая easing для движения")]
    public AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Start Settings")]
    [Tooltip("Начать движение автоматически")]
    public bool autoStart = true;

    [Tooltip("Начальная позиция (0 = A, 1 = B)")]
    [Range(0f, 1f)]
    public float startPosition = 0f;

    [Tooltip("Задержка перед началом движения")]
    public float startDelay = 0f;

    [Header("Rotation Settings")]
    [Tooltip("Вращать объект во время движения")]
    public bool enableRotation = false;

    [Tooltip("Скорость вращения в точке A")]
    public Vector3 rotationSpeedAtA = Vector3.zero;

    [Tooltip("Скорость вращения в точке B")]
    public Vector3 rotationSpeedAtB = new Vector3(0f, 90f, 0f);

    [Header("Player Interaction")]
    [Tooltip("Игрок двигается вместе с платформой")]
    public bool carryPlayer = true;

    [Tooltip("Сила, с которой платформа удерживает игрока")]
    public float carryForce = 50f;

    [Header("Visual Settings")]
    [Tooltip("Показывать путь в редакторе")]
    public bool showPath = true;

    [Tooltip("Цвет пути")]
    public Color pathColor = new Color(1f, 1f, 0f, 0.5f);

    [Tooltip("Размер маркеров точек")]
    public float markerSize = 0.5f;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent onReachedPointA;
    public UnityEngine.Events.UnityEvent onReachedPointB;
    public UnityEngine.Events.UnityEvent onStartedMoving;

    public enum MovementType
    {
        PingPong,    // Движение туда-обратно
        Loop,        // Зацикленное движение A->B->A->B...
        Once,        // Один раз A->B и остановка
        OnceReturn   // A->B->A и остановка
    }

    // Приватные переменные
    private Shape4D shape4D;
    private Vector3 startPosition3D;
    private float startPositionW;
    private float journeyLength;
    private float journeyLengthW;
    private float currentJourney;
    private bool movingForward = true;
    private bool isWaiting;
    private bool hasStarted;
    private bool hasFinished;
    private float waitTimer;
    private Vector3 previousPosition;
    private float previousWPosition;
    private Vector3 platformVelocity;
    private float platformWVelocity;

    // Ссылка на игрока, который стоит на платформе
    private Transform carriedPlayer;
    private PlayerController carriedPlayerController;

    void Start()
    {
        // Получаем компонент Shape4D если есть
        shape4D = GetComponent<Shape4D>();

        // Сохраняем стартовую позицию
        startPosition3D = transform.position;
        if (shape4D != null)
            startPositionW = shape4D.positionW;

        // Вычисляем длину пути
        journeyLength = Vector3.Distance(pointA, pointB);
        journeyLengthW = Mathf.Abs(pointBW - pointAW);

        // Устанавливаем начальную позицию
        currentJourney = startPosition * journeyLength;

        // Устанавливаем объект в начальную позицию
        UpdatePosition();

        // Сохраняем предыдущую позицию для расчета скорости
        previousPosition = transform.position;
        previousWPosition = shape4D != null ? shape4D.positionW : 0f;

        // Запускаем движение
        if (autoStart && startDelay <= 0)
        {
            StartMovement();
        }
        else if (autoStart && startDelay > 0)
        {
            Invoke(nameof(StartMovement), startDelay);
        }
    }

    void Update()
    {
        if (!hasStarted || isWaiting || hasFinished) return;

        // Сохраняем предыдущую позицию
        previousPosition = transform.position;
        previousWPosition = shape4D != null ? shape4D.positionW : 0f;

        // Двигаем объект
        float movement = speed * Time.deltaTime;

        if (movingForward)
        {
            currentJourney += movement;

            if (currentJourney >= journeyLength)
            {
                currentJourney = journeyLength;
                OnReachedPoint(true); // Достигли точки B
            }
        }
        else
        {
            currentJourney -= movement;

            if (currentJourney <= 0f)
            {
                currentJourney = 0f;
                OnReachedPoint(false); // Достигли точки A
            }
        }

        // Обновляем позицию
        UpdatePosition();

        // Применяем вращение
        if (enableRotation)
        {
            UpdateRotation();
        }

        // Вычисляем скорость платформы
        platformVelocity = (transform.position - previousPosition) / Time.deltaTime;
        if (shape4D != null)
            platformWVelocity = (shape4D.positionW - previousWPosition) / Time.deltaTime;

        // Двигаем игрока вместе с платформой
        if (carryPlayer && carriedPlayer != null)
        {
            CarryPlayer();
        }
    }

    void UpdatePosition()
    {
        // Вычисляем прогресс (0-1)
        float progress = journeyLength > 0 ? currentJourney / journeyLength : 0f;

        // Применяем easing если нужно
        float easedProgress = useEasing ? easingCurve.Evaluate(progress) : progress;

        // Интерполируем 3D позицию
        Vector3 newPosition = Vector3.Lerp(pointA, pointB, easedProgress);
        transform.position = newPosition;

        // Интерполируем W позицию если включено 4D движение
        if (enable4DMovement && shape4D != null)
        {
            float wProgress = journeyLengthW > 0 ? currentJourney / journeyLength : 0f;
            float easedWProgress = useEasing ? easingCurve.Evaluate(wProgress) : wProgress;
            shape4D.positionW = Mathf.Lerp(pointAW, pointBW, easedWProgress);
        }
    }

    void UpdateRotation()
    {
        // Интерполируем скорость вращения в зависимости от позиции
        float progress = journeyLength > 0 ? currentJourney / journeyLength : 0f;
        Vector3 currentRotationSpeed = Vector3.Lerp(rotationSpeedAtA, rotationSpeedAtB, progress);

        // Применяем вращение
        transform.Rotate(currentRotationSpeed * Time.deltaTime);
    }

    void OnReachedPoint(bool isPointB)
    {
        float waitTime = isPointB ? waitAtB : waitAtA;

        if (isPointB)
        {
            onReachedPointB?.Invoke();
        }
        else
        {
            onReachedPointA?.Invoke();
        }

        switch (movementType)
        {
            case MovementType.PingPong:
                if (waitTime > 0)
                {
                    StartWait(waitTime, () => {
                        movingForward = !movingForward;
                    });
                }
                else
                {
                    movingForward = !movingForward;
                }
                break;

            case MovementType.Loop:
                if (waitTime > 0)
                {
                    StartWait(waitTime, () => {
                        currentJourney = 0f;
                        movingForward = true;
                    });
                }
                else
                {
                    currentJourney = 0f;
                    movingForward = true;
                }
                break;

            case MovementType.Once:
                hasFinished = true;
                break;

            case MovementType.OnceReturn:
                if (isPointB)
                {
                    if (waitTime > 0)
                    {
                        StartWait(waitTime, () => {
                            movingForward = false;
                        });
                    }
                    else
                    {
                        movingForward = false;
                    }
                }
                else
                {
                    hasFinished = true;
                }
                break;
        }
    }

    void StartWait(float duration, System.Action onComplete)
    {
        isWaiting = true;
        waitTimer = duration;
        StartCoroutine(WaitRoutine(onComplete));
    }

    IEnumerator WaitRoutine(System.Action onComplete)
    {
        yield return new WaitForSeconds(waitTimer);
        isWaiting = false;
        onComplete?.Invoke();
    }

    void CarryPlayer()
    {
        if (carriedPlayer != null)
        {
            // Двигаем игрока с той же скоростью, что и платформа
            carriedPlayer.position += platformVelocity * Time.deltaTime;

            // Двигаем игрока по W если есть контроллер
            if (carriedPlayerController != null && enable4DMovement)
            {
                var wPositionField = typeof(PlayerController).GetField("wPosition",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (wPositionField != null)
                {
                    float currentW = (float)wPositionField.GetValue(carriedPlayerController);
                    wPositionField.SetValue(carriedPlayerController, currentW + platformWVelocity * Time.deltaTime);
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (carryPlayer && other.CompareTag("Player"))
        {
            carriedPlayer = other.transform;
            carriedPlayerController = carriedPlayer.GetComponent<PlayerController>();
            if (carriedPlayerController == null)
                carriedPlayerController = carriedPlayer.GetComponentInParent<PlayerController>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && carriedPlayer == other.transform)
        {
            carriedPlayer = null;
            carriedPlayerController = null;
        }
    }

    // Публичные методы для управления

    /// <summary>
    /// Запустить движение платформы
    /// </summary>
    public void StartMovement()
    {
        hasStarted = true;
        onStartedMoving?.Invoke();
    }

    /// <summary>
    /// Остановить движение платформы
    /// </summary>
    public void StopMovement()
    {
        hasStarted = false;
    }

    /// <summary>
    /// Сбросить платформу в начальное состояние
    /// </summary>
    public void Reset()
    {
        StopAllCoroutines();
        currentJourney = startPosition * journeyLength;
        movingForward = true;
        isWaiting = false;
        hasStarted = false;
        hasFinished = false;
        UpdatePosition();

        if (autoStart)
            StartMovement();
    }

    /// <summary>
    /// Телепортировать платформу в точку A
    /// </summary>
    public void GoToPointA()
    {
        currentJourney = 0f;
        movingForward = true;
        UpdatePosition();
    }

    /// <summary>
    /// Телепортировать платформу в точку B
    /// </summary>
    public void GoToPointB()
    {
        currentJourney = journeyLength;
        movingForward = false;
        UpdatePosition();
    }

    /// <summary>
    /// Установить скорость движения
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0f, newSpeed);
    }

    /// <summary>
    /// Получить текущий прогресс движения (0-1)
    /// </summary>
    public float GetProgress()
    {
        return journeyLength > 0 ? currentJourney / journeyLength : 0f;
    }

    void OnDrawGizmos()
    {
        if (!showPath) return;

        // Рисуем точки A и B
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pointA, markerSize);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pointB, markerSize);

        // Рисуем линию пути
        Gizmos.color = pathColor;
        Gizmos.DrawLine(pointA, pointB);

        // Рисуем стрелки направления
        Vector3 direction = (pointB - pointA).normalized;
        float arrowSize = markerSize * 2f;

        if (journeyLength > 0)
        {
            // Стрелка от A к B
            Vector3 midPoint = Vector3.Lerp(pointA, pointB, 0.5f);
            DrawArrowGizmo(midPoint, direction, arrowSize, pathColor);

            if (movementType == MovementType.PingPong || movementType == MovementType.OnceReturn)
            {
                // Стрелка от B к A
                Vector3 midPointReturn = Vector3.Lerp(pointA, pointB, 0.7f);
                DrawArrowGizmo(midPointReturn, -direction, arrowSize, pathColor);
            }
        }

#if UNITY_EDITOR
        // Подписи
        UnityEditor.Handles.color = Color.green;
        UnityEditor.Handles.Label(pointA + Vector3.up * 0.3f, "A");
        
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.Label(pointB + Vector3.up * 0.3f, "B");
        
        // Тип движения
        UnityEditor.Handles.color = Color.white;
        Vector3 labelPos = Vector3.Lerp(pointA, pointB, 0.5f) + Vector3.up * 1f;
        UnityEditor.Handles.Label(labelPos, $"{gameObject.name}\n{movementType}\nSpeed: {speed}");
#endif
    }

    void DrawArrowGizmo(Vector3 position, Vector3 direction, float size, Color color)
    {
        Gizmos.color = color;

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -150, 0) * Vector3.forward;

        Gizmos.DrawRay(position, direction * size);
        Gizmos.DrawRay(position + direction * size, right * size * 0.5f);
        Gizmos.DrawRay(position + direction * size, left * size * 0.5f);
    }
}

