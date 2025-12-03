using UnityEngine;
using System.Collections.Generic;

public class VRFreehandDrawer : MonoBehaviour
{
    [Header("Oculus Settings")]
    // Select "RTouch" for Right Hand or "LTouch" for Left Hand in the Inspector
    public OVRInput.Controller controller = OVRInput.Controller.RTouch; 
    
    [Header("Drawing Settings")]
    public float minDistance = 0.05f; // Minimum distance to add a new point
    public float triggerThreshold = 0.1f; // How hard you press to start drawing (0.1 = light press)
    public GameObject linePrefab;     // The prefab with the LineRenderer
    
    [Header("References")]
    public ShapeCreator shapeCreator; // Drag your ShapeManager here
    public Transform drawingTip;      // The tip of your pen/controller

    // Internal State
    private bool isDrawing = false;
    private GameObject currentLineObject;
    private LineRenderer currentLineRenderer;
    private List<Vector3> recordedPoints = new List<Vector3>();

    void Update()
    {
        // 1. GET INPUT FROM OCULUS SDK
        // We get the float value (0.0 to 1.0) just like in your other script
        // get right controller secondary button press
        // float triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
        // Returns 1.0 if pressed, 0.0 if not
        bool triggerPressed = OVRInput.Get(OVRInput.Button.Two, controller);

        // Convert the float to a simple "Is Pressed?" boolean
        // bool triggerPressed = triggerValue > triggerThreshold;

        // Also keep Spacebar for keyboard testing if needed
        if (Input.GetKey(KeyCode.Space)) triggerPressed = true;

        // 2. START DRAWING
        if (triggerPressed && !isDrawing)
        {
            StartStroke();
        }
        
        // 3. WHILE DRAWING
        else if (triggerPressed && isDrawing)
        {
            UpdateStroke();
        }
        
        // 4. STOP DRAWING & BUILD SHAPE
        else if (!triggerPressed && isDrawing)
        {
            EndStroke();
        }
    }

    void StartStroke()
    {
        isDrawing = true;
        recordedPoints.Clear();
        
        // Spawn the visible line
        currentLineObject = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
        currentLineRenderer = currentLineObject.GetComponent<LineRenderer>();
        
        // IMPORTANT: Ensure the line uses World Space so it stays where you drew it
        currentLineRenderer.useWorldSpace = true;
        currentLineRenderer.positionCount = 0;
    }

    void UpdateStroke()
    {
        Vector3 currentPos = drawingTip.position;

        // Optimization: Only add point if we moved far enough
        if (recordedPoints.Count == 0 || Vector3.Distance(recordedPoints[recordedPoints.Count - 1], currentPos) > minDistance)
        {
            recordedPoints.Add(currentPos);
            
            // Update Visuals
            currentLineRenderer.positionCount = recordedPoints.Count;
            currentLineRenderer.SetPositions(recordedPoints.ToArray());
        }
    }

    void EndStroke()
    {
        isDrawing = false;

        // Check if we have enough points to make a shape (at least 3)
        if (recordedPoints.Count > 3)
        {
            // Check distance between Start Point and End Point
            float distance = Vector3.Distance(recordedPoints[0], recordedPoints[recordedPoints.Count - 1]);
            
            // If the gap is small (less than 20cm), close the loop!
            if (distance < 0.2f) 
            {
                shapeCreator.GenerateFlatPlane(recordedPoints);
                Destroy(currentLineObject); // Remove the line, show the shape
            }
        }
    }
}