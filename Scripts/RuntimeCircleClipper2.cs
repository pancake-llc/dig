using System.Collections;
using UnityEngine;
using Vector2i = ClipperLib.IntPoint;
using Vector2f = UnityEngine.Vector2;

public class RuntimeCircleClipper2 : IClip
{
    private ClipType clipType = ClipType.Sub;

    private struct TouchLineOverlapCheck
    {
        public float a;
        public float b;
        public float c;
        public float angle;
        public float dividend;

        public TouchLineOverlapCheck(Vector2 p1, Vector2 p2)
        {
            Vector2 d = p2 - p1;
            float m = d.magnitude;
            a = -d.y / m;
            b = d.x / m;
            c = -(a * p1.x + b * p1.y);
            angle = Mathf.Rad2Deg * Mathf.Atan2(-a, b);

            float da;
            if (d.x / d.y < 0f)
                da = 45 + angle;
            else
                da = 45 - angle;

            dividend = Mathf.Abs(1.0f / 1.4f * Mathf.Cos(Mathf.Deg2Rad * da));
        }

        public float GetDistance(Vector2 p) { return Mathf.Abs(a * p.x + b * p.y + c); }
    }

    private float radius = 1.2f;

    private int segmentCount = 10;

    private float touchMoveDistance = 0.1f;

    private Vector2f currentTouchPoint;

    private Vector2f previousTouchPoint;

    private TouchPhase touchPhase;

    private TouchLineOverlapCheck touchLine;

    private Vector2i[] vertices;

    private Camera mainCamera;

    private float cameraZPos;

    private Mesh mesh;

    public bool CheckBlockOverlapping(Vector2f p, float size)
    {
        if (touchPhase == TouchPhase.Began)
        {
            float dx = Mathf.Abs(currentTouchPoint.x - p.x) - radius - size / 2;
            float dy = Mathf.Abs(currentTouchPoint.y - p.y) - radius - size / 2;
            return dx < 0f && dy < 0f;
        }
        else if (touchPhase == TouchPhase.Moved)
        {
            float distance = touchLine.GetDistance(p) - radius - size / touchLine.dividend;
            return distance < 0f;
        }
        else
            return false;
    }

    public ClipBounds GetBounds()
    {
        if (touchPhase == TouchPhase.Began)
        {
            return new ClipBounds
            {
                lowerPoint = new Vector2f(currentTouchPoint.x - radius, currentTouchPoint.y - radius),
                upperPoint = new Vector2f(currentTouchPoint.x + radius, currentTouchPoint.y + radius)
            };
        }
        else if (touchPhase == TouchPhase.Moved)
        {
            Vector2f upperPoint = currentTouchPoint;
            Vector2f lowerPoint = previousTouchPoint;
            if (previousTouchPoint.x > currentTouchPoint.x)
            {
                upperPoint.x = previousTouchPoint.x;
                lowerPoint.x = currentTouchPoint.x;
            }

            if (previousTouchPoint.y > currentTouchPoint.y)
            {
                upperPoint.y = previousTouchPoint.y;
                lowerPoint.y = currentTouchPoint.y;
            }

            return new ClipBounds
            {
                lowerPoint = new Vector2f(lowerPoint.x - radius, lowerPoint.y - radius), upperPoint = new Vector2f(upperPoint.x + radius, upperPoint.y + radius)
            };
        }
        else
            return new ClipBounds();
    }

    public Vector2i[] GetVertices() { return vertices; }

    public Mesh GetMesh() { return mesh; }


    public RuntimeCircleClipper2(
        ClipType clipType,
        float radius,
        int segmentCount,
        float touchMoveDistance,
        Vector2f previousTouchPoint,
        Vector2f currentTouchPoint,
        TouchPhase touchPhase)
    {
        this.clipType = clipType;
        this.radius = radius;
        this.segmentCount = segmentCount;
        this.touchMoveDistance = touchMoveDistance;
        this.previousTouchPoint = previousTouchPoint;
        this.currentTouchPoint = currentTouchPoint;
        this.touchPhase = touchPhase;
        mesh = new Mesh();
        mesh.MarkDynamic();
    }

    public IEnumerator Dig(Vector2 begin)
    {
        var r = 0.6f;
        while (r < radius)
        {
            r += 5 * Time.deltaTime;
            yield return null;
            Build(begin, r);
            DestructibleTerrainManager.Instance.Clip(this, clipType);
        }
    }

    public IEnumerator Dig2(Vector2 begin, Vector2 end)
    {
        var r = 0.6f;
        while (r < radius)
        {
            r += 5 * Time.deltaTime;
            if ((end - begin).sqrMagnitude > touchMoveDistance * touchMoveDistance)
            {
                Build(begin, end, r);
                DestructibleTerrainManager.Instance.Clip(this, clipType);
            }

            yield return null;
        }
    }

    void Build(Vector2 center, float rad)
    {
        Vector3[] meshVertices = new Vector3[segmentCount];
        Vector3[] meshNormals = new Vector3[segmentCount];
        vertices = new Vector2i[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = Mathf.Deg2Rad * (-90f - 360f / segmentCount * i);

            Vector2 point = new Vector2(center.x + rad * Mathf.Cos(angle), center.y + rad * Mathf.Sin(angle));
            vertices[i] = point.ToVector2i();

            meshVertices[i] = point.ToVector3f();
            meshNormals[i] = (meshVertices[i] - center.ToVector3f()) / rad;
        }

        mesh.Clear();
        mesh.vertices = meshVertices;
        mesh.normals = meshNormals;
        mesh.triangles = Triangulate.Execute(meshVertices).ToArray();
    }

    void Build(Vector2 begin, Vector2 end, float rad)
    {
        int halfSegmentCount = segmentCount / 2;
        touchLine = new TouchLineOverlapCheck(begin, end);

        Vector3[] meshVertices = new Vector3[segmentCount + 2];
        Vector3[] meshNormals = new Vector3[segmentCount + 2];
        vertices = new Vector2i[segmentCount + 2];

        for (int i = 0; i <= halfSegmentCount; i++)
        {
            float angle = Mathf.Deg2Rad * (touchLine.angle + 270f - 360f / segmentCount * i);

            Vector2 point = new Vector2(begin.x + rad * Mathf.Cos(angle), begin.y + rad * Mathf.Sin(angle));
            vertices[i] = point.ToVector2i();

            meshVertices[i] = point.ToVector3f();
            meshNormals[i] = (meshVertices[i] - begin.ToVector3f()) / rad;
        }

        for (int i = halfSegmentCount; i <= segmentCount; i++)
        {
            float angle = Mathf.Deg2Rad * (touchLine.angle + 270f - 360f / segmentCount * i);

            Vector2 point = new Vector2(end.x + rad * Mathf.Cos(angle), end.y + rad * Mathf.Sin(angle));
            vertices[i + 1] = point.ToVector2i();

            meshVertices[i + 1] = point.ToVector3f();
            meshNormals[i + 1] = (meshVertices[i + 1] - end.ToVector3f()) / rad;
        }

        mesh.Clear();
        mesh.vertices = meshVertices;
        mesh.normals = meshNormals;
        mesh.triangles = Triangulate.Execute(meshVertices).ToArray();
    }
}