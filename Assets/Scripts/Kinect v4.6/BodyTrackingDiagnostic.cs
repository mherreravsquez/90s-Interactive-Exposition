using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;
using TMPro;

public class BodyTrackingDiagnostic : MonoBehaviour
{
    public TextMeshProUGUI diagnosticText;
    private BodySourceManager bodyManager;
    private KinectSensor sensor;
    private float lastLogTime;
    private int frameCount = 0;

    void Start()
    {
        sensor = KinectSensor.GetDefault();
        bodyManager = GetComponent<BodySourceManager>();
        
        if (bodyManager == null)
        {
            bodyManager = gameObject.AddComponent<BodySourceManager>();
        }

        diagnosticText.text = "Diagnóstico Body Tracking iniciado...\n";
        
        if (sensor != null && sensor.IsOpen)
        {
            diagnosticText.text += "Kinect conectada y abierta\n";
        }
    }

    void Update()
    {
        frameCount++;
        
        if (Time.time - lastLogTime > 2f) // Log cada 2 segundos
        {
            lastLogTime = Time.time;
            UpdateDiagnosticText();
        }
    }

    void UpdateDiagnosticText()
    {
        string log = $"Frame: {frameCount} | Time: {Time.time:F1}s\n";
        
        if (sensor == null)
        {
            log += "Sensor Kinect no encontrado\n";
        }
        else
        {
            log += $"Kinect: {sensor.UniqueKinectId}\n";
            log += $"Estado: {sensor.IsOpen} | Cuerpos máx: {sensor.BodyFrameSource.BodyCount}\n";
        }

        if (bodyManager != null)
        {
            Body[] bodies = bodyManager.GetData();
            if (bodies == null)
            {
                log += "bodies array es null\n";
            }
            else
            {
                int trackedBodies = 0;
                for (int i = 0; i < bodies.Length; i++)
                {
                    if (bodies[i] != null && bodies[i].IsTracked)
                    {
                        trackedBodies++;
                        log += $"Cuerpo {i} trackeado - ";
                        log += $"Mano: {bodies[i].HandRightState}\n";
                    }
                }
                
                if (trackedBodies == 0)
                {
                    log += "Esperando cuerpos... (párate frente al sensor)\n";
                }
            }
        }

        diagnosticText.text = log;
    }
}