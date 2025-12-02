using UnityEngine;
using System.Collections.Generic; // REQUIRED: Adds Lists and Stacks
using Oculus.Interaction;
using Oculus.Interaction.Input;

public class MetaPen : MonoBehaviour
{
    [Header("Configuration")]
    public Transform penTip;
    public Material lineMaterial;
    [Range(0.01f, 0.1f)] public float lineWidth = 0.01f;

    [Header("Auto-Filled")]
    public Grabbable grabbable;

    // --- NEW VARIABLES ---
    // Stores the history of lines
    private Stack<GameObject> drawingHistory = new Stack<GameObject>(); 
    // Prevents the undo from triggering 60 times a second
    private bool isUndoTriggerDown = false; 
    // ---------------------

    private GameObject currentDrawing;
    private OVRInput.Controller activeController = OVRInput.Controller.None;
    private bool isDrawing = false;
    private CoordinateSpaceController coordinateSpace;

    void Start()
    {
        if (grabbable == null) grabbable = GetComponent<Grabbable>();
        grabbable.WhenPointerEventRaised += HandlePointerEvent;

        if (lineMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            lineMaterial = new Material(shader);
            lineMaterial.color = Color.red;
        }
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        // CASE 1: We just grabbed the pen
        if (evt.Type == PointerEventType.Select)
        {
            if (evt.Data is MonoBehaviour interactor)
            {
                string interactorName = interactor.name;
                string parentName = interactor.transform.parent ? interactor.transform.parent.name : "NoParent";
                Debug.Log($"[MetaPen] Grabbed by Object: {interactorName}, Parent: {parentName}");

                if (interactorName.Contains("Left") || parentName.Contains("Left"))
                {
                    activeController = OVRInput.Controller.LTouch;
                    Debug.Log("[MetaPen] Identified LEFT Controller");
                }
                else if (interactorName.Contains("Right") || parentName.Contains("Right"))
                {
                    activeController = OVRInput.Controller.RTouch;
                    Debug.Log("[MetaPen] Identified RIGHT Controller");
                }
                else
                {
                    Debug.LogWarning("[MetaPen] Could not identify Left/Right. Defaulting to Right Touch.");
                    activeController = OVRInput.Controller.RTouch;
                }
            }
        }
        // CASE 2: We released the pen
        else if (evt.Type == PointerEventType.Unselect)
        {
            Debug.Log("[MetaPen] Pen Dropped");
            // Important: We must call EndDraw BEFORE clearing the controller so we save the final line
            if (isDrawing) EndDraw(); 
            activeController = OVRInput.Controller.None;
        }
    }

    void Update()
    {
        // Safety check: If pen tip is null, stop everything
        if (penTip == null)
        {
            if (isDrawing)
            {
                // Clean up any active drawing
                if (currentDrawing != null)
                {
                    Destroy(currentDrawing);
                    currentDrawing = null;
                }
                isDrawing = false;
            }
            return;
        }
        
        // If not holding, stop
        if (activeController == OVRInput.Controller.None) return;

        // ---------------------------------------------------------
        // 1. DRAWING LOGIC (Holding Hand)
        // ---------------------------------------------------------
        float triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, activeController);

        if (triggerValue > 0.2f && !isDrawing)
        {
            StartDraw();
        }
        else if (triggerValue < 0.1f && isDrawing)
        {
            EndDraw();
        }

        // ---------------------------------------------------------
        // 2. UNDO LOGIC (Other Hand)
        // ---------------------------------------------------------
        CheckUndoInput();
    }
    
    void OnDestroy()
    {
        // Clean up any active drawing when pen is destroyed
        if (isDrawing && currentDrawing != null)
        {
            Destroy(currentDrawing);
            currentDrawing = null;
        }
        
        // Unsubscribe from events
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }
    }

    void CheckUndoInput()
    {
        // Determine which hand is the "Other" hand
        OVRInput.Controller otherHand = (activeController == OVRInput.Controller.LTouch) 
                                        ? OVRInput.Controller.RTouch 
                                        : OVRInput.Controller.LTouch;

        // Read the trigger of the OTHER hand
        float undoValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, otherHand);

        // If pressed past 50%
        if (undoValue > 0.5f)
        {
            // Only fire once per press (Debounce)
            if (!isUndoTriggerDown)
            {
                UndoLastStroke();
                isUndoTriggerDown = true;
            }
        }
        else
        {
            // Reset when released
            isUndoTriggerDown = false;
        }
    }

    void UndoLastStroke()
    {
        if (drawingHistory.Count > 0)
        {
            GameObject lastStroke = drawingHistory.Pop();
            if (lastStroke != null)
            {
                Destroy(lastStroke);
                Debug.Log("[MetaPen] Undid last stroke. Remaining: " + drawingHistory.Count);
            }
        }
    }

    void StartDraw()
    {
        // Safety check
        if (penTip == null)
        {
            Debug.LogWarning("[MetaPen] Cannot start drawing - penTip is null!");
            return;
        }
        
        isDrawing = true;
        currentDrawing = new GameObject("DrawLine");
        
        // Try to find coordinate space when starting to draw
        if (coordinateSpace == null)
        {
            coordinateSpace = FindCoordinateSpace();
        }
        
        // Parent to pen tip first so trail follows the tip
        currentDrawing.transform.SetParent(penTip, false);
        currentDrawing.transform.localPosition = Vector3.zero;
        currentDrawing.transform.localRotation = Quaternion.identity;
        
        TrailRenderer trail = currentDrawing.AddComponent<TrailRenderer>();
        trail.time = Mathf.Infinity; 
        trail.minVertexDistance = 0.005f; 
        trail.startWidth = lineWidth;
        trail.endWidth = lineWidth;
        trail.material = lineMaterial;
    }

    void EndDraw()
    {
        isDrawing = false;
        if (currentDrawing != null)
        {
            // Get the TrailRenderer
            TrailRenderer trail = currentDrawing.GetComponent<TrailRenderer>();
            
            // Find and parent to coordinate space FIRST
            if (coordinateSpace == null)
            {
                coordinateSpace = FindCoordinateSpace();
            }
            
            if (trail != null && coordinateSpace != null)
            {
                // Bake the trail into a LineRenderer with local positions
                BakeTrailToLineRenderer(trail, coordinateSpace.transform);
            }
            else if (trail != null)
            {
                // No coordinate space found - bake without parenting
                BakeTrailToLineRenderer(trail, null);
            }
            
            // Parent to coordinate space
            if (coordinateSpace != null)
            {
                currentDrawing.transform.SetParent(coordinateSpace.transform, false);
                currentDrawing.transform.localPosition = Vector3.zero;
                currentDrawing.transform.localRotation = Quaternion.identity;
                currentDrawing.transform.localScale = Vector3.one;
            }
            else
            {
                // Fallback: leave at world position if no coordinate space found
                currentDrawing.transform.parent = null;
            }
            
            // Add the finished line to our history stack
            drawingHistory.Push(currentDrawing);
            
            currentDrawing = null;
        }
    }
    
    private void BakeTrailToLineRenderer(TrailRenderer trail, Transform coordinateSpaceTransform)
    {
        // Get all positions from the trail (in world space)
        Vector3[] worldPositions = new Vector3[trail.positionCount];
        trail.GetPositions(worldPositions);
        
        if (worldPositions.Length < 2)
        {
            // Not enough points to make a line
            return;
        }
        
        // Convert to local space relative to coordinate space if available
        Vector3[] localPositions = new Vector3[worldPositions.Length];
        if (coordinateSpaceTransform != null)
        {
            for (int i = 0; i < worldPositions.Length; i++)
            {
                localPositions[i] = coordinateSpaceTransform.InverseTransformPoint(worldPositions[i]);
            }
        }
        else
        {
            localPositions = worldPositions;
        }
        
        // Remove the TrailRenderer
        Destroy(trail);
        
        // Add a LineRenderer instead
        LineRenderer lineRenderer = currentDrawing.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false; // Use local space so it moves/rotates/scales with parent
        lineRenderer.positionCount = localPositions.Length;
        lineRenderer.SetPositions(localPositions);
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = lineMaterial;
        lineRenderer.numCornerVertices = 5;
        lineRenderer.numCapVertices = 5;
    }
    
    private CoordinateSpaceController FindCoordinateSpace()
    {
        // First try to find the placed coordinate space (not the preview)
        var candidates = FindObjectsByType<CoordinateSpaceController>(FindObjectsSortMode.None);
        foreach (var candidate in candidates)
        {
            if (candidate.gameObject.activeInHierarchy && 
                candidate.gameObject.name != "CoordinateSpace_Preview")
            {
                return candidate;
            }
        }
        return null;
    }
}