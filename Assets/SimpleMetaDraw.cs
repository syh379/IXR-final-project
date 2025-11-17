using UnityEngine;
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

    private GameObject currentDrawing;
    private OVRInput.Controller activeController = OVRInput.Controller.None;
    private bool isDrawing = false;


    public string drawingTag = "DrawLine";


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
            // The 'Data' is the Interactor (the hand/controller)
            if (evt.Data is MonoBehaviour interactor)
            {
                // LOGGING: Let's see what exactly grabbed us
                string interactorName = interactor.name;
                string parentName = interactor.transform.parent ? interactor.transform.parent.name : "NoParent";
                Debug.Log($"[MetaPen] Grabbed by Object: {interactorName}, Parent: {parentName}");

                // IMPROVED DETECTION: Check the object name AND the parent name for "Left" or "Right"
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
                    // Fallback: If we can't tell, assume Right Hand (common for testing)
                    Debug.LogWarning("[MetaPen] Could not identify Left/Right. Defaulting to Right Touch.");
                    activeController = OVRInput.Controller.RTouch;
                }
            }
        }
        // CASE 2: We released the pen
        else if (evt.Type == PointerEventType.Unselect)
        {
            Debug.Log("[MetaPen] Pen Dropped");
            activeController = OVRInput.Controller.None;
            EndDraw();
        }
    }

    void Update()
    {
        // If not holding, stop
        if (activeController == OVRInput.Controller.None) return;

        // Read Trigger Value (0.0 to 1.0)
        float triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, activeController);

        // LOGGING: Uncomment this line if you suspect the trigger isn't working at all
        // Debug.Log($"Trigger Value: {triggerValue}");

        if (triggerValue > 0.2f && !isDrawing)
        {
            Debug.Log("[MetaPen] Start Drawing");
            StartDraw();
        }
        else if (triggerValue < 0.1f && isDrawing)
        {
            Debug.Log("[MetaPen] Stop Drawing");
            EndDraw();
        }
    }

    void StartDraw()
    {
        isDrawing = true;
        currentDrawing = new GameObject("DrawLine");
        
        // Create the visual line
        TrailRenderer trail = currentDrawing.AddComponent<TrailRenderer>();
        trail.time = Mathf.Infinity; 
        trail.minVertexDistance = 0.005f; // High fidelity
        trail.startWidth = lineWidth;
        trail.endWidth = lineWidth;
        trail.material = lineMaterial;

        // Important: Reset position to tip before parenting
        currentDrawing.transform.position = penTip.position;
        currentDrawing.transform.rotation = Quaternion.identity;
        currentDrawing.transform.parent = penTip;
    }


    // Replace your old EndDraw function with this one:
    void EndDraw()
    {
        isDrawing = false;
        if (currentDrawing != null)
        {
            // 1. Detach from pen
            currentDrawing.transform.parent = null;

            // 2. BAKE THE MESH: Turn the visual trail into a physical mesh
            TrailRenderer trail = currentDrawing.GetComponent<TrailRenderer>();
            Mesh mesh = new Mesh();
            trail.BakeMesh(mesh, Camera.main, true); // Bake the trail geometry
            
            // 3. Add a MeshCollider so we can touch it
            MeshCollider collider = currentDrawing.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = true; // Required for interaction with triggers
            collider.isTrigger = true; // Make it a trigger so it doesn't block movement

            // 4. Tag it so the eraser knows what it is
            currentDrawing.tag = drawingTag;

            currentDrawing = null;
        }
    }
}