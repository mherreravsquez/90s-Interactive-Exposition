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
    
    public void MuteAllTracks()
    {
        if (client == null) return;
        
        // Mutear tracks del 1 al 12 (ajusta según tus necesidades)
        for (int i = 0; i < 13; i++)
        {
            SendTrackMute(i);
        }
        
        Debug.Log("Todos los tracks han sido muteados");
    }
    
    public void StartReaperPlayback()
    {
        if (client == null) return;
        
        if (!isReaperPlaying)
        {
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
            client.Send("/stop", 1);
            isReaperPlaying = false;
            Debug.Log("Deteniendo reproducción en REAPER");
            client.Send("/rewind", 1);
        }
    }
    
    // Método para acciones especiales en REAPER
    public void SendSpecialAction()
    {
        if (client == null) return;
        
        // EJEMPLO: Cambiar a un patrón especial o efecto
        // client.Send("/special/pattern", 1);
        
        // EJEMPLO: Activar filtro especial
        // client.Send("/filter/lowpass", 500); // Frecuencia de corte
        
        Debug.Log("Enviando acción especial a REAPER");
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
        MuteAllTracks();
        
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