using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using OscJack;

public class InteractionSystem : MonoBehaviour
{
    public bool grabbing = false;
    [SerializeField] GameObject actualInstrument;
    private Vector2 offset;
    private Mouse mouse;
    private float originalZPos;
    private RectTransform canvasRect;
    private Canvas canvas;
    
    private OscServer oscServer;

    private void Start()
    {
        mouse = Mouse.current;
        
        // Encontrar el Canvas en la escena
        canvas = FindObjectOfType<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogError("No se encontró un Canvas con render mode World Space");
        }
        
        // Server Initialization
        oscServer = new OscServer(8888);
        oscServer.MessageDispatcher.AddCallback("/touchpad", OnOSCTouchpadMessage);
    }
    
    private void OnOSCTouchpadMessage(string address, OscDataHandle data)
    {
        float x = data.GetElementAsFloat(0);
        float y = data.GetElementAsFloat(1);

        Vector2 localPos = new Vector2(
            x * canvasRect.sizeDelta.x - canvasRect.sizeDelta.x / 2,
            y * canvasRect.sizeDelta.y - canvasRect.sizeDelta.y / 2
        );
    
        Vector3 worldPos = canvasRect.TransformPoint(localPos);
        worldPos.z = originalZPos;

        if (grabbing && actualInstrument != null)
        {
            actualInstrument.transform.position = worldPos;
        }
    }

    void Update()
    {
        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryGrabInstrument();
        }

        if (grabbing && actualInstrument != null)
        {
            MoveInstrument();
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            ReleaseInstrument();
        }
    }

    #region Instrument Interaction Methods

    private void TryGrabInstrument()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = mouse.position.ReadValue()
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                if (result.gameObject.CompareTag("Instrument"))
                {
                    actualInstrument = result.gameObject;
                    grabbing = true;
                    originalZPos = actualInstrument.transform.position.z;
                    
                    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(
                        new Vector3(mouse.position.ReadValue().x, mouse.position.ReadValue().y, 
                        canvas.planeDistance));
                    offset = actualInstrument.transform.position - mouseWorldPos;
                    break;
                }
            }
        }
    }

    private void MoveInstrument()
    {
        // Convertir posición del mouse a coordenadas en el plano del Canvas
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(mouse.position.ReadValue().x, mouse.position.ReadValue().y, 
            canvas.planeDistance));
        
        Vector3 targetPosition = mouseWorldPos + (Vector3)offset;
        targetPosition.z = originalZPos;
        
        // Aplicar límites al movimiento
        targetPosition = ClampPositionToCanvas(targetPosition);
        
        actualInstrument.transform.position = targetPosition;
    }

    private Vector3 ClampPositionToCanvas(Vector3 targetPosition)
    {
        if (canvasRect == null) return targetPosition;
        
        // Convertir la posición mundial a posición local del Canvas
        Vector2 localPosition = canvasRect.InverseTransformPoint(targetPosition);
        
        // Obtener los límites del Canvas (asumiendo pivote central)
        Vector2 canvasHalfSize = canvasRect.sizeDelta / 2f;
        
        // Aplicar límites
        localPosition.x = Mathf.Clamp(localPosition.x, -canvasHalfSize.x, canvasHalfSize.x);
        localPosition.y = Mathf.Clamp(localPosition.y, -canvasHalfSize.y, canvasHalfSize.y);
        
        // Convertir de vuelta a posición mundial
        return canvasRect.TransformPoint(localPosition);
    }

    private void ReleaseInstrument()
    {
        grabbing = false;
        actualInstrument = null;
    }
    
    #endregion
    
}