using UnityEngine;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Input;

public class MetaPen : MonoBehaviour
{
    [Header("Configuration")]
    public Transform penTip;
    public Material lineMaterial;
    [Range(0.01f, 0.1f)] public float lineWidth = 0.01f;

    [Header("Color Settings")]
    public List<Color> penColors = new List<Color>() { Color.red, Color.blue, Color.green, Color.yellow, Color.white };
    private int currentColorIndex = 0;
    private Color currentDrawColor = Color.red;

    [Header("Auto-Filled")]
    public Grabbable grabbable;

    private Stack<GameObject> drawingHistory = new Stack<GameObject>();
    private bool isUndoTriggerDown = false;
    private bool isColorButtonDown = false;

    // --- NEW: Clear Button Debounce ---
    private bool isClearButtonDown = false;
    // ----------------------------------

    private GameObject currentDrawing;
    private OVRInput.Controller activeController = OVRInput.Controller.None;
    private bool isDrawing = false;
    private CoordinateSpaceController coordinateSpace;

    void Start()
    {
        if (grabbable == null) grabbable = GetComponent<Grabbable>();
        grabbable.WhenPointerEventRaised += HandlePointerEvent;

        if (lineMaterial == null || lineMaterial.shader.name == "Standard")
        {
            var shader = Shader.Find("Sprites/Default");
            lineMaterial = new Material(shader);
        }

        if (penColors.Count > 0) currentDrawColor = penColors[0];
        UpdatePenTipVisual();
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            if (evt.Data is MonoBehaviour interactor)
            {
                string interactorName = interactor.name;
                string parentName = interactor.transform.parent ? interactor.transform.parent.name : "NoParent";
                
                if (interactorName.Contains("Left") || parentName.Contains("Left"))
                    activeController = OVRInput.Controller.LTouch;
                else
                    activeController = OVRInput.Controller.RTouch;
            }
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            if (isDrawing) EndDraw();
            activeController = OVRInput.Controller.None;
        }
    }

    void Update()
    {
        if (penTip == null) return;
        if (activeController == OVRInput.Controller.None) return;

        // 1. DRAWING LOGIC (Index Trigger)
        float triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, activeController);
        if (triggerValue > 0.2f && !isDrawing) StartDraw();
        else if (triggerValue < 0.1f && isDrawing) EndDraw();

        // 2. UNDO LOGIC (Other Hand Trigger)
        CheckUndoInput();

        // 3. CHANGE COLOR LOGIC (Button A / X)
        CheckColorInput();

        // --- NEW: CLEAR ALL LOGIC (Button B / Y) ---
        CheckClearAllInput();
        // -------------------------------------------
    }

    // --- NEW: Clears Global History ---
    void CheckClearAllInput()
    {
        // Button Two maps to "B" on Right Controller and "Y" on Left Controller
        if (OVRInput.Get(OVRInput.Button.Two, activeController))
        {
            if (!isClearButtonDown)
            {
                ClearAllDrawings();
                isClearButtonDown = true;
            }
        }
        else
        {
            isClearButtonDown = false;
        }
    }

    public void ClearAllDrawings()
    {
        // 1. Clear the local history stack for this pen
        drawingHistory.Clear();

        // 2. Find ALL LineRenderers in the entire scene
        // This catches lines drawn by this pen, previous pens, or other pens.
        LineRenderer[] allLines = FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);

        int count = 0;
        foreach (LineRenderer line in allLines)
        {
            // We specifically named our lines "DrawLine" in the StartDraw method.
            // We check this name so we don't accidentally delete other game objects 
            // that might have LineRenderers (like UI pointers or laser beams).
            if (line.gameObject.name == "DrawLine")
            {
                Destroy(line.gameObject);
                count++;
            }
        }

        Debug.Log($"[MetaPen] Cleared {count} drawings from the scene.");
    }
    // ----------------------------------

    void CheckColorInput()
    {
        if (OVRInput.Get(OVRInput.Button.One, activeController))
        {
            if (!isColorButtonDown)
            {
                CycleColor();
                isColorButtonDown = true;
            }
        }
        else isColorButtonDown = false;
    }

    public void CycleColor()
    {
        if (penColors.Count == 0) return;
        currentColorIndex = (currentColorIndex + 1) % penColors.Count;
        currentDrawColor = penColors[currentColorIndex];
        UpdatePenTipVisual();
    }

    void UpdatePenTipVisual()
    {
        Renderer tipRenderer = penTip.GetComponent<Renderer>();
        if (tipRenderer != null) tipRenderer.material.color = currentDrawColor;
    }

    void CheckUndoInput()
    {
        OVRInput.Controller otherHand = (activeController == OVRInput.Controller.LTouch)
                                        ? OVRInput.Controller.RTouch
                                        : OVRInput.Controller.LTouch;

        if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, otherHand) > 0.5f)
        {
            if (!isUndoTriggerDown)
            {
                UndoLastStroke();
                isUndoTriggerDown = true;
            }
        }
        else isUndoTriggerDown = false;
    }

    void UndoLastStroke()
    {
        if (drawingHistory.Count > 0)
        {
            GameObject lastStroke = drawingHistory.Pop();
            if (lastStroke != null) Destroy(lastStroke);
        }
    }

    void StartDraw()
    {
        isDrawing = true;
        // IMPORTANT: We name the object "DrawLine" so ClearAllDrawings can find it later
        currentDrawing = new GameObject("DrawLine");

        if (coordinateSpace == null) coordinateSpace = FindCoordinateSpace();

        currentDrawing.transform.SetParent(penTip, false);
        currentDrawing.transform.localPosition = Vector3.zero;
        currentDrawing.transform.localRotation = Quaternion.identity;

        TrailRenderer trail = currentDrawing.AddComponent<TrailRenderer>();
        trail.time = Mathf.Infinity;
        trail.minVertexDistance = 0.005f;
        trail.startWidth = lineWidth;
        trail.endWidth = lineWidth;
        trail.material = lineMaterial;
        trail.startColor = currentDrawColor;
        trail.endColor = currentDrawColor;
    }

    void EndDraw()
    {
        isDrawing = false;
        if (currentDrawing != null)
        {
            TrailRenderer trail = currentDrawing.GetComponent<TrailRenderer>();
            if (coordinateSpace == null) coordinateSpace = FindCoordinateSpace();

            if (trail != null && coordinateSpace != null)
                BakeTrailToLineRenderer(trail, coordinateSpace.transform);
            else if (trail != null)
                BakeTrailToLineRenderer(trail, null);

            if (coordinateSpace != null)
            {
                currentDrawing.transform.SetParent(coordinateSpace.transform, false);
                currentDrawing.transform.localPosition = Vector3.zero;
                currentDrawing.transform.localRotation = Quaternion.identity;
                currentDrawing.transform.localScale = Vector3.one;
            }
            else currentDrawing.transform.parent = null;

            drawingHistory.Push(currentDrawing);
            currentDrawing = null;
        }
    }

    private void BakeTrailToLineRenderer(TrailRenderer trail, Transform coordinateSpaceTransform)
    {
        Vector3[] worldPositions = new Vector3[trail.positionCount];
        trail.GetPositions(worldPositions);
        if (worldPositions.Length < 2) return;

        Vector3[] localPositions = new Vector3[worldPositions.Length];
        if (coordinateSpaceTransform != null)
        {
            for (int i = 0; i < worldPositions.Length; i++)
                localPositions[i] = coordinateSpaceTransform.InverseTransformPoint(worldPositions[i]);
        }
        else localPositions = worldPositions;

        Color bakedColor = trail.startColor;
        Destroy(trail);

        LineRenderer lineRenderer = currentDrawing.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = localPositions.Length;
        lineRenderer.SetPositions(localPositions);
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = lineMaterial;
        lineRenderer.numCornerVertices = 5;
        lineRenderer.numCapVertices = 5;
        lineRenderer.startColor = bakedColor;
        lineRenderer.endColor = bakedColor;
    }

    private CoordinateSpaceController FindCoordinateSpace()
    {
        var candidates = FindObjectsByType<CoordinateSpaceController>(FindObjectsSortMode.None);
        foreach (var candidate in candidates)
        {
            if (candidate.gameObject.activeInHierarchy && candidate.gameObject.name != "CoordinateSpace_Preview")
                return candidate;
        }
        return null;
    }

    void OnDestroy()
    {
        if (isDrawing && currentDrawing != null) Destroy(currentDrawing);
        if (grabbable != null) grabbable.WhenPointerEventRaised -= HandlePointerEvent;
    }
}