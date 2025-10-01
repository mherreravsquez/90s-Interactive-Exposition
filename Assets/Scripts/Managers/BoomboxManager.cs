using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;

public class BoomboxManager : MonoBehaviour
{
    public ReaperOSCManager oscManager;
    public ReachFeedback reachFeedback;
    public int instrumentLimit = 3;
    public Collider spreadArea;
    public float resetDelay = 2f;
    
    [Header("Instrument Prefabs")]
    public List<GameObject> instrumentPrefabs; // Prefabs de instrumentos disponibles
    public int initialInstrumentCount; // Cuántos instrumentos instanciar al inicio
    public Transform instrumentsParent;
    
    [Header("Animation")]
    public Animator boomboxAnimator;
    public string animationStateParameter = "InstrumentCount";
    public string specialEffectTrigger = "SpecialEffect";
    
    // Listas para trackear instrumentos
    [SerializeField] List<InstrumentData> activeInstruments = new List<InstrumentData>();
    [SerializeField] List<GameObject> spawnedInstruments = new List<GameObject>(); // Instrumentos instanciados
    private bool isLimitReached = false;
    private bool isInSpecialEffect = false;
    
    void Start()
    {
        // Instanciar instrumentos al inicio
        ClearForInstruments();
        
        // Inicializar animación
        if (boomboxAnimator != null)
        {
            boomboxAnimator.SetInteger(animationStateParameter, 0);
        }
    }
    
    private void ClearForInstruments()
    {
        // Limpiar lista por si acaso
        spawnedInstruments.Clear();
        
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
        
        // Instanciar la cantidad inicial de instrumentos
        for (int i = 0; i < initialInstrumentCount; i++)
        {
            SpawnRandomInstrument();
        }
        
        Debug.Log($"Se instanciaron {spawnedInstruments.Count} instrumentos iniciales");
    }
    
    private void SpawnRandomInstrument()
    {
        if (instrumentPrefabs.Count == 0) return;
        
        // Elegir un prefab aleatorio
        int randomIndex = Random.Range(0, instrumentPrefabs.Count);
        GameObject instrumentPrefab = instrumentPrefabs[randomIndex];
        
        // Instanciar el instrumento
        GameObject newInstrument = Instantiate(instrumentPrefab);
        InstrumentData newData = newInstrument.GetComponent<Instrument>().instrumentData;
        
        // Posicionar en un lugar aleatorio del área
        Vector3 randomPosition = GetRandomPositionInArea();
        newInstrument.transform.position = randomPosition;
        
        // Asegurarse de que tenga el tag correcto                                               
        newInstrument.tag = "Instrument";

        newInstrument.transform.SetParent(instrumentsParent, true);
        
        // Agregar a la lista de instrumentos instanciados
        spawnedInstruments.Add(newInstrument);

        for (int i = spawnedInstruments.Count - 1; i >= 0; i--)
        {
            GameObject instrumentOnList = spawnedInstruments[i];

            if (instrumentOnList == null) continue;

            Instrument existingInstrument = instrumentOnList.GetComponent<Instrument>();

            if (existingInstrument != null && existingInstrument.instrumentData != null)
            {
                if (newData == existingInstrument.instrumentData)
                {
                    // REVISAR, DESTRUIR SOLO INSTRUMENTS CLONADOS

                    //Destroy(newInstrument);
                    //spawnedInstruments.Remove(newInstrument);
                }
                
            }
        }
        
        Debug.Log($"Instanciado instrumento: {newInstrument.name} en posición {randomPosition}");
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
        
        // ReactivateAndSpreadInstruments();

        foreach (InstrumentData data in activeInstruments)
        {
            GameObject obj = data.GetComponent<Transform>().gameObject;
            Destroy(obj);
        }

        ClearForInstruments();
        
        activeInstruments.Clear();
        isLimitReached = false;
        isInSpecialEffect = false;
        
        // Restablecer animación a estado inicial
        UpdateAnimationState();
        
        Debug.Log("Sistema de Boombox reiniciado");
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
    
    //private void ReactivateAndSpreadInstruments()
    //{
    //    Debug.Log($"Reactivando instrumentos para esparcido...");
        
    //    // Reactivar todos los instrumentos que están en la boombox
    //    foreach (Transform child in transform)
    //    {
    //        if (child.CompareTag("Instrument"))
    //        {
    //            ReactivateInstrument(child.gameObject);
    //        }
    //    }
        
    //    SpreadAllInstruments();
    //}
    
    //private void ReactivateInstrument(GameObject instrument)
    //{
    //    if (instrument == null) return;
        
    //    // Remover de la jerarquía de la boombox
    //    instrument.transform.SetParent(null);
        
    //    // Reactivar componentes
    //    MeshRenderer renderer = instrument.GetComponent<MeshRenderer>();
    //    if (renderer != null)
    //        renderer.enabled = true;
            
    //    Collider[] colliders = instrument.GetComponents<Collider>();
    //    foreach (Collider collider in colliders)
    //    {
    //        collider.enabled = true;
    //    }
        
    //    AudioSource audioSource = instrument.GetComponent<AudioSource>();
    //    if (audioSource != null)
    //    {
    //        audioSource.enabled = true;
    //    }
        
    //    // Marcar como interactuable nuevamente
    //    Instrument instrumentComp = instrument.GetComponent<Instrument>();
    //    if (instrumentComp != null)
    //    {
    //        instrumentComp.isInBoombox = false;
    //    }
        
    //    Debug.Log($"Instrumento {instrument.name} reactivado");
    //}
    
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
                instrument.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                
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
        
        // Desactivar el renderizado
        MeshRenderer renderer = instrument.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = false;
        
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
    
    // Método para agregar más instrumentos dinámicamente si es necesario
    public void AddMoreInstruments(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnRandomInstrument();
        }
        Debug.Log($"Se agregaron {count} instrumentos nuevos");
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