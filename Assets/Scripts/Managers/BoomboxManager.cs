using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class BoomboxManager : MonoBehaviour
{
    public ReaperOSCManager oscManager;
    public ReachFeedback reachFeedback;
    public int instrumentLimit = 3;
    public Collider spreadArea;
    public float resetDelay = 2f;
    
    [Header("Instrument Prefabs")]
    public List<GameObject> instrumentPrefabs;
    public int initialInstrumentCount;
    public Transform instrumentsParent;
    
    [Header("Animation")]
    public Animator boomboxAnimator;
    public string animationStateParameter = "InstrumentCount";
    public string specialEffectTrigger = "SpecialEffect";
    
    [SerializeField] List<InstrumentData> activeInstruments = new List<InstrumentData>();
    [SerializeField] List<GameObject> spawnedInstruments = new List<GameObject>();
    [SerializeField] List<InstrumentData> usedInstrumentTypes = new List<InstrumentData>();
    
    private bool isLimitReached = false;
    private bool isInSpecialEffect = false;
    
    void Start()
    {
        ClearForInstruments();
        
        if (boomboxAnimator != null)
        {
            boomboxAnimator.SetInteger(animationStateParameter, 0);
        }
    }
    
    private void ClearForInstruments()
    {
        // Limpiar listas
        spawnedInstruments.Clear();
        usedInstrumentTypes.Clear();
        
        if (instrumentPrefabs == null || instrumentPrefabs.Count == 0)
        {
            Debug.LogError("No hay prefabs de instrumentos asignados en BoomboxManager");
            return;
        }
        
        if (spreadArea == null)
        {
            Debug.LogError("No hay área de esparcido asignada para colocar instrumentos");
            return;
        }
        
        // Instanciar exactamente initialInstrumentCount instrumentos únicos
        int instrumentsToSpawn = Mathf.Min(initialInstrumentCount, instrumentPrefabs.Count);
        for (int i = 0; i < instrumentsToSpawn; i++)
        {
            SpawnUniqueInstrument();
        }
        
        Debug.Log($"Se instanciaron {spawnedInstruments.Count} instrumentos iniciales");
    }
    
    private void SpawnUniqueInstrument()
    {
        if (instrumentPrefabs.Count == 0) return;
        
        // Obtener prefabs que no han sido usados
        var availablePrefabs = instrumentPrefabs.Where(prefab => 
        {
            if (prefab == null) return false;
            Instrument instrument = prefab.GetComponent<Instrument>();
            return instrument != null && instrument.instrumentData != null && 
                   !usedInstrumentTypes.Contains(instrument.instrumentData);
        }).ToList();
        
        // Si no hay más tipos únicos, reiniciar el pool
        if (availablePrefabs.Count == 0)
        {
            Debug.LogWarning("No hay más instrumentos únicos disponibles. Reiniciando pool...");
            usedInstrumentTypes.Clear();
            availablePrefabs = instrumentPrefabs.Where(prefab => prefab != null).ToList();
        }
        
        if (availablePrefabs.Count == 0) return;
        
        // Elegir un prefab aleatorio de los disponibles
        int randomIndex = Random.Range(0, availablePrefabs.Count);
        GameObject instrumentPrefab = availablePrefabs[randomIndex];
        
        // Instanciar el instrumento
        GameObject newInstrument = Instantiate(instrumentPrefab);
        Instrument newInstrumentComponent = newInstrument.GetComponent<Instrument>();
        
        if (newInstrumentComponent == null || newInstrumentComponent.instrumentData == null)
        {
            Debug.LogError($"El prefab {instrumentPrefab.name} no tiene componente Instrument o instrumentData");
            Destroy(newInstrument);
            return;
        }
        
        // Registrar el tipo de instrumento usado
        usedInstrumentTypes.Add(newInstrumentComponent.instrumentData);
        
        // Posicionar en un lugar aleatorio del área
        Vector3 randomPosition = GetRandomPositionInArea();
        newInstrument.transform.position = randomPosition;
        
        // Asegurarse de que tenga el tag correcto                                               
        newInstrument.tag = "Instrument";

        if (instrumentsParent != null)
        {
            newInstrument.transform.SetParent(instrumentsParent, true);
        }
        
        // Agregar a la lista de instrumentos instanciados
        spawnedInstruments.Add(newInstrument);
        
        Debug.Log($"Instanciado instrumento único: {newInstrument.name} en posición {randomPosition}");
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Instrument") && !isLimitReached && !isInSpecialEffect)
        {
            // Verificar que el instrumento sea uno de los instanciados
            if (!spawnedInstruments.Contains(other.gameObject))
            {
                Debug.LogWarning($"Instrumento {other.name} no está en la lista de instrumentos instanciados");
                return;
            }
            
            Instrument instrumentComponent = other.GetComponent<Instrument>();
            
            if (instrumentComponent == null || instrumentComponent.instrumentData == null)
            {
                Debug.LogWarning($"Instrumento {other.name} no tiene componente Instrument o instrumentData");
                return;
            }

            // Marcar el instrumento como no interactuable
            instrumentComponent.isInBoombox = true;

            // Detener y desactivar el audio local del instrumento
            AudioSource audioSource = other.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }

            // Remover del ReachFeedback si está ahí
            if (reachFeedback != null)
            {
                reachFeedback.ForceRemoveInstrument(other.gameObject);
            }

            InstrumentData instrument = instrumentComponent.instrumentData;
            
            // Agregar a la lista de instrumentos activos
            if (!activeInstruments.Contains(instrument))
            {
                activeInstruments.Add(instrument);
                
                // Actualizar animación
                UpdateAnimationState();
            }
            
            Debug.Log($"Instrumento {instrument.instrumentType} soltado en el trigger. Instrumentos activos: {activeInstruments.Count}");
            
            // Enviar comando para desmutear el track en REAPER
            if (oscManager != null)
            {
                oscManager.SendTrackUnmute(instrument.instrumentID);
                
                // Si es el primer instrumento, iniciar reproducción en REAPER
                if (activeInstruments.Count == 1)
                {
                    oscManager.StartReaperPlayback();
                }
                
                // Ocultar y preparar el instrumento
                PrepareInstrumentForBoombox(other.gameObject);
            }
            else
            {
                Debug.LogWarning("OSC Manager no asignado en BoomboxManager");
            }
            
            // Verificar si se alcanzó el límite
            if (activeInstruments.Count >= instrumentLimit && !isLimitReached)
            {
                isLimitReached = true;
                StartCoroutine(ExecuteSpecialAction());
            }
        }
    }
    
    private IEnumerator ExecuteSpecialAction()
    {
        Debug.Log("¡Límite de instrumentos alcanzado! Ejecutando acción especial...");
        isInSpecialEffect = true;
        
        // Activar animación de efecto especial
        if (boomboxAnimator != null)
        {
            boomboxAnimator.SetTrigger(specialEffectTrigger);
        }
        
        // 1. Realizar acción especial
        yield return StartCoroutine(PerformSpecialAction());
        
        // 2. Esperar a que la animación de efecto especial termine
        yield return new WaitForSeconds(resetDelay);
        
        // 3. Reiniciar todo el sistema
        ResetBoomboxSystem();
    }
    
    private IEnumerator PerformSpecialAction()
    {
        // Efectos visuales y de sonido
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }
        
        if (oscManager != null)
        {
            oscManager.SendSpecialAction();
        }
        
        // Esperar mientras se ejecuta la animación de efecto especial
        yield return new WaitForSeconds(3f);
        
        Debug.Log("Acción especial completada");
    }
    
    private void ResetBoomboxSystem()
    {
        Debug.Log("Reiniciando sistema de Boombox...");
        
        if (oscManager != null)
        {
            oscManager.StopReaperPlayback();
            oscManager.MuteAllTracks();
        }
        
        // 1. Destruir instrumentos que están en la boombox
        List<GameObject> instrumentsToRemove = new List<GameObject>();
        
        // Encontrar instrumentos que son hijos de la boombox (los que fueron insertados)
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Instrument"))
            {
                instrumentsToRemove.Add(child.gameObject);
            }
        }
        
        // También buscar en spawnedInstruments por instrumentos que estén marcados como en boombox
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
        
        // Destruir y remover instrumentos
        foreach (GameObject instrument in instrumentsToRemove)
        {
            if (instrument != null)
            {
                // Remover de usedInstrumentTypes
                Instrument instrumentComp = instrument.GetComponent<Instrument>();
                if (instrumentComp != null && instrumentComp.instrumentData != null)
                {
                    usedInstrumentTypes.Remove(instrumentComp.instrumentData);
                }
                
                // Remover de spawnedInstruments
                spawnedInstruments.Remove(instrument);
                
                // Destruir el GameObject
                Destroy(instrument);
            }
        }
        
        Debug.Log($"Se eliminaron {instrumentsToRemove.Count} instrumentos de la boombox");
        
        // 2. Reponer instrumentos hasta alcanzar initialInstrumentCount
        int currentInstrumentCount = spawnedInstruments.Count;
        int instrumentsNeeded = initialInstrumentCount - currentInstrumentCount;
        
        Debug.Log($"Instrumentos actuales: {currentInstrumentCount}, Necesarios: {instrumentsNeeded}");
        
        for (int i = 0; i < instrumentsNeeded; i++)
        {
            SpawnUniqueInstrument();
        }
        
        // 3. Esparcir todos los instrumentos restantes
        SpreadAllInstruments();
        
        // 4. Limpiar estados
        activeInstruments.Clear();
        isLimitReached = false;
        isInSpecialEffect = false;
        
        // 5. Restablecer animación a estado inicial
        UpdateAnimationState();
        
        Debug.Log($"Sistema de Boombox reiniciado. Total instrumentos: {spawnedInstruments.Count}");
    }
    
    private void UpdateAnimationState()
    {
        if (boomboxAnimator != null)
        {
            if (isInSpecialEffect)
            {
                // El estado de efecto especial se maneja con trigger
                return;
            }
            
            // Establecer el estado basado en la cantidad de instrumentos
            int instrumentCount = activeInstruments.Count;
            boomboxAnimator.SetInteger(animationStateParameter, instrumentCount);
            
            Debug.Log($"Actualizando animación a estado: {instrumentCount} instrumentos");
        }
    }
    
    private void SpreadAllInstruments()
    {
        if (spreadArea == null)
        {
            Debug.LogWarning("No hay área de esparcido asignada");
            return;
        }
        
        Debug.Log($"Esparciendo {spawnedInstruments.Count} instrumentos instanciados");
        
        // Esparcir todos los instrumentos instanciados
        foreach (GameObject instrument in spawnedInstruments)
        {
            if (instrument != null)
            {
                Vector3 randomPosition = GetRandomPositionInArea();
                instrument.transform.position = randomPosition;
                
                Debug.Log($"Instrumento {instrument.name} movido a {randomPosition}");
            }
        }
        
        Debug.Log($"Se esparcieron {spawnedInstruments.Count} instrumentos");
    }
    
    private Vector3 GetRandomPositionInArea()
    {
        if (spreadArea == null) return Vector3.zero;
        
        Bounds bounds = spreadArea.bounds;
        
        Vector3 randomPosition = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
        
        return randomPosition;
    }
    
    private void PrepareInstrumentForBoombox(GameObject instrument)
    {
        if (instrument == null) return;
        
        MeshRenderer meshRenderer = instrument.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;
        
        // Desactivar el Animator
        Animator animator = instrument.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;
        
        // Desactivar colliders para evitar nuevas interacciones
        Collider[] colliders = instrument.GetComponents<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
        
        // Hacer hijo de la boombox y centrar
        instrument.transform.SetParent(transform);
        instrument.transform.localPosition = Vector3.zero;
        instrument.transform.localRotation = Quaternion.identity;
        
        Debug.Log($"Instrumento {instrument.name} preparado para boombox");
    }
    
    // Método para limpiar y reiniciar todos los instrumentos
    public void ResetAllInstruments()
    {
        // Destruir todos los instrumentos instanciados
        foreach (GameObject instrument in spawnedInstruments)
        {
            if (instrument != null)
            {
                Destroy(instrument);
            }
        }
        spawnedInstruments.Clear();
        usedInstrumentTypes.Clear();
        
        // Instanciar nuevos instrumentos
        ClearForInstruments();
        
        Debug.Log("Todos los instrumentos han sido reiniciados");
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