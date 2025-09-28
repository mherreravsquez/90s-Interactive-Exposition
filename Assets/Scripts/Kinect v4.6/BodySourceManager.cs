using System;
using UnityEngine;
using System.Collections;
using Windows.Kinect;

public class BodySourceManager : MonoBehaviour 
{
    public static BodySourceManager instance;
    
    private KinectSensor _Sensor;
    private BodyFrameReader _Reader;
    private Body[] _Data = null;
    
    public Body[] GetData()
    {
        return _Data;
    }
    
    public bool IsInitialized { get; private set; }
    public System.Action<bool> OnBodyDataUpdated;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);        
    }

    void Start () 
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
            
            IsInitialized = true;
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
                OnBodyDataUpdated?.Invoke(true);
            }
        }
    }
    
    void Update()
    {
        // Debug visual de los datos
        if (_Data != null)
        {
            for (int i = 0; i < _Data.Length; i++)
            {
                if (_Data[i] != null && _Data[i].IsTracked)
                {
                    Debug.Log($"Cuerpo {i} trackeado - Mano derecha: {_Data[i].HandRightState}");
                }
            }
        }
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