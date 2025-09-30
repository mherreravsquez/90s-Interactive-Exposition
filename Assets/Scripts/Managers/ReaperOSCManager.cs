using UnityEngine;
using OscJack;

public class ReaperOSCManager : MonoBehaviour
{
    [Tooltip("Port number for OSC communication with Reaper")]
    public int reaperPort = 8888;
    
    private OscClient client;
    private bool isReaperPlaying = false;
    
    void Start()
    {
        SetupOSC();
        
        StopReaperPlayback();
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
        
        client.Send($"/track/{trackId}/mute", 0);
        Debug.Log($"Enviando unmute al track: {trackId}");
    }
    
    public void SendTrackMute(int trackId)
    {
        if (client == null) return;
        
        client.Send($"/track/{trackId}/mute", 1);
        Debug.Log($"Enviando mute al track: {trackId}");
    }
    
    public void StartReaperPlayback()
    {
        if (client == null) return;
        
        if (!isReaperPlaying)
        {
            // Comando para iniciar reproducción en REAPER
            client.Send("/play", 1);
            isReaperPlaying = true;
            Debug.Log("Iniciando reproducción en REAPER");
        }
    }
    
    public void StopReaperPlayback()
    {
        if (client == null) return;
        
        if (isReaperPlaying)
        {
            // Comando para detener reproducción en REAPER
            client.Send("/stop", 1);
            isReaperPlaying = false;
            Debug.Log("Deteniendo reproducción en REAPER");
            
            // Opcional: Reiniciar posición de reproducción
            client.Send("/rewind", 1);
        }
    }
    
    public void ToggleReaperPlayback()
    {
        if (client == null) return;
        
        client.Send("/play", isReaperPlaying ? 0 : 1);
        isReaperPlaying = !isReaperPlaying;
        Debug.Log($"Alternando reproducción: {isReaperPlaying}");
    }
    
    void OnApplicationQuit()
    {
        Debug.Log("Cerrando aplicación - Deteniendo REAPER");
        StopReaperPlayback();
        
        // Mutear todos los tracks por seguridad
        for (int i = 0; i < 10; i++) // Ajusta el rango según tus tracks
        {
            SendTrackMute(i);
        }
        
        if (client != null)
            client.Dispose();
    }
    
    void OnDestroy()
    {
        Debug.Log("Destruyendo OSC Manager - Deteniendo REAPER");
        StopReaperPlayback();
        
        if (client != null)
            client.Dispose();
    }
    
    #if UNITY_EDITOR
    void OnDisable()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Editor detenido - Deteniendo REAPER");
            StopReaperPlayback();
        }
    }
    #endif
}