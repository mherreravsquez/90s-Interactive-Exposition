using System;
using UnityEngine;

public class ReachFeedback : MonoBehaviour
{
    public AudioSource instrumentOnReach;
    bool inReach = false;

    public Transform reachPivot;

    private void Start()
    {
        reachPivot = GetComponentInChildren<Transform>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Instrument"))
        {
            instrumentOnReach = other.gameObject.GetComponent<AudioSource>();
            instrumentOnReach.Play();
            inReach = true;
            
            Debug.Log(other.name + " is in trigger");
            
        }
    }

    private void Update()
    {
        if (inReach)
        {
            float distance = Vector3.Distance(reachPivot.position, instrumentOnReach.transform.position);
            
            
            // Subir el volumen gradualmente mientras más cerca se encuentre del reachPivot
            // instrumentOnReach.volume = 
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Instrument"))
        {
            inReach  = false;
            instrumentOnReach.Stop();
            instrumentOnReach = null;
        }
    }
}