using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using Windows.Kinect;
using AudioSource = UnityEngine.AudioSource;

public class InteractionSystem : MonoBehaviour
{
    public bool grabbing = false;
    [SerializeField] GameObject actualInstrument;
    [SerializeField] AudioSource actualInstrumentAudioSource;
    [SerializeField] InstrumentData actualInstrumentData;
    private Vector3 offset;
    private Mouse mouse;
    private Camera mainCamera;
    
    private float zDepth = 10f; // Profundidad más adecuada para cámara ortográfica
    
    // Variables para control por Kinect
    [Header("Kinect Controls")]
    [SerializeField] private bool useKinect = true;
    [SerializeField] private GameObject kinectCursor; // ASIGNA ESTE OBJETO EN EL INSPECTOR
    private bool wasFirePressed = false;
    private Vector3 kinectHandScreenPosition = Vector3.zero;
    
    // Rangos de calibración para Kinect (ajusta según necesites)
    [SerializeField] private float minX = -1f, maxX = 1f;
    [SerializeField] private float minY = 0.5f, maxY = 2f;

    private void Start()
    {
        mouse = Mouse.current;
        mainCamera = Camera.main;
        
        // Asegurar que el cursor de Kinect esté desactivado inicialmente
        if (kinectCursor != null)
            kinectCursor.SetActive(false);
    }
    
    void Update()
    {
        UpdateKinectHandPosition();
        
        if (WasGrabInputPressed())
        {
            TryGrabInstrument();
        }

        if (grabbing && actualInstrument != null)
        {
            MoveInstrument();
        }

        if (WasGrabInputReleased())
        {
            ReleaseInstrument();
        }
        
        if (useKinect && KinectManager.Instance != null && KinectManager.Instance.IsAvailable)
        {
            Vector3 inputPosition = GetInputPosition();
            inputPosition.z = 0f;
            Ray ray = mainCamera.ScreenPointToRay(mainCamera.WorldToScreenPoint(inputPosition));
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.green);
        }
    }

    private void UpdateKinectHandPosition()
    {
        if (!useKinect || KinectManager.Instance == null || !KinectManager.Instance.IsAvailable)
        {
            if (kinectCursor != null) 
                kinectCursor.SetActive(false);
            return;
        }

        Body[] bodies = KinectManager.Instance.GetBodies();
        if (bodies == null) return;

        var trackedBody = bodies.FirstOrDefault(b => b.IsTracked);
        if (trackedBody == null) 
        {
            if (kinectCursor != null) 
                kinectCursor.SetActive(false);
            return;
        }

        if (kinectCursor != null) 
            kinectCursor.SetActive(true);

        var handRight = trackedBody.Joints[JointType.HandRight].Position;
        
        // Mapeo mejorado
        float screenX = MapValue(handRight.X, minX, maxX, 0f, Screen.width);
        float screenY = MapValue(handRight.Y, minY, maxY, Screen.height, 0f); // Y invertido
        
        screenX = Mathf.Clamp(screenX, 0f, Screen.width);
        screenY = Mathf.Clamp(screenY, 0f, Screen.height);
        
        kinectHandScreenPosition = new Vector3(screenX, screenY, 10f);
        
        UpdateKinectCursor();
    }

    private void UpdateKinectCursor()
    {
        if (kinectCursor == null)
        {
            Debug.LogWarning("KinectCursor reference is null!");
            return;
        }
        
        // Convertir posición de pantalla a posición mundial
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(kinectHandScreenPosition);
        worldPos.z = 0f;
        Debug.Log($"World Position: {worldPos}");
        
        kinectCursor.transform.position = worldPos;
        
        // Actualizar el estado de agarre en el cursor visual
        KinectCursor cursorScript = kinectCursor.GetComponent<KinectCursor>();
        if (cursorScript != null)
        {
            cursorScript.SetGrabbingState(KinectManager.Instance.IsFire);
        }
    }

    private bool WasGrabInputPressed()
    {
        if (useKinect && KinectManager.Instance != null && KinectManager.Instance.IsAvailable)
        {
            bool isFirePressed = KinectManager.Instance.IsFire;
            bool pressed = isFirePressed && !wasFirePressed;
            wasFirePressed = isFirePressed;
            return pressed;
        }
        else
        {
            return mouse.leftButton.wasPressedThisFrame;
        }
    }

    private bool WasGrabInputReleased()
    {
        if (useKinect && KinectManager.Instance != null && KinectManager.Instance.IsAvailable)
        {
            bool isFirePressed = KinectManager.Instance.IsFire;
            bool released = !isFirePressed && wasFirePressed;
            wasFirePressed = isFirePressed;
            return released;
        }
        else
        {
            return mouse.leftButton.wasReleasedThisFrame;
        }
    }

    private Vector3 GetInputPosition()
    {
        if (useKinect && KinectManager.Instance != null && KinectManager.Instance.IsAvailable)
        {
            return mainCamera.ScreenToWorldPoint(kinectHandScreenPosition);
        }
        else
        {
            Vector3 mousePos = mouse.position.ReadValue();
            return mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, zDepth));
        }
    }

    private float MapValue(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
    }

    #region Instrument Interaction Methods

    private void TryGrabInstrument()
    {
        Vector3 inputPosition = GetInputPosition();
        
        // Para Kinect, usar ScreenPointToRay con las coordenadas de pantalla
        Ray ray = useKinect ? 
            mainCamera.ScreenPointToRay(kinectHandScreenPosition) :
            mainCamera.ScreenPointToRay(mouse.position.ReadValue());
            
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.CompareTag("Instrument"))
            {
                actualInstrument = hit.collider.gameObject;
                actualInstrumentAudioSource = actualInstrument.GetComponent<AudioSource>();
                
                Instrument instrumentComponent = actualInstrument.GetComponent<Instrument>();
                if (instrumentComponent != null)
                {
                    actualInstrumentData = instrumentComponent.instrumentData;
                }
                
                grabbing = true;

                // Establecer la profundidad Z
                zDepth = Mathf.Abs(mainCamera.transform.position.z - actualInstrument.transform.position.z);
                
                // Calcular offset
                Vector3 inputWorldPos = GetInputPosition();
                offset = actualInstrument.transform.position - inputWorldPos;

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
        Vector3 inputWorldPos = GetInputPosition();
        Vector3 targetPosition = inputWorldPos + offset;
        
        // Aplicar límites de pantalla
        targetPosition = ClampToScreenBounds(targetPosition);
        
        actualInstrument.transform.position = targetPosition;
    }

    private Vector3 ClampToScreenBounds(Vector3 position)
    {
        return ClampToViewport(position);
    }

    private Vector3 ClampToViewport(Vector3 position)
    {
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(position);
        
        viewportPos.x = Mathf.Clamp01(viewportPos.x);
        viewportPos.y = Mathf.Clamp01(viewportPos.y);
        
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

    // Método para debug - ver valores de Kinect en tiempo real
    private void OnGUI()
    {
        if (useKinect && KinectManager.Instance != null && KinectManager.Instance.IsAvailable)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Kinect Hand: {kinectHandScreenPosition}");
            GUI.Label(new Rect(10, 30, 300, 20), $"IsFire: {KinectManager.Instance.IsFire}");
        }
    }
}