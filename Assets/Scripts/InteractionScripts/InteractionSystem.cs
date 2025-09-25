using UnityEngine;
using UnityEngine.InputSystem;
using OscJack;

public class InteractionSystem : MonoBehaviour
{
    public bool grabbing = false;
    [SerializeField] GameObject actualInstrument;
    [SerializeField] AudioSource actualInstrumentAudioSource;
    [SerializeField] InstrumentData actualInstrumentData;
    private Vector3 offset;
    private Mouse mouse;
    private Camera mainCamera;
    
    private OscServer oscServer;
    private float zDepth = 0f; // Profundidad Z fija para el movimiento

    private void Start()
    {
        mouse = Mouse.current;
        mainCamera = Camera.main;
        
        // Server Initialization
        oscServer = new OscServer(8888);
        oscServer.MessageDispatcher.AddCallback("/touchpad", OnOSCTouchpadMessage);
    }
    
    private void OnOSCTouchpadMessage(string address, OscDataHandle data)
    {
        float x = data.GetElementAsFloat(0);
        float y = data.GetElementAsFloat(1);

        // Convertir coordenadas normalizadas a posición en pantalla
        Vector2 screenPos = new Vector2(x * Screen.width, y * Screen.height);
        
        // Convertir a posición 3D en el mundo con límites de pantalla
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDepth));
        worldPos = ClampToScreenBounds(worldPos);

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
        Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Instrument"))
            {
                actualInstrument = hit.collider.gameObject;
                actualInstrumentAudioSource = actualInstrument.GetComponent<AudioSource>();
                actualInstrumentData = actualInstrument.GetComponent<Instrument>().instrumentData; 
                grabbing = true;

                // Establecer la profundidad Z basada en la posición actual del instrumento
                zDepth = Mathf.Abs(mainCamera.transform.position.z - actualInstrument.transform.position.z);
                
                // Calcular offset
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(
                    new Vector3(mouse.position.ReadValue().x, mouse.position.ReadValue().y, zDepth));
                offset = actualInstrument.transform.position - mouseWorldPos;

                // Reproducir sonido
                if (actualInstrumentAudioSource != null && actualInstrumentData != null)
                {
                    actualInstrumentAudioSource.clip = actualInstrumentData.previewClip;
                    actualInstrumentAudioSource.Play();
                }
            }
        }
    }

    private void MoveInstrument()
    {
        Vector3 mouseScreenPos = new Vector3(mouse.position.ReadValue().x, mouse.position.ReadValue().y, zDepth);
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        Vector3 targetPosition = mouseWorldPos + offset;
        
        // Aplicar límites de pantalla
        targetPosition = ClampToScreenBounds(targetPosition);
        
        actualInstrument.transform.position = targetPosition;
    }

    private Vector3 ClampToScreenBounds(Vector3 position)
    {
        // Convertir posición mundial a posición de pantalla
        Vector3 screenPos = mainCamera.WorldToScreenPoint(position);
        
        // Calcular los bordes de la pantalla en coordenadas mundiales
        float orthoSize = mainCamera.orthographicSize;
        float aspectRatio = (float)Screen.width / Screen.height;
        float cameraWidth = orthoSize * aspectRatio;
        
        Vector3 cameraPos = mainCamera.transform.position;
        
        // Límites en coordenadas mundiales
        float leftBound = cameraPos.x - cameraWidth;
        float rightBound = cameraPos.x + cameraWidth;
        float bottomBound = cameraPos.y - orthoSize;
        float topBound = cameraPos.y + orthoSize;
        
        // Aplicar límites
        position.x = Mathf.Clamp(position.x, leftBound, rightBound);
        position.y = Mathf.Clamp(position.y, bottomBound, topBound);
        position.z = Mathf.Clamp(position.z, cameraPos.z - 10f, cameraPos.z + 10f); // Pequeño margen en Z
        
        return position;
    }

    // Método alternativo usando Viewport (puede ser más preciso)
    private Vector3 ClampToViewport(Vector3 position)
    {
        // Convertir a coordenadas de viewport (0-1)
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(position);
        
        // Aplicar límites en viewport
        viewportPos.x = Mathf.Clamp01(viewportPos.x);
        viewportPos.y = Mathf.Clamp01(viewportPos.y);
        
        // Convertir de vuelta a coordenadas mundiales
        return mainCamera.ViewportToWorldPoint(viewportPos);
    }

    private void ReleaseInstrument()
    {
        grabbing = false;
        
        if (actualInstrumentAudioSource != null)
        {
            actualInstrumentAudioSource.Stop();
        }
        
        actualInstrument = null;
        actualInstrumentAudioSource = null;
        actualInstrumentData = null;
    }
    
    #endregion

    private void OnDestroy()
    {
        oscServer?.Dispose();
    }

    // Método opcional para debug - visualizar los límites en el Editor
    private void OnDrawGizmos()
    {
        if (mainCamera != null && mainCamera.orthographic)
        {
            Gizmos.color = Color.red;
            float orthoSize = mainCamera.orthographicSize;
            float aspect = mainCamera.aspect;
            Vector3 cameraPos = mainCamera.transform.position;
            
            Vector3 size = new Vector3(orthoSize * aspect * 2, orthoSize * 2, 0.1f);
            Gizmos.DrawWireCube(cameraPos, size);
        }
    }
}