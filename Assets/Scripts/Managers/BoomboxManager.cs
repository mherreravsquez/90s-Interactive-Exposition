using UnityEngine;

public class BoomboxManager : MonoBehaviour
{
    public ReaperOSCManager oscManager;
    
    void OnTriggerEnter(Collider other)
    {
        InstrumentData instrument = other.GetComponent<Instrument>().instrumentData;
        if (instrument != null)
        {
            Debug.Log($"Instrumento {instrument.instrumentType} soltado en el trigger");
            
            // Enviar comando para desmutear el track en REAPER
            if (oscManager != null)
            {
                oscManager.SendTrackUnmute(instrument.instrumentID);
                
                other.gameObject.GetComponent<MeshRenderer>().enabled = false;
                other.gameObject.transform.parent = transform;
                other.gameObject.transform.localPosition = Vector3.zero;
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        InstrumentData instrument = other.GetComponent<Instrument>().instrumentData;
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
