using UnityEngine;
using Oculus.Interaction;

public class CoordinateSpacePlacer : MonoBehaviour
{
    [Header("Placement Settings")]
    [SerializeField] private GameObject coordinateSpacePrefab;
    [SerializeField] private Transform controllerTransform;
    [SerializeField] private float placementDistance = 2f;
    
    [Header("Input (OVR Input)")]
    [SerializeField] private OVRInput.Button placeButton = OVRInput.Button.One; // A button on right controller
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;
    
    [Header("Manipulation Settings")]
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 10f;
    [SerializeField] private float scaleSpeed = 1f;
    [SerializeField] private float rotationSpeed = 60f;
    
    private GameObject currentPreview;
    private GameObject placedCoordinateSpace;
    private bool isAnchored = false;
    private bool isHoldingSpace = false;
    
    private CoordinateSpaceController coordSpaceController;
    private SimplePlayerController playerController;
    
    public delegate void UnanchorStateChanged(bool isUnanchored);
    public event UnanchorStateChanged OnUnanchorStateChanged;
    
    void Update()
    {
        // Update held space position (takes priority)
        if (isHoldingSpace && placedCoordinateSpace != null)
        {
            UpdateHeldSpacePosition();
            HandleManipulationInput();
        }
        // Update preview position if not placed yet
        else if (currentPreview != null && !isAnchored && placedCoordinateSpace == null)
        {
            UpdatePreviewPosition();
        }
        
        // Handle placement/re-placement with A button
        if (OVRInput.GetDown(placeButton, controller))
        {
            if (!isAnchored)
            {
                // Initial placement or re-placement after unanchoring
                PlaceCoordinateSpace();
            }
        }
    }
    
    private void HandleManipulationInput()
    {
        if (placedCoordinateSpace == null) return;
        
        // Ensure player movement is disabled (redundant safety check)
        if (playerController != null && playerController.enabled)
        {
            playerController.enabled = false;
            Debug.LogWarning("[CoordinateSpacePlacer] Player controller was still enabled during manipulation - disabling it now");
        }
        
        // Right joystick: Scale coordinate space transform (scales everything including shapes)
        Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        if (Mathf.Abs(rightStick.y) > 0.1f)
        {
            float currentScale = placedCoordinateSpace.transform.localScale.x;
            float newScale = currentScale + (rightStick.y * scaleSpeed * Time.deltaTime);
            newScale = Mathf.Clamp(newScale, minScale, maxScale);
            placedCoordinateSpace.transform.localScale = Vector3.one * newScale;
        }
        
        // Left joystick: Rotate coordinate space
        Vector2 leftStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        if (leftStick.magnitude > 0.1f)
        {
            // Rotate around Y axis (horizontal input)
            float yaw = leftStick.x * rotationSpeed * Time.deltaTime;
            
            // Rotate around X axis (vertical input) 
            float pitch = -leftStick.y * rotationSpeed * Time.deltaTime;
            
            placedCoordinateSpace.transform.Rotate(Vector3.up, yaw, Space.World);
            placedCoordinateSpace.transform.Rotate(Vector3.right, pitch, Space.Self);
        }
    }
    
    private void UpdatePreviewPosition()
    {
        if (controllerTransform == null) return;
        
        Vector3 targetPos = controllerTransform.position + controllerTransform.forward * placementDistance;
        currentPreview.transform.position = targetPos;
        currentPreview.transform.rotation = Quaternion.identity; // Keep axes aligned to world
    }
    
    private void UpdateHeldSpacePosition()
    {
        if (controllerTransform == null || placedCoordinateSpace == null) return;
        
        Vector3 targetPos = controllerTransform.position + controllerTransform.forward * placementDistance;
        placedCoordinateSpace.transform.position = targetPos;
        // Don't reset rotation - allow user to manipulate it with joystick
    }
    
    private void PlaceCoordinateSpace()
    {
        if (placedCoordinateSpace == null && currentPreview != null && coordinateSpacePrefab != null)
        {
            // Initial placement: Instantiate the coordinate space at preview location
            placedCoordinateSpace = Instantiate(coordinateSpacePrefab, 
                currentPreview.transform.position, 
                currentPreview.transform.rotation);
            
            // Hide preview after placement
            currentPreview.SetActive(false);
            
            // Get the controller component
            coordSpaceController = placedCoordinateSpace.GetComponent<CoordinateSpaceController>();
        }
        
        // Anchor the coordinate space (works for both initial placement and re-placement)
        isAnchored = true;
        isHoldingSpace = false;
        
        // Re-enable player movement
        EnablePlayerMovement(true);
        
        // Notify listeners (for UI sync)
        OnUnanchorStateChanged?.Invoke(false);
    }
    
    // Called by HandleMenu toggle
    public void ToggleUnanchor(bool unanchored)
    {
        if (placedCoordinateSpace == null) return;
        
        if (unanchored && isAnchored)
        {
            // Unanchor: coordinate space will follow controller
            isAnchored = false;
            isHoldingSpace = true;
            
            // Get controller reference if not already set
            if (coordSpaceController == null)
            {
                coordSpaceController = placedCoordinateSpace.GetComponent<CoordinateSpaceController>();
            }
            
            // Disable auto-extend when manually manipulating to avoid conflicts
            if (coordSpaceController != null)
            {
                coordSpaceController.SetAutoExtend(false);
            }
            
            // Disable player movement
            EnablePlayerMovement(false);
            
            // Notify listeners (for UI sync if needed)
            OnUnanchorStateChanged?.Invoke(true);
        }
        else if (!unanchored && isHoldingSpace)
        {
            // Re-anchor via toggle
            isAnchored = true;
            isHoldingSpace = false;
            
            // Re-enable player movement
            EnablePlayerMovement(true);
            
            // Notify listeners
            OnUnanchorStateChanged?.Invoke(false);
        }
    }
    
    private void EnablePlayerMovement(bool enable)
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<SimplePlayerController>();
        }
        
        if (playerController != null)
        {
            playerController.enabled = enable;
            Debug.Log($"[CoordinateSpacePlacer] Player movement {(enable ? "ENABLED" : "DISABLED")}");
        }
        else
        {
            Debug.LogWarning("[CoordinateSpacePlacer] SimplePlayerController not found - cannot disable movement!");
        }
    }
    
    public void ResetPlacement()
    {
        if (placedCoordinateSpace != null)
        {
            Destroy(placedCoordinateSpace);
            placedCoordinateSpace = null;
        }
        
        isAnchored = false;
        isHoldingSpace = false;
        
        if (currentPreview != null)
        {
            currentPreview.SetActive(true);
        }
    }
    
    void Start()
    {
        // Create preview
        if (coordinateSpacePrefab != null)
        {
            currentPreview = Instantiate(coordinateSpacePrefab);
            currentPreview.name = "CoordinateSpace_Preview";
        }
        
        // Find player controller
        playerController = FindFirstObjectByType<SimplePlayerController>();
        
        if (playerController != null)
        {
            Debug.Log($"[CoordinateSpacePlacer] Found SimplePlayerController on: {playerController.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[CoordinateSpacePlacer] SimplePlayerController not found in scene!");
        }
    }
}

