using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private TriggerType triggerType = TriggerType.OnEnable;

    [Header("Sound Settings")]
    [SerializeField] private string soundId;
    [SerializeField] private bool usePosition = true;
    [SerializeField] private Vector3 soundPosition;

    [Header("Music Settings")]
    [SerializeField] private bool playMusic;
    [SerializeField] private string musicId;
    [SerializeField] private bool stopMusic;
    [SerializeField] private bool fadeMusic = true;

    [Header("Advanced")]
    [SerializeField] private bool oneShot = true;
    [SerializeField] private float delay = 0f;
    [SerializeField] private float volumeMultiplier = 1f;
    [SerializeField] private float pitchMultiplier = 1f;

    private bool hasTriggered;
    private Coroutine delayCoroutine;

    public enum TriggerType
    {
        OnEnable,
        OnDisable,
        OnTriggerEnter,
        OnTriggerExit,
        OnCollisionEnter,
        OnCollisionExit,
        Manual
    }

    private void OnEnable()
    {
        if (triggerType == TriggerType.OnEnable)
        {
            Trigger();
        }
    }

    private void OnDisable()
    {
        if (triggerType == TriggerType.OnDisable)
        {
            Trigger();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerType == TriggerType.OnTriggerEnter && other.CompareTag("Player"))
        {
            Trigger();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (triggerType == TriggerType.OnTriggerExit && other.CompareTag("Player"))
        {
            Trigger();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (triggerType == TriggerType.OnCollisionEnter && collision.gameObject.CompareTag("Player"))
        {
            Trigger();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (triggerType == TriggerType.OnCollisionExit && collision.gameObject.CompareTag("Player"))
        {
            Trigger();
        }
    }

    public void Trigger()
    {
        if (oneShot && hasTriggered) return;

        if (delay > 0)
        {
            if (delayCoroutine != null) StopCoroutine(delayCoroutine);
            delayCoroutine = StartCoroutine(DelayedTrigger());
        }
        else
        {
            ExecuteTrigger();
        }
    }

    private IEnumerator DelayedTrigger()
    {
        yield return new WaitForSeconds(delay);
        ExecuteTrigger();
    }

    private void ExecuteTrigger()
    {
        hasTriggered = true;
        AudioManager manager = AudioManager.Instance;

        // Проигрываем звук
        if (!string.IsNullOrEmpty(soundId))
        {
            Vector3 pos = usePosition ? (transform.position + soundPosition) : transform.position;
            manager.PlaySoundWithParams(soundId, pos, volumeMultiplier, pitchMultiplier);
        }

        // Управляем музыкой
        if (playMusic && !string.IsNullOrEmpty(musicId))
        {
            manager.PlayMusic(musicId, fadeMusic);
        }
        else if (stopMusic)
        {
            manager.StopMusic(fadeMusic);
        }
    }

    public void Reset()
    {
        hasTriggered = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (usePosition)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + soundPosition, 0.3f);
            Gizmos.DrawLine(transform.position, transform.position + soundPosition);
        }
    }
}

