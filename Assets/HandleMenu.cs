using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HandleMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CoordinateSpaceController coordinateSpace; // Coordinate system root

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
        // CreateOrReplaceShape(PrimitiveType.Cube, "OriginCube");

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
        if (coordinateSpace == null)
        {
            Debug.LogWarning("HandleMenu: CoordinateSpace reference not set.");
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
        // Use mesh bounds in local mesh space then scale
        var mf = shape.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
            return Vector3.zero;

        Bounds meshBounds = mf.sharedMesh.bounds; // in mesh local coordinates
        // meshBounds.center is usually (0,0,0). extents are half-size in mesh units.
        // Offset = scaled extents so min corner aligns with origin.
        Vector3 extents = meshBounds.extents;

        // Adjust for non-uniform scale: extents * scale
        Vector3 scaledExtents = new Vector3(
            extents.x * shape.transform.localScale.x,
            extents.y * shape.transform.localScale.y,
            extents.z * shape.transform.localScale.z
        );

        // Special case: Unity cylinder has height 2 (extents.y = 1). This logic already handles it.
        return scaledExtents;
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

    // Optional public methods (compatibility)
    public void OnRemoveShapeClicked() => DestroyOriginShape();
    public void OnAddCubeClicked() => CreateOrReplaceShape(PrimitiveType.Cube, "OriginCube");
    public void OnAddSphereClicked() => CreateOrReplaceShape(PrimitiveType.Sphere, "OriginSphere");
    public void OnAddCylinderClicked() => CreateOrReplaceShape(PrimitiveType.Cylinder, "OriginCylinder");
}