using UnityEngine;

/// <summary>
/// Controls camera movement and zoom for the jungle gym building view.
/// Allows panning with arrow keys/WASD and zooming with mouse wheel.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed of camera panning (units per second)")]
    public float panSpeed = 10f;
    
    [Tooltip("Use WASD keys in addition to arrow keys")]
    public bool useWASD = true;
    
    [Header("Zoom Settings")]
    [Tooltip("Enable mouse wheel zoom")]
    public bool enableZoom = true;
    
    [Tooltip("Zoom speed (orthographic size change per scroll)")]
    public float zoomSpeed = 2f;
    
    [Tooltip("Minimum orthographic size (max zoom in)")]
    public float minZoom = 5f;
    
    [Tooltip("Maximum orthographic size (max zoom out)")]
    public float maxZoom = 30f;
    
    [Header("Boundaries (Optional)")]
    [Tooltip("Enable camera movement boundaries")]
    public bool useBoundaries = true;
    
    [Tooltip("Minimum X position the camera can move to")]
    public float minX = -20f;
    
    [Tooltip("Maximum X position the camera can move to")]
    public float maxX = 20f;
    
    [Tooltip("Minimum Y position the camera can move to")]
    public float minY = 0f;
    
    [Tooltip("Maximum Y position the camera can move to")]
    public float maxY = 40f;
    
    private Camera cam;
    
    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
        
        if (cam != null && !cam.orthographic)
        {
            Debug.LogWarning("CameraController: Camera is not set to orthographic. This script is designed for orthographic cameras.");
        }
    }
    
    private void Update()
    {
        HandlePanning();
        
        if (enableZoom)
        {
            HandleZoom();
        }
    }
    
    private void HandlePanning()
    {
        Vector3 movement = Vector3.zero;
        
        // Arrow keys
        if (Input.GetKey(KeyCode.UpArrow))
        {
            movement.y += 1f;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            movement.y -= 1f;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            movement.x -= 1f;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            movement.x += 1f;
        }
        
        // WASD keys (optional)
        if (useWASD)
        {
            if (Input.GetKey(KeyCode.W))
            {
                movement.y += 1f;
            }
            if (Input.GetKey(KeyCode.S))
            {
                movement.y -= 1f;
            }
            if (Input.GetKey(KeyCode.A))
            {
                movement.x -= 1f;
            }
            if (Input.GetKey(KeyCode.D))
            {
                movement.x += 1f;
            }
        }
        
        // Normalize diagonal movement so it's not faster
        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }
        
        // Apply movement
        if (movement != Vector3.zero)
        {
            // Transform movement to camera's local space
            Vector3 localMovement = transform.TransformDirection(movement);
            Vector3 newPosition = transform.position + localMovement * panSpeed * Time.deltaTime;

            // Apply boundaries if enabled
            if (useBoundaries)
            {
                newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
            }

            transform.position = newPosition;
        }
    }
    
    private void HandleZoom()
    {
        if (cam == null || !cam.orthographic) return;
        
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (scrollInput != 0f)
        {
            // Negative scroll = zoom out (increase size)
            // Positive scroll = zoom in (decrease size)
            float newSize = cam.orthographicSize - scrollInput * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
    
    /// <summary>
    /// Instantly move camera to a specific position
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }
    
    /// <summary>
    /// Smoothly move camera to a specific position over time
    /// </summary>
    public void MoveTo(Vector3 targetPosition, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(MoveToCoroutine(targetPosition, duration));
    }
    
    private System.Collections.IEnumerator MoveToCoroutine(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = transform.position;
        targetPosition.z = transform.position.z; // Keep Z position
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Smooth interpolation
            t = t * t * (3f - 2f * t); // Smoothstep
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }
        
        transform.position = targetPosition;
    }
    
    /// <summary>
    /// Set the zoom level (orthographic size)
    /// </summary>
    public void SetZoom(float orthographicSize)
    {
        if (cam != null && cam.orthographic)
        {
            cam.orthographicSize = Mathf.Clamp(orthographicSize, minZoom, maxZoom);
        }
    }
    
    /// <summary>
    /// Reset camera to default position and zoom
    /// </summary>
    public void ResetCamera(Vector3 defaultPosition, float defaultZoom)
    {
        SetPosition(defaultPosition);
        SetZoom(defaultZoom);
    }
}

