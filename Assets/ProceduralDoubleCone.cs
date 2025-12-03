using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a simple double-cone (two cones sharing the apex at the origin).
/// The apex is at y=0; the two cone bases are at y = ±(height/2).
/// Height is tip-to-tip; radius is the base radius at each end.
/// Now includes top and bottom base caps.
/// </summary>
public static class ProceduralDoubleCone
{
    public static Mesh Build(float radius, float height, int segments = 32)
    {
        var mesh = new Mesh();
        mesh.name = "ProceduralDoubleCone";

        int seg = Mathf.Max(3, segments);
        float halfH = height * 0.5f;

        // vertices: top ring (y = +halfH), bottom ring (y = -halfH), shared apex at origin,
        // plus top/bottom cap centers
        var verts = new List<Vector3>(seg * 2 + 3);
        var uvs = new List<Vector2>(seg * 2 + 3);
        // side triangles (2*seg) + cap triangles (2*seg) => total 4*seg triangles => 12*seg indices
        var tris = new List<int>(seg * 12);

        // top ring (0..seg-1)
        for (int i = 0; i < seg; i++)
        {
            float ang = (i / (float)seg) * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(ang) * radius, halfH, Mathf.Sin(ang) * radius));
            uvs.Add(new Vector2(i / (float)seg, 1f));
        }

        // bottom ring (seg..2*seg-1)
        for (int i = 0; i < seg; i++)
        {
            float ang = (i / (float)seg) * Mathf.PI * 2f;
            verts.Add(new Vector3(Mathf.Cos(ang) * radius, -halfH, Mathf.Sin(ang) * radius));
            uvs.Add(new Vector2(i / (float)seg, 0f));
        }

        // shared apex at origin
        int apexIndex = verts.Count;
        verts.Add(Vector3.zero);
        uvs.Add(new Vector2(0.5f, 0.5f));

        // cap centers
        int topCenterIndex = verts.Count;
        verts.Add(new Vector3(0f, halfH, 0f));
        uvs.Add(new Vector2(0.5f, 1f));

        int bottomCenterIndex = verts.Count;
        verts.Add(new Vector3(0f, -halfH, 0f));
        uvs.Add(new Vector2(0.5f, 0f));

        // top cone side triangles: (apex, topRing[i], topRing[i+1])
        for (int i = 0; i < seg; i++)
        {
            int n0 = apexIndex;
            int n1 = i;
            int n2 = (i + 1) % seg;
            tris.Add(n0); tris.Add(n1); tris.Add(n2);
        }

        // bottom cone side triangles: (apex, bottomRing[i+1], bottomRing[i])
        for (int i = 0; i < seg; i++)
        {
            int n0 = apexIndex;
            int n1 = seg + ((i + 1) % seg);
            int n2 = seg + i;
            tris.Add(n0); tris.Add(n1); tris.Add(n2);
        }

        // top cap triangles (wound to face upward)
        for (int i = 0; i < seg; i++)
        {
            int i0 = i;
            int i1 = (i + 1) % seg;
            // Use (center, next, current) so the cap faces outward (upwards)
            tris.Add(topCenterIndex); tris.Add(i1); tris.Add(i0);
        }

        // bottom cap triangles (wound to face downward)
        for (int i = 0; i < seg; i++)
        {
            int j0 = seg + i;
            int j1 = seg + ((i + 1) % seg);
            // Use (center, current, next) so the cap faces outward (downwards)
            tris.Add(bottomCenterIndex); tris.Add(j0); tris.Add(j1);
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}