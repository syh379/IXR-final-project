using UnityEngine;

public static class ProceduralCone
{
    // Builds a cone centered at the local origin:
    // - base lies at y = -height/2
    // - apex lies at y = +height/2
    // This keeps mesh.bounds.center at (0,0,0), so using "extents" aligns min corner at (0,0,0).
    public static Mesh Build(float radius = 0.5f, float height = 1f, int segments = 24, bool capBase = true)
    {
        segments = Mathf.Max(3, segments);

        // Vertices:
        // - ring (segments)
        // - apex (1)
        // - base center (1, optional)
        int ringCount = segments;
        int apexIndex = ringCount;
        int baseCenterIndex = capBase ? ringCount + 1 : -1;

        var verts = new Vector3[ringCount + 1 + (capBase ? 1 : 0)];
        var uvs = new Vector2[verts.Length];

        float halfH = height * 0.5f;
        float baseY = -halfH;
        float apexY = +halfH;

        // Ring vertices
        for (int i = 0; i < ringCount; i++)
        {
            float t = (float)i / segments;
            float ang = t * Mathf.PI * 2f;
            float x = Mathf.Cos(ang) * radius;
            float z = Mathf.Sin(ang) * radius;
            verts[i] = new Vector3(x, baseY, z);
            uvs[i] = new Vector2((float)i / (segments - 1), 0f);
        }

        // Apex
        verts[apexIndex] = new Vector3(0f, apexY, 0f);
        uvs[apexIndex] = new Vector2(0.5f, 1f);

        // Base center (optional)
        if (capBase)
        {
            verts[baseCenterIndex] = new Vector3(0f, baseY, 0f);
            uvs[baseCenterIndex] = new Vector2(0.5f, 0.5f);
        }

        // Triangles
        // Sides: segments triangles
        int sideTriCount = segments * 3;
        int baseTriCount = capBase ? segments * 3 : 0;
        var tris = new int[sideTriCount + baseTriCount];

        int ti = 0;

        // Side triangles (ensure outward winding)
        for (int i = 0; i < segments; i++)
        {
            int i0 = i;
            int i1 = (i + 1) % segments;

            // Triangle: i0 -> i1 -> apex
            tris[ti++] = i0;
            tris[ti++] = i1;
            tris[ti++] = apexIndex;
        }

        // Base cap (faces downward by default)
        if (capBase)
        {
            for (int i = 0; i < segments; i++)
            {
                int i0 = i;
                int i1 = (i + 1) % segments;

                // Triangle: baseCenter -> i1 -> i0 (clockwise when looking from below)
                tris[ti++] = baseCenterIndex;
                tris[ti++] = i1;
                tris[ti++] = i0;
            }
        }

        var mesh = new Mesh
        {
            name = "ProceduralCone"
        };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        return mesh;
    }
}