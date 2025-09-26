using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;
using TMPro;

public class KinectHandCursor : MonoBehaviour
{
    [Header("Settings")]
    public bool useRightHand = true;
    public float smoothFactor = 10f;
    
    [Header("References")]
    public RectTransform handCursor;
    public Camera uiCamera;
    public TextMeshProUGUI debugText;

    private BodySourceManager _bodyManager;
    private KinectSensor _sensor;

    void Start()
    {
        Debug.Log("Iniciando KinectHandCursor...");
        
        // Buscar o crear BodySourceManager
        _bodyManager = FindObjectOfType<BodySourceManager>();
        if (_bodyManager == null)
        {
            GameObject managerObj = new GameObject("BodySourceManager");
            _bodyManager = managerObj.AddComponent<BodySourceManager>();
        }

        _sensor = KinectSensor.GetDefault();
        
        UpdateDebugText("Sistema iniciado - Esperando datos del cuerpo...");
    }

    void Update()
    {
        if (_bodyManager == null || _sensor == null || !_sensor.IsOpen) return;

        try
        {
            Body[] bodies = _bodyManager.GetData();
            if (bodies == null) return;

            foreach (var body in bodies)
            {
                if (body != null && body.IsTracked)
                {
                    UpdateCursorWithBody(body);
                    return; // Solo procesar el primer cuerpo
                }
            }
            
            // Si no hay cuerpos trackeados
            UpdateDebugText("Por favor, párate frente al sensor Kinect");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error en Update: {e.Message}");
        }
    }

    private void UpdateCursorWithBody(Body body)
    {
        if (handCursor == null) return;

        JointType handType = useRightHand ? JointType.HandRight : JointType.HandLeft;
        var handJoint = body.Joints[handType];
        
        if (handJoint.TrackingState == TrackingState.Tracked)
        {
            Vector2 screenPos = MapToScreenPoint(handJoint.Position);

            // O AQUÍ
             
            SetCursorPosition(screenPos);
            
            // Debug info
            string handState = useRightHand ? 
                body.HandRightState.ToString() : body.HandLeftState.ToString();
            UpdateDebugText($"Mano detectada | Estado: {handState} | Pos: {screenPos}");
        }
        else
        {
            UpdateDebugText("Mano encontrada pero no trackeada correctamente");
        }
    }

    private Vector2 MapToScreenPoint(Windows.Kinect.CameraSpacePoint position)
    {
        if (_sensor == null) return Vector2.zero;

        try
        {
            // Mapear directamente a coordenadas de pantalla
            ColorSpacePoint colorPoint = _sensor.CoordinateMapper.MapCameraPointToColorSpace(position);
            
            // Ajustar a resolución de pantalla
            float x = (colorPoint.X / 1920f) * Screen.width;
            float y = (1 - (colorPoint.Y / 1920f)) * Screen.height;
            
            return new Vector2(x, y);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error en mapeo: {e.Message}");
            return Vector2.zero;
        }
    }

    private void SetCursorPosition(Vector2 screenPosition)
    {
         // EL ERROR ESTÁ AQUÍ

        if (handCursor == null || uiCamera == null) return;

        try
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)handCursor.parent,
                screenPosition,
                uiCamera,
                out localPoint
            );
            
            // Aplicar con suavizado
            handCursor.anchoredPosition = Vector2.Lerp(
                handCursor.anchoredPosition, 
                localPoint, 
                Time.deltaTime * smoothFactor
            );
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error posicionando cursor: {e.Message}");
        }
    }

    private void UpdateDebugText(string message)
    {
        if (debugText != null)
        {
            debugText.text = message;
        }
        Debug.Log(message);
    }
}