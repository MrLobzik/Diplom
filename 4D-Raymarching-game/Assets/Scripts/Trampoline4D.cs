using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trampoline4D : MonoBehaviour
{
    [Header("Trampoline Settings")]
    [Tooltip("Сила отскока в 3D пространстве")]
    public Vector3 bounceForce3D = new Vector3(0, 10f, 0);

    [Tooltip("Сила отскока по W измерению")]
    public float bounceForceW = 5f;

    [Tooltip("Множитель ускорения (чем больше, тем резче отскок)")]
    [Range(0.1f, 10f)]
    public float accelerationMultiplier = 2f;

    [Tooltip("Задержка перед применением силы (в секундах)")]
    public float bounceDelay = 0f;

    [Header("Visual Feedback")]
    [Tooltip("Префаб эффекта при отскоке")]
    public GameObject bounceEffect;

    [Tooltip("Звук при отскоке")]
    public AudioSource bounceSound;

    [Header("Advanced Settings")]
    [Tooltip("Если true, батут сохраняет текущую скорость игрока и добавляет к ней")]
    public bool additiveVelocity = false;

    [Tooltip("Если true, батут устанавливает конкретную скорость вместо добавления")]
    public bool setExactVelocity = false;

    private PlayerController playerController;
    private bool isBouncing;
    private float bounceTimer;

    // Этот метод подключается к Shape4DTrigger.OnTriggerEnter
    public void Bounce(Transform player)
    {
        if (player == null) return;

        // Ищем PlayerController у игрока или его родителя
        if (playerController == null)
        {
            playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = player.GetComponentInParent<PlayerController>();
            }
        }

        if (playerController != null && !isBouncing)
        {
            StartCoroutine(ApplyBounce(playerController));
        }
    }

    // Этот метод подключается к Shape4DTrigger.OnTriggerStay
    public void BounceStay(Transform player)
    {
        // Можно добавить постоянное ускорение пока игрок в зоне
        if (player != null && playerController != null)
        {
            // Опционально: постоянная сила пока игрок в триггере
        }
    }

    private IEnumerator ApplyBounce(PlayerController controller)
    {
        isBouncing = true;

        // Задержка если нужна
        if (bounceDelay > 0)
        {
            yield return new WaitForSeconds(bounceDelay);
        }

        // Применяем отскок
        if (setExactVelocity)
        {
            // Устанавливаем точную скорость
            controller.SetExactVelocity(bounceForce3D, bounceForceW);
        }
        else if (additiveVelocity)
        {
            // Добавляем к текущей скорости
            controller.AddVelocity(bounceForce3D * accelerationMultiplier,
                                 bounceForceW * accelerationMultiplier);
        }
        else
        {
            // Стандартный режим: заменяем скорость с учетом множителя
            controller.SetBounceVelocity(bounceForce3D * accelerationMultiplier,
                                       bounceForceW * accelerationMultiplier);
        }

        // Визуальные эффекты
        PlayBounceEffects();

        // Небольшая задержка перед следующей активацией
        yield return new WaitForSeconds(0.1f);
        isBouncing = false;
    }

    private void PlayBounceEffects()
    {
        // Спавним эффект
        if (bounceEffect != null)
        {
            Instantiate(bounceEffect, transform.position, Quaternion.identity);
        }

        // Проигрываем звук
        if (bounceSound != null)
        {
            bounceSound.Play();
        }
    }

    // Визуализация в редакторе
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;

        // Рисуем направление отскока
        Vector3 bounceDir = bounceForce3D.normalized;
        float bounceMagnitude = bounceForce3D.magnitude;

        // Основное направление
        Gizmos.DrawRay(Vector3.zero, bounceDir * bounceMagnitude * 0.5f);

        // Конус направления
        DrawArrow(Vector3.zero, bounceDir * bounceMagnitude * 0.5f, Color.green);

        // W-ось
        Gizmos.color = Color.cyan;
        if (bounceForceW > 0)
        {
            Vector3 wDir = new Vector3(0, bounceForceW * 0.5f, 0);
            Gizmos.DrawRay(Vector3.zero, wDir);
        }
        else if (bounceForceW < 0)
        {
            Vector3 wDir = new Vector3(0, bounceForceW * 0.5f, 0);
            Gizmos.DrawRay(Vector3.zero, wDir);
        }
    }

    private void DrawArrow(Vector3 start, Vector3 direction, Color color)
    {
        float arrowSize = 0.3f;
        Vector3 end = start + direction;

        // Линия
        Gizmos.DrawRay(start, direction);

        // Наконечник
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 30, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 30, 0) * Vector3.forward;
        Gizmos.DrawRay(end, right * arrowSize);
        Gizmos.DrawRay(end, left * arrowSize);
    }
}

