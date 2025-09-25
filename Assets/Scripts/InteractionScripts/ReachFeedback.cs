using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ReachFeedback : MonoBehaviour
{
    public List<AudioSource> instruments = new List<AudioSource>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Instrument"))
        {
            instruments.Add(other.gameObject.GetComponent<AudioSource>());
            
            Debug.Log(other.name + " is in trigger");
            
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Instrument"))
        {
            instruments.Remove(other.gameObject.GetComponent<AudioSource>());
        }
    }
}