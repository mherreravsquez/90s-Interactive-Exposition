using UnityEngine;

public class ReachFeedback : MonoBehaviour
{
    public AudioSource instrumentOnReach;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Instrument"))
        {
            instrumentOnReach = other.gameObject.GetComponent<AudioSource>();
            
            Debug.Log(other.name + " is in trigger");
            
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Instrument"))
        {
            instrumentOnReach = other.gameObject.GetComponent<AudioSource>();
        }
    }
}