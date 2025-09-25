using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;
using TMPro;

public class KinectDiagnostic : MonoBehaviour
{
    public TextMeshProUGUI diagnosticText;
    private KinectSensor sensor;
    private float lastUpdateTime;
    
    void Start()
    {
        sensor = KinectSensor.GetDefault();
        diagnosticText.text = "Iniciando diagnóstico...";
        
        if (sensor != null)
        {
            diagnosticText.text = $"Kinect encontrada: {sensor.UniqueKinectId}";
            
            if (!sensor.IsOpen)
            {
                sensor.Open();
                diagnosticText.text += "\nKinect abierta correctamente";
            }
            
            diagnosticText.text += $"\nEstado: {sensor.IsOpen}";
        }
        else
        {
            diagnosticText.text = "ERROR: Kinect no detectada";
        }
    }
    
    void Update()
    {
        if (Time.time - lastUpdateTime > 1f)
        {
            lastUpdateTime = Time.time;
            diagnosticText.text += $"\nUpdate ejecutándose: {Time.time}";
        }
    }
}