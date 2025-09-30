using System;
using UnityEngine;

public class Instrument : MonoBehaviour
{
    public InstrumentData instrumentData;
    public AudioSource audioSource;
    public bool isInBoombox = false;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = instrumentData.previewClip;
        }
    }
}