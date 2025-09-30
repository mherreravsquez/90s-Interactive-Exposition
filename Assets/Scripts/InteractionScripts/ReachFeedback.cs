using System;
using UnityEngine;

public class ReachFeedback : MonoBehaviour
{
    public AudioSource instrumentOnReach;
    public Transform reachPivot;
    public float minDistance = 0.5f;
    public float maxDistance = 2f;

    private bool inReach = false;
    private GameObject currentInstrument;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Instrument") && !inReach)
        {
            currentInstrument = other.gameObject;
            instrumentOnReach = currentInstrument.GetComponent<AudioSource>();
            if (instrumentOnReach != null)
            {
                inReach = true;
                instrumentOnReach.volume = 0f;
                instrumentOnReach.Play();
                Debug.Log(other.name + " is in trigger");
            }
        }
    }

    private void Update()
    {
        if (inReach && instrumentOnReach != null && reachPivot != null)
        {
            float distance = Vector3.Distance(reachPivot.position, currentInstrument.transform.position);
            // Normalizar la distancia entre minDistance y maxDistance para obtener un valor entre 0 y 1 (invertido: más cerca = más volumen)
            float normalizedDistance = Mathf.InverseLerp(minDistance, maxDistance, distance);
            // Invertir porque queremos que sea más fuerte cuando está más cerca
            float volume = 1f - Mathf.Clamp01(normalizedDistance);
            instrumentOnReach.volume = volume;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Instrument") && other.gameObject == currentInstrument)
        {
            inReach = false;
            if (instrumentOnReach != null)
            {
                instrumentOnReach.Stop();
                instrumentOnReach = null;
            }
            currentInstrument = null;
        }
    }
}