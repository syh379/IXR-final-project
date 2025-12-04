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

    [Header("Tutorial")]
    [SerializeField] private Toggle tutorialToggle;          // Toggle in the list that shows/hides the tutorial
    [SerializeField] private GameObject tutorialBoard;       // Board GameObject that contains the tutorial UI

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
        Debug.Log("steps length: " + (steps != null ? steps.Length : 0));

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

        // Tutorial toggle - show/hide tutorial board
        if (tutorialToggle != null)
        {
            tutorialToggle.onValueChanged.AddListener(OnTutorialToggleChanged);
        }

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

        // Initialize tutorial board hidden state if toggle exists
        if (tutorialBoard != null && tutorialToggle != null)
        {
            tutorialBoard.SetActive(tutorialToggle.isOn);
        }

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

        if (tutorialToggle != null) tutorialToggle.onValueChanged.RemoveAllListeners();

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

    private void OnEnable()
    {
        UpdateInstructionUI();
    }

    // --- Tutorial show/hide ---
    private void OnTutorialToggleChanged(bool isOn)
    {
        if (tutorialBoard == null)
        {
            Debug.LogWarning("HandleMenu: TutorialBoard GameObject is not assigned.");
            return;
        }

        tutorialBoard.SetActive(isOn);

        // Always refresh text content when the toggle changes
        UpdateInstructionUI();
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
        _stepIndex = clamped;
        UpdateInstructionUI();
    }

    private void UpdateInstructionUI()
    {
        // Diagnostics to confirm state
        Debug.Log($"HandleMenu: stepsLen={(steps != null ? steps.Length : 0)}, stepIndex={_stepIndex}");

        // Clamp index to available range
        var stepsLen = steps != null ? steps.Length : 0;
        if (stepsLen == 0)
        {
            if (instructionTitleText != null) instructionTitleText.text = "Step 1 - The Setup";
            if (instructionObjectiveText != null) instructionObjectiveText.text = "Visualize the 3D workspace.";
            if (instructionBodyText != null) instructionBodyText.text = "1. Trace...\n2. Draw...\n3. Draw...";
            if (prevStepButton != null) prevStepButton.interactable = false;
            if (nextStepButton != null) nextStepButton.interactable = false;
            return;
        }

        _stepIndex = Mathf.Clamp(_stepIndex, 0, stepsLen - 1);
        var current = steps[_stepIndex];

        // Title/objective/instructions with safe fallbacks
        if (instructionTitleText != null)
            instructionTitleText.text = !string.IsNullOrEmpty(current?.title) ? current.title : "Step 1 - The Setup";

        if (instructionObjectiveText != null)
            instructionObjectiveText.text = !string.IsNullOrEmpty(current?.objective) ? current.objective : "Visualize the 3D workspace.";

        if (instructionBodyText != null)
        {
            var lines = (current?.instructions) ?? Array.Empty<string>();
            if (lines.Length > 0)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < lines.Length; i++)
                {
                    sb.Append(i + 1).Append(". ").Append(lines[i]);
                    if (i < lines.Length - 1) sb.Append('\n');
                }
                instructionBodyText.text = sb.ToString();
            }
            else
            {
                instructionBodyText.text = "1. Trace...\n2. Draw...\n3. Draw...";
            }
        }

        if (prevStepButton != null) prevStepButton.interactable = _stepIndex > 0;
        if (nextStepButton != null) nextStepButton.interactable = _stepIndex < (stepsLen - 1);
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
        if (currentPenInstance != null) return;
        if (penPrefab == null)
        {
            Debug.LogWarning("HandleMenu: Pen Prefab is not assigned in Inspector!");
            return;
        }

        Transform headTransform = Camera.main.transform;
        Vector3 spawnPos = headTransform.position + (headTransform.forward * penSpawnDistance);
        currentPenInstance = Instantiate(penPrefab, spawnPos, Quaternion.identity);
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

    private void AttachAndWireISDKRayGrabInteraction(GameObject parentShape, Grabbable grabbable)
    {
        if (parentShape == null || grabbable == null) return;

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

        var pointable = parentShape.GetComponent<PointableElement>();
        if (pointable == null) pointable = parentShape.AddComponent<PointableElement>();

        var surface = parentShape.GetComponent<ColliderSurface>();
        if (surface == null) surface = parentShape.AddComponent<ColliderSurface>();
        var surfaceType = typeof(ColliderSurface);
        var colliderField = surfaceType.GetField("_collider", BindingFlags.NonPublic | BindingFlags.Instance);
        if (colliderField != null) colliderField.SetValue(surface, shapeCollider);

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

        var comps = clone.GetComponents<MonoBehaviour>();
        foreach (var comp in comps)
        {
            if (comp == null) continue;
            var t = comp.GetType();

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

            grabbable.InjectOptionalRigidbody(rb);

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