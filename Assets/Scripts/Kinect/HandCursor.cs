using UnityEngine;
using UnityEngine.UI;

public class HandCursor : MonoBehaviour
{
    [Header("Cursor Visuals")]
    public Image cursorImage;
    public Sprite openHandSprite;
    public Sprite closedHandSprite;
    public Color[] playerColors = new Color[] { Color.blue, Color.green };
    
    [Header("Debug Settings")]
    public bool logMovement = true; // Activar/desactivar logs de movimiento
    public float movementLogThreshold = 5f; // Umbral mínimo de movimiento para log
    public float logInterval = 0.1f; // Intervalo mínimo entre logs
    
    private int cursorId;
    private KinectHandsManager manager;
    private Camera orthoCamera;
    private RectTransform rectTransform;
    private Vector2 canvasWorldSize;
    private Vector2 lastLoggedPosition;
    private float lastLogTime;
    private int playerIndex;
    private bool isLeftHand;
    
    // Estado del cursor
    public bool IsActive { get; private set; }
    public bool IsHandClosed { get; private set; }
    public bool IsGrabbing { get; private set; }
    public GameObject GrabbedInstrument { get; private set; }
    public Vector3? LastRaycastHit { get; private set; }
    
    public void Initialize(int id, KinectHandsManager kinectManager, Camera camera, Vector2 canvasSize)
    {
        cursorId = id;
        manager = kinectManager;
        orthoCamera = camera;
        canvasWorldSize = canvasSize;
        rectTransform = GetComponent<RectTransform>();
        playerIndex = id / 2;
        isLeftHand = (id % 2 == 0);
        
        if (playerIndex < playerColors.Length)
        {
            cursorImage.color = playerColors[playerIndex];
        }
        
        lastLoggedPosition = rectTransform.anchoredPosition;
        lastLogTime = Time.time;
        
        HideCursor();
        
        Debug.Log($"🖐️ Cursor inicializado: Player {playerIndex + 1} ({(isLeftHand ? "Izquierda" : "Derecha")}) - ID: {cursorId}");
    }
    
    public void UpdateCanvasPosition(Vector2 localCanvasPosition)
    {
        if (!IsActive) ShowCursor();
        
        // Guardar posición anterior para calcular movimiento
        Vector2 previousPosition = rectTransform.anchoredPosition;
        
        // Mover cursor en las coordenadas locales del Canvas World Space
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition, 
            localCanvasPosition, 
            Time.deltaTime * manager.cursorSmoothness
        );
        
        // Log del movimiento si cumple las condiciones
        LogMovement(previousPosition, rectTransform.anchoredPosition);
    }
    
    private void LogMovement(Vector2 fromPosition, Vector2 toPosition)
    {
        if (!logMovement) return;
        
        // Calcular distancia movida
        float distanceMoved = Vector2.Distance(fromPosition, toPosition);
        float timeSinceLastLog = Time.time - lastLogTime;
        
        // Solo loggear si supera el umbral y ha pasado el intervalo mínimo
        if (distanceMoved >= movementLogThreshold && timeSinceLastLog >= logInterval)
        {
            // Calcular velocidad
            float speed = distanceMoved / Time.deltaTime;
            
            string handType = isLeftHand ? "Izquierda" : "Derecha";
            string movementInfo = $"Moved {distanceMoved:F1} units at {speed:F1} u/s";
            
            Debug.Log($"🎯 Player {playerIndex + 1} - Mano {handType}: {movementInfo} | Position: ({toPosition.x:F1}, {toPosition.y:F1})");
            
            // Actualizar última posición y tiempo loggeados
            lastLoggedPosition = toPosition;
            lastLogTime = Time.time;
        }
    }
    
    public Vector3 GetWorldPosition()
    {
        Vector3 worldPos = rectTransform.position;
        
        // Log opcional de posición mundial (cada 2 segundos)
        if (logMovement && Time.time - lastLogTime >= 2f)
        {
            Debug.Log($"🌍 Player {playerIndex + 1} - World Position: {worldPos}");
            lastLogTime = Time.time;
        }
        
        return worldPos;
    }
    
    public void UpdateRaycastResult(bool hit, GameObject hitObject, Vector3 hitPoint)
    {
        Vector3? previousHit = LastRaycastHit;
        LastRaycastHit = hit ? hitPoint : (Vector3?)null;
        
        // Log del resultado del raycast
        if (hit)
        {
            if (previousHit == null || Vector3.Distance(previousHit.Value, hitPoint) > 1f)
            {
                string handType = isLeftHand ? "Izquierda" : "Derecha";
                string objectName = hitObject != null ? hitObject.name : "Unknown";
                Debug.Log($"🎯 Player {playerIndex + 1} - Mano {handType}: Raycast HIT {objectName} at {hitPoint}");
            }
        }
        else if (previousHit != null)
        {
            string handType = isLeftHand ? "Izquierda" : "Derecha";
            Debug.Log($"🎯 Player {playerIndex + 1} - Mano {handType}: Raycast MISS");
        }
        
        // Feedback visual
        if (hit && hitObject.CompareTag("Instrument"))
        {
            cursorImage.color = Color.yellow;
        }
        else
        {
            if (playerIndex < playerColors.Length)
            {
                cursorImage.color = playerColors[playerIndex];
            }
        }
    }
    
    public void SetHandState(bool handClosed)
    {
        // Log del cambio de estado de la mano
        if (IsHandClosed != handClosed)
        {
            string handType = isLeftHand ? "Izquierda" : "Derecha";
            string state = handClosed ? "CERRADA 👊" : "ABIERTA 🖐️";
            Debug.Log($"✋ Player {playerIndex + 1} - Mano {handType}: {state}");
        }
        
        IsHandClosed = handClosed;
        cursorImage.sprite = handClosed ? closedHandSprite : openHandSprite;
        
        // Feedback visual adicional
        cursorImage.transform.localScale = handClosed ? Vector3.one * 1.2f : Vector3.one;
    }
    
    public void GrabInstrument(GameObject instrument)
    {
        if (IsGrabbing || instrument == null) return;
        
        IsGrabbing = true;
        GrabbedInstrument = instrument;
        cursorImage.color = Color.red;
        
        string handType = isLeftHand ? "Izquierda" : "Derecha";
        Debug.Log($"🎮 Player {playerIndex + 1} - Mano {handType}: AGARRANDO instrumento '{instrument.name}'");
    }
    
    public void ReleaseInstrument()
    {
        if (!IsGrabbing) return;
        
        string instrumentName = GrabbedInstrument != null ? GrabbedInstrument.name : "Unknown";
        string handType = isLeftHand ? "Izquierda" : "Derecha";
        
        IsGrabbing = false;
        int playerIndex = cursorId / 2;
        if (playerIndex < playerColors.Length)
        {
            cursorImage.color = playerColors[playerIndex];
        }
        
        Debug.Log($"🎮 Player {playerIndex + 1} - Mano {handType}: SOLTANDO instrumento '{instrumentName}'");
        
        GrabbedInstrument = null;
        LastRaycastHit = null;
    }
    
    public void ShowCursor()
    {
        if (!IsActive)
        {
            IsActive = true;
            cursorImage.enabled = true;
            
            string handType = isLeftHand ? "Izquierda" : "Derecha";
            Debug.Log($"👁️ Player {playerIndex + 1} - Mano {handType}: Cursor ACTIVADO");
        }
    }
    
    public void HideCursor()
    {
        if (IsActive)
        {
            IsActive = false;
            cursorImage.enabled = false;
            
            string handType = isLeftHand ? "Izquierda" : "Derecha";
            Debug.Log($"👁️ Player {playerIndex + 1} - Mano {handType}: Cursor DESACTIVADO");
            
            if (IsGrabbing) 
            {
                ReleaseInstrument();
            }
        }
    }
    
    // Método para información de diagnóstico
    public void PrintDiagnosticInfo()
    {
        string handType = isLeftHand ? "Izquierda" : "Derecha";
        string grabbingInfo = GrabbedInstrument != null ? $"grabing '{GrabbedInstrument.name}'" : "not grabbing";
        
        Debug.Log($"📊 Player {playerIndex + 1} - Mano {handType}: " +
                 $"Active: {IsActive}, HandClosed: {IsHandClosed}, {grabbingInfo}, " +
                 $"Position: {rectTransform.anchoredPosition}");
    }
    
    // Llamar este método para forzar un log del estado actual
    public void LogCurrentState()
    {
        string handType = isLeftHand ? "Izquierda" : "Derecha";
        Debug.Log($"📝 Player {playerIndex + 1} - Mano {handType} - " +
                 $"Pos: {rectTransform.anchoredPosition}, " +
                 $"Closed: {IsHandClosed}, Grabbing: {IsGrabbing}");
    }
}