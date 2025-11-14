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
    
    private GameObject currentPreview;
    private GameObject placedCoordinateSpace;
    
    void Update()
    {
        // Update preview position
        if (currentPreview != null && placedCoordinateSpace == null)
        {
            UpdatePreviewPosition();
        }
        
        // Handle placement
        if (OVRInput.GetDown(placeButton, controller))
        {
            PlaceCoordinateSpace();
        }
    }
    
    private void UpdatePreviewPosition()
    {
        if (controllerTransform == null) return;
        
        Vector3 targetPos = controllerTransform.position + controllerTransform.forward * placementDistance;
        currentPreview.transform.position = targetPos;
        currentPreview.transform.rotation = Quaternion.identity; // Keep axes aligned to world
    }
    
    private void PlaceCoordinateSpace()
    {
        if (placedCoordinateSpace != null)
        {
            // Already placed, do nothing (or allow re-placing by destroying old one)
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
        }
    }
    
    public void ResetPlacement()
    {
        if (placedCoordinateSpace != null)
        {
            Destroy(placedCoordinateSpace);
            placedCoordinateSpace = null;
        }
        
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

