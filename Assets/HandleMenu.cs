using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction;

public class HandleMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CoordinateSpaceController coordinateSpace;
    [SerializeField] private CoordinateSpacePlacer coordinateSpacePlacer; // For unanchoring control 
    [SerializeField] private GameObject isdkRayGrabInteractionTemplate; // Template in-scene to clone for each spawned shape

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

    // --- NEW PEN SETTINGS ---
    [Header("Pen Settings")]
    [SerializeField] private Toggle penToggle;      // Assign your UI Toggle here
    [SerializeField] private GameObject penPrefab;  // Assign your Pen Prefab here
    [SerializeField] private float penSpawnDistance = 0.5f; // How far in front of user
    
    private GameObject currentPenInstance;
    // ------------------------

    private GameObject originShape;
    private Vector3 currentShapeOffset = Vector3.zero;
    private bool _suppressCallbacks;

    void Awake()
    {
        ResolveCoordinateSpace(); 

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
        
        // Unsubscribe from events
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
        if (_suppressCallbacks || !isOn) return;

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
    /// Finds a scene GameObject named "ISDK_RayGrabInteraction", clones it as a child
    /// of the spawned shape, and reflectively assigns any Grabbable-typed fields or
    /// properties on components of the cloned interaction object to the provided
    /// grabbable instance.
    /// </summary>
    private void AttachAndWireISDKRayGrabInteraction(GameObject parentShape, Grabbable grabbable)
    {
        if (parentShape == null || grabbable == null) return;

        // Prefer serialized template; fall back to searching the scene by name
        var template = isdkRayGrabInteractionTemplate != null ? isdkRayGrabInteractionTemplate : GameObject.Find("ISDK_RayGrabInteraction");
        if (template == null)
        {
            Debug.LogWarning("HandleMenu: ISDK_RayGrabInteraction template not found (check serialized field or scene). Skipping attach.");
            return;
        }

        var clone = Instantiate(template, parentShape.transform, false);
        clone.transform.localPosition = Vector3.zero;
        clone.transform.localRotation = Quaternion.identity;
        clone.name = "ISDK_RayGrabInteraction"; // normalize name under parent

        // Walk components on the cloned template and assign Grabbable references
        var comps = clone.GetComponents<MonoBehaviour>();
        foreach (var comp in comps)
        {
            if (comp == null) continue;
            var t = comp.GetType();

            // Fields
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in fields)
            {
                if (f.FieldType == typeof(Grabbable))
                {
                    f.SetValue(comp, grabbable);
                }
            }

            // Properties
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var p in props)
            {
                if (!p.CanWrite) continue;
                if (p.PropertyType == typeof(Grabbable))
                {
                    try { p.SetValue(comp, grabbable, null); } catch { }
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
            rb.isKinematic = false;

            var grabbable = shape.GetComponent<Grabbable>();
            if (grabbable == null) grabbable = shape.AddComponent<Grabbable>();

            if (shape.GetComponent<GrabInteractable>() == null)
                shape.AddComponent<GrabInteractable>();
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
        var candidates = Object.FindObjectsByType<CoordinateSpaceController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
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