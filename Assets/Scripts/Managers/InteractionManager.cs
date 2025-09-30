using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    [SerializeField] InstrumentData actualInstrumentData;
    [SerializeField] private Camera orthoCamera;
    
    private Instrument draggedInstrument;
    private Vector3 grabOffset;
    private bool isDragging = false;
    private Mouse mouse;
    
    private void Start()
    {
        if (orthoCamera == null)
            orthoCamera = Camera.main;
            
        mouse = Mouse.current;
    }

    private void Update()
    {
        HandleMouseInput();
        
        if (isDragging)
            UpdateDraggedPosition();
    }

    #region Mouse Input
    
    private void HandleMouseInput()
    {
        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryStartDrag();
        }
        
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            EndDrag();
        }
    }
    
    private void TryStartDrag()
    {
        RaycastHit hit;
        Ray ray = orthoCamera.ScreenPointToRay(mouse.position.ReadValue());
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            if (hit.collider.CompareTag("Instrument"))
            {
                Instrument instrument = hit.collider.GetComponent<Instrument>();
                if (instrument != null && instrument.instrumentData != null)
                {
                    StartDrag(instrument, hit.point);
                }
            }
        }
    }
    
    private void StartDrag(Instrument instrument, Vector3 hitPoint)
    {
        draggedInstrument = instrument;
        actualInstrumentData = instrument.instrumentData;
        isDragging = true;
        
        grabOffset = draggedInstrument.transform.position - hitPoint;
        grabOffset.z = 0;
        
        Debug.Log($"Comenzando drag de: {actualInstrumentData.instrumentType}");
    }
    
    private void UpdateDraggedPosition()
    {
        if (draggedInstrument == null) return;
        
        // Convertir posición del mouse a mundo en la cámara ortográfica
        Vector2 mousePos = mouse.position.ReadValue();
        Vector3 mousePosition = new Vector3(mousePos.x, mousePos.y, orthoCamera.nearClipPlane + 1f);
        
        Vector3 worldPosition = orthoCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = draggedInstrument.transform.position.z; // Mantener Z original
        
        // Aplicar posición con offset
        draggedInstrument.transform.position = worldPosition + grabOffset;
    }
    
    private void EndDrag()
    {
        if (!isDragging) return;
        
        Debug.Log($"Finalizando drag de: {actualInstrumentData?.instrumentType}");
        
        draggedInstrument = null;
        isDragging = false;
    }
    
    public bool IsDraggingInstrument()
    {
        return isDragging;
    }
    
    public Instrument GetDraggedInstrument()
    {
        return draggedInstrument;
    }
    
    private void OnDrawGizmos()
    {
        if (isDragging && draggedInstrument != null && mouse != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(draggedInstrument.transform.position, 0.5f);
            
            // Dibujar línea desde el centro hasta la posición del mouse
            Vector2 mousePos = mouse.position.ReadValue();
            Vector3 mousePosition = new Vector3(mousePos.x, mousePos.y, orthoCamera.nearClipPlane + 1f);
            Vector3 worldPosition = orthoCamera.ScreenToWorldPoint(mousePosition);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(draggedInstrument.transform.position, worldPosition);
        }
    }
    
    #endregion
}