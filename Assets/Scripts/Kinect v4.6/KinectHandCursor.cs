using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;
using TMPro;
using Joint = Windows.Kinect.Joint;

public class KinectHandCursor : MonoBehaviour
{
    [Header("Settings")]
    [Space(5)]
    public bool useRightHand = true;
    public float smoothFactor = 10f;
    [Space(15)]
    
    [Header("References")]
    [Space(5)]
    public RectTransform cursor;
    public Camera sceneCamera;
    public TextMeshProUGUI debugText;
    [Space(5)]
    public GameObject grabbedInstrument;
    [Space(15)]
    
    [Header("Hand Sprites")]
    [Space(5)]
    public Sprite[] handSprites;
    [Space(15)]

    [Header("Grab Settings")]
    [Space(5)]
    public float grabDistance = 5f;
    public LayerMask instrumentLayer;
    [Space(15)]
    
    [Header("Screen Boundaries")]
    [Space(5)]
    public float borderMargin = 0.02f;
    
    // Hand state tracking
    private HandState _previousHandState = HandState.Unknown;
    private bool _handClosedThisFrame = false;
    
    // Grab system variables
    private GameObject _grabbedObject;
    private Vector3 _grabOffset;
    private bool _isGrabbing = false;

    private BodySourceManager _bodyManager;
    private KinectSensor _kinect;
    private Vector2 _previousScreenPosition;

    void Start()
    {
        _bodyManager = BodySourceManager.instance;
        _kinect = KinectSensor.GetDefault();
        
        UpdateDebugText("System ready. Move your hands.");

        if (sceneCamera == null)
            sceneCamera = Camera.main;
    }

    void Update()
    {
        if (_bodyManager == null || _kinect == null || !_kinect.IsOpen) return;
        
        _handClosedThisFrame = false;

        Body[] bodies = _bodyManager.GetData();
        if (bodies == null) return;

        foreach (Body body in bodies)
        {
            if (body != null && body.IsTracked)
            {
                MoveCursor(body);
                DetectHandGestures(body);
                UpdateGrabSystem();
                UpdateCursorAppearance();
                return;
            }
        }
        
        UpdateDebugText("Please stand in front of Kinect sensor");
    }

    private void MoveCursor(Body body)
    {
        if (cursor == null) return;

        JointType handType = useRightHand ? JointType.HandRight : JointType.HandLeft;
        Joint hand = body.Joints[handType];
        
        if (hand.TrackingState == TrackingState.Tracked)
        {
            Vector2 screenPosition = ConvertToScreen(hand.Position);
            
            MoveCursorUI(screenPosition);
            
            string handState = useRightHand ? body.HandRightState.ToString() : body.HandLeftState.ToString();
            string grabStatus = _isGrabbing ? $"Grabbing: {_grabbedObject.name}" : "Ready to grab";
            UpdateDebugText($"Hand: {handState} | {grabStatus}");
        }
    }
    
    private void DetectHandGestures(Body body)
    {
        HandState currentHandState = useRightHand ? body.HandRightState : body.HandLeftState;

        // Detect when hand CLOSES (grab)
        if (_previousHandState != HandState.Closed && currentHandState == HandState.Closed)
        {
            _handClosedThisFrame = true;
            if (!_isGrabbing)
                TryGrabInstrument();
        }
        
        // Detect when hand OPENS (release)
        if (_isGrabbing && _previousHandState == HandState.Closed && currentHandState != HandState.Closed)
        {
            ReleaseInstrument();
        }

        _previousHandState = currentHandState;
    }

    #region Interactions
    
    private void TryGrabInstrument()
    {
        if (_isGrabbing) return;

        Vector3 cursorWorldPos = cursor.position;
        Vector3 rayDirection = sceneCamera.transform.forward;

        RaycastHit[] hits = Physics.SphereCastAll(
            cursorWorldPos, 
            0.3f, // Radio mayor para mejor detección
            rayDirection, 
            grabDistance, 
            instrumentLayer
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Instrument"))
            {
                grabbedInstrument = hit.collider.gameObject;
                GrabInstrument(hit.collider.gameObject, hit.point);
                break;
            }
        }
    }

    private void GrabInstrument(GameObject instrument, Vector3 grabPoint)
    {
        _grabbedObject = instrument;
        _isGrabbing = true;
        
        // Calculate offset only in X and Y axes (ignore Z)
        Vector3 instrumentPosition = instrument.transform.position;
        Vector3 cursorPosition = cursor.position;
        _grabOffset = new Vector3(
            instrumentPosition.x - cursorPosition.x,
            instrumentPosition.y - cursorPosition.y,
            0f
        );
        
        // Debug.Log($"Grabbed instrument: {instrument.name}");
    }

    private void ReleaseInstrument()
    {
        if (!_isGrabbing) return;
        
        // Debug.Log($"Released instrument: {_grabbedObject.name}");
        _grabbedObject = null;
        _isGrabbing = false;

        grabbedInstrument = null;
    }

    #endregion
    
    private void UpdateGrabSystem()
    {
        if (_isGrabbing && _grabbedObject != null)
        {
            // Calculate the target position
            Vector3 targetPosition = new Vector3(
                cursor.position.x + _grabOffset.x,
                cursor.position.y + _grabOffset.y,
                _grabbedObject.transform.position.z
            );
            
            // Move instrument
            _grabbedObject.transform.position = targetPosition;
        }
    }
    
    private void UpdateCursorAppearance()
    {
        if (cursor == null || handSprites == null || handSprites.Length < 2) return;

        var image = cursor.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.sprite = _isGrabbing ? handSprites[1] : handSprites[0];
        }
    }
    
    private Vector2 ConvertToScreen(Windows.Kinect.CameraSpacePoint position3D)
    {
        if (_kinect == null) return Vector2.zero;

        try
        {
            ColorSpacePoint colorPoint = _kinect.CoordinateMapper.MapCameraPointToColorSpace(position3D);
            
            // Square resolution
            float squareSize = Mathf.Min(Screen.width, Screen.height);
            float xOffset = (Screen.width - squareSize) / 2f;
            float yOffset = (Screen.height - squareSize) / 2f;
            
            // Map to square area
            float normalizedX = Mathf.Clamp((colorPoint.X - 960f) / 1080f + 0.5f, 0f, 1f);
            float normalizedY = Mathf.Clamp((colorPoint.Y - 540f) / 1080f + 0.5f, 0f, 1f);
            
            float screenX = xOffset + normalizedX * squareSize;
            float screenY = yOffset + (1 - normalizedY) * squareSize;
            
            return new Vector2(screenX, screenY);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Coordinate conversion error: {e.Message}");
            return Vector2.zero;
        }
    }

    private void MoveCursorUI(Vector2 screenPosition)
    {
        if (cursor == null || sceneCamera == null) return;

        // Clamp screen position to camera viewport
        Vector2 clampedScreenPosition = ClampToCameraViewport(screenPosition);

        // For square resolution, ensure cursor stays in visible area
        Vector3 viewportPoint = sceneCamera.ScreenToViewportPoint(
            new Vector3(clampedScreenPosition.x, clampedScreenPosition.y, 0f)
        );

        // Convert to world position
        Vector3 worldPosition = sceneCamera.ViewportToWorldPoint(
            new Vector3(viewportPoint.x, viewportPoint.y, 10f)
        );
        
        // Lock Z position to 0
        Vector3 fixedPosition = new Vector3(worldPosition.x, worldPosition.y, 0f);

        // Smooth movement
        cursor.position = Vector3.Lerp(
            cursor.position, 
            fixedPosition, 
            Time.deltaTime * smoothFactor
        );
    }

    private Vector2 ClampToCameraViewport(Vector2 screenPosition)
    {
        Vector3 viewportPoint = sceneCamera.ScreenToViewportPoint(
            new Vector3(screenPosition.x, screenPosition.y, 0f)
        );

        viewportPoint.x = Mathf.Clamp(viewportPoint.x, borderMargin, 1f - borderMargin);
        viewportPoint.y = Mathf.Clamp(viewportPoint.y, borderMargin, 1f - borderMargin);

        Vector3 clampedScreenPoint = sceneCamera.ViewportToScreenPoint(viewportPoint);
        return new Vector2(clampedScreenPoint.x, clampedScreenPoint.y);
    }

    private void UpdateDebugText(string message)
    {
        if (debugText != null) debugText.text = message;
    }
    
    void OnDrawGizmos()
    {
        if (cursor != null && sceneCamera != null)
        {
            // Draw grab ray
            Gizmos.color = _isGrabbing ? Color.green : Color.blue;
            Vector3 rayDirection = sceneCamera.transform.forward * grabDistance;
            Gizmos.DrawRay(cursor.position, rayDirection);
            
            // Draw connection to grabbed object
            if (_isGrabbing && _grabbedObject != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(cursor.position, _grabbedObject.transform.position);
            }

            // Draw camera viewport boundaries
            DrawViewportBoundaries();
        }
    }

    private void DrawViewportBoundaries()
    {
        Vector3 bottomLeft = sceneCamera.ViewportToWorldPoint(new Vector3(borderMargin, borderMargin, 10f));
        Vector3 bottomRight = sceneCamera.ViewportToWorldPoint(new Vector3(1f - borderMargin, borderMargin, 10f));
        Vector3 topLeft = sceneCamera.ViewportToWorldPoint(new Vector3(borderMargin, 1f - borderMargin, 10f));
        Vector3 topRight = sceneCamera.ViewportToWorldPoint(new Vector3(1f - borderMargin, 1f - borderMargin, 10f));

        bottomLeft.z = bottomRight.z = topLeft.z = topRight.z = 0f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
    }

    void OnDestroy()
    {
        if (_kinect != null && _kinect.IsOpen)
        {
            _kinect.Close();
        }
    }
}