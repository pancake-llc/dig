using System.Collections.Generic;
using UnityEngine;

using int64 = System.Int64;
using Vector2i = ClipperLib.IntPoint;
using Vector2f = UnityEngine.Vector2;

public class DestructibleBlock
{
    public DestructibleBlock()
    {
        polygons = new List<List<Vector2i>>();

        subVertices = new Vector3[0];
        subTexCoords = new Vector2[0]; ;
        subTriangles = new int[0];
        subNormals = new Vector3[0]; 
    }

    public List<List<Vector2i>> polygons;

    public Vector3[] subVertices;

    public Vector2[] subTexCoords;

    public Vector3[] subNormals;

    public int[] subTriangles;

    private List<List<Vector2>> edgesList = new List<List<Vector2>>();
    private List<EdgeCollider2D> colliders = new List<EdgeCollider2D>();


    public void UpdateSubEdgeMesh(List<List<Vector2i>> inPolygons, float depth, GameObject go)
    {
        polygons.Clear();
        polygons = inPolygons;
        edgesList.Clear();
        
        int totalVertexCount = 0;
        int triangleIndexCount = 0;
        
        for (int i = 0; i < inPolygons.Count; i++)
        {
            BlockSimplification.Execute(inPolygons[i], edgesList);
        }

        for (int i = 0; i < polygons.Count; i++)
        {
            totalVertexCount += polygons[i].Count * 2;
            triangleIndexCount += polygons[i].Count * 6;
        }

        Vector3[] vertices = new Vector3[totalVertexCount];
        Vector2[] texCoords = new Vector2[totalVertexCount];
        Vector3[] normals = new Vector3[totalVertexCount];
        int[] triangles = new int[triangleIndexCount];

        int vertexIndex = 0;
        int triangleIndex = 0;

        for (int i = 0; i < polygons.Count; i++)
        {
            List<Vector2i> edgePoints = polygons[i];
            int vertexCount = edgePoints.Count;
            Vector3 point;

            for (int j = 0; j < vertexCount; j++)
            {
                point = edgePoints[j].ToVector3f();

                vertices[vertexIndex] = point;
                texCoords[vertexIndex] = Vector2f.zero;
                normals[vertexIndex] = Vector3.up;

                point.z += depth;

                vertices[vertexIndex + 1] = point;
                texCoords[vertexIndex + 1] = new Vector2f(0f, 0.1f);
                normals[vertexIndex + 1] = Vector3.up;

                triangles[triangleIndex + 0] = vertexIndex;
                triangles[triangleIndex + 1] = (j != vertexCount - 1) ? vertexIndex + 2 : vertexIndex - j * 2;
                triangles[triangleIndex + 2] = vertexIndex + 1;

                triangles[triangleIndex + 3] = (j != vertexCount - 1) ? vertexIndex + 2 : vertexIndex - j * 2;
                triangles[triangleIndex + 4] = (j != vertexCount - 1) ? vertexIndex + 3 : vertexIndex - j * 2 + 1;
                triangles[triangleIndex + 5] = vertexIndex + 1;

                vertexIndex += 2;
                triangleIndex += 6;
            }
        }

        subVertices = vertices;
        subTexCoords = texCoords;
        subTriangles = triangles;
        subNormals = normals;
        
        UpdateColliders(go);
    }
    
    public void UpdateColliders(GameObject go)
    {
        int colliderCount = colliders.Count;
        int edgesCount = edgesList.Count;
           
        if (colliderCount < edgesCount)
        {
            for (int i = edgesCount - colliderCount; i > 0; i--)
            {
                colliders.Add(go.AddComponent<EdgeCollider2D>());          
            }
        }
        else if (edgesCount < colliderCount)
        {
            for (int i = colliderCount - 1; i >= edgesCount; i--)
            {
                Object.Destroy(colliders[i]);
                colliders.RemoveAt(i);
            }
        }

        for (int i = 0; i < colliders.Count; i++)
        {
            colliders[i].points = edgesList[i].ToArray();
        }
    }
}
