using UnityEngine;

public class BoomboxManager : MonoBehaviour
{
    public ReaperOSCManager oscManager;
    
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
            Debug.Log($"Instrumento {instrument.instrumentType} soltado en el trigger");
            
            // Enviar comando para desmutear el track en REAPER
            if (oscManager != null)
            {
                oscManager.SendTrackUnmute(instrument.instrumentID);
                
                // Ocultar y preparar el instrumento
                PrepareInstrumentForBoombox(other.gameObject);
            }
            else
            {
                Debug.LogWarning("OSC Manager no asignado en BoomboxManager");
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
        
        // Opcional: desactivar scripts de interacción del instrumento
        KinectHandCursor grabController = instrument.GetComponent<KinectHandCursor>();
        if (grabController != null)
            grabController.enabled = false;
            
        ReachFeedback reachFeedback = instrument.GetComponent<ReachFeedback>();
        if (reachFeedback != null)
            reachFeedback.enabled = false;
    }
    
    // Método opcional para reactivar instrumento si se necesita sacar de la boombox
    // public void ReleaseInstrument(GameObject instrument)
    // {
    //     if (instrument.transform.parent == transform)
    //     {
    //         instrument.transform.SetParent(null);
    //         
    //         // Reactivar componentes
    //         MeshRenderer renderer = instrument.GetComponent<MeshRenderer>();
    //         if (renderer != null)
    //             renderer.enabled = true;
    //             
    //         Collider[] colliders = instrument.GetComponents<Collider>();
    //         foreach (Collider collider in colliders)
    //         {
    //             collider.enabled = true;
    //         }
    //         
    //         AudioSource audioSource = instrument.GetComponent<AudioSource>();
    //         if (audioSource != null)
    //         {
    //             audioSource.enabled = true;
    //         }
    //         
    //         // Reactivar scripts
    //         KinectHandCursor grabController = instrument.GetComponent<KinectHandCursor>();
    //         if (grabController != null)
    //             grabController.enabled = true;
    //             
    //         ReachFeedback reachFeedback = instrument.GetComponent<ReachFeedback>();
    //         if (reachFeedback != null)
    //             reachFeedback.enabled = true;
    //             
    //         // Mutar el track en REAPER
    //         Instrument instrumentComponent = instrument.GetComponent<Instrument>();
    //         if (instrumentComponent != null && instrumentComponent.instrumentData != null && oscManager != null)
    //         {
    //             oscManager.SendTrackMute(instrumentComponent.instrumentData.instrumentID);
    //         }
    //     }
    // }
}