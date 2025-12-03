using UnityEngine;
using System.Collections.Generic;

public class ShapeCreator : MonoBehaviour
{
    public Material planeMaterial;

    // Call this function when the user finishes the gesture
    public void GenerateFlatPlane(List<Vector3> drawnPoints)
    {
        if (drawnPoints.Count < 3) return;

        // 1. Calculate the "Average Rotation" to flatten against
        // We assume the user is drawing roughly on a wall or floor.
        // For simplicity, we will project points onto the XY plane relative to the first point.
        
        // Convert 3D world points to Local 2D points
        Vector2[] uvs = new Vector2[drawnPoints.Count];
        Vector3[] vertices = new Vector3[drawnPoints.Count];
        
        // We use the first point as the "Anchor"
        Vector3 anchor = drawnPoints[0];
        
        // Determine approximate facing direction (Normal)
        // This is a simplified way to guess if they drew on a wall or floor
        // Ideally, you pass the orientation of the user's hand here.
        Quaternion flattenRotation = Quaternion.Inverse(Camera.main.transform.rotation); 

        for (int i = 0; i < drawnPoints.Count; i++)
        {
            // We make the points relative to the start (0,0,0)
            vertices[i] = drawnPoints[i] - anchor;
            
            // We "Flatten" by ignoring one axis (Z) after rotating to camera view
            // This is a simple projection technique
            Vector3 temp = flattenRotation * vertices[i];
            uvs[i] = new Vector2(temp.x, temp.y);
        }

        // 2. Use the Triangulator to handle ANY shape (Concave or Convex)
        Triangulator tr = new Triangulator(uvs);
        int[] indices = tr.Triangulate();

        // 3. Create the Mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices; // These are the 3D points
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // 4. Spawn the Object
        GameObject newPlane = new GameObject("UserShape");
        newPlane.transform.position = anchor;
        
        // Add Mesh & Renderer
        MeshFilter filter = newPlane.AddComponent<MeshFilter>();
        filter.mesh = mesh;
        MeshRenderer renderer = newPlane.AddComponent<MeshRenderer>();
        renderer.material = planeMaterial;
    
        
        // 1. Add Collider (MeshCollider is best for irregular shapes)
        MeshCollider col = newPlane.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;
        col.convex = true; // MUST be Convex to work with a Rigidbody!

        // 2. Add Rigidbody (Allows physics interaction)
        Rigidbody rb = newPlane.AddComponent<Rigidbody>();
        rb.useGravity = false; // Set to true if you want it to fall to the floor
        rb.isKinematic = false; 
        
        // 3. Set Layer (Optional but recommended)
        newPlane.layer = LayerMask.NameToLayer("Grabbable");
    }
}