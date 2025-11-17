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
    [SerializeField] private Toggle deleteToggle;

    [Header("Shape Settings")]
    [SerializeField] private Vector3 shapeScale = Vector3.one * 0.2f;
    [SerializeField] private Material shapeMaterial;

    private GameObject originShape;

    void Awake()
    {
        // When a toggle is turned on, create/replace the shape immediately
        if (cubeToggle != null) cubeToggle.onValueChanged.AddListener(isOn => { if (isOn) CreateOrReplaceShape(PrimitiveType.Cube, "OriginCube"); });
        if (sphereToggle != null) sphereToggle.onValueChanged.AddListener(isOn => { if (isOn) CreateOrReplaceShape(PrimitiveType.Sphere, "OriginSphere"); });
        if (cylinderToggle != null) cylinderToggle.onValueChanged.AddListener(isOn => { if (isOn) CreateOrReplaceShape(PrimitiveType.Cylinder, "OriginCylinder"); });

        if (deleteToggle != null) deleteToggle.onValueChanged.AddListener(isOn => { if (isOn) DestroyOriginShape();  });
    }

    // Optional public methods (kept for compatibility with existing UI hookups)
    public void OnRemoveShapeClicked() => DestroyOriginShape();
    public void OnAddCubeClicked() => CreateOrReplaceShape(PrimitiveType.Cube, "OriginCube");
    public void OnAddSphereClicked() => CreateOrReplaceShape(PrimitiveType.Sphere, "OriginSphere");
    public void OnAddCylinderClicked() => CreateOrReplaceShape(PrimitiveType.Cylinder, "OriginCylinder");

    // Keep shape anchored even if something re-parents it externally
    void LateUpdate()
    {
        if (originShape != null && coordinateSpace != null && originShape.transform.parent != coordinateSpace.transform)
        {
            originShape.transform.SetParent(coordinateSpace.transform, false);
            originShape.transform.localPosition = Vector3.zero;
            originShape.transform.localRotation = Quaternion.identity;
        }
    }

    private void CreateOrReplaceShape(PrimitiveType type, string shapeName)
    {
        if (coordinateSpace == null)
        {
            Debug.LogWarning("HandleMenu: CoordinateSpace reference not set.");
            return;
        }

        // Remove previous shape before adding a new one
        if (originShape != null)
        {
            Destroy(originShape);
            originShape = null;
        }

        originShape = GameObject.CreatePrimitive(type);
        originShape.name = shapeName;
        originShape.transform.SetParent(coordinateSpace.transform, false);
        originShape.transform.localPosition = Vector3.zero;
        originShape.transform.localRotation = Quaternion.identity;
        originShape.transform.localScale = shapeScale;

        if (shapeMaterial != null)
        {
            var renderer = originShape.GetComponent<Renderer>();
            if (renderer != null) renderer.material = shapeMaterial;
        }
    }

    private void DestroyOriginShape()
    {
        if (originShape != null)
        {
            Destroy(originShape);
            originShape = null;
        }
    }
}