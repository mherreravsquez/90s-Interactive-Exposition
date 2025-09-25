using UnityEngine;
using System.Collections.Generic;
using Windows.Kinect;

public class KinectHandsManager : MonoBehaviour
{
    [Header("Kinect Configuration")]
    public bool enableKinect = true;
    public int maxPlayers = 2;

    [Header("World Space Canvas Settings")]
    public Canvas targetCanvas;
    public GameObject handCursorPrefab;
    public float cursorSmoothness = 5f;

    [Header("Camera Settings")]
    public Camera orthoCamera;
    public float canvasDistance = 10f;

    [Header("Raycast Settings")]
    public float raycastDistance = 50f;
    public LayerMask instrumentLayerMask = -1;
    public bool showRaycastDebug = true;

    [Header("Debug Settings")]
    public bool verboseLogging = true;
    public float debugLogInterval = 2f;

    // Reference to your KinectManager
    private KinectManager kinectManager;
    private InteractionSystem interactionSystem;
    private List<HandCursor> handCursors = new List<HandCursor>();
    private Vector2 canvasWorldSize;
    private float lastDebugLogTime;
    private int framesWithoutBodies = 0;

    void Start()
    {
        interactionSystem = FindObjectOfType<InteractionSystem>();
        
        // Get your KinectManager instance
        kinectManager = KinectManager.Instance;

        if (kinectManager == null)
        {
            Debug.LogError("❌ KinectManager not found. Please ensure the KinectManager script is in the scene.");
            enableKinect = false;
            return;
        }

        Debug.Log("✅ KinectManager encontrado: " + kinectManager.name);

        if (orthoCamera == null)
        {
            orthoCamera = Camera.main;
            Debug.LogWarning("⚠️ Usando cámara principal como cámara ortográfica: " + orthoCamera.name);
        }

        if (targetCanvas == null)
        {
            Debug.LogError("❌ No Canvas assigned!");
            enableKinect = false;
            return;
        }

        Debug.Log("✅ Canvas asignado: " + targetCanvas.name + " | Render Mode: " + targetCanvas.renderMode);

        // Calcular tamaño mundial del Canvas
        CalculateCanvasWorldSize();
        CreateHandCursors();

        // Log inicial del estado de Kinect
        LogKinectStatus();
    }

    void CalculateCanvasWorldSize()
    {
        RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
        canvasWorldSize = new Vector2(canvasRect.rect.width, canvasRect.rect.height);
        Debug.Log($"📐 Canvas World Size: {canvasWorldSize}");
    }

    void CreateHandCursors()
    {
        for (int i = 0; i < maxPlayers * 2; i++)
        {
            GameObject cursorObj = Instantiate(handCursorPrefab);
            cursorObj.transform.SetParent(targetCanvas.transform, false);
            cursorObj.transform.localPosition = Vector3.zero;
            cursorObj.transform.localRotation = Quaternion.identity;
            cursorObj.transform.localScale = Vector3.one;

            HandCursor cursor = cursorObj.GetComponent<HandCursor>();
            cursor.Initialize(i, this, orthoCamera, canvasWorldSize);
            handCursors.Add(cursor);
            
            string handType = (i % 2 == 0) ? "Left" : "Right";
            cursorObj.name = $"HandCursor_{handType}_Player{(i / 2 + 1)}";
            
            Debug.Log($"✅ Cursor creado: {cursorObj.name}");
        }
    }

    void Update()
    {
        if (!enableKinect || kinectManager == null) return;

        // Log periódico del estado (cada 2 segundos)
        if (Time.time - lastDebugLogTime >= debugLogInterval)
        {
            LogKinectStatus();
            lastDebugLogTime = Time.time;
        }

        if (kinectManager.IsAvailable)
        {
            UpdateHandCursors();
            CheckHandInteractions();
        }
        else
        {
            if (verboseLogging) Debug.LogWarning("⚠️ Kinect no está disponible");
        }
    }

    void UpdateHandCursors()
    {
        Body[] bodies = kinectManager.GetBodies();
        
        if (bodies == null)
        {
            framesWithoutBodies++;
            if (framesWithoutBodies % 30 == 0) // Log cada 30 frames
            {
                Debug.LogWarning("⚠️ Bodies array es null");
            }
            return;
        }

        framesWithoutBodies = 0;

        if (verboseLogging && bodies.Length > 0)
        {
            Debug.Log($"👥 Bodies detectados: {bodies.Length}");
        }

        int trackedPlayerCount = 0;

        for (int i = 0; i < handCursors.Count; i++)
        {
            int playerIndex = i / 2;
            bool isLeftHand = (i % 2 == 0);

            // Verificar si hay suficientes cuerpos
            if (playerIndex >= bodies.Length)
            {
                handCursors[i].HideCursor();
                if (verboseLogging) Debug.Log($"🚫 No hay body para player {playerIndex + 1}");
                continue;
            }

            Body body = bodies[playerIndex];
            
            if (body == null)
            {
                handCursors[i].HideCursor();
                if (verboseLogging) Debug.Log($"🚫 Body {playerIndex} es null");
                continue;
            }

            if (!body.IsTracked)
            {
                handCursors[i].HideCursor();
                if (verboseLogging && framesWithoutBodies % 60 == 0)
                {
                    Debug.Log($"🚫 Body {playerIndex} no está siendo trackeado");
                }
                continue;
            }

            trackedPlayerCount++;

            // Get hand position from Kinect
            CameraSpacePoint handPosition = isLeftHand ? 
                body.Joints[JointType.HandLeft].Position : 
                body.Joints[JointType.HandRight].Position;

            // DEBUG: Log de las coordenadas raw de Kinect
            if (verboseLogging && framesWithoutBodies % 90 == 0)
            {
                string handType = isLeftHand ? "Left" : "Right";
                Debug.Log($"📊 Player {playerIndex + 1} - Mano {handType}: " +
                         $"Kinect Raw: X={handPosition.X:F3}, Y={handPosition.Y:F3}, Z={handPosition.Z:F3}");
            }

            // Convertir coordenadas Kinect a coordenadas normalizadas del Canvas
            Vector2 canvasLocalPos = KinectToCanvasLocalPosition(handPosition);
            
            // DEBUG: Log de la conversión
            if (verboseLogging && framesWithoutBodies % 90 == 0)
            {
                string handType = isLeftHand ? "Left" : "Right";
                Debug.Log($"🔄 Player {playerIndex + 1} - Mano {handType}: " +
                         $"Canvas Local: X={canvasLocalPos.x:F1}, Y={canvasLocalPos.y:F1}");
            }

            // Actualizar posición del cursor
            handCursors[i].UpdateCanvasPosition(canvasLocalPos);

            // Get hand state
            HandState handState = isLeftHand ? 
                body.HandLeftState : 
                body.HandRightState;

            bool isHandClosed = (handState == HandState.Closed || handState == HandState.Lasso);
            handCursors[i].SetHandState(isHandClosed);

            // DEBUG: Log del estado de la mano
            if (handState != HandState.Unknown && verboseLogging && framesWithoutBodies % 120 == 0)
            {
                string handType = isLeftHand ? "Left" : "Right";
                Debug.Log($"✋ Player {playerIndex + 1} - Mano {handType}: Estado = {handState}");
            }
        }

        if (trackedPlayerCount == 0 && framesWithoutBodies % 60 == 0)
        {
            Debug.LogWarning("⚠️ No hay jugadores siendo trackeados");
        }
    }

    Vector2 KinectToCanvasLocalPosition(CameraSpacePoint kinectPosition)
    {
        // Ajustar estos valores según tu setup de Kinect
        // Kinect típicamente devuelve: X[-1,1], Y[-1,1], Z[0,4]
        
        // Mapear coordenadas Kinect a coordenadas de Canvas
        float normalizedX = Mathf.InverseLerp(-1f, 1f, kinectPosition.X);
        float normalizedY = Mathf.InverseLerp(-1f, 1f, kinectPosition.Y);
        
        float canvasX = Mathf.Lerp(-canvasWorldSize.x / 2f, canvasWorldSize.x / 2f, normalizedX);
        float canvasY = Mathf.Lerp(-canvasWorldSize.y / 2f, canvasWorldSize.y / 2f, normalizedY);

        return new Vector2(canvasX, canvasY);
    }

    void LogKinectStatus()
    {
        if (kinectManager == null) return;

        Debug.Log($"🔍 Estado Kinect - Disponible: {kinectManager.IsAvailable}");

        if (kinectManager.IsAvailable)
        {
            Body[] bodies = kinectManager.GetBodies();
            int trackedBodies = 0;
            
            if (bodies != null)
            {
                foreach (var body in bodies)
                {
                    if (body != null && body.IsTracked) trackedBodies++;
                }
            }
            
            Debug.Log($"🔍 Cuerpos trackeados: {trackedBodies}/{bodies?.Length ?? 0}");
        }
    }

    void CheckHandInteractions()
    {
        foreach (var cursor in handCursors)
        {
            if (!cursor.IsActive) continue;

            // Disparar raycast desde la posición del cursor en el Canvas World Space
            RaycastHit hit;
            Vector3 rayOrigin = cursor.GetWorldPosition();
            
            // Disparar raycast hacia adelante (en la dirección Z del Canvas)
            Vector3 rayDirection = targetCanvas.transform.forward;
            Ray ray = new Ray(rayOrigin, rayDirection);

            bool hitInstrument = Physics.Raycast(ray, out hit, raycastDistance, instrumentLayerMask);
            GameObject hitObject = hitInstrument ? hit.collider.gameObject : null;

            cursor.UpdateRaycastResult(hitInstrument, hitObject, hit.point);

            if (cursor.IsHandClosed)
            {
                if (hitInstrument && hit.collider.CompareTag("Instrument"))
                {
                    float distance = Vector3.Distance(rayOrigin, hit.point);
                    
                    if (distance <= raycastDistance)
                    {
                        if (!cursor.IsGrabbing)
                        {
                            TryGrabWithHand(cursor, hit.collider.gameObject);
                        }
                        else
                        {
                            MoveInstrumentWithHand(cursor, hit.point);
                        }
                    }
                    else if (cursor.IsGrabbing)
                    {
                        ReleaseWithHand(cursor);
                    }
                }
                else if (cursor.IsGrabbing)
                {
                    // Si no hay instrumento pero la mano está cerrada, mantener posición actual
                    MoveInstrumentWithHand(cursor, cursor.LastRaycastHit ?? rayOrigin + rayDirection * raycastDistance);
                }
            }
            else if (cursor.IsGrabbing)
            {
                ReleaseWithHand(cursor);
            }
        }
    }

    void TryGrabWithHand(HandCursor cursor, GameObject instrument)
    {
        if (instrument == null) return;

        if (interactionSystem != null && !interactionSystem.grabbing)
        {
            cursor.GrabInstrument(instrument);
            
            // Integración con InteractionSystem existente
            // interactionSystem.GrabInstrument(instrument);
        }
        else
        {
            cursor.GrabInstrument(instrument);
        }
    }

    void MoveInstrumentWithHand(HandCursor cursor, Vector3 targetPosition)
    {
        if (cursor.GrabbedInstrument == null) return;

        // Mover instrumento a la posición del hit del raycast
        cursor.GrabbedInstrument.transform.position = Vector3.Lerp(
            cursor.GrabbedInstrument.transform.position,
            targetPosition,
            Time.deltaTime * cursorSmoothness
        );
    }

    void ReleaseWithHand(HandCursor cursor)
    {
        if (interactionSystem != null && interactionSystem.grabbing)
        {
            // interactionSystem.ReleaseInstrument();
        }
        
        cursor.ReleaseInstrument();
    }

    // Dibujar debug de raycasts
    void OnDrawGizmos()
    {
        if (!showRaycastDebug || !Application.isPlaying) return;

        foreach (var cursor in handCursors)
        {
            if (!cursor.IsActive) continue;

            Vector3 rayOrigin = cursor.GetWorldPosition();
            Vector3 rayDirection = targetCanvas.transform.forward;

            Gizmos.color = cursor.IsHandClosed ? Color.red : Color.green;
            Gizmos.DrawRay(rayOrigin, rayDirection * raycastDistance);

            if (cursor.LastRaycastHit.HasValue)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(cursor.LastRaycastHit.Value, 0.1f);
                Gizmos.DrawLine(rayOrigin, cursor.LastRaycastHit.Value);
            }
        }
    }

    // Public method to check if any hand is currently grabbing
    public bool IsAnyHandGrabbing()
    {
        foreach (var cursor in handCursors)
        {
            if (cursor.IsGrabbing) return true;
        }
        return false;
    }

    // Get hand cursor by player index and hand type
    public HandCursor GetHandCursor(int playerIndex, bool isLeftHand)
    {
        int cursorIndex = playerIndex * 2 + (isLeftHand ? 0 : 1);
        if (cursorIndex >= 0 && cursorIndex < handCursors.Count)
        {
            return handCursors[cursorIndex];
        }
        return null;
    }
}