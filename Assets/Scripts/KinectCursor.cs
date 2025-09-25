using UnityEngine;

public class KinectCursor : MonoBehaviour
{
    [Header("Sprite Settings")]
    [SerializeField] private Sprite openHandSprite;    // Sprite para mano abierta
    [SerializeField] private Sprite closedHandSprite;  // Sprite para mano cerrada
    [SerializeField] private float grabScale = 1.2f;
    
    [Header("Animation Settings")]
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minPulseScale = 0.9f;
    [SerializeField] private float maxPulseScale = 1.1f;
    
    private Vector3 originalScale;
    private SpriteRenderer cursorRenderer;
    private bool isGrabbing = false;

    void Start()
    {
        originalScale = transform.localScale;
        cursorRenderer = GetComponent<SpriteRenderer>();
        
        // Verificar que los sprites estén asignados
        if (cursorRenderer != null)
        {
            if (openHandSprite != null)
            {
                cursorRenderer.sprite = openHandSprite;
            }
            else
            {
                Debug.LogWarning("OpenHandSprite no asignado en KinectCursor");
            }
        }
        
        gameObject.SetActive(false);
    }

    void Update()
    {
        // Rotación continua suave
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        
        // Animación de pulso (solo cuando no está agarrando)
        if (!isGrabbing)
        {
            float pulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float scale = Mathf.Lerp(minPulseScale, maxPulseScale, pulse);
            transform.localScale = originalScale * scale;
        }
    }
    
    public void SetGrabbingState(bool grabbing)
    {
        if (isGrabbing == grabbing) return;
        
        isGrabbing = grabbing;
        
        if (cursorRenderer != null)
        {
            // Cambiar sprite según el estado
            if (grabbing && closedHandSprite != null)
            {
                cursorRenderer.sprite = closedHandSprite;
            }
            else if (!grabbing && openHandSprite != null)
            {
                cursorRenderer.sprite = openHandSprite;
            }
            
            // Cambiar escala cuando está agarrando
            transform.localScale = originalScale * (grabbing ? grabScale : 1f);
        }
    }
    
    public void SetCursorActive(bool active)
    {
        gameObject.SetActive(active);
        
        if (active && cursorRenderer != null && openHandSprite != null)
        {
            // Resetear al sprite de mano abierta cuando se activa
            cursorRenderer.sprite = openHandSprite;
            isGrabbing = false;
            transform.localScale = originalScale;
        }
    }
    
    // Método para actualizar sprites en tiempo de ejecución (opcional)
    public void SetSprites(Sprite openSprite, Sprite closedSprite)
    {
        openHandSprite = openSprite;
        closedHandSprite = closedSprite;
        
        if (cursorRenderer != null)
        {
            cursorRenderer.sprite = isGrabbing ? closedHandSprite : openHandSprite;
        }
    }
}