using UnityEngine;
using OscJack;

public class ReaperOSCManager : MonoBehaviour
{
    [Tooltip("Port number for OSC communication with Reaper")]
    public int reaperPort = 8888; // Puerto debe coincidir con la configuración de REAPER
    
    private OscClient client;
    
    void Start()
    {
        SetupOSC();
    }
    
    public void SetupOSC()
    {
        // Cerrar cliente existente si hay uno
        if (client != null)
        {
            client.Dispose();
            client = null;
        }
        
        // Usar dirección de loopback (127.0.0.1) para comunicación local
        client = new OscClient("127.0.0.1", reaperPort);
        
        Debug.Log($"Conectado a REAPER en localhost:{reaperPort}");
    }
    
    public void SendTrackUnmute(int trackId)
    {
        if (client == null) return;
        
        client.Send($"/track/{trackId}/unmute", 1);
        Debug.Log($"Enviando comando para desmutear track: {trackId}");
    }
    
    public void SendTrackMute(int trackId)
    {
        if (client == null) return;
        
        client.Send($"/track/{trackId}/mute", 1);
        Debug.Log($"Enviando comando para mutear track: {trackId}");
    }
    
    void OnApplicationQuit()
    {
        if (client != null)
            client.Dispose();
    }
    
    void OnDestroy()
    {
        if (client != null)
            client.Dispose();
    }
}