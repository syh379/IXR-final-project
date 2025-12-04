using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using Text = TMPro.TMP_Text;

public class HandleMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CoordinateSpaceController coordinateSpace;
    [SerializeField] private CoordinateSpacePlacer coordinateSpacePlacer; // For unanchoring control 
    [SerializeField] private GameObject isdkRayGrabInteractionTemplate; // Prefab in-scene to clone for each spawned shape

    [Header("Shape Selection (ToggleGroup)")]
    [SerializeField] private ToggleGroup shapeToggleGroup;
    [SerializeField] private Toggle cubeToggle;
    [SerializeField] private Toggle sphereToggle;
    [SerializeField] private Toggle cylinderToggle;
    [SerializeField] private Toggle deleteToggle;
    [SerializeField] private Toggle coneToggle;
    [SerializeField] private Toggle doubleConeToggle;

    [Header("Coordinate Space Control")]
    [SerializeField] private Toggle unanchorToggle; // Toggle to unanchor coordinate space
    [SerializeField] private Toggle autoExtendToggle; // Toggle for auto-extending axes

    [Header("Shape Settings")]
    [SerializeField] private Vector3 shapeScale = Vector3.one * 1f;
    [SerializeField] private Material shapeMaterial;

    [Header("Pen Settings")]
    [SerializeField] private Toggle penToggle;
    [SerializeField] private GameObject penPrefab;
    [SerializeField] private float penSpawnDistance = 0.5f;

    // --- Instructions UI ---
    [Header("Instructions UI")]
    [SerializeField] private Text instructionTitleText;      // UI Text for title
    [SerializeField] private Text instructionObjectiveText;  // UI Text for objective
    [SerializeField] private Text instructionBodyText;       // UI Text for instructions (numbered list)
    [SerializeField] private Button nextStepButton;          // Next step
    [SerializeField] private Button prevStepButton;          // Previous step
    [SerializeField] private TextAsset instructionsJson;     // Optional JSON asset to populate steps

    private GameObject currentPenInstance;

    private GameObject originShape;
    private Vector3 currentShapeOffset = Vector3.zero;


    // --- Instruction data/state ---
    [Serializable]
    private class Step
    {
        public string title;
        public string objective;
        public string[] instructions;
    }
    [Serializable]
    private class StepsWrapper
    {
        public Step[] steps;
    }
    [SerializeField]
    private Step[] steps =
    {
        new Step
        {
            title = "Step 1 - The Setup",
            objective = "Visualize the 3D workspace.",
            instructions = new[]
            {
                "Trace the two cones inserted into the space",
                "Draw a vertical line through the middle",
                "Draw a cutting plane and grab it",
            }
        },
        new Step
        {
            title = "Step 2 - The Circle",
            objective = "Generate a perfectly round, closed curve.",
            instructions = new[]
            {
                "Position the cutting plane perfectly horizontal.",
                "Ensure the plane is perpendicular (90°) to the vertical Axis.",
                "Slice through one cone to reveal a perfect Circle."
            }
        },
        new Step
        {
            title = "Step 3 - The Ellipse",
            objective = "Generate an oval-shaped, closed curve.",
            instructions = new[]
            {
                "Start with a horizontal plane.",
                "Tilt the plane slightly so it is diagonal.",
                "Slice through one cone only, ensuring the cut goes in one side and out the other."
            }
        },
        new Step
        {
            title = "Step 4 - The Parabola",
            objective = "Generate an open U-shaped curve.",
            instructions = new[]
            {
                "Tilt the plane further until its angle matches the slope of the cone's side.",
                "Ensure the plane is parallel to the Generator (the slant edge).",
                "Slice the cone; the curve will remain open because the plane and cone edge never intersect."
            }
        },
        new Step
        {
            title = "Step 5 - The Hyperbola",
            objective = "Generate two separate, mirrored curves.",
            instructions = new[]
            {
                "Tilt the plane until it is vertical (steepest angle).",
                "Slice straight down parallel to the vertical Axis.",
                "Intersect both the top cone and the bottom cone to create two separate curves."
            }
        }
    };

    private int _stepIndex = 0;

    void Awake()
    {
        ResolveCoordinateSpace();

        // Load instructions from JSON if provided
        if (instructionsJson != null)
        {
            try
            {
                string json = instructionsJson.text;
                string trimmed = json.TrimStart();
                if (trimmed.StartsWith("["))
                {
                    json = "{\"steps\":" + json + "}";
                    var wrapper = JsonUtility.FromJson<StepsWrapper>(json);
                    steps = wrapper != null ? wrapper.steps : steps;
                }
                else
                {
                    var wrapper = JsonUtility.FromJson<StepsWrapper>(json);
                    steps = wrapper != null ? wrapper.steps : steps;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"HandleMenu: Failed to parse instructions JSON: {ex.Message}");
            }
        }

        // Register listeners
        if (cubeToggle != null) cubeToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(cubeToggle, isOn));
        if (sphereToggle != null) sphereToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(sphereToggle, isOn));
        if (cylinderToggle != null) cylinderToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(cylinderToggle, isOn));
        if (deleteToggle != null) deleteToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(deleteToggle, isOn));
        if (coneToggle != null) coneToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(coneToggle, isOn));
        if (doubleConeToggle != null) doubleConeToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(doubleConeToggle, isOn));

        // Pen toggle
        if (penToggle != null) penToggle.onValueChanged.AddListener(isOn => OnPenToggleChanged(isOn));

        // Coordinate space controls
        if (unanchorToggle != null) unanchorToggle.onValueChanged.AddListener(isOn => OnUnanchorToggleChanged(isOn));
        if (autoExtendToggle != null) autoExtendToggle.onValueChanged.AddListener(isOn => OnAutoExtendToggleChanged(isOn));

        // Subscribe to unanchor state changes for UI sync
        if (coordinateSpacePlacer == null)
        {
            coordinateSpacePlacer = FindFirstObjectByType<CoordinateSpacePlacer>();
        }
        if (coordinateSpacePlacer != null)
        {
            coordinateSpacePlacer.OnUnanchorStateChanged += SyncUnanchorToggle;
        }

        // Step navigation buttons
        if (nextStepButton != null) nextStepButton.onClick.AddListener(OnNextStepClicked);
        if (prevStepButton != null) prevStepButton.onClick.AddListener(OnPrevStepClicked);

        // Initialize instruction UI
        UpdateInstructionUI();
    }

    void OnDestroy()
    {
        if (cubeToggle != null) cubeToggle.onValueChanged.RemoveAllListeners();
        if (sphereToggle != null) sphereToggle.onValueChanged.RemoveAllListeners();
        if (cylinderToggle != null) cylinderToggle.onValueChanged.RemoveAllListeners();
        if (deleteToggle != null) deleteToggle.onValueChanged.RemoveAllListeners();
        if (coneToggle != null) coneToggle.onValueChanged.RemoveAllListeners();
        if (doubleConeToggle != null) doubleConeToggle.onValueChanged.RemoveAllListeners();

        if (penToggle != null) penToggle.onValueChanged.RemoveAllListeners();
        if (unanchorToggle != null) unanchorToggle.onValueChanged.RemoveAllListeners();
        if (autoExtendToggle != null) autoExtendToggle.onValueChanged.RemoveAllListeners();

        if (nextStepButton != null) nextStepButton.onClick.RemoveAllListeners();
        if (prevStepButton != null) prevStepButton.onClick.RemoveAllListeners();

        if (coordinateSpacePlacer != null)
        {
            coordinateSpacePlacer.OnUnanchorStateChanged -= SyncUnanchorToggle;
        }
    }

    void LateUpdate()
    {
        ResolveCoordinateSpace();

        if (originShape != null && coordinateSpace != null && originShape.transform.parent != coordinateSpace.transform)
        {
            originShape.transform.SetParent(coordinateSpace.transform, false);
            originShape.transform.localPosition = currentShapeOffset;
            originShape.transform.localRotation = Quaternion.identity;
        }
    }

    // --- Step navigation logic ---
    private void OnNextStepClicked()
    {
        SetStepIndex(_stepIndex + 1);
    }

    private void OnPrevStepClicked()
    {
        SetStepIndex(_stepIndex - 1);
    }

    private void SetStepIndex(int newIndex)
    {
        int maxStep = (steps != null && steps.Length > 0) ? steps.Length - 1 : 0;
        int clamped = Mathf.Clamp(newIndex, 0, maxStep);
        if (clamped != _stepIndex)
        {
            _stepIndex = clamped;
            UpdateInstructionUI();
        }
        else
        {
            UpdateInstructionUI();
        }
    }

    private void UpdateInstructionUI()
    {
        bool hasSteps = steps != null && steps.Length > 0;
        Step current = hasSteps ? steps[_stepIndex] : null;

        if (instructionTitleText != null)
        {
            instructionTitleText.text = hasSteps && !string.IsNullOrEmpty(current.title) ? current.title : string.Empty;
        }
        if (instructionObjectiveText != null)
        {
            instructionObjectiveText.text = hasSteps && !string.IsNullOrEmpty(current.objective) ? current.objective : string.Empty;
        }
        if (instructionBodyText != null)
        {
            if (hasSteps && current.instructions != null && current.instructions.Length > 0)
            {
                // Build numbered list
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < current.instructions.Length; i++)
                {
                    sb.Append(i + 1).Append(". ").Append(current.instructions[i]);
                    if (i < current.instructions.Length - 1) sb.Append('\n');
                }
                instructionBodyText.text = sb.ToString();
            }
            else
            {
                instructionBodyText.text = string.Empty;
            }
        }

        // Enable/disable step buttons appropriately
        if (prevStepButton != null) prevStepButton.interactable = hasSteps && _stepIndex > 0;
        if (nextStepButton != null) nextStepButton.interactable = hasSteps && _stepIndex < (steps.Length - 1);
    }

    // --- NEW PEN LOGIC ---
    private void OnPenToggleChanged(bool isOn)
    {
        if (isOn)
        {
            SpawnPen();
        }
        else
        {
            DestroyPen();
        }
    }

    private void SpawnPen()
    {
        // 1. Check if pen already exists
        if (currentPenInstance != null) return;

        // 2. Check if we have a prefab
        if (penPrefab == null)
        {
            Debug.LogWarning("HandleMenu: Pen Prefab is not assigned in Inspector!");
            return;
        }

        // 3. Find User's Head (Main Camera)
        Transform headTransform = Camera.main.transform;

        // 4. Calculate Position (Front of face)
        Vector3 spawnPos = headTransform.position + (headTransform.forward * penSpawnDistance);

        // 5. Instantiate (We do NOT parent to coordinate space, so you can pick it up freely)
        currentPenInstance = Instantiate(penPrefab, spawnPos, Quaternion.identity);

        // Optional: Rotate pen to face the user roughly, or keep identity
        // currentPenInstance.transform.LookAt(headTransform); 
    }

    private void DestroyPen()
    {
        if (currentPenInstance != null)
        {
            Destroy(currentPenInstance);
            currentPenInstance = null;
        }
    }
    // ---------------------

    // --- UNANCHOR COORDINATE SPACE LOGIC ---
    private void OnUnanchorToggleChanged(bool isOn)
    {
        if (coordinateSpacePlacer == null)
        {
            // Try to find it
            coordinateSpacePlacer = FindFirstObjectByType<CoordinateSpacePlacer>();
            if (coordinateSpacePlacer == null)
            {
                Debug.LogWarning("HandleMenu: CoordinateSpacePlacer not found in scene!");
                return;
            }
        }

        coordinateSpacePlacer.ToggleUnanchor(isOn);
    }

    private void OnAutoExtendToggleChanged(bool isOn)
    {
        ResolveCoordinateSpace();

        if (coordinateSpace != null)
        {
            coordinateSpace.SetAutoExtend(isOn);
        }
    }

    private void SyncUnanchorToggle(bool isUnanchored)
    {
        if (unanchorToggle != null)
        {
            // Temporarily remove listener to avoid triggering callback
            unanchorToggle.onValueChanged.RemoveListener(OnUnanchorToggleChanged);
            unanchorToggle.isOn = isUnanchored;
            unanchorToggle.onValueChanged.AddListener(isOn => OnUnanchorToggleChanged(isOn));
        }
    }
    // ---------------------

    private void OnShapeToggleChanged(Toggle source, bool isOn)
    {
        if (!isOn) return;

        if (deleteToggle != null && source == deleteToggle)
        {
            DestroyOriginShape();
            return;
        }
        if (cubeToggle != null && source == cubeToggle)
        {
            CreateOrReplacePrimitive(PrimitiveType.Cube, "OriginCube");
            return;
        }
        if (sphereToggle != null && source == sphereToggle)
        {
            CreateOrReplacePrimitive(PrimitiveType.Sphere, "OriginSphere");
            return;
        }
        if (cylinderToggle != null && source == cylinderToggle)
        {
            CreateOrReplacePrimitive(PrimitiveType.Cylinder, "OriginCylinder");
            return;
        }
        if (coneToggle != null && source == coneToggle)
        {
            CreateOrReplaceCone("OriginCone");
            return;
        }
        if (doubleConeToggle != null && source == doubleConeToggle)
        {
            CreateOrReplaceDoubleCone("OriginDoubleCone");
            return;
        }
    }

    private void CreateOrReplacePrimitive(PrimitiveType type, string shapeName)
    {
        ResolveCoordinateSpace();

        if (coordinateSpace == null)
        {
            Debug.LogWarning("HandleMenu: CoordinateSpace reference not set or not found (preview/placed).");
            return;
        }

        if (originShape != null)
        {
            Destroy(originShape);
            originShape = null;
        }

        originShape = GameObject.CreatePrimitive(type);
        originShape.name = shapeName;
        originShape.transform.SetParent(coordinateSpace.transform, false);

        originShape.transform.localRotation = Quaternion.identity;
        originShape.transform.localScale = shapeScale;

        currentShapeOffset = ComputePositiveSideOffset(originShape);
        originShape.transform.localPosition = currentShapeOffset;

        if (shapeMaterial != null)
        {
            var renderer = originShape.GetComponent<Renderer>();
            if (renderer != null) renderer.material = shapeMaterial;
        }

        // Configure grabbable/interaction components (Rigidbody, Grabbable, transformers, template wiring)
        EnsureGrabbableSetup(originShape);
    }

    private void CreateOrReplaceCone(string shapeName)
    {
        ResolveCoordinateSpace();

        if (coordinateSpace == null) return;

        if (originShape != null)
        {
            Destroy(originShape);
            originShape = null;
        }

        originShape = new GameObject(shapeName);
        originShape.transform.SetParent(coordinateSpace.transform, false);

        var mf = originShape.AddComponent<MeshFilter>();
        var mr = originShape.AddComponent<MeshRenderer>();

        // Assumes you have ProceduralCone class elsewhere
        mf.sharedMesh = ProceduralCone.Build(0.5f, 1f, 32, capBase: true);

        var mc = originShape.AddComponent<MeshCollider>();
        mc.sharedMesh = mf.sharedMesh;
        mc.convex = true;

        if (shapeMaterial != null) mr.material = shapeMaterial;

        originShape.transform.localRotation = Quaternion.identity;
        originShape.transform.localScale = shapeScale;

        currentShapeOffset = ComputePositiveSideOffset(originShape);
        originShape.transform.localPosition = currentShapeOffset;

        EnsureGrabbableSetup(originShape);
    }

    private void CreateOrReplaceDoubleCone(string shapeName)
    {
        ResolveCoordinateSpace();

        if (coordinateSpace == null)
        {
            Debug.LogWarning("HandleMenu: CoordinateSpace reference not set or not found (preview/placed).");
            return;
        }

        if (originShape != null)
        {
            Destroy(originShape);
            originShape = null;
        }

        originShape = new GameObject(shapeName);
        originShape.transform.SetParent(coordinateSpace.transform, false);

        var mf = originShape.AddComponent<MeshFilter>();
        var mr = originShape.AddComponent<MeshRenderer>();

        mf.sharedMesh = ProceduralDoubleCone.Build(0.5f, 1f, 32);

        var mc = originShape.AddComponent<MeshCollider>();
        mc.sharedMesh = mf.sharedMesh;
        mc.convex = true;

        if (shapeMaterial != null) mr.material = shapeMaterial;

        originShape.transform.localRotation = Quaternion.identity;
        originShape.transform.localScale = shapeScale;

        currentShapeOffset = ComputePositiveSideOffset(originShape);
        originShape.transform.localPosition = currentShapeOffset;

        EnsureGrabbableSetup(originShape);
    }

    private Vector3 ComputePositiveSideOffset(GameObject shape)
    {
        var mf = shape.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
            return Vector3.zero;

        var b = mf.sharedMesh.bounds;
        var ext = b.extents;
        var s = shape.transform.localScale;
        return new Vector3(ext.x * s.x, ext.y * s.y, ext.z * s.z);
    }

    /// <summary>
    /// Clone template under shape and wire Grabbable, ColliderSurface, Collider and PointableElement dependencies.
    /// Fixes "collider surface" and "pointable element" unassigned warnings.
    /// Also assigns Ray Interaction script's pointableElement to the shape's PointableElement.
    /// </summary>
    private void AttachAndWireISDKRayGrabInteraction(GameObject parentShape, Grabbable grabbable)
    {
        if (parentShape == null || grabbable == null) return;

        // Ensure the shape has a Collider
        var shapeCollider = parentShape.GetComponent<Collider>();
        if (shapeCollider == null)
        {
            var mf = parentShape.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var meshCol = parentShape.AddComponent<MeshCollider>();
                meshCol.sharedMesh = mf.sharedMesh;
                meshCol.convex = true;
                shapeCollider = meshCol;
            }
            else
            {
                shapeCollider = parentShape.AddComponent<BoxCollider>();
            }
        }

        // Ensure a PointableElement exists on the shape
        var pointable = parentShape.GetComponent<PointableElement>();
        if (pointable == null) pointable = parentShape.AddComponent<PointableElement>();

        // Ensure a ColliderSurface exists (attach to the shape so it references its collider)
        var surface = parentShape.GetComponent<ColliderSurface>();
        if (surface == null) surface = parentShape.AddComponent<ColliderSurface>();
        // ColliderSurface has a private serialized field; assign via reflection for runtime wiring
        var surfaceType = typeof(ColliderSurface);
        var colliderField = surfaceType.GetField("_collider", BindingFlags.NonPublic | BindingFlags.Instance);
        if (colliderField != null) colliderField.SetValue(surface, shapeCollider);

        // Get template to clone
        var template = isdkRayGrabInteractionTemplate != null ? isdkRayGrabInteractionTemplate : GameObject.Find("ISDK_RayGrabInteraction");
        if (template == null)
        {
            Debug.LogWarning("HandleMenu: ISDK_RayGrabInteraction template not found (check serialized field or scene). Skipping attach.");
            return;
        }

        var clone = Instantiate(template, parentShape.transform, false);
        clone.transform.localPosition = Vector3.zero;
        clone.transform.localRotation = Quaternion.identity;
        clone.name = "ISDK_RayGrabInteraction";

        var cloneRayInteractable = clone.GetComponent<RayInteractable>();
        cloneRayInteractable.InjectOptionalPointableElement(grabbable);

        // Walk components on the cloned template and assign references
        var comps = clone.GetComponents<MonoBehaviour>();
        foreach (var comp in comps)
        {
            if (comp == null) continue;
            var t = comp.GetType();

            // Assign fields
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in fields)
            {
                if (f.FieldType == typeof(Grabbable))
                {
                    f.SetValue(comp, grabbable);
                    continue;
                }
                if (f.FieldType == typeof(ColliderSurface))
                {
                    f.SetValue(comp, surface);
                    continue;
                }
                if (f.FieldType == typeof(Collider))
                {
                    f.SetValue(comp, shapeCollider);
                    continue;
                }
                if (f.FieldType == typeof(PointableElement))
                {
                    f.SetValue(comp, pointable);
                    continue;
                }
            }

            // Assign properties (when writable)
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var p in props)
            {
                if (!p.CanWrite) continue;

                if (p.PropertyType == typeof(Grabbable))
                {
                    try { p.SetValue(comp, grabbable, null); } catch { }
                    continue;
                }
                if (p.PropertyType == typeof(ColliderSurface))
                {
                    try { p.SetValue(comp, surface, null); } catch { }
                    continue;
                }
                if (p.PropertyType == typeof(Collider))
                {
                    try { p.SetValue(comp, shapeCollider, null); } catch { }
                    continue;
                }
                if (p.PropertyType == typeof(PointableElement))
                {
                    try { p.SetValue(comp, pointable, null); } catch { }
                    continue;
                }

                // If a script exposes a property named "pointableElement" regardless of type checks, attempt assignment
                if (p.Name.ToLower().Contains("pointableelement") && p.CanWrite)
                {
                    try { p.SetValue(comp, pointable, null); } catch { }
                }
            }
        }
    }

    private void EnsureGrabbableSetup(GameObject shape)
    {
        if (shape == null) return;
        try
        {
            var rb = shape.GetComponent<Rigidbody>();
            if (rb == null) rb = shape.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true; // kinematic for ray/hand interactions

            var grabbable = shape.GetComponent<Grabbable>();
            if (grabbable == null) grabbable = shape.AddComponent<Grabbable>();

            // Assign the shape's Rigidbody to the Grabbable via reflection (handles private serialized field)
            grabbable.InjectOptionalRigidbody(rb);

            // Ensure PointableElement exists on the same shape (used by ray interaction)
            var pointable = shape.GetComponent<PointableElement>();
            if (pointable == null) pointable = shape.AddComponent<PointableElement>();

            if (shape.GetComponent<GrabInteractable>() == null)
                shape.AddComponent<GrabInteractable>();

            var grabInteractable = shape.GetComponent<GrabInteractable>();
            grabInteractable.InjectRigidbody(rb);

            if (shape.GetComponent<GrabFreeTransformer>() == null)
                shape.AddComponent<GrabFreeTransformer>();

            AttachAndWireISDKRayGrabInteraction(shape, grabbable);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"HandleMenu: Failed to auto-wire grabbable components: {ex.Message}");
        }
    }

    private void DestroyOriginShape()
    {
        if (originShape != null)
        {
            Destroy(originShape);
            originShape = null;
            currentShapeOffset = Vector3.zero;
        }
    }

    private void ResolveCoordinateSpace()
    {
        if (coordinateSpace != null && coordinateSpace.gameObject.activeInHierarchy) return;

        var previewGO = GameObject.Find("CoordinateSpace_Preview");
        if (previewGO != null && previewGO.activeInHierarchy)
        {
            var previewCtrl = previewGO.GetComponent<CoordinateSpaceController>();
            if (previewCtrl != null)
            {
                coordinateSpace = previewCtrl;
                return;
            }
        }

#if UNITY_2023_1_OR_NEWER
        var candidates = UnityEngine.Object.FindObjectsByType<CoordinateSpaceController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var candidates = Object.FindObjectsOfType<CoordinateSpaceController>();
#endif
        var placed = candidates.FirstOrDefault(c => c != null && c.gameObject.activeInHierarchy && c.gameObject.name != "CoordinateSpace_Preview");
        if (placed != null)
        {
            coordinateSpace = placed;
        }
    }

    // Public compatibility
    public void OnRemoveShapeClicked() => DestroyOriginShape();
    public void OnAddCubeClicked() => CreateOrReplacePrimitive(PrimitiveType.Cube, "OriginCube");
    public void OnAddSphereClicked() => CreateOrReplacePrimitive(PrimitiveType.Sphere, "OriginSphere");
    public void OnAddCylinderClicked() => CreateOrReplacePrimitive(PrimitiveType.Cylinder, "OriginCylinder");
    public void OnAddConeClicked() => CreateOrReplaceCone("OriginCone");
}