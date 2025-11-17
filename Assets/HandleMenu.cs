using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HandleMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CoordinateSpaceController coordinateSpace; // Auto-resolved at runtime

    [Header("Shape Selection (ToggleGroup)")]
    [SerializeField] private ToggleGroup shapeToggleGroup;
    [SerializeField] private Toggle cubeToggle;
    [SerializeField] private Toggle sphereToggle;
    [SerializeField] private Toggle cylinderToggle;
    [SerializeField] private Toggle deleteToggle; // selecting this removes the current shape

    [Header("Shape Settings")]
    [SerializeField] private Vector3 shapeScale = Vector3.one * 1f;
    [SerializeField] private Material shapeMaterial;

    private GameObject originShape;
    private Vector3 currentShapeOffset = Vector3.zero;
    private bool _suppressCallbacks;

    void Awake()
    {
        ResolveCoordinateSpace(); // use preview if available

        // Register listeners
        if (cubeToggle != null) cubeToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(cubeToggle, isOn));
        if (sphereToggle != null) sphereToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(sphereToggle, isOn));
        if (cylinderToggle != null) cylinderToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(cylinderToggle, isOn));
        if (deleteToggle != null) deleteToggle.onValueChanged.AddListener(isOn => OnShapeToggleChanged(deleteToggle, isOn));
    }

    void OnDestroy()
    {
        if (cubeToggle != null) cubeToggle.onValueChanged.RemoveAllListeners();
        if (sphereToggle != null) sphereToggle.onValueChanged.RemoveAllListeners();
        if (cylinderToggle != null) cylinderToggle.onValueChanged.RemoveAllListeners();
        if (deleteToggle != null) deleteToggle.onValueChanged.RemoveAllListeners();
    }

    // Keep shape anchored; ensure its min corner stays at (0,0,0)
    void LateUpdate()
    {
        // If preview was hidden after placement, switch to the placed instance automatically
        ResolveCoordinateSpace();

        if (originShape != null && coordinateSpace != null && originShape.transform.parent != coordinateSpace.transform)
        {
            originShape.transform.SetParent(coordinateSpace.transform, false);
            originShape.transform.localPosition = currentShapeOffset;
            originShape.transform.localRotation = Quaternion.identity;
        }
    }

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
            CreateOrReplaceShape(PrimitiveType.Cube, "OriginCube");
            return;
        }
        if (sphereToggle != null && source == sphereToggle)
        {
            CreateOrReplaceShape(PrimitiveType.Sphere, "OriginSphere");
            return;
        }
        if (cylinderToggle != null && source == cylinderToggle)
        {
            CreateOrReplaceShape(PrimitiveType.Cylinder, "OriginCylinder");
            return;
        }
    }

    private void CreateOrReplaceShape(PrimitiveType type, string shapeName)
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

        // Compute offset so the object's local AABB min corner is at (0,0,0)
        currentShapeOffset = ComputePositiveSideOffset(originShape);
        originShape.transform.localPosition = currentShapeOffset;

        if (shapeMaterial != null)
        {
            var renderer = originShape.GetComponent<Renderer>();
            if (renderer != null) renderer.material = shapeMaterial;
        }
    }

    private Vector3 ComputePositiveSideOffset(GameObject shape)
    {
        var mf = shape.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
            return Vector3.zero;

        Bounds meshBounds = mf.sharedMesh.bounds; // in mesh local coordinates
        Vector3 extents = meshBounds.extents;

        // Adjust for non-uniform scale
        return new Vector3(
            extents.x * shape.transform.localScale.x,
            extents.y * shape.transform.localScale.y,
            extents.z * shape.transform.localScale.z
        );
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

    // Attempts to use the preview first, then the placed coordinate space.
    private void ResolveCoordinateSpace()
    {
        // If we already have an active reference, keep it
        if (coordinateSpace != null && coordinateSpace.gameObject.activeInHierarchy) return;

        // 1) Try the preview created by CoordinateSpacePlacer
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

        // 2) Fallback: find any active placed CoordinateSpaceController (not the preview)
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

    // Optional public methods (compatibility)
    public void OnRemoveShapeClicked() => DestroyOriginShape();
    public void OnAddCubeClicked() => CreateOrReplaceShape(PrimitiveType.Cube, "OriginCube");
    public void OnAddSphereClicked() => CreateOrReplaceShape(PrimitiveType.Sphere, "OriginSphere");
    public void OnAddCylinderClicked() => CreateOrReplaceShape(PrimitiveType.Cylinder, "OriginCylinder");
}