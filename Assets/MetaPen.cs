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
        isDrawing = true;
        currentDrawing = new GameObject("DrawLine");
        
        TrailRenderer trail = currentDrawing.AddComponent<TrailRenderer>();
        trail.time = Mathf.Infinity; 
        trail.minVertexDistance = 0.005f; 
        trail.startWidth = lineWidth;
        trail.endWidth = lineWidth;
        trail.material = lineMaterial;

        currentDrawing.transform.position = penTip.position;
        currentDrawing.transform.rotation = Quaternion.identity;
        currentDrawing.transform.parent = penTip;
    }

    void EndDraw()
    {
        isDrawing = false;
        if (currentDrawing != null)
        {
            currentDrawing.transform.parent = null;
            
            // --- NEW: Add the finished line to our history stack ---
            drawingHistory.Push(currentDrawing);
            
            currentDrawing = null;
        }
    }
}