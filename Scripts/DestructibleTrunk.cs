using System.Collections.Generic;
using UnityEngine;
using Vector2f = UnityEngine.Vector2;
using Vector2i = ClipperLib.IntPoint;
using int64 = System.Int64;

public class DestructibleTrunk : MonoBehaviour, IDestructible
{
    public Material faceMaterial;
    public Material edgeMaterial;

    [Range(0.25f, 5.0f)] public float blockSize;
    [Range(2, 100)] public int resolutionX = 10;
    [Range(2, 100)] public int resolutionY = 10;

    public float depth = 1.0f;

    private int64 _blockSizeScaled;
    private float _width;
    private float _height;
    private Mesh _faceMesh;
    private Mesh _edgeMesh;
    private GameObject _edgeVisual;
    private DestructibleBlock[] _blocks;
    private DestructibleTrunkFaceRenderer _faceRenderer;

    private void Awake()
    {
        DestructibleTerrainManager.Instance.AddSubject(this);

        _width = blockSize * resolutionX;
        _height = blockSize * resolutionY;
        _blockSizeScaled = (int64) (blockSize * VectorEx.float2int64);
        _faceRenderer = GetComponent<DestructibleTrunkFaceRenderer>();

        Initialize();
    }

    public void Initialize()
    {
        _blocks = new DestructibleBlock[resolutionX * resolutionY];

        GameObject faceVisual = new GameObject();
        faceVisual.transform.SetParent(transform);
        faceVisual.transform.localPosition = Vector3.zero;
        faceVisual.name = "FaceVisual";

        _faceMesh = new Mesh();
        _faceMesh.vertices = new Vector3[] {new Vector3(0f, 0f, 0f), new Vector3(0f, _height, 0f), new Vector3(_width, _height, 0f), new Vector3(_width, 0f, 0f)};
        _faceMesh.uv = new Vector2[] {new Vector2f(0f, 0f), new Vector2f(0f, 1f), new Vector2f(1f, 1f), new Vector2f(1f, 0f)};
        _faceMesh.normals = new Vector3[] {new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, -1f)};
        _faceMesh.triangles = new int[] {0, 1, 3, 3, 1, 2};

        MeshFilter meshFilter = faceVisual.AddComponent<MeshFilter>();
        meshFilter.mesh = _faceMesh;

        MeshRenderer meshRenderer = faceVisual.AddComponent<MeshRenderer>();
        meshRenderer.material = faceMaterial;
        meshRenderer.material.SetTexture("_MaskTex", _faceRenderer.GetRenderTexture());
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        _edgeVisual = new GameObject();
        _edgeVisual.transform.SetParent(transform);
        _edgeVisual.transform.localPosition = Vector3.zero;
        _edgeVisual.name = "EdgeVisual";

        _edgeMesh = new Mesh();
        meshFilter = _edgeVisual.AddComponent<MeshFilter>();
        meshFilter.mesh = _edgeMesh;

        meshRenderer = _edgeVisual.AddComponent<MeshRenderer>();
        meshRenderer.material = edgeMaterial;

        for (int x = 0; x < resolutionX; x++)
        {
            for (int y = 0; y < resolutionY; y++)
            {
                List<List<Vector2i>> polygons = new List<List<Vector2i>>();

                List<Vector2i> vertices = new List<Vector2i>();
                vertices.Add(new Vector2i {x = x * _blockSizeScaled, y = (y + 1) * _blockSizeScaled});
                vertices.Add(new Vector2i {x = x * _blockSizeScaled, y = y * _blockSizeScaled});
                vertices.Add(new Vector2i {x = (x + 1) * _blockSizeScaled, y = y * _blockSizeScaled});
                vertices.Add(new Vector2i {x = (x + 1) * _blockSizeScaled, y = (y + 1) * _blockSizeScaled});

                polygons.Add(vertices);

                int idx = x + resolutionX * y;

                DestructibleBlock block = new DestructibleBlock();
                _blocks[idx] = block;

                UpdateBlockBounds(x, y);

                block.UpdateSubEdgeMesh(polygons, depth, gameObject);
            }
        }

        UpdateEdgeMesh();
    }

    private void UpdateBlockBounds(int x, int y)
    {
        int lx = x;
        int ly = y;
        int ux = x + 1;
        int uy = y + 1;

        if (lx == 0) lx = -1;
        if (ly == 0) ly = -1;
        if (ux == resolutionX) ux = resolutionX + 1;
        if (uy == resolutionY) uy = resolutionY + 1;

        BlockSimplification.currentLowerPoint = new Vector2i {x = lx * _blockSizeScaled, y = ly * _blockSizeScaled};

        BlockSimplification.currentUpperPoint = new Vector2i {x = ux * _blockSizeScaled, y = uy * _blockSizeScaled};
    }

    public void ExecuteClip(IClip clip, ClipType clipType = ClipType.Sub)
    {
        Vector2f worldPositionF = transform.position;
        Vector2i worldPositionI = worldPositionF.ToVector2i();

        ClipBounds bounds = clip.GetBounds();
        int x1 = Mathf.Max(0, (int) ((bounds.lowerPoint.x - worldPositionF.x) / blockSize));
        if (x1 > resolutionX - 1) return;
        int y1 = Mathf.Max(0, (int) ((bounds.lowerPoint.y - worldPositionF.y) / blockSize));
        if (y1 > resolutionY - 1) return;
        int x2 = Mathf.Min(resolutionX - 1, (int) ((bounds.upperPoint.x - worldPositionF.x) / blockSize));
        if (x2 < 0) return;
        int y2 = Mathf.Min(resolutionY - 1, (int) ((bounds.upperPoint.y - worldPositionF.y) / blockSize));
        if (y2 < 0) return;

        Vector2i[] verts = clip.GetVertices();

        List<Vector2i> clipVertices = new List<Vector2i>(verts.Length);
        for (int i = 0; i < verts.Length; i++)
        {
            clipVertices.Add(new Vector2i(verts[i].x - worldPositionI.x, verts[i].y - worldPositionI.y));
        }

        for (int x = x1; x <= x2; x++)
        {
            for (int y = y1; y <= y2; y++)
            {
                if (clip.CheckBlockOverlapping(new Vector2f((x + 0.5f) * blockSize + worldPositionF.x, (y + 0.5f) * blockSize + worldPositionF.y), blockSize))
                {
                    DestructibleBlock block = _blocks[x + resolutionX * y];

                    List<List<Vector2i>> solutions = new List<List<Vector2i>>();

                    ClipperLib.Clipper clipper = new ClipperLib.Clipper();
                    clipper.AddPolygons(block.polygons, ClipperLib.PolyType.ptSubject);
                    clipper.AddPolygon(clipVertices, ClipperLib.PolyType.ptClip);
                    clipper.Execute(clipType == ClipType.Sub ? ClipperLib.ClipType.ctDifference : ClipperLib.ClipType.ctUnion,
                        solutions,
                        ClipperLib.PolyFillType.pftNonZero,
                        ClipperLib.PolyFillType.pftNonZero);

                    if (clipType == ClipType.Add)
                    {
                        List<Vector2i> squareClipper = new List<Vector2i>();
                        squareClipper.Add(new Vector2i(x * _blockSizeScaled, (y + 1) * _blockSizeScaled));
                        squareClipper.Add(new Vector2i(x * _blockSizeScaled, y * _blockSizeScaled));
                        squareClipper.Add(new Vector2i((x + 1) * _blockSizeScaled, y * _blockSizeScaled));
                        squareClipper.Add(new Vector2i((x + 1) * _blockSizeScaled, (y + 1) * _blockSizeScaled));

                        clipper = new ClipperLib.Clipper();
                        clipper.AddPolygons(solutions, ClipperLib.PolyType.ptSubject);
                        clipper.AddPolygon(squareClipper, ClipperLib.PolyType.ptClip);
                        clipper.Execute(ClipperLib.ClipType.ctIntersection, solutions, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero);
                    }

                    UpdateBlockBounds(x, y);

                    block.UpdateSubEdgeMesh(solutions, depth, gameObject);
                }
            }
        }

        UpdateEdgeMesh();
        _faceRenderer.DrawBrushInTrunkSpace(clip.GetMesh(), worldPositionF, new Vector2f(_width, _height), clipType);

        clipVertices.Clear();
    }

    public void UpdateEdgeMesh()
    {
        int totalVertexCount = 0;
        int totalTriangleCount = 0;

        for (int i = 0; i < _blocks.Length; i++)
        {
            totalVertexCount += _blocks[i].subVertices.Length;
            totalTriangleCount += _blocks[i].subTriangles.Length;
        }

        Vector3[] vertices = new Vector3[totalVertexCount];
        Vector2[] texCoords = new Vector2[totalVertexCount];
        Vector3[] normals = new Vector3[totalVertexCount];
        int[] triangles = new int[totalTriangleCount];

        int vertexIndex = 0;
        int triangleIndex = 0;

        for (int i = 0; i < _blocks.Length; i++)
        {
            int vertexCount = _blocks[i].subVertices.Length;
            int triangleCount = _blocks[i].subTriangles.Length;

            System.Array.Copy(_blocks[i].subVertices,
                0,
                vertices,
                vertexIndex,
                vertexCount);
            System.Array.Copy(_blocks[i].subTexCoords,
                0,
                texCoords,
                vertexIndex,
                vertexCount);
            System.Array.Copy(_blocks[i].subNormals,
                0,
                normals,
                vertexIndex,
                vertexCount);
            System.Array.Copy(_blocks[i].subTriangles,
                0,
                triangles,
                triangleIndex,
                triangleCount);

            for (int j = triangleIndex; j < triangleIndex + triangleCount; j++)
            {
                triangles[j] += vertexIndex;
            }

            vertexIndex += vertexCount;
            triangleIndex += triangleCount;
        }

        _edgeMesh.Clear();
        _edgeMesh.vertices = vertices;
        _edgeMesh.uv = texCoords;
        _edgeMesh.normals = normals;
        _edgeMesh.triangles = triangles;
    }
}