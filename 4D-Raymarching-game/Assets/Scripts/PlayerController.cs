using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float playerSpeed = 10f;
    public float acceleration = 15f;
    public float decceleration = 20f;
    public float rotationSpeed = 15f;

    [Header("4D Movement Settings")]
    public float wSpeed = 2f;
    public float wAcceleration = 5f;
    public float wDecceleration = 8f;
    [SerializeField]
    private float wPosition;

    public float DeathDistance;

    [Header("Jump Settings")]
    [Tooltip("Сила прыжка")]
    public float jumpForce = 8f;

    [Tooltip("Время удержания кнопки для увеличения высоты прыжка")]
    public float jumpHoldTime = 0.2f;

    [Tooltip("Множитель силы при удержании кнопки прыжка")]
    public float jumpHoldMultiplier = 1.5f;

    [Tooltip("Гравитация, применяемая к игроку")]
    public float gravity = 15f;

    [Tooltip("Максимальная скорость падения")]
    public float maxFallSpeed = 20f;

    [Tooltip("Множитель гравитации при падении (для более быстрого падения)")]
    public float fallMultiplier = 2.5f;

    [Tooltip("Множитель гравитации при коротком нажатии прыжка (для низких прыжков)")]
    public float lowJumpMultiplier = 2f;

    [Tooltip("Буфер ввода прыжка (в секундах)")]
    public float jumpBufferTime = 0.1f;

    [Tooltip("Время койота (возможность прыгнуть чуть позже после ухода с края)")]
    public float coyoteTime = 0.1f;

    [Header("References")]
    [SerializeField] private RaymarchCam raymarchCam;
    [SerializeField] private PlayerRayMarchCollider playerCollider;

    private Vector3 StartPos;
    private Vector3 currentVelocity;
    private float currentWVelocity;
    private bool endGame = false;

    // Переменные для прыжка
    private float verticalVelocity;
    private bool isJumping;
    private float jumpTimeCounter;
    private bool jumpHeld;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private bool isGrounded;

    // Для внешнего управления скоростью (батуты)
    private Vector3 externalVelocity;
    private float externalWVelocity;
    private bool hasExternalVelocity;
    private float externalVelocityDecay = 0.95f;

    private void Start()
    {
        StartPos = transform.position;
        currentVelocity = Vector3.zero;
        currentWVelocity = 0f;
        wPosition = 0f;
        verticalVelocity = 0f;
        externalVelocity = Vector3.zero;
        externalWVelocity = 0f;
        isJumping = false;

        if (raymarchCam == null && Camera.main != null)
        {
            raymarchCam = Camera.main.GetComponent<RaymarchCam>();
        }

        if (playerCollider == null)
        {
            playerCollider = GetComponent<PlayerRayMarchCollider>();
        }
    }

    void Update()
    {
        if (transform.position.y < DeathDistance)
        {
            CheckpointManager.Instance.RespawnAtCurrentCheckpoint();
        }

        if (!endGame)
        {
            CheckGrounded();
            HandleJumpInput();
            MovePlayer();
            MoveW();
            ApplyGravity();
            UpdateExternalVelocity();
        }

        if (raymarchCam != null)
        {
            raymarchCam._wPosition = wPosition;
        }
    }

    void CheckGrounded()
    {
        if (playerCollider != null)
        {
            isGrounded = playerCollider.IsGrounded();
        }
        else
        {
            // Если нет коллайдера, считаем что на земле когда verticalVelocity <= 0
            // Это запасной вариант
            isGrounded = verticalVelocity <= 0.01f;
        }

        // Coyote time - даем небольшое окно для прыжка после ухода с края
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    void HandleJumpInput()
    {
        // Jump buffer - запоминаем нажатие прыжка на короткое время
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
            jumpHeld = true;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            jumpHeld = false;
        }

        // Уменьшаем буфер
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Проверяем возможность прыжка
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0 && !isJumping)
        {
            // Начинаем прыжок
            verticalVelocity = jumpForce;
            isJumping = true;
            jumpTimeCounter = jumpHoldTime;
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;

            // Сообщаем коллайдеру о прыжке
            if (playerCollider != null)
            {
                playerCollider.ResetFallVelocity();
            }
        }

        // Удержание прыжка для увеличения высоты
        if (isJumping)
        {
            if (jumpTimeCounter > 0 && jumpHeld)
            {
                // Продолжаем добавлять силу пока удерживаем кнопку
                float additionalForce = jumpForce * jumpHoldMultiplier * Time.deltaTime / jumpHoldTime;
                verticalVelocity += additionalForce;
                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                // Закончили фазу удержания
                jumpTimeCounter = 0;
            }
        }
    }

    void ApplyGravity()
    {
        if (!isGrounded)
        {
            // Разная гравитация в зависимости от фазы прыжка
            if (isJumping && verticalVelocity > 0)
            {
                // Фаза подъема
                if (!jumpHeld || jumpTimeCounter <= 0)
                {
                    // Если кнопку отпустили или время вышло - увеличиваем гравитацию для низкого прыжка
                    verticalVelocity -= gravity * lowJumpMultiplier * Time.deltaTime;
                }
                else
                {
                    // Нормальная гравитация при удержании
                    verticalVelocity -= gravity * Time.deltaTime;
                }
            }
            else if (verticalVelocity < 0)
            {
                // Фаза падения - увеличенная гравитация для более быстрого падения
                verticalVelocity -= gravity * fallMultiplier * Time.deltaTime;
            }
            else
            {
                // Начало падения
                verticalVelocity -= gravity * Time.deltaTime;
            }

            // Ограничение скорости падения
            verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);
        }
        else
        {
            // На земле - сбрасываем вертикальную скорость
            if (!isJumping)
            {
                verticalVelocity = 0f;
            }
        }

        // Применяем вертикальное движение
        if (!isGrounded || isJumping)
        {
            transform.Translate(Vector3.up * verticalVelocity * Time.deltaTime, Space.World);
        }

        // Проверяем, закончился ли прыжок
        if (isJumping && verticalVelocity <= 0)
        {
            isJumping = false;
        }
    }

    // Обновление внешней скорости с затуханием
    void UpdateExternalVelocity()
    {
        if (hasExternalVelocity)
        {
            // Применяем затухание
            externalVelocity *= externalVelocityDecay;
            externalWVelocity *= externalVelocityDecay;

            // Применяем внешнюю скорость
            transform.Translate(externalVelocity * Time.deltaTime, Space.World);
            wPosition += externalWVelocity * Time.deltaTime;

            // Отключаем внешнюю скорость если она стала очень маленькой
            if (externalVelocity.magnitude < 0.01f && Mathf.Abs(externalWVelocity) < 0.01f)
            {
                hasExternalVelocity = false;
                externalVelocity = Vector3.zero;
                externalWVelocity = 0f;
            }
        }
    }

    // Метод для батута: заменяет скорость
    public void SetBounceVelocity(Vector3 velocity3D, float velocityW)
    {
        currentVelocity = Vector3.zero;
        currentWVelocity = 0f;
        externalVelocity = velocity3D;
        externalWVelocity = velocityW;
        hasExternalVelocity = true;

        // Сбрасываем вертикальную скорость при отскоке
        verticalVelocity = velocity3D.y;
    }

    // Метод для батута: добавляет скорость
    public void AddVelocity(Vector3 velocity3D, float velocityW)
    {
        externalVelocity += velocity3D;
        externalWVelocity += velocityW;
        hasExternalVelocity = true;

        // Добавляем к вертикальной скорости
        verticalVelocity += velocity3D.y;

        Debug.Log($"Added velocity: 3D={velocity3D}, W={velocityW}");
    }

    // Метод для батута: устанавливает точную скорость
    public void SetExactVelocity(Vector3 velocity3D, float velocityW)
    {
        currentVelocity = velocity3D;
        currentWVelocity = velocityW;
        externalVelocity = Vector3.zero;
        externalWVelocity = 0f;
        hasExternalVelocity = false;

        // Устанавливаем вертикальную скорость
        verticalVelocity = velocity3D.y;
    }

    public void ResetPlayer()
    {
        transform.position = StartPos;
        currentVelocity = Vector3.zero;
        currentWVelocity = 0f;
        wPosition = 0f;
        verticalVelocity = 0f;
        externalVelocity = Vector3.zero;
        externalWVelocity = 0f;
        hasExternalVelocity = false;
        isJumping = false;
    }

    void MovePlayer()
    {
        Vector3 inputDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        if (inputDirection.magnitude > 0.01f)
        {
            inputDirection.Normalize();
            Vector3 targetVelocity = inputDirection * playerSpeed;

            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                targetVelocity,
                acceleration * Time.deltaTime
            );
        }
        else
        {
            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                Vector3.zero,
                decceleration * Time.deltaTime
            );
        }

        // Применяем горизонтальное движение (вертикальное теперь управляется гравитацией)
        Vector3 horizontalMovement = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        transform.Translate(horizontalMovement * Time.deltaTime, Space.World);

        if (currentVelocity.magnitude > 0.1f)
        {
            Vector3 lookDirection = currentVelocity.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    void MoveW()
    {
        float wInput = 0f;

        if (Input.GetKey(KeyCode.Q))
        {
            wInput -= 1f;
        }

        if (Input.GetKey(KeyCode.E))
        {
            wInput += 1f;
        }

        if (Mathf.Abs(wInput) > 0.01f)
        {
            float targetWVelocity = wInput * wSpeed;

            currentWVelocity = Mathf.MoveTowards(
                currentWVelocity,
                targetWVelocity,
                wAcceleration * Time.deltaTime
            );
        }
        else
        {
            currentWVelocity = Mathf.MoveTowards(
                currentWVelocity,
                0f,
                wDecceleration * Time.deltaTime
            );
        }

        wPosition += currentWVelocity * Time.deltaTime;
    }

    // Публичный метод для проверки состояния прыжка
    public bool IsJumping()
    {
        return isJumping;
    }

    public void EndGame()
    {
        endGame = true;
        currentVelocity = Vector3.zero;
        currentWVelocity = 0f;
        verticalVelocity = 0f;
        externalVelocity = Vector3.zero;
        externalWVelocity = 0f;
        hasExternalVelocity = false;
        isJumping = false;
    }

    void OnDrawGizmosSelected()
    {
        if (raymarchCam != null)
        {
            raymarchCam._wPosition = wPosition;
        }
    }
}

