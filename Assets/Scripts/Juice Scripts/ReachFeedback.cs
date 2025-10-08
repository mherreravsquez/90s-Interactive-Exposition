using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public class ReachFeedback : MonoBehaviour
{
    public Transform reachPivot;
    
    [Header("Distance Settings")]
    public float minDistance = 0.5f;
    public float maxDistance = 8f;  // Much wider range for gradual transition
    
    [Header("Volume Settings")]
    public float minVolume = 0.15f;  // Minimum audible volume
    public float maxVolume = 0.3f;
    [Tooltip("Curve that controls how volume changes with distance")]
    public AnimationCurve volumeCurve = new AnimationCurve(
        new Keyframe(0f, 1f),    // At minDistance: maximum volume
        new Keyframe(0.3f, 0.8f), // At 30% distance: 80% volume
        new Keyframe(0.6f, 0.4f), // At 60% distance: 40% volume  
        new Keyframe(1f, 0.15f)   // At maxDistance: minimum volume
    );
    public float volumeSmoothTime = 0.5f;  // Smoother transition
    
    [Header("Fade Settings")]
    public float fadeInDuration = 1f;      // More gradual fade in
    public float fadeOutDuration = 0.8f;   // More gradual fade out

    [Header("Visual Feedback")]
    public SpriteRenderer feedbackSprite;
    public ParticleSystem feedbackParticles;
    public float spriteFadeInDuration = 0.5f;
    public float spriteFadeOutDuration = 0.3f;

    // Dictionaries to manage multiple instruments
    private Dictionary<GameObject, AudioSource> activeInstruments = new Dictionary<GameObject, AudioSource>();
    private Dictionary<AudioSource, float> volumeVelocities = new Dictionary<AudioSource, float>();
    private Dictionary<AudioSource, float> fadeTimers = new Dictionary<AudioSource, float>();
    
    private int activeInstrumentCount = 0;
    private Color originalSpriteColor;

    private void Start()
    {
        // Ensure curve has default values if empty
        if (volumeCurve == null || volumeCurve.length == 0)
        {
            volumeCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.3f, 0.8f),
                new Keyframe(0.6f, 0.4f),
                new Keyframe(1f, 0.15f)
            );
        }
        
        // Store original sprite color and set initial transparency
        if (feedbackSprite != null)
        {
            originalSpriteColor = feedbackSprite.color;
            Color transparentColor = originalSpriteColor;
            transparentColor.a = 0f;
            feedbackSprite.color = transparentColor;
        }
        
        // Stop particles initially
        if (feedbackParticles != null && feedbackParticles.isPlaying)
        {
            feedbackParticles.Stop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Instrument") && !activeInstruments.ContainsKey(other.gameObject))
        {
            // Check if instrument is already in boombox (non-interactable)
            Instrument instrument = other.GetComponent<Instrument>();
            if (instrument != null && instrument.isInBoombox) 
                return;

            AudioSource audioSource = other.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                activeInstruments[other.gameObject] = audioSource;
                volumeVelocities[audioSource] = 0f;
                fadeTimers[audioSource] = 0f;
                
                audioSource.volume = minVolume;  // Start with minimum volume
                audioSource.Play();
                
                activeInstrumentCount++;
                
                // Activate visual feedback when first instrument enters
                if (activeInstrumentCount == 1)
                {
                    ActivateVisualFeedback();
                }
                
                // Debug.Log($"{other.name} entered reach area. Active instruments: {activeInstruments.Count}");
            }
        }
    }

    private void Update()
    {
        if (reachPivot == null) return;

        // List of instruments to remove (in case they left or became non-interactable)
        List<GameObject> toRemove = new List<GameObject>();

        foreach (var kvp in activeInstruments)
        {
            GameObject instrumentObj = kvp.Key;
            AudioSource audioSource = kvp.Value;

            // Check if instrument is still valid
            if (instrumentObj == null || audioSource == null)
            {
                toRemove.Add(instrumentObj);
                continue;
            }

            // Check if instrument is now in boombox
            Instrument instrument = instrumentObj.GetComponent<Instrument>();
            if (instrument != null && instrument.isInBoombox)
            {
                toRemove.Add(instrumentObj);
                continue;
            }

            // Calculate distance and volume
            float distance = Vector3.Distance(reachPivot.position, instrumentObj.transform.position);
            
            // If within minimum range, maximum volume
            if (distance <= minDistance)
            {
                SetAudioVolume(audioSource, maxVolume);
                continue;
            }
            
            // If outside maximum range, minimum volume
            if (distance >= maxDistance)
            {
                SetAudioVolume(audioSource, minVolume);
                continue;
            }
            
            // Calculate volume based on custom curve
            float normalizedDistance = Mathf.Clamp01((distance - minDistance) / (maxDistance - minDistance));
            float curveValue = volumeCurve.Evaluate(normalizedDistance);
            float targetVolume = Mathf.Lerp(minVolume, maxVolume, curveValue);

            SetAudioVolume(audioSource, targetVolume);
        }

        // Remove invalid instruments
        foreach (GameObject instrumentObj in toRemove)
        {
            RemoveInstrument(instrumentObj);
        }
    }

    // AUXILIARY METHOD TO HANDLE VOLUME WITH FADE
    private void SetAudioVolume(AudioSource audioSource, float targetVolume)
    {
        // Apply fade in if needed
        if (fadeTimers.ContainsKey(audioSource) && fadeTimers[audioSource] < fadeInDuration)
        {
            fadeTimers[audioSource] += Time.deltaTime;
            float fadeProgress = Mathf.Clamp01(fadeTimers[audioSource] / fadeInDuration);
            targetVolume *= fadeProgress;
        }

        // Volume smoothing
        float currentVelocity = volumeVelocities.ContainsKey(audioSource) ? volumeVelocities[audioSource] : 0f;
        audioSource.volume = Mathf.SmoothDamp(audioSource.volume, targetVolume, ref currentVelocity, volumeSmoothTime);
        volumeVelocities[audioSource] = currentVelocity;
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
            
            activeInstrumentCount--;
            
            // Deactivate visual feedback when no instruments remain
            if (activeInstrumentCount == 0)
            {
                DeactivateVisualFeedback();
            }
            
            // Debug.Log($"{instrumentObj.name} left reach area. Active instruments: {activeInstruments.Count}");
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

    // Method for BoomboxManager to force removal of an instrument
    public void ForceRemoveInstrument(GameObject instrument)
    {
        if (activeInstruments.ContainsKey(instrument))
        {
            RemoveInstrument(instrument);
        }
    }

    private void ActivateVisualFeedback()
    {
        // Fade in sprite using DOTween
        if (feedbackSprite != null)
        {
            feedbackSprite.DOKill(); // Kill any existing tween
            feedbackSprite.DOFade(originalSpriteColor.a, spriteFadeInDuration)
                .SetEase(Ease.OutQuad);
        }
        
        // Play particles
        if (feedbackParticles != null && !feedbackParticles.isPlaying)
        {
            feedbackParticles.Play();
        }
        
        Debug.Log("Visual feedback activated");
    }

    private void DeactivateVisualFeedback()
    {
        // Fade out sprite using DOTween
        if (feedbackSprite != null)
        {
            feedbackSprite.DOKill(); // Kill any existing tween
            feedbackSprite.DOFade(0f, spriteFadeOutDuration)
                .SetEase(Ease.InQuad);
        }
        
        // Stop particles
        if (feedbackParticles != null && feedbackParticles.isPlaying)
        {
            feedbackParticles.Stop();
        }
        
        Debug.Log("Visual feedback deactivated");
    }

    private void OnDrawGizmosSelected()
    {
        if (reachPivot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(reachPivot.position, minDistance);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(reachPivot.position, maxDistance);
            
            // Draw lines to all active instruments
            Gizmos.color = Color.green;
            foreach (var instrumentObj in activeInstruments.Keys)
            {
                if (instrumentObj != null)
                    Gizmos.DrawLine(reachPivot.position, instrumentObj.transform.position);
            }
        }
    }
}