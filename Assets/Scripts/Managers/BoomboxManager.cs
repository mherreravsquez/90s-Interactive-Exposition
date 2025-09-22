using UnityEngine;

public class BoomboxManager : MonoBehaviour
{
    public ReaperOSCManager oscManager;
    
    void OnTriggerEnter(Collider other)
    {
        InstrumentData instrument = other.GetComponent<InstrumentData>();
        if (instrument != null)
        {
            Debug.Log($"Instrumento {instrument.instrumentType} soltado en el trigger");
            
            // Enviar comando para desmutear el track en REAPER
            if (oscManager != null)
            {
                oscManager.SendTrackUnmute(instrument.instrumentID);
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        InstrumentData instrument = other.GetComponent<InstrumentData>();
        if (instrument != null)
        {
            Debug.Log($"Instrumento {instrument.instrumentType} salió del trigger");
            
            // Opcional: mutear el track cuando sale del trigger
            if (oscManager != null)
            {
                oscManager.SendTrackMute(instrument.instrumentID);
            }
        }
    }
}
