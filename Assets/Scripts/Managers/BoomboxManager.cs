using UnityEngine;
using System.Collections.Generic;

public class BoomboxManager : MonoBehaviour
{
    public ReaperOSCManager oscManager;
    
    // Lista para trackear instrumentos activos
    private List<InstrumentData> activeInstruments = new List<InstrumentData>();
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Instrument"))
        {
            // Obtener referencias de manera segura
            AudioSource audioSource = other.GetComponent<AudioSource>();
            Instrument instrumentComponent = other.GetComponent<Instrument>();
            
            if (instrumentComponent == null || instrumentComponent.instrumentData == null)
            {
                Debug.LogWarning($"Instrumento {other.name} no tiene componente Instrument o instrumentData");
                return;
            }

            // Detener y desactivar el audio local del instrumento
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }

            InstrumentData instrument = instrumentComponent.instrumentData;
            
            // Agregar a la lista de instrumentos activos
            if (!activeInstruments.Contains(instrument))
            {
                activeInstruments.Add(instrument);
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
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Instrument"))
        {
            Instrument instrumentComponent = other.GetComponent<Instrument>();
            if (instrumentComponent != null && instrumentComponent.instrumentData != null)
            {
                InstrumentData instrument = instrumentComponent.instrumentData;
                
                // Remover de la lista de instrumentos activos
                if (activeInstruments.Contains(instrument))
                {
                    activeInstruments.Remove(instrument);
                    
                    // Mutear el track en REAPER
                    if (oscManager != null)
                    {
                        oscManager.SendTrackMute(instrument.instrumentID);
                    }
                }
                
                Debug.Log($"Instrumento {instrument.instrumentType} salió del trigger. Instrumentos activos: {activeInstruments.Count}");
                
                // Si no hay instrumentos activos, detener REAPER
                if (activeInstruments.Count == 0 && oscManager != null)
                {
                    oscManager.StopReaperPlayback();
                }
            }
        }
    }
    
    private void PrepareInstrumentForBoombox(GameObject instrument)
    {
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
    }
    
    // Método para obtener la cantidad de instrumentos activos
    public int GetActiveInstrumentCount()
    {
        return activeInstruments.Count;
    }
    
    // Método para limpiar cuando se desactiva el objeto
    private void OnDisable()
    {
        // Si la boombox se desactiva, detener REAPER
        if (oscManager != null && activeInstruments.Count > 0)
        {
            oscManager.StopReaperPlayback();
        }
    }
}