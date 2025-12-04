using UnityEngine;
using System.Collections.Generic;

public class CoordinateSpaceController : MonoBehaviour
{
    [Header("Axis Settings")]
    [SerializeField] private float maxAxisLength = 50f;
    [SerializeField] private float extensionThreshold = 10f; // Extend when user is within this distance from edge
    [SerializeField] private float extensionAmount = 20f; // How much to extend
    [SerializeField] private bool autoExtendEnabled = false; // Toggleable auto-extension
    [SerializeField] private Color xAxisColor = Color.red;
    [SerializeField] private Color yAxisColor = Color.green;
    [SerializeField] private Color zAxisColor = Color.blue;
    [SerializeField] private float axisWidth = 0.03f;
    
    [Header("Tick Settings")]
    [SerializeField] private bool showTicks = true;
    [SerializeField] private float tickSpacing = 1f;
    [SerializeField] private float tickSize = 0.1f;
    [SerializeField] private float tickWidth = 0.005f;
    
    [Header("Grid Settings")]
    [SerializeField] private bool showGrids = false;
    [SerializeField] private Color gridColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private float gridWidth = 0.005f;
    
    [Header("Input Settings")]
    [SerializeField] private OVRInput.Button gridToggleButton = OVRInput.Button.Two; // B button
    [SerializeField] private OVRInput.Controller inputController = OVRInput.Controller.LTouch;
    
    [Header("References")]
    [SerializeField] private LineRenderer xAxisLine;
    [SerializeField] private LineRenderer yAxisLine;
    [SerializeField] private LineRenderer zAxisLine;
    [SerializeField] private Material lineMaterial;
    
    private GameObject ticksContainer;
    private GameObject gridsContainer;
    private List<LineRenderer> tickLines = new List<LineRenderer>();
    private List<LineRenderer> gridLines = new List<LineRenderer>();
    private Camera mainCamera;
    private bool needsUpdate = false;
    
    void Start()
    {
        // Auto-find line renderers if not assigned
        if (xAxisLine == null || yAxisLine == null || zAxisLine == null)
        {
            LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>();
            foreach (var lr in lineRenderers)
            {
                if (lr.gameObject.name == "x-axis") xAxisLine = lr;
                else if (lr.gameObject.name == "y-axis") yAxisLine = lr;
                else if (lr.gameObject.name == "z-axis") zAxisLine = lr;
            }
        }
        
        // Get main camera (VR camera)
        mainCamera = Camera.main;
        
        // Create containers for ticks and grids
        ticksContainer = new GameObject("Ticks");
        ticksContainer.transform.SetParent(transform, false);
        ticksContainer.transform.localPosition = Vector3.zero;
        ticksContainer.transform.localRotation = Quaternion.identity;
        ticksContainer.transform.localScale = Vector3.one;
        
        gridsContainer = new GameObject("Grids");
        gridsContainer.transform.SetParent(transform, false);
        gridsContainer.transform.localPosition = Vector3.zero;
        gridsContainer.transform.localRotation = Quaternion.identity;
        gridsContainer.transform.localScale = Vector3.one;
        
        UpdateAxes();
        UpdateTicks();
        UpdateGrids();
    }
    
    void Update()
    {
        // Check for grid toggle input
        if (OVRInput.GetDown(gridToggleButton, inputController))
        {
            ToggleGrids();
        }
        
        // Check if axes need to be extended (only if auto-extend is enabled)
        if (mainCamera != null && autoExtendEnabled)
        {
            CheckAndExtendAxes();
        }
        
        // Update if needed (to avoid updating every frame)
        if (needsUpdate)
        {
            UpdateAxes();
            UpdateTicks();
            UpdateGrids();
            needsUpdate = false;
        }
    }
    
    private void CheckAndExtendAxes()
    {
        // Account for transform scale when checking distance
        Vector3 worldCameraPos = mainCamera.transform.position;
        Vector3 localCameraPos = transform.InverseTransformPoint(worldCameraPos);
        
        // Effective axis length accounts for the transform scale
        float effectiveLength = maxAxisLength;
        
        // Check each axis direction
        if (Mathf.Abs(localCameraPos.x) > effectiveLength - extensionThreshold)
        {
            maxAxisLength += extensionAmount;
            needsUpdate = true;
        }
        else if (Mathf.Abs(localCameraPos.y) > effectiveLength - extensionThreshold)
        {
            maxAxisLength += extensionAmount;
            needsUpdate = true;
        }
        else if (Mathf.Abs(localCameraPos.z) > effectiveLength - extensionThreshold)
        {
            maxAxisLength += extensionAmount;
            needsUpdate = true;
        }
    }
    
    void OnValidate()
    {
        // Called when values change in inspector
        if (Application.isPlaying)
        {
            UpdateAxes();
            UpdateTicks();
            UpdateGrids();
        }
    }
    
    private void UpdateAxes()
    {
        if (xAxisLine != null)
        {
            SetupAxis(xAxisLine, Vector3.right, xAxisColor);
        }
        
        if (yAxisLine != null)
        {
            SetupAxis(yAxisLine, Vector3.up, yAxisColor);
        }
        
        if (zAxisLine != null)
        {
            SetupAxis(zAxisLine, Vector3.forward, zAxisColor);
        }
    }
    
    private void SetupAxis(LineRenderer line, Vector3 direction, Color color)
    {
        line.useWorldSpace = false;
        line.startColor = color;
        line.endColor = color;
        line.startWidth = axisWidth;
        line.endWidth = axisWidth;
        
        line.positionCount = 2;
        line.SetPosition(0, Vector3.zero);
        line.SetPosition(1, direction * maxAxisLength);
    }
    
    private void UpdateTicks()
    {
        if (ticksContainer == null) return;
        
        // Clear old ticks
        foreach (var tick in tickLines)
        {
            if (tick != null) Destroy(tick.gameObject);
        }
        tickLines.Clear();
        
        if (!showTicks) return;
        
        // Create ticks for each axis
        CreateTicksForAxis(Vector3.right, Vector3.up, xAxisColor);      // X-axis
        CreateTicksForAxis(Vector3.up, Vector3.right, yAxisColor);      // Y-axis
        CreateTicksForAxis(Vector3.forward, Vector3.up, zAxisColor);    // Z-axis
    }
    
    private void CreateTicksForAxis(Vector3 axisDir, Vector3 perpDir, Color color)
    {
        int numTicks = Mathf.FloorToInt(maxAxisLength / tickSpacing);
        
        for (int i = 1; i <= numTicks; i++)
        {
            float pos = i * tickSpacing;
            Vector3 tickPos = axisDir * pos;
            
            GameObject tickObj = new GameObject($"Tick_{axisDir}_{i}");
            tickObj.transform.SetParent(ticksContainer.transform, false);
            tickObj.transform.localPosition = Vector3.zero;
            tickObj.transform.localRotation = Quaternion.identity;
            tickObj.transform.localScale = Vector3.one;
            
            LineRenderer lr = tickObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.startWidth = tickWidth;
            lr.endWidth = tickWidth;
            lr.startColor = color;
            lr.endColor = color;
            lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            
            lr.SetPosition(0, tickPos - perpDir * tickSize / 2f);
            lr.SetPosition(1, tickPos + perpDir * tickSize / 2f);
            
            tickLines.Add(lr);
        }
    }
    
    private void UpdateGrids()
    {
        if (gridsContainer == null) return;
        
        // Clear old grids
        foreach (var grid in gridLines)
        {
            if (grid != null) Destroy(grid.gameObject);
        }
        gridLines.Clear();
        
        gridsContainer.SetActive(showGrids);
        
        if (!showGrids) return;
        
        // Create XY plane grid
        CreateGridPlane(Vector3.right, Vector3.up, Vector3.forward);
        // Create XZ plane grid
        CreateGridPlane(Vector3.right, Vector3.forward, Vector3.up);
        // Create YZ plane grid
        CreateGridPlane(Vector3.up, Vector3.forward, Vector3.right);
    }
    
    private void CreateGridPlane(Vector3 dir1, Vector3 dir2, Vector3 normal)
    {
        int numLines1 = Mathf.FloorToInt(maxAxisLength / tickSpacing);
        int numLines2 = Mathf.FloorToInt(maxAxisLength / tickSpacing);
        
        // Lines along dir1
        for (int i = 1; i <= numLines1; i++)
        {
            float pos = i * tickSpacing;
            
            GameObject lineObj = new GameObject($"Grid_{dir1}_{i}");
            lineObj.transform.SetParent(gridsContainer.transform, false);
            lineObj.transform.localPosition = Vector3.zero;
            lineObj.transform.localRotation = Quaternion.identity;
            lineObj.transform.localScale = Vector3.one;
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.startWidth = gridWidth;
            lr.endWidth = gridWidth;
            lr.startColor = gridColor;
            lr.endColor = gridColor;
            lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            
            lr.SetPosition(0, dir1 * pos);
            lr.SetPosition(1, dir1 * pos + dir2 * maxAxisLength);
            
            gridLines.Add(lr);
        }
        
        // Lines along dir2
        for (int i = 1; i <= numLines2; i++)
        {
            float pos = i * tickSpacing;
            
            GameObject lineObj = new GameObject($"Grid_{dir2}_{i}");
            lineObj.transform.SetParent(gridsContainer.transform, false);
            lineObj.transform.localPosition = Vector3.zero;
            lineObj.transform.localRotation = Quaternion.identity;
            lineObj.transform.localScale = Vector3.one;
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.startWidth = gridWidth;
            lr.endWidth = gridWidth;
            lr.startColor = gridColor;
            lr.endColor = gridColor;
            lr.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            
            lr.SetPosition(0, dir2 * pos);
            lr.SetPosition(1, dir2 * pos + dir1 * maxAxisLength);
            
            gridLines.Add(lr);
        }
    }
    
    public void ToggleGrids()
    {
        showGrids = !showGrids;
        UpdateGrids();
    }
    
    public void ForceUpdate()
    {
        UpdateAxes();
        UpdateTicks();
        UpdateGrids();
    }
    
    // Public methods for external control
    public float GetAxisLength()
    {
        return maxAxisLength;
    }
    
    public void SetAxisLength(float length)
    {
        maxAxisLength = length;
        needsUpdate = true;
    }
    
    public float GetTickSpacing()
    {
        return tickSpacing;
    }
    
    public void SetTickSpacing(float spacing)
    {
        tickSpacing = spacing;
        needsUpdate = true;
    }
    
    public void SetAutoExtend(bool enabled)
    {
        autoExtendEnabled = enabled;
    }
    
    public bool GetAutoExtend()
    {
        return autoExtendEnabled;
    }
}
