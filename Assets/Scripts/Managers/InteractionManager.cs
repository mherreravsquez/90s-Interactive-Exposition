using UnityEngine;
using UnityEngine.InputSystem;
using Windows.Kinect;
using TMPro;
using System.Collections.Generic;
using Joint = Windows.Kinect.Joint;

public class InteractionManager : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Camera orthoCamera;
    
    [Header("Mouse Input")]
    private Instrument draggedInstrument;
    private Vector3 grabOffset;
    private bool isDragging = false;
    private Mouse mouse;

    [Header("Kinect Cursors System")]
    [SerializeField] private List<KinectCursor> kinectCursors = new List<KinectCursor>();
    public LayerMask instrumentLayer;
    public float grabDistance = 5f;

    [Header("Debug")]
    public TextMeshProUGUI debugText;

    // Kinect management
    private BodySourceManager _bodyManager;
    private KinectSensor _kinect;

    [System.Serializable]
    public class KinectCursor
    {
        public string cursorId;
        public bool useRightHand = true;
        public RectTransform cursorTransform;
        public Sprite[] handSprites;
        public float smoothFactor = 10f;
        public float borderMargin = 0.02f;
        
        [Header("Runtime State")]
        public HandState previousHandState = HandState.Unknown;
        public bool handClosedThisFrame = false;
        public GameObject grabbedObject;
        public Vector3 grabOffset;
        public bool isGrabbing = false;
        public Vector2 currentScreenPosition;
    }

    private void Start()
    {
        if (orthoCamera == null)
            orthoCamera = Camera.main;
            
        mouse = Mouse.current;

        // Initialize Kinect
        _bodyManager = BodySourceManager.instance;
        _kinect = KinectSensor.GetDefault();
        
        if (_bodyManager != null)
        {
            _bodyManager.OnRightHandStateChanged += OnRightHandStateChanged;
            _bodyManager.OnLeftHandStateChanged += OnLeftHandStateChanged;
        }

        UpdateDebugText("System ready. Multiple cursors initialized.");
    }

    private void Update()
    {
        HandleMouseInput();
        
        if (isDragging)
            UpdateDraggedPosition();

        UpdateKinectCursors();
    }

    #region Mouse Input (Existing functionality)
    
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
        isDragging = true;
        
        grabOffset = draggedInstrument.transform.position - hitPoint;
        grabOffset.z = 0;
        
        Debug.Log($"Started dragging: {instrument.instrumentData.instrumentType}");
    }
    
    private void UpdateDraggedPosition()
    {
        if (draggedInstrument == null) return;
        
        Vector2 mousePos = mouse.position.ReadValue();
        Vector3 mousePosition = new Vector3(mousePos.x, mousePos.y, orthoCamera.nearClipPlane + 1f);
        
        Vector3 worldPosition = orthoCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = draggedInstrument.transform.position.z;
        
        draggedInstrument.transform.position = worldPosition + grabOffset;
    }
    
    private void EndDrag()
    {
        if (!isDragging) return;
        
        Debug.Log($"Ended dragging: {draggedInstrument.instrumentData.instrumentType}");
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
    
    #endregion

    #region Kinect Cursors System

    private void UpdateKinectCursors()
    {
        if (_bodyManager == null || !_bodyManager.IsSensorAvailable) 
        {
            UpdateDebugText("Kinect not available");
            return;
        }

        Body firstBody = _bodyManager.FirstTrackedBody;
        if (firstBody != null)
        {
            foreach (var cursor in kinectCursors)
            {
                if (cursor.cursorTransform != null)
                {
                    UpdateCursorPosition(cursor, firstBody);
                    UpdateCursorAppearance(cursor);
                    UpdateGrabSystem(cursor);
                }
            }

            // Update debug text with first cursor status
            if (kinectCursors.Count > 0)
            {
                string handState = kinectCursors[0].useRightHand ? 
                    firstBody.HandRightState.ToString() : firstBody.HandLeftState.ToString();
                string grabStatus = kinectCursors[0].isGrabbing ? 
                    $"Grabbing: {kinectCursors[0].grabbedObject.name}" : "Ready to grab";
                UpdateDebugText($"Hand: {handState} | {grabStatus} | Cursors: {kinectCursors.Count}");
            }
        }
        else
        {
            UpdateDebugText("Please stand in front of Kinect sensor");
        }
    }

    private void UpdateCursorPosition(KinectCursor cursor, Body body)
    {
        JointType handType = cursor.useRightHand ? JointType.HandRight : JointType.HandLeft;
        Joint hand = body.Joints[handType];
        
        if (hand.TrackingState == TrackingState.Tracked)
        {
            Vector2 screenPosition = ConvertToScreen(hand.Position);
            cursor.currentScreenPosition = screenPosition;
            MoveCursorUI(cursor, screenPosition);
        }
    }

    private void MoveCursorUI(KinectCursor cursor, Vector2 screenPosition)
    {
        if (cursor.cursorTransform == null || orthoCamera == null) return;

        Vector2 clampedScreenPosition = ClampToCameraViewport(cursor, screenPosition);

        Vector3 viewportPoint = orthoCamera.ScreenToViewportPoint(
            new Vector3(clampedScreenPosition.x, clampedScreenPosition.y, 0f)
        );

        Vector3 worldPosition = orthoCamera.ViewportToWorldPoint(
            new Vector3(viewportPoint.x, viewportPoint.y, 10f)
        );
        
        Vector3 fixedPosition = new Vector3(worldPosition.x, worldPosition.y, 0f);

        cursor.cursorTransform.position = Vector3.Lerp(
            cursor.cursorTransform.position, 
            fixedPosition, 
            Time.deltaTime * cursor.smoothFactor
        );
    }

    private Vector2 ClampToCameraViewport(KinectCursor cursor, Vector2 screenPosition)
    {
        Vector3 viewportPoint = orthoCamera.ScreenToViewportPoint(
            new Vector3(screenPosition.x, screenPosition.y, 0f)
        );

        viewportPoint.x = Mathf.Clamp(viewportPoint.x, cursor.borderMargin, 1f - cursor.borderMargin);
        viewportPoint.y = Mathf.Clamp(viewportPoint.y, cursor.borderMargin, 1f - cursor.borderMargin);

        Vector3 clampedScreenPoint = orthoCamera.ViewportToScreenPoint(viewportPoint);
        return new Vector2(clampedScreenPoint.x, clampedScreenPoint.y);
    }

    private void UpdateCursorAppearance(KinectCursor cursor)
    {
        if (cursor.cursorTransform == null || cursor.handSprites == null || cursor.handSprites.Length < 2) return;

        var image = cursor.cursorTransform.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.sprite = cursor.isGrabbing ? cursor.handSprites[1] : cursor.handSprites[0];
        }
    }

    private void UpdateGrabSystem(KinectCursor cursor)
    {
        if (cursor.isGrabbing && cursor.grabbedObject != null)
        {
            Vector3 targetPosition = new Vector3(
                cursor.cursorTransform.position.x + cursor.grabOffset.x,
                cursor.cursorTransform.position.y + cursor.grabOffset.y,
                cursor.grabbedObject.transform.position.z
            );
            
            cursor.grabbedObject.transform.position = targetPosition;
        }
    }

    #endregion

    #region Hand State Events

    private void OnRightHandStateChanged(HandState newState)
    {
        foreach (var cursor in kinectCursors)
        {
            if (cursor.useRightHand)
            {
                ProcessHandStateChange(cursor, newState);
            }
        }
    }

    private void OnLeftHandStateChanged(HandState newState)
    {
        foreach (var cursor in kinectCursors)
        {
            if (!cursor.useRightHand)
            {
                ProcessHandStateChange(cursor, newState);
            }
        }
    }

    private void ProcessHandStateChange(KinectCursor cursor, HandState newState)
    {
        // Detect hand CLOSED (grab)
        if (cursor.previousHandState != HandState.Closed && newState == HandState.Closed)
        {
            cursor.handClosedThisFrame = true;
            if (!cursor.isGrabbing)
                TryGrabInstrument(cursor);
        }
        
        // Detect hand OPENED (release)
        if (cursor.isGrabbing && cursor.previousHandState == HandState.Closed && newState != HandState.Closed)
        {
            ReleaseInstrument(cursor);
        }

        cursor.previousHandState = newState;
    }

    private void TryGrabInstrument(KinectCursor cursor)
    {
        if (cursor.isGrabbing) return;

        Vector3 cursorWorldPos = cursor.cursorTransform.position;
        Vector3 rayDirection = orthoCamera.transform.forward;

        RaycastHit[] hits = Physics.SphereCastAll(
            cursorWorldPos, 
            0.3f,
            rayDirection, 
            grabDistance, 
            instrumentLayer
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Instrument"))
            {
                GrabInstrument(cursor, hit.collider.gameObject, hit.point);
                break;
            }
        }
    }

    private void GrabInstrument(KinectCursor cursor, GameObject instrument, Vector3 grabPoint)
    {
        cursor.grabbedObject = instrument;
        cursor.isGrabbing = true;
        
        Vector3 instrumentPosition = instrument.transform.position;
        Vector3 cursorPosition = cursor.cursorTransform.position;
        cursor.grabOffset = new Vector3(
            instrumentPosition.x - cursorPosition.x,
            instrumentPosition.y - cursorPosition.y,
            0f
        );
    }

    private void ReleaseInstrument(KinectCursor cursor)
    {
        if (!cursor.isGrabbing) return;
        
        cursor.grabbedObject = null;
        cursor.isGrabbing = false;
    }

    #endregion

    #region Utility Methods

    private Vector2 ConvertToScreen(CameraSpacePoint position3D)
    {
        if (_kinect == null) return Vector2.zero;

        try
        {
            ColorSpacePoint colorPoint = _kinect.CoordinateMapper.MapCameraPointToColorSpace(position3D);
            
            float squareSize = Mathf.Min(Screen.width, Screen.height);
            float xOffset = (Screen.width - squareSize) / 2f;
            float yOffset = (Screen.height - squareSize) / 2f;
            
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

    private void UpdateDebugText(string message)
    {
        if (debugText != null) debugText.text = message;
    }

    #endregion

    #region Public Methods for Cursor Management

    public void AddKinectCursor(KinectCursor newCursor)
    {
        kinectCursors.Add(newCursor);
    }

    public void RemoveKinectCursor(string cursorId)
    {
        kinectCursors.RemoveAll(c => c.cursorId == cursorId);
    }

    public KinectCursor GetCursor(string cursorId)
    {
        return kinectCursors.Find(c => c.cursorId == cursorId);
    }

    #endregion

    void OnDestroy()
    {
        if (_bodyManager != null)
        {
            _bodyManager.OnRightHandStateChanged -= OnRightHandStateChanged;
            _bodyManager.OnLeftHandStateChanged -= OnLeftHandStateChanged;
        }

        if (_kinect != null && _kinect.IsOpen)
        {
            _kinect.Close();
        }
    }
}