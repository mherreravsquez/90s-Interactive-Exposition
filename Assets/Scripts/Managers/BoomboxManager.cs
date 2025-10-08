using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine.Serialization;
using DG.Tweening;
using UnityEngine.UI;

public class BoomboxManager : MonoBehaviour
{
    public ReaperOSCManager oscManager;
    public ReachFeedback reachFeedback;
    
    public int instrumentLimit = 3;
    [SerializeField] public Collider drumsSpreadArea;
    [SerializeField] public Collider bassSpreadArea;
    public Collider instrumentalSpreadArea;
    public float resetDelay = 2f;
    
    [Header("Instrument Prefabs")]
    public List<GameObject> instrumentPrefabs;
    public int initialInstrumentCount;
    public Transform instrumentsParent;
    
    [Header("Animation")]
    public Animator boomboxAnimator;
    public string animationStateParameter = "InstrumentCount";
    public string specialEffectTrigger = "SpecialEffect";
    
    [Header("Reach Feedback Reference")]
    public Collider reacherArea;
    
    [Header("Animation Settings")]
    public float instrumentExitDelay = 0.5f;
    public float instrumentMoveDuration = 1f;
    public Ease instrumentMoveEase = Ease.OutBack;
    
    [Header("Error Feedback")]
    public RawImage errorFeedbackImage;
    public Color errorColor = Color.red;
    public float shakeDuration = 0.4f;
    public float shakeStrength = 15f;
    public int shakeVibrato = 5;
    public float shakeRandomness = 30f;
    public float colorFlashDuration = 0.3f;
    
    [SerializeField] List<InstrumentData> activeInstruments = new List<InstrumentData>();
    [SerializeField] List<GameObject> spawnedInstruments = new List<GameObject>();
    [SerializeField] List<InstrumentData> usedInstrumentTypes = new List<InstrumentData>();
    
    private bool isLimitReached = false;
    private bool isInSpecialEffect = false;
    private Color originalImageColor;
    
    void Start()
    {
        // Guardar color y posición originales de la imagen
        if (errorFeedbackImage != null)
        {
            originalImageColor = errorFeedbackImage.color;
        }
        
        ClearForInstruments();
        
        if (boomboxAnimator != null)
        {
            boomboxAnimator.SetInteger(animationStateParameter, 0);
        }
    }
    
    private void ClearForInstruments()
    {
        // Clear lists
        spawnedInstruments.Clear();
        usedInstrumentTypes.Clear();
        
        if (instrumentPrefabs == null || instrumentPrefabs.Count == 0)
        {
            Debug.LogError("No instrument prefabs assigned in BoomboxManager");
            return;
        }
        
        if (drumsSpreadArea == null || bassSpreadArea == null || instrumentalSpreadArea == null)
        {
            Debug.LogError("Missing spread areas for instrument placement");
            return;
        }
        
        // Spawn exactly 2 instruments of each type
        SpawnInstrumentsByType(InstrumentType.Drums, 2);
        SpawnInstrumentsByType(InstrumentType.Bass, 2);
        SpawnInstrumentsByType(InstrumentType.Instrumental, 2);
        
        Debug.Log($"Spawned {spawnedInstruments.Count} initial instruments (2 of each type)");
    }
    
    private void SpawnInstrumentsByType(InstrumentType type, int count)
    {
        // Get all prefabs of the specified type
        var typePrefabs = instrumentPrefabs.Where(prefab => 
        {
            if (prefab == null) return false;
            Instrument instrument = prefab.GetComponent<Instrument>();
            return instrument != null && instrument.instrumentData != null && 
                   instrument.instrumentData.instrumentType == type;
        }).ToList();
        
        if (typePrefabs.Count == 0)
        {
            Debug.LogWarning($"No instrument prefabs found for type {type}");
            return;
        }
        
        // Spawn the requested number of instruments
        for (int i = 0; i < count; i++)
        {
            // Select random prefab from available ones of this type
            int randomIndex = Random.Range(0, typePrefabs.Count);
            GameObject instrumentPrefab = typePrefabs[randomIndex];
            
            // Instantiate instrument
            GameObject newInstrument = Instantiate(instrumentPrefab);
            Instrument newInstrumentComponent = newInstrument.GetComponent<Instrument>();
            
            if (newInstrumentComponent == null || newInstrumentComponent.instrumentData == null)
            {
                Debug.LogError($"Prefab {instrumentPrefab.name} missing Instrument component or instrumentData");
                Destroy(newInstrument);
                continue;
            }
            
            // Register used instrument type
            usedInstrumentTypes.Add(newInstrumentComponent.instrumentData);
            
            // Position in corresponding area based on type
            Vector3 randomPosition = GetPositionByInstrumentType(type);
            newInstrument.transform.position = randomPosition;
            
            // Ensure correct tag                                               
            newInstrument.tag = "Instrument";

            if (instrumentsParent != null)
            {
                newInstrument.transform.SetParent(instrumentsParent, true);
            }
            
            // Add to spawned instruments list
            spawnedInstruments.Add(newInstrument);
            
            Debug.Log($"Spawned instrument: {newInstrument.name} (Type: {type}) at position {randomPosition}");
        }
    }
    
    private Vector3 GetPositionByInstrumentType(InstrumentType instrumentType)
    {
        Collider targetArea = GetAreaForInstrumentType(instrumentType);
        
        if (targetArea == null)
        {
            Debug.LogWarning($"No area found for type {instrumentType}, using drums area as default");
            targetArea = drumsSpreadArea;
        }
        
        return GetRandomPositionInArea(targetArea);
    }
    
    private Collider GetAreaForInstrumentType(InstrumentType instrumentType)
    {
        switch (instrumentType)
        {
            case InstrumentType.Drums:
                return drumsSpreadArea;
            case InstrumentType.Bass:
                return bassSpreadArea;
            case InstrumentType.Instrumental:
                return instrumentalSpreadArea;
            default:
                Debug.LogWarning($"Unrecognized instrument type: {instrumentType}, using instrumental area as default");
                return instrumentalSpreadArea;
        }
    }
    
    private Vector3 GetRandomPositionInArea(Collider area)
    {
        if (area == null)
        {
            Debug.LogError("Area is null, cannot generate position");
            return Vector3.zero;
        }
        
        Bounds bounds = area.bounds;
        
        Vector3 randomPosition = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
        
        return randomPosition;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Instrument") && !isLimitReached && !isInSpecialEffect)
        {
            // Verify instrument is one of the spawned ones
            if (!spawnedInstruments.Contains(other.gameObject))
            {
                Debug.LogWarning($"Instrument {other.name} is not in spawned instruments list");
                return;
            }
            
            Instrument instrumentComponent = other.GetComponent<Instrument>();
            
            if (instrumentComponent == null || instrumentComponent.instrumentData == null)
            {
                Debug.LogWarning($"Instrument {other.name} missing Instrument component or instrumentData");
                return;
            }

            InstrumentData newInstrument = instrumentComponent.instrumentData;
            
            // Check if an instrument of the same type is already in the boombox
            InstrumentData existingInstrumentOfSameType = activeInstruments.Find(instr => instr.instrumentType == newInstrument.instrumentType);
            
            if (existingInstrumentOfSameType != null)
            {
                // If same type already exists, show error feedback and remove the last instrument
                Debug.Log($"Instrument of type {newInstrument.instrumentType} already in boombox. Removing last instrument.");
                PlayErrorFeedback();
                RemoveLastInstrument();
                return; // Exit and wait for the removal to complete
            }

            // Mark instrument as non-interactable
            instrumentComponent.isInBoombox = true;

            // Stop and disable local audio
            AudioSource audioSource = other.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }

            // Remove from ReachFeedback if present
            if (reachFeedback != null)
            {
                reachFeedback.ForceRemoveInstrument(other.gameObject);
            }
            
            // Add to active instruments list
            if (!activeInstruments.Contains(newInstrument))
            {
                activeInstruments.Add(newInstrument);
                
                // Update animation
                UpdateAnimationState();
            }
            
            Debug.Log($"Instrument {newInstrument.instrumentType} dropped in trigger. Active instruments: {activeInstruments.Count}");
            
            // Send unmute command to REAPER
            if (oscManager != null)
            {
                oscManager.SendTrackUnmute(newInstrument.instrumentID);
                
                // If first instrument, start REAPER playback
                if (activeInstruments.Count == 1)
                {
                    oscManager.StartReaperPlayback();
                }
                
                // Hide and prepare instrument
                PrepareInstrumentForBoombox(other.gameObject);
            }
            else
            {
                Debug.LogWarning("OSC Manager not assigned in BoomboxManager");
            }
            
            // Check if limit reached (now 3 different types)
            if (activeInstruments.Count >= instrumentLimit && !isLimitReached)
            {
                isLimitReached = true;
                StartCoroutine(ExecuteSpecialAction());
            }
        }
    }
    
    // Método para mostrar feedback de error
    private void PlayErrorFeedback()
    {
        if (errorFeedbackImage == null) return;
        
        // Detener cualquier animación previa
        errorFeedbackImage.DOKill();
        
        // Sacudir suavemente la RawImage en el eje X
        errorFeedbackImage.rectTransform.DOShakeAnchorPos(shakeDuration, new Vector2(shakeStrength, 0), shakeVibrato, shakeRandomness, false, true)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                // Asegurar que vuelva a la posición original
                errorFeedbackImage.rectTransform.anchoredPosition = new Vector2(0, errorFeedbackImage.rectTransform.anchoredPosition.y);
            });
        
        // Cambiar el color de la RawImage a rojo y luego volver al original
        Sequence colorSequence = DOTween.Sequence();
        colorSequence.Append(errorFeedbackImage.DOColor(errorColor, colorFlashDuration * 0.3f));
        colorSequence.Append(errorFeedbackImage.DOColor(originalImageColor, colorFlashDuration * 0.7f));
        colorSequence.Play();
        
        Debug.Log("Error feedback played: Instrument of same type already in boombox");
    }
    
    // Method to remove last instrument with animation
    public void RemoveLastInstrument()
    {
        // Check if there are active instruments
        if (activeInstruments.Count == 0)
        {
            Debug.Log("No active instruments to remove");
            return;
        }

        // Get last active instrument (most recent)
        InstrumentData lastInstrument = activeInstruments[activeInstruments.Count - 1];
        
        // Find GameObject corresponding to last instrument
        GameObject instrumentObject = FindInstrumentObject(lastInstrument);
        
        if (instrumentObject != null)
        {
            StartCoroutine(RemoveLastInstrumentWithAnimation(instrumentObject, lastInstrument));
        }
        else
        {
            Debug.LogWarning("Could not find GameObject for last instrument");
        }
    }

    private IEnumerator RemoveLastInstrumentWithAnimation(GameObject instrumentObject, InstrumentData instrumentData)
    {
        // 1. Reactivate all instrument components (except colliders)
        ReactivateInstrument(instrumentObject, false);
        
        // 2. Mute track in REAPER
        if (oscManager != null)
        {
            oscManager.SendTrackMute(instrumentData.instrumentID);
        }
        
        // 3. Wait before starting animation
        yield return new WaitForSeconds(instrumentExitDelay);
        
        // 4. Get target position in reacher area
        Vector3 targetPosition = GetRandomPositionInReacherArea();
        
        // 5. DOTween animation - smoothly move to target position
        instrumentObject.transform.DOMove(targetPosition, instrumentMoveDuration)
            .SetEase(instrumentMoveEase)
            .OnStart(() => {
                Debug.Log($"Starting instrument {instrumentObject.name} animation to reacher area");
            })
            .OnComplete(() => {
                // Enable colliders only when animation completes
                EnableInstrumentColliders(instrumentObject);
                Debug.Log($"Instrument {instrumentObject.name} reached destination");
            });
        
        // 6. Optional: Add small scale animation for better effect
        instrumentObject.transform.DOScale(1.1f, instrumentMoveDuration * 0.3f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
        
        // 7. Remove from active instruments list
        activeInstruments.Remove(instrumentData);
        
        // 8. Update animation
        UpdateAnimationState();
        
        // 9. If it was the only instrument, stop REAPER playback
        if (activeInstruments.Count == 0 && oscManager != null)
        {
            oscManager.StopReaperPlayback();
        }
        
        Debug.Log($"Instrument {instrumentData.instrumentType} removed from boombox and animated to reacher");
    }

    private void EnableInstrumentColliders(GameObject instrument)
    {
        if (instrument == null) return;
        
        // Enable colliders
        Collider[] colliders = instrument.GetComponents<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = true;
        }
        
        Debug.Log($"Enabled colliders for instrument {instrument.name}");
    }

    private Vector3 GetRandomPositionInReacherArea()
    {
        if (reacherArea == null)
        {
            Debug.LogWarning("No reacher area assigned");
            return GetRandomPositionInArea(drumsSpreadArea);
        }
        
        return GetRandomPositionInArea(reacherArea);
    }

    private GameObject FindInstrumentObject(InstrumentData instrumentData)
    {
        // Search spawned instruments marked as in boombox
        foreach (GameObject instrument in spawnedInstruments)
        {
            if (instrument == null) continue;
            
            Instrument instrumentComp = instrument.GetComponent<Instrument>();
            if (instrumentComp != null && 
                instrumentComp.instrumentData == instrumentData && 
                instrumentComp.isInBoombox)
            {
                return instrument;
            }
        }
        
        // Search boombox children
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Instrument"))
            {
                Instrument instrumentComp = child.GetComponent<Instrument>();
                if (instrumentComp != null && instrumentComp.instrumentData == instrumentData)
                {
                    return child.gameObject;
                }
            }
        }
        
        return null;
    }

    private void ReactivateInstrument(GameObject instrument, bool enableColliders = true)
    {
        if (instrument == null) return;
        
        Instrument instrumentComp = instrument.GetComponent<Instrument>();
        if (instrumentComp != null)
        {
            instrumentComp.isInBoombox = false;
        }
        
        // Reactivate MeshRenderer
        MeshRenderer meshRenderer = instrument.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = true;
        
        // Reactivate Animator
        Animator animator = instrument.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = true;
        
        // Conditionally reactivate colliders
        if (enableColliders)
        {
            EnableInstrumentColliders(instrument);
        }
        else
        {
            // Disable colliders during animation
            Collider[] colliders = instrument.GetComponents<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }
        }
        
        // Reactivate AudioSource (if needed)
        AudioSource audioSource = instrument.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.enabled = true;
        }
        
        // Remove as child of boombox
        if (instrumentsParent != null)
        {
            instrument.transform.SetParent(instrumentsParent);
        }
        else
        {
            instrument.transform.SetParent(null);
        }
        
        // Ensure normal scale before any animation
        instrument.transform.localScale = Vector3.one;
        
        Debug.Log($"Instrument {instrument.name} reactivated");
    }
    
    private IEnumerator ExecuteSpecialAction()
    {
        Debug.Log("Instrument limit reached! Executing special action...");
        isInSpecialEffect = true;
        
        // Activate special effect animation
        if (boomboxAnimator != null)
        {
            boomboxAnimator.SetTrigger(specialEffectTrigger);
        }
        
        // 1. Perform special action
        yield return StartCoroutine(PerformSpecialAction());
        
        // 2. Wait for special effect animation to finish
        yield return new WaitForSeconds(resetDelay);
        
        // 3. Reset entire system
        ResetBoomboxSystem();
    }
    
    private IEnumerator PerformSpecialAction()
    {
        // Visual and sound effects
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }
        
        if (oscManager != null)
        {
            oscManager.SendSpecialAction();
        }
        
        // Wait while special effect animation plays
        yield return new WaitForSeconds(3f);
        
        Debug.Log("Special action completed");
    }
    
    private void ResetBoomboxSystem()
    {
        Debug.Log("Resetting Boombox system...");
        
        if (oscManager != null)
        {
            oscManager.StopReaperPlayback();
            oscManager.MuteAllTracks();
        }
        
        // 1. Destroy instruments in boombox
        List<GameObject> instrumentsToRemove = new List<GameObject>();
        
        // Find instruments that are children of boombox (inserted ones)
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Instrument"))
            {
                instrumentsToRemove.Add(child.gameObject);
            }
        }
        
        // Also search spawnedInstruments for instruments marked as in boombox
        foreach (GameObject instrument in spawnedInstruments.ToList())
        {
            if (instrument == null) continue;
            
            Instrument instrumentComp = instrument.GetComponent<Instrument>();
            if (instrumentComp != null && instrumentComp.isInBoombox)
            {
                if (!instrumentsToRemove.Contains(instrument))
                {
                    instrumentsToRemove.Add(instrument);
                }
            }
        }
        
        // Destroy and remove instruments
        foreach (GameObject instrument in instrumentsToRemove)
        {
            if (instrument != null)
            {
                // Remove from usedInstrumentTypes
                Instrument instrumentComp = instrument.GetComponent<Instrument>();
                if (instrumentComp != null && instrumentComp.instrumentData != null)
                {
                    usedInstrumentTypes.Remove(instrumentComp.instrumentData);
                }
                
                // Remove from spawnedInstruments
                spawnedInstruments.Remove(instrument);
                
                // Destroy GameObject
                Destroy(instrument);
            }
        }
        
        Debug.Log($"Removed {instrumentsToRemove.Count} instruments from boombox");
        
        // 2. Replenish instruments up to 6 (2 of each type)
        int currentInstrumentCount = spawnedInstruments.Count;
        int instrumentsNeeded = 6 - currentInstrumentCount;
        
        Debug.Log($"Current instruments: {currentInstrumentCount}, Needed: {instrumentsNeeded}");
        
        // Spawn the missing instruments by type
        int drumsCount = spawnedInstruments.Count(instr => 
            instr != null && instr.GetComponent<Instrument>()?.instrumentData?.instrumentType == InstrumentType.Drums);
        int bassCount = spawnedInstruments.Count(instr => 
            instr != null && instr.GetComponent<Instrument>()?.instrumentData?.instrumentType == InstrumentType.Bass);
        int instrumentalCount = spawnedInstruments.Count(instr => 
            instr != null && instr.GetComponent<Instrument>()?.instrumentData?.instrumentType == InstrumentType.Instrumental);
        
        SpawnInstrumentsByType(InstrumentType.Drums, 2 - drumsCount);
        SpawnInstrumentsByType(InstrumentType.Bass, 2 - bassCount);
        SpawnInstrumentsByType(InstrumentType.Instrumental, 2 - instrumentalCount);
        
        // 3. Spread all remaining instruments
        SpreadAllInstruments();
        
        // 4. Clear states
        activeInstruments.Clear();
        isLimitReached = false;
        isInSpecialEffect = false;
        
        // 5. Reset animation to initial state
        UpdateAnimationState();
        
        Debug.Log($"Boombox system reset. Total instruments: {spawnedInstruments.Count}");
    }
    
    private void UpdateAnimationState()
    {
        if (boomboxAnimator != null)
        {
            if (isInSpecialEffect)
            {
                // Special effect state handled by trigger
                return;
            }
            
            // Set state based on instrument count
            int instrumentCount = activeInstruments.Count;
            boomboxAnimator.SetInteger(animationStateParameter, instrumentCount);
            
            Debug.Log($"Updating animation to state: {instrumentCount} instruments");
        }
    }
    
    private void SpreadAllInstruments()
    {
        if (drumsSpreadArea == null || bassSpreadArea == null || instrumentalSpreadArea == null)
        {
            Debug.LogWarning("Missing spread areas assigned");
            return;
        }
        
        Debug.Log($"Spreading {spawnedInstruments.Count} spawned instruments by type");
        
        // Spread all spawned instruments to their corresponding areas
        foreach (GameObject instrument in spawnedInstruments)
        {
            if (instrument != null)
            {
                Instrument instrumentComp = instrument.GetComponent<Instrument>();
                if (instrumentComp != null && instrumentComp.instrumentData != null)
                {
                    Vector3 randomPosition = GetPositionByInstrumentType(instrumentComp.instrumentData.instrumentType);
                    instrument.transform.position = randomPosition;
                    
                    Debug.Log($"Instrument {instrument.name} (Type: {instrumentComp.instrumentData.instrumentType}) moved to {randomPosition}");
                }
            }
        }
        
        Debug.Log($"Spread {spawnedInstruments.Count} instruments by type");
    }
    
    private void PrepareInstrumentForBoombox(GameObject instrument)
    {
        if (instrument == null) return;
        
        MeshRenderer meshRenderer = instrument.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;
        
        // Disable Animator
        Animator animator = instrument.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;
        
        // Disable colliders to prevent further interactions
        Collider[] colliders = instrument.GetComponents<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
        
        // Make child of boombox and center
        instrument.transform.SetParent(transform);
        instrument.transform.localPosition = Vector3.zero;
        instrument.transform.localRotation = Quaternion.identity;
        
        Debug.Log($"Instrument {instrument.name} prepared for boombox");
    }
    
    // Method to clear and reset all instruments
    public void ResetAllInstruments()
    {
        // Destroy all spawned instruments
        foreach (GameObject instrument in spawnedInstruments)
        {
            if (instrument != null)
            {
                Destroy(instrument);
            }
        }
        spawnedInstruments.Clear();
        usedInstrumentTypes.Clear();
        
        // Spawn new instruments
        ClearForInstruments();
        
        Debug.Log("All instruments have been reset");
    }
    
    public int GetActiveInstrumentCount()
    {
        return activeInstruments.Count;
    }
    
    public int GetTotalInstrumentCount()
    {
        return spawnedInstruments.Count;
    }
    
    public void ForceReset()
    {
        ResetBoomboxSystem();
    }
}