using UnityEngine;
using OscJack;

public class ReaperOSCManager : MonoBehaviour
{
    [Tooltip("Port number for OSC communication with Reaper")]
    public int reaperPort = 8888;
    
    private OscClient client;
    
    void Start()
    {
        SetupOSC();
    }
    
    public void SetupOSC()
    {
        if (client != null)
        {
            client.Dispose();
            client = null;
        }
        
        client = new OscClient("127.0.0.1", reaperPort);
        Debug.Log($"Conectado a REAPER en localhost:{reaperPort}");
    }
    
    public void SendTrackUnmute(int trackId)
    {
        if (client == null) return;
        
        // FORMATOS ALTERNATIVOS PARA PROBAR:
        
        // Opción 1: Formato estándar de REAPER
        client.Send($"/track/{trackId}/mute", 0);
        
        // Opción 2: Con dirección completa
        // client.Send($"/action/{trackId}/unmute", 1);
        
        // Opción 3: Usando el sistema de acciones de REAPER
        // client.Send($"/action/40743", 1); // 40743 es el ID para "Track: Unmute tracks"
        
        Debug.Log($"Enviando unmute al track: {trackId}");
    }
    
    public void SendTrackMute(int trackId)
    {
        if (client == null) return;
        
        client.Send($"/track/{trackId}/mute", 1);
        Debug.Log($"Enviando mute al track: {trackId}");
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