using UnityEngine;

public class KinectIntegrationBridge : MonoBehaviour
{
    private KinectHandsManager kinectHandsManager;
    private InteractionSystem interactionSystem;
    
    void Start()
    {
        kinectHandsManager = FindObjectOfType<KinectHandsManager>();
        interactionSystem = FindObjectOfType<InteractionSystem>();
    }
    
    void Update()
    {
        // Este bridge asegura que ambos sistemas trabajen juntos
        SyncKinectWithInteractionSystem();
    }
    
    void SyncKinectWithInteractionSystem()
    {
        // Si Kinect está activo y agarrando, desactivar temporalmente el mouse/OSC
        if (kinectHandsManager != null && kinectHandsManager.IsAnyHandGrabbing())
        {
            // Puedes agregar lógica para priorizar Kinect sobre otros inputs
        }
    }
}