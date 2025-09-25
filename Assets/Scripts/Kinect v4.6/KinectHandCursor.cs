using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Windows.Kinect;
using TMPro;

public class KinectHandCursor : MonoBehaviour
{
    [Header("Kinect Settings")]
    public bool useRightHand = true;
    public float smoothFactor = 8f;
    
    [Header("UI References")]
    public RectTransform handCursor;
    public Camera uiCamera;
    public TextMeshProUGUI debugText;

    // Kinect variables
    private KinectSensor _sensor;
    private BodyFrameReader _bodyReader;
    private Body[] _bodies;
    private HandState _lastHandState = HandState.Unknown;
    private bool _isKinectInitialized = false;

    void Start()
    {
        Debug.Log("Iniciando KinectHandCursor...");
        InitializeKinect();
    }

    void Update()
    {
        if (_isKinectInitialized && _sensor != null && _sensor.IsOpen)
        {
            UpdateBodyData();
        }
    }

    private void InitializeKinect()
    {
        try
        {
            _sensor = KinectSensor.GetDefault();
            
            if (_sensor == null)
            {
                Debug.LogError("No se pudo encontrar el sensor Kinect");
                UpdateDebugText("ERROR: Kinect no encontrada");
                return;
            }

            if (!_sensor.IsOpen)
            {
                _sensor.Open();
                Debug.Log("Kinect abierta correctamente");
            }

            _bodyReader = _sensor.BodyFrameSource.OpenReader();
            _bodies = new Body[_sensor.BodyFrameSource.BodyCount];
            
            _isKinectInitialized = true;
            UpdateDebugText("Kinect inicializada - Esperando cuerpo...");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error inicializando Kinect: {e.Message}");
            UpdateDebugText($"ERROR: {e.Message}");
        }
    }

    private void UpdateBodyData()
    {
        if (_bodyReader == null) return;

        try
        {
            var frame = _bodyReader.AcquireLatestFrame();
            if (frame != null)
            {
                frame.GetAndRefreshBodyData(_bodies);
                ProcessTrackedBodies();
                frame.Dispose();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error en frame: {e.Message}");
        }
    }

    private void ProcessTrackedBodies()
    {
        bool bodyFound = false;
        
        if (_bodies == null) return;

        for (int i = 0; i < _bodies.Length; i++)
        {
            if (_bodies[i] != null && _bodies[i].IsTracked)
            {
                UpdateCursorPosition(_bodies[i]);
                CheckHandGesture(_bodies[i]);
                bodyFound = true;
                break;
            }
        }

        if (!bodyFound)
        {
            UpdateDebugText("Kinect activa - Mueve tus manos frente al sensor");
        }
    }

    private void UpdateCursorPosition(Body body)
    {
        if (body == null || handCursor == null) return;

        JointType handType = useRightHand ? JointType.HandRight : JointType.HandLeft;
        var handJoint = body.Joints[handType];
        
        if (handJoint.TrackingState == TrackingState.Tracked)
        {
            Vector2 screenPos = GetHandScreenPosition(handJoint);
            SetCursorPosition(screenPos);
            
            UpdateDebugText($"Mano detectada - Posición: {screenPos}");
        }
        else
        {
            UpdateDebugText("Mano no trackeada - Asegúrate de estar frente al sensor");
        }
    }

    private Vector2 GetHandScreenPosition(Windows.Kinect.Joint handJoint)
    {
        try
        {
            ColorSpacePoint colorSpacePoint = _sensor.CoordinateMapper.MapCameraPointToColorSpace(handJoint.Position);
            
            // Ajustar coordenadas (puede necesitar calibración)
            float x = Mathf.Clamp(colorSpacePoint.X / 1920f, 0f, 1f);
            float y = Mathf.Clamp(colorSpacePoint.Y / 1080f, 0f, 1f);
            
            Vector2 screenPos = new Vector2(x * Screen.width, (1 - y) * Screen.height);
            return screenPos;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error en mapeo de coordenadas: {e.Message}");
            return Vector2.zero;
        }
    }

    private void SetCursorPosition(Vector2 screenPosition)
    {
        if (handCursor == null || uiCamera == null)
        {
            Debug.LogWarning("Referencias faltantes: handCursor o uiCamera");
            return;
        }

        try
        {
            Vector2 localPoint;
            RectTransform parentCanvas = handCursor.parent as RectTransform;
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas, screenPosition, uiCamera, out localPoint))
            {
                handCursor.anchoredPosition = Vector2.Lerp(
                    handCursor.anchoredPosition, 
                    localPoint, 
                    Time.deltaTime * smoothFactor);
            }
            else
            {
                Debug.LogWarning("No se pudo convertir coordenadas de pantalla");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error moviendo cursor: {e.Message}");
        }
    }

    private void UpdateDebugText(string message)
    {
        if (debugText != null)
        {
            debugText.text = message;
        }
    }
    
    private void CheckHandGesture(Body body)
    {
        HandState currentHandState = useRightHand ? body.HandRightState : body.HandLeftState;
        
        if (_lastHandState != HandState.Closed && currentHandState == HandState.Closed)
        {
            DetectUIClick();
        }
        
        _lastHandState = currentHandState;
    }
    
    private void DetectUIClick()
    {
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, handCursor.position);
        
        var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        pointerData.position = screenPos;
        
        var results = new List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);
        
        if (results.Count > 0)
        {
            GameObject clickedObject = results[0].gameObject;
            Button button = clickedObject.GetComponent<Button>();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
        }
    }
    
    void OnDestroy()
    {
        if (_bodyReader != null)
        {
            _bodyReader.Dispose();
            _bodyReader = null;
        }
        
        if (_sensor != null && _sensor.IsOpen)
        {
            _sensor.Close();
        }
    }
}