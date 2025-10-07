using System;
using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Linq;
using System.Collections.Generic;

public class BodySourceManager : MonoBehaviour 
{
    public static BodySourceManager instance;
    
    private KinectSensor _Sensor;
    private BodyFrameReader _Reader;
    private Body[] _Data = null;
    
    // Tracked bodies sorted by X position (left to right)
    public List<Body> TrackedBodies { get; private set; } = new List<Body>();
    public bool IsSensorAvailable => _Sensor != null && _Sensor.IsOpen;

    // Events for hand state changes per body
    public System.Action<bool> OnBodyDataUpdated;
    public System.Action<int, HandState> OnRightHandStateChanged; // int: body index (0 = left player, 1 = right player)
    public System.Action<int, HandState> OnLeftHandStateChanged;

    // Previous hand states per body
    private HandState[] _previousRightHandStates = new HandState[2];
    private HandState[] _previousLeftHandStates = new HandState[2];

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);        
    }

    void Start() 
    {
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null) 
        {
            _Reader = _Sensor.BodyFrameSource.OpenReader();
            _Reader.FrameArrived += Reader_FrameArrived;
            
            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
            
            Debug.Log("BodySourceManager started - Waiting for frames...");
        }
    }
    
    private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
    {
        using (var frame = e.FrameReference.AcquireFrame())
        {
            if (frame != null)
            {
                if (_Data == null)
                {
                    _Data = new Body[_Sensor.BodyFrameSource.BodyCount];
                }
                
                frame.GetAndRefreshBodyData(_Data);
                ProcessBodyData();
                OnBodyDataUpdated?.Invoke(true);
            }
        }
    }
    
    private void ProcessBodyData()
    {
        // Clear previous tracked bodies
        TrackedBodies.Clear();

        // Get all tracked bodies
        var trackedBodies = new List<Body>();
        if (_Data != null)
        {
            foreach (var body in _Data)
            {
                if (body != null && body.IsTracked)
                {
                    trackedBodies.Add(body);
                }
            }
        }

        // Sort bodies by X position (left to right)
        TrackedBodies = trackedBodies
            .OrderBy(b => b.Joints[JointType.SpineBase].Position.X)
            .Take(2) // Only take 2 players max
            .ToList();

        // Process hand state changes for each tracked body
        for (int i = 0; i < TrackedBodies.Count; i++)
        {
            var body = TrackedBodies[i];
            HandState currentRightState = body.HandRightState;
            HandState currentLeftState = body.HandLeftState;

            // Check for right hand state changes
            if (_previousRightHandStates[i] != currentRightState)
            {
                OnRightHandStateChanged?.Invoke(i, currentRightState);
                _previousRightHandStates[i] = currentRightState;
            }

            // Check for left hand state changes
            if (_previousLeftHandStates[i] != currentLeftState)
            {
                OnLeftHandStateChanged?.Invoke(i, currentLeftState);
                _previousLeftHandStates[i] = currentLeftState;
            }
        }
    }

    // Helper method to get a specific body by index (0 = left player, 1 = right player)
    public Body GetBody(int bodyIndex)
    {
        if (bodyIndex >= 0 && bodyIndex < TrackedBodies.Count)
            return TrackedBodies[bodyIndex];
        return null;
    }

    // Get body by spatial position (0 = leftmost, 1 = rightmost)
    public Body GetLeftPlayer()
    {
        return TrackedBodies.Count > 0 ? TrackedBodies[0] : null;
    }

    public Body GetRightPlayer()
    {
        return TrackedBodies.Count > 1 ? TrackedBodies[1] : null;
    }

    // Get hand state for specific body and hand
    public HandState GetHandState(int bodyIndex, bool isRightHand)
    {
        var body = GetBody(bodyIndex);
        if (body != null)
            return isRightHand ? body.HandRightState : body.HandLeftState;
        return HandState.Unknown;
    }

    public Body[] GetData()
    {
        return _Data;
    }
    
    void OnApplicationQuit()
    {
        if (_Reader != null)
        {
            _Reader.FrameArrived -= Reader_FrameArrived;
            _Reader.Dispose();
        }
        
        if (_Sensor != null && _Sensor.IsOpen)
        {
            _Sensor.Close();
        }
    }
}