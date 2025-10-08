using System;
using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Linq;
using TMPro;

public class BodySourceManager : MonoBehaviour 
{
    public static BodySourceManager instance;
    
    private KinectSensor _Sensor;
    private BodyFrameReader _Reader;
    private Body[] _Data = null;
    
    public Body FirstTrackedBody => _Data?.FirstOrDefault(b => b != null && b.IsTracked);
    public HandState RightHandState => FirstTrackedBody?.HandRightState ?? HandState.Unknown;
    public HandState LeftHandState => FirstTrackedBody?.HandLeftState ?? HandState.Unknown;
    public Vector3 BodyLean => FirstTrackedBody != null ? 
        new Vector3(FirstTrackedBody.Lean.X, FirstTrackedBody.Lean.Y, 0) : Vector3.zero;
    public bool IsSensorAvailable => _Sensor != null && _Sensor.IsOpen;

    public System.Action<bool> OnBodyDataUpdated;
    public System.Action<HandState> OnRightHandStateChanged;
    public System.Action<HandState> OnLeftHandStateChanged;
    
    private HandState _previousRightHandState = HandState.Unknown;
    private HandState _previousLeftHandState = HandState.Unknown;

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
            
            Debug.Log("BodySourceManager iniciado - Esperando frames...");
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
        if (FirstTrackedBody != null)
        {
            // Detectar cambios de estado de manos
            if (_previousRightHandState != RightHandState)
            {
                OnRightHandStateChanged?.Invoke(RightHandState);
                _previousRightHandState = RightHandState;
            }
            
            if (_previousLeftHandState != LeftHandState)
            {
                OnLeftHandStateChanged?.Invoke(LeftHandState);
                _previousLeftHandState = LeftHandState;
            }
        }
    }

    // Método auxiliar para escalar valores
    private float RescalingToRanges(float scaleAStart, float scaleAEnd, float scaleBStart, float scaleBEnd, float valueA)
    {
        return (((valueA - scaleAStart) * (scaleBEnd - scaleBStart)) / (scaleAEnd - scaleAStart)) + scaleBStart;
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