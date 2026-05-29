using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class TriggerEvent : UnityEvent<Transform> { }

public class Shape4DTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("The radius within which the trigger activates")]
    public float triggerRadius = 1.5f;

    [Header("Trigger Events")]
    [SerializeField] public TriggerEvent onTriggerEnter = new TriggerEvent();
    [SerializeField] public TriggerEvent onTriggerStay = new TriggerEvent();
    [SerializeField] public TriggerEvent onTriggerExit = new TriggerEvent();

    [Header("Trigger State")]
    [SerializeField]
    private bool isTriggered;
    [SerializeField]
    private float currentDistance;

    [Header("One Shot Settings")]
    [Tooltip("If true, the trigger will only fire once")]
    public bool oneShot;
    private bool hasFired;

    [Header("Stay Trigger Settings")]
    [Tooltip("If true, OnTriggerStay will be called repeatedly while inside")]
    public bool continuousStay = true;
    [Tooltip("Delay between Stay calls (seconds)")]
    public float stayInterval = 0.1f;
    private float stayTimer;

    public void OnTriggerEnter(Transform player)
    {
        if (oneShot && hasFired) return;

        if (!isTriggered)
        {
            isTriggered = true;
            hasFired = true;
            onTriggerEnter.Invoke(player);
            Debug.Log($"Player entered 4D trigger: {gameObject.name}");

            if (continuousStay)
            {
                stayTimer = 0f;
            }
        }
    }

    public void OnTriggerExit(Transform player)
    {
        if (oneShot) return;

        if (isTriggered)
        {
            isTriggered = false;
            onTriggerExit.Invoke(player);
            Debug.Log($"Player exited 4D trigger: {gameObject.name}");
        }
    }

    public void OnTriggerStay(Transform player)
    {
        if (oneShot && hasFired) return;

        if (isTriggered)
        {
            if (continuousStay)
            {
                stayTimer += Time.deltaTime;
                if (stayTimer >= stayInterval)
                {
                    stayTimer = 0f;
                    onTriggerStay.Invoke(player);
                }
            }
            else
            {
                onTriggerStay.Invoke(player);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isTriggered ? Color.red : Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;

        // Рисуем сферу триггера
        Gizmos.DrawWireSphere(Vector3.zero, triggerRadius);

        // Рисуем W-ось
        Gizmos.color = Color.cyan;
        Vector3 wStart = new Vector3(0, -triggerRadius, 0);
        Vector3 wEnd = new Vector3(0, triggerRadius, 0);
        Gizmos.DrawLine(wStart, wEnd);

        // Подписи
#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * (triggerRadius + 0.2f), 
            gameObject.name + "\nTrigger: " + (isTriggered ? "ACTIVE" : "idle"));
#endif
    }
}

