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
    [SerializeField] private OVRInput.Button unanchorButton = OVRInput.Button.PrimaryHandTrigger; // Grip button (try Three/Four if doesn't work)
    [SerializeField] private OVRInput.Controller controller = OVRInput.Controller.RTouch;
    [SerializeField] private bool useIndexTriggerForUnanchor = false; // Alternative: use index trigger instead of grip
    
    private GameObject currentPreview;
    private GameObject placedCoordinateSpace;
    private bool isAnchored = false;
    private bool isHoldingSpace = false;
    
    void Update()
    {
        // Update held space position (takes priority)
        if (isHoldingSpace && placedCoordinateSpace != null)
        {
            Debug.Log($"Frame update: isHoldingSpace={isHoldingSpace}, calling UpdateHeldSpacePosition");
            UpdateHeldSpacePosition();
        }
        // Update preview position if not placed yet
        else if (currentPreview != null && !isAnchored && placedCoordinateSpace == null)
        {
            UpdatePreviewPosition();
        }
        
        // Debug state
        if (isHoldingSpace)
        {
            Debug.Log($"State: isHoldingSpace={isHoldingSpace}, isAnchored={isAnchored}, placedCoordinateSpace={placedCoordinateSpace != null}");
        }
        
        // Handle placement/anchoring
        if (OVRInput.GetDown(placeButton, controller))
        {
            if (!isAnchored)
            {
                PlaceCoordinateSpace();
            }
        }
        
        // Handle unanchoring - check both button and trigger input
        bool unanchorPressed = false;
        bool unanchorReleased = false;
        
        if (useIndexTriggerForUnanchor)
        {
            // Use index trigger squeeze (0-1 value)
            float triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
            unanchorPressed = triggerValue > 0.8f && !isHoldingSpace;
            unanchorReleased = triggerValue < 0.2f && isHoldingSpace;
        }
        else
        {
            // Use grip button
            unanchorPressed = OVRInput.GetDown(unanchorButton, controller);
            unanchorReleased = OVRInput.GetUp(unanchorButton, controller);
        }
        
        // Debug
        if (unanchorPressed)
        {
            Debug.Log($"Unanchor input detected! isAnchored: {isAnchored}, placedCoordinateSpace: {placedCoordinateSpace != null}");
        }
        
        // Handle unanchoring (hold to grab, release to anchor)
        if (unanchorPressed && isAnchored)
        {
            Debug.Log("Unanchoring coordinate space");
            UnanchorCoordinateSpace();
        }
        else if (unanchorReleased && isHoldingSpace)
        {
            Debug.Log("Anchoring coordinate space");
            AnchorCoordinateSpace();
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
        if (controllerTransform == null)
        {
            Debug.LogWarning("UpdateHeldSpacePosition: controllerTransform is null!");
            return;
        }
        if (placedCoordinateSpace == null)
        {
            Debug.LogWarning("UpdateHeldSpacePosition: placedCoordinateSpace is null!");
            return;
        }
        
        Vector3 targetPos = controllerTransform.position + controllerTransform.forward * placementDistance;
        placedCoordinateSpace.transform.position = targetPos;
        placedCoordinateSpace.transform.rotation = Quaternion.identity; // Keep axes aligned to world
        Debug.Log($"Moving coordinate space to: {targetPos}");
    }
    
    private void PlaceCoordinateSpace()
    {
        if (placedCoordinateSpace != null)
        {
            // Already placed, do nothing
            return;
        }
        
        if (currentPreview != null && coordinateSpacePrefab != null)
        {
            // Instantiate the actual coordinate space at preview location
            placedCoordinateSpace = Instantiate(coordinateSpacePrefab, 
                currentPreview.transform.position, 
                currentPreview.transform.rotation);
            
            // Hide preview after placement
            currentPreview.SetActive(false);
            isAnchored = true;
        }
    }
    
    private void UnanchorCoordinateSpace()
    {
        Debug.Log($"UnanchorCoordinateSpace called. placedCoordinateSpace: {placedCoordinateSpace != null}");
        if (placedCoordinateSpace != null)
        {
            isAnchored = false;
            isHoldingSpace = true;
            Debug.Log($"Unanchored! isHoldingSpace: {isHoldingSpace}, isAnchored: {isAnchored}");
        }
        else
        {
            Debug.LogWarning("Cannot unanchor - placedCoordinateSpace is null!");
        }
    }
    
    private void AnchorCoordinateSpace()
    {
        if (placedCoordinateSpace != null)
        {
            isAnchored = true;
            isHoldingSpace = false;
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
            
            // Make preview slightly transparent (optional - needs material adjustment)
            // For now, just keep it visible
        }
    }
}

