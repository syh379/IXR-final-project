// using System.Linq;
// using UnityEngine;
// using UnityEngine.UI;

// public class HandleMenu : MonoBehaviour
// {
//     [Header("References")]
//     [SerializeField] private CoordinateSpaceController coordinateSpace; // Auto-resolved at runtime

//     [Header("Shape Selection (ToggleGroup)")]
//     [SerializeField] private ToggleGroup shapeToggleGroup;
//     [SerializeField] private Toggle cubeToggle;
//     [SerializeField] private Toggle sphereToggle;
//     [SerializeField] private Toggle cylinderToggle;
//     [SerializeField] private Toggle deleteToggle; // selecting this removes the current shape
//     [SerializeField] private Toggle coneToggle;   // NEW: cone toggle

//     [Header("Shape Settings")]
//     [SerializeField] private Vector3 shapeScale = Vector3.one * 1f;
//     [SerializeField] private Material shapeMaterial;

//     private GameObject originShape;
//     private Vector3 currentShapeOffset = Vector3.zero;
//     private bool _suppressCallbacks;

//     void Awake()
//     {
//         ResolveCoordinateSpace(); // use preview if available

//         // Register listeners
//         if (cubeToggle != null) cubeToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(cubeToggle, isOn));
//         if (sphereToggle != null) sphereToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(sphereToggle, isOn));
//         if (cylinderToggle != null) cylinderToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(cylinderToggle, isOn));
//         if (deleteToggle != null) deleteToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(deleteToggle, isOn));
//         if (coneToggle != null) coneToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(coneToggle, isOn));
//     }

//     void OnDestroy()
//     {
//         if (cubeToggle != null) cubeToggle.onValueChanged.RemoveAllListeners();
//         if (sphereToggle != null) sphereToggle.onValueChanged.RemoveAllListeners();
//         if (cylinderToggle != null) cylinderToggle.onValueChanged.RemoveAllListeners();
//         if (deleteToggle != null) deleteToggle.onValueChanged.RemoveAllListeners();
//         if (coneToggle != null) coneToggle.onValueChanged.RemoveAllListeners();
//     }

//     // Keep shape anchored; ensure its min corner stays at (0,0,0)
//     void LateUpdate()
//     {
//         ResolveCoordinateSpace();

//         if (originShape != null && coordinateSpace != null && originShape.transform.parent != coordinateSpace.transform)
//         {
//             originShape.transform.SetParent(coordinateSpace.transform, false);
//             originShape.transform.localPosition = currentShapeOffset;
//             originShape.transform.localRotation = Quaternion.identity;
//         }
//     }

//     private void OnShapeToggleChanged(Toggle source, bool isOn)
//     {
//         if (_suppressCallbacks || !isOn) return;

//         if (deleteToggle != null && source == deleteToggle)
//         {
//             DestroyOriginShape();
//             return;
//         }
//         if (cubeToggle != null && source == cubeToggle)
//         {
//             CreateOrReplacePrimitive(PrimitiveType.Cube, "OriginCube");
//             return;
//         }
//         if (sphereToggle != null && source == sphereToggle)
//         {
//             CreateOrReplacePrimitive(PrimitiveType.Sphere, "OriginSphere");
//             return;
//         }
//         if (cylinderToggle != null && source == cylinderToggle)
//         {
//             CreateOrReplacePrimitive(PrimitiveType.Cylinder, "OriginCylinder");
//             return;
//         }
//         if (coneToggle != null && source == coneToggle)
//         {
//             CreateOrReplaceCone("OriginCone");
//             return;
//         }
//     }

//     private void CreateOrReplacePrimitive(PrimitiveType type, string shapeName)
//     {
//         ResolveCoordinateSpace();

//         if (coordinateSpace == null)
//         {
//             Debug.LogWarning("HandleMenu: CoordinateSpace reference not set or not found (preview/placed).");
//             return;
//         }

//         if (originShape != null)
//         {
//             Destroy(originShape);
//             originShape = null;
//         }

//         originShape = GameObject.CreatePrimitive(type);
//         originShape.name = shapeName;
//         originShape.transform.SetParent(coordinateSpace.transform, false);

//         originShape.transform.localRotation = Quaternion.identity;
//         originShape.transform.localScale = shapeScale;

//         // Compute offset so min AABB corner aligns at origin (positive octant)
//         currentShapeOffset = ComputePositiveSideOffset(originShape);
//         originShape.transform.localPosition = currentShapeOffset;

//         if (shapeMaterial != null)
//         {
//             var renderer = originShape.GetComponent<Renderer>();
//             if (renderer != null) renderer.material = shapeMaterial;
//         }
//     }

//     private void CreateOrReplaceCone(string shapeName)
//     {
//         ResolveCoordinateSpace();

//         if (coordinateSpace == null)
//         {
//             Debug.LogWarning("HandleMenu: CoordinateSpace reference not set or not found (preview/placed).");
//             return;
//         }

//         if (originShape != null)
//         {
//             Destroy(originShape);
//             originShape = null;
//         }

//         originShape = new GameObject(shapeName);
//         originShape.transform.SetParent(coordinateSpace.transform, false);

//         var mf = originShape.AddComponent<MeshFilter>();
//         var mr = originShape.AddComponent<MeshRenderer>();

//         // Build a unit-like cone (radius 0.5, height 1) so extents are ~0.5 in X/Z and 0.5 in Y
//         mf.sharedMesh = ProceduralCone.Build(0.5f, 1f, 32, capBase: true);

//         // Optional collider (similar to primitives having colliders)
//         var mc = originShape.AddComponent<MeshCollider>();
//         mc.sharedMesh = mf.sharedMesh;
//         mc.convex = true;

//         if (shapeMaterial != null) mr.material = shapeMaterial;

//         originShape.transform.localRotation = Quaternion.identity;
//         originShape.transform.localScale = shapeScale;

//         // Keep entire shape in positive octant using current extents-based logic
//         currentShapeOffset = ComputePositiveSideOffset(originShape);
//         originShape.transform.localPosition = currentShapeOffset;
//     }

//     private Vector3 ComputePositiveSideOffset(GameObject shape)
//     {
//         var mf = shape.GetComponent<MeshFilter>();
//         if (mf == null || mf.sharedMesh == null)
//             return Vector3.zero;

//         // Works correctly when mesh.bounds.center == (0,0,0)
//         var b = mf.sharedMesh.bounds;
//         var ext = b.extents;
//         var s = shape.transform.localScale;
//         return new Vector3(ext.x * s.x, ext.y * s.y, ext.z * s.z);
//     }

//     private void DestroyOriginShape()
//     {
//         if (originShape != null)
//         {
//             Destroy(originShape);
//             originShape = null;
//             currentShapeOffset = Vector3.zero;
//         }
//     }

//     private void ResolveCoordinateSpace()
//     {
//         if (coordinateSpace != null && coordinateSpace.gameObject.activeInHierarchy) return;

//         var previewGO = GameObject.Find("CoordinateSpace_Preview");
//         if (previewGO != null && previewGO.activeInHierarchy)
//         {
//             var previewCtrl = previewGO.GetComponent<CoordinateSpaceController>();
//             if (previewCtrl != null)
//             {
//                 coordinateSpace = previewCtrl;
//                 return;
//             }
//         }

// #if UNITY_2023_1_OR_NEWER
//         var candidates = Object.FindObjectsByType<CoordinateSpaceController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
// #else
//         var candidates = Object.FindObjectsOfType<CoordinateSpaceController>();
// #endif
//         var placed = candidates.FirstOrDefault(c => c != null && c.gameObject.activeInHierarchy && c.gameObject.name != "CoordinateSpace_Preview");
//         if (placed != null)
//         {
//             coordinateSpace = placed;
//         }
//     }

//     // Optional public compatibility
//     public void OnRemoveShapeClicked() => DestroyOriginShape();
//     public void OnAddCubeClicked() => CreateOrReplacePrimitive(PrimitiveType.Cube, "OriginCube");
//     public void OnAddSphereClicked() => CreateOrReplacePrimitive(PrimitiveType.Sphere, "OriginSphere");
//     public void OnAddCylinderClicked() => CreateOrReplacePrimitive(PrimitiveType.Cylinder, "OriginCylinder");
//     public void OnAddConeClicked() => CreateOrReplaceCone("OriginCone");
// }


using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HandleMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CoordinateSpaceController coordinateSpace; 

    [Header("Shape Selection (ToggleGroup)")]
    [SerializeField] private ToggleGroup shapeToggleGroup;
    [SerializeField] private Toggle cubeToggle;
    [SerializeField] private Toggle sphereToggle;
    [SerializeField] private Toggle cylinderToggle;
    [SerializeField] private Toggle deleteToggle; 
    [SerializeField] private Toggle coneToggle;   

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
        
        // --- NEW LISTENER ---
        if (penToggle != null) penToggle.onValueChanged.AddListener(isOn => OnPenToggleChanged(isOn));
    }

    void OnDestroy()
    {
        if (cubeToggle != null) cubeToggle.onValueChanged.RemoveAllListeners();
        if (sphereToggle != null) sphereToggle.onValueChanged.RemoveAllListeners();
        if (cylinderToggle != null) cylinderToggle.onValueChanged.RemoveAllListeners();
        if (deleteToggle != null) deleteToggle.onValueChanged.RemoveAllListeners();
        if (coneToggle != null) coneToggle.onValueChanged.RemoveAllListeners();
        
        // --- NEW REMOVE LISTENER ---
        if (penToggle != null) penToggle.onValueChanged.RemoveAllListeners();
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