using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;
using TMPro;
using Joint = Windows.Kinect.Joint;

public class KinectHandCursor : MonoBehaviour
{
    [Header("Settings")]
    public bool useRightHand = true;
    public float smoothFactor = 10f;
    
    [Header("References")]
    public RectTransform cursor;
    public TextMeshProUGUI debugText;

    private BodySourceManager _bodyManager;
    private KinectSensor _kinect;

    void Start()
    {
        // Automatically find BodySourceManager
        _bodyManager = FindObjectOfType<BodySourceManager>();
        _kinect = KinectSensor.GetDefault();
        
        UpdateDebugText("✅ System ready. Move your hands.");
    }

    void Update()
    {
        if (_bodyManager == null || _kinect == null || !_kinect.IsOpen) return;

        // Get body data
        Body[] bodies = _bodyManager.GetData();
        if (bodies == null) return;

        // Find first tracked body
        foreach (Body body in bodies)
        {
            if (body != null && body.IsTracked)
            {
                MoveCursor(body);
                return;
            }
        }
        
        UpdateDebugText("👀 Please stand in front of Kinect sensor");
    }

    private void MoveCursor(Body body)
    {
        if (cursor == null) return;

        // Choose which hand to follow
        JointType handType = useRightHand ? JointType.HandRight : JointType.HandLeft;
        Joint hand = body.Joints[handType];
        
        if (hand.TrackingState == TrackingState.Tracked)
        {
            // 1. Get hand position in 2D screen coordinates
            Vector2 screenPosition = ConvertToScreen(hand.Position);
            
            // 2. Move the cursor
            MoveCursorUI(screenPosition);
            
            // 3. Show debug information
            string handState = useRightHand ? 
                body.HandRightState.ToString() : body.HandLeftState.ToString();
            UpdateDebugText($"✋ Hand detected | State: {handState}");
        }
    }

    private Vector2 ConvertToScreen(Windows.Kinect.CameraSpacePoint position3D)
    {
        try
        {
            // Convert Kinect 3D coordinates to 2D color coordinates
            ColorSpacePoint colorPoint = _kinect.CoordinateMapper.MapCameraPointToColorSpace(position3D);
            
            // Adjust to Kinect camera resolution (1920x1080)
            float normalizedX = colorPoint.X / 1920f;
            float normalizedY = colorPoint.Y / 1080f;
            
            // Convert to Unity screen pixels (invert Y)
            float screenX = normalizedX * Screen.width;
            float screenY = (1 - normalizedY) * Screen.height;
            
            return new Vector2(screenX, screenY);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Conversion error: {e.Message}");
            return Vector2.zero;
        }
    }

    private void MoveCursorUI(Vector2 screenPosition)
    {
        if (cursor == null) return;

        try
        {
            // For Screen Space - Overlay Canvas, it's simpler
            Vector2 localPoint;
            
            // If cursor is directly in Canvas
            RectTransform canvasRect = cursor.parent as RectTransform;
            
            // Convert screen coordinates to Canvas local coordinates
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                null, // No camera needed for Overlay mode
                out localPoint
            );
            
            // Apply smooth movement
            cursor.anchoredPosition = Vector2.Lerp(
                cursor.anchoredPosition, 
                localPoint, 
                Time.deltaTime * smoothFactor
            );
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Cursor movement error: {e.Message}");
        }
    }

    private void UpdateDebugText(string message)
    {
        if (debugText != null) debugText.text = message;
        Debug.Log(message);
    }
}