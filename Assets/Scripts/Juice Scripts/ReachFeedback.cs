using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ReachFeedback : MonoBehaviour
{
    public Transform reachPivot;
    public float minDistance = 0.5f;
    public float maxDistance = 2f;
    
    [Header("Volume Settings")]
    public float maxVolume = 0.3f;
    public AnimationCurve volumeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float volumeSmoothTime = 0.3f;
    
    [Header("Fade Settings")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.3f;

    // Diccionario para manejar múltiples instrumentos
    private Dictionary<GameObject, AudioSource> activeInstruments = new Dictionary<GameObject, AudioSource>();
    private Dictionary<AudioSource, float> volumeVelocities = new Dictionary<AudioSource, float>();
    private Dictionary<AudioSource, float> fadeTimers = new Dictionary<AudioSource, float>();

    private void Start()
    {
        if (volumeCurve == null || volumeCurve.length == 0)
        {
            volumeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Instrument") && !activeInstruments.ContainsKey(other.gameObject))
        {
            // Verificar si el instrumento ya está en la boombox (no interactuable)
            Instrument instrument = other.GetComponent<Instrument>();
            if (instrument != null && instrument.isInBoombox) 
                return;

            AudioSource audioSource = other.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                activeInstruments[other.gameObject] = audioSource;
                volumeVelocities[audioSource] = 0f;
                fadeTimers[audioSource] = 0f;
                
                audioSource.volume = 0f;
                audioSource.Play();
                
                Debug.Log($"{other.name} entered reach area. Active instruments: {activeInstruments.Count}");
            }
        }
    }

    private void Update()
    {
        if (reachPivot == null) return;

        // Lista de instrumentos a remover (por si se fueron o se volvieron no interactuables)
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var kvp in activeInstruments)
        {
            GameObject instrumentObj = kvp.Key;
            AudioSource audioSource = kvp.Value;

            // Verificar si el instrumento sigue siendo válido
            if (instrumentObj == null || audioSource == null)
            {
                toRemove.Add(instrumentObj);
                continue;
            }

            // Verificar si el instrumento ahora está en la boombox
            Instrument instrument = instrumentObj.GetComponent<Instrument>();
            if (instrument != null && instrument.isInBoombox)
            {
                toRemove.Add(instrumentObj);
                continue;
            }

            // Calcular distancia y volumen
            float distance = Vector3.Distance(reachPivot.position, instrumentObj.transform.position);
            float normalizedDistance = Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance));
            
            float curveValue = volumeCurve.Evaluate(1f - normalizedDistance);
            float targetVolume = curveValue * maxVolume;

            // Aplicar fade in
            if (fadeTimers.ContainsKey(audioSource) && fadeTimers[audioSource] < fadeInDuration)
            {
                fadeTimers[audioSource] += Time.deltaTime;
                float fadeProgress = Mathf.Clamp01(fadeTimers[audioSource] / fadeInDuration);
                targetVolume *= fadeProgress;
            }

            // Aplicar suavizado al volumen
            float currentVelocity = volumeVelocities.ContainsKey(audioSource) ? volumeVelocities[audioSource] : 0f;
            audioSource.volume = Mathf.SmoothDamp(audioSource.volume, targetVolume, ref currentVelocity, volumeSmoothTime);
            volumeVelocities[audioSource] = currentVelocity;
        }

        // Remover instrumentos inválidos
        foreach (GameObject instrumentObj in toRemove)
        {
            RemoveInstrument(instrumentObj);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Instrument") && activeInstruments.ContainsKey(other.gameObject))
        {
            RemoveInstrument(other.gameObject);
        }
    }

    private void RemoveInstrument(GameObject instrumentObj)
    {
        if (activeInstruments.ContainsKey(instrumentObj))
        {
            AudioSource audioSource = activeInstruments[instrumentObj];
            StartCoroutine(FadeOutAndStop(audioSource));
            
            activeInstruments.Remove(instrumentObj);
            volumeVelocities.Remove(audioSource);
            fadeTimers.Remove(audioSource);
            
            Debug.Log($"{instrumentObj.name} left reach area. Active instruments: {activeInstruments.Count}");
        }
    }

    private System.Collections.IEnumerator FadeOutAndStop(AudioSource audioSource)
    {
        if (audioSource == null) yield break;

        float startVolume = audioSource.volume;
        float timer = 0f;
        
        while (timer < fadeOutDuration && audioSource != null)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeOutDuration;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, progress);
            yield return null;
        }
        
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = 0f;
        }
    }

    // Método para que BoomboxManager pueda forzar la remoción de un instrumento
    public void ForceRemoveInstrument(GameObject instrument)
    {
        if (activeInstruments.ContainsKey(instrument))
        {
            RemoveInstrument(instrument);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (reachPivot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(reachPivot.position, minDistance);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(reachPivot.position, maxDistance);
            
            // Dibujar líneas a todos los instrumentos activos
            Gizmos.color = Color.green;
            foreach (var instrumentObj in activeInstruments.Keys)
            {
                if (instrumentObj != null)
                    Gizmos.DrawLine(reachPivot.position, instrumentObj.transform.position);
            }
        }
    }
}